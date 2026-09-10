using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Gameplay.Physics;

namespace TheOmegaStrain.Tests.Physics;

[DoNotParallelize]
[TestClass]
public class FlightSettingsBehaviorTests
{
    private const float DeltaTime90 = 1f / 90f;

    [TestInitialize]
    public void Setup()
    {
        GameState.DeltaTime = DeltaTime90;
        GameState.GamePlayState = new GamePlayState
        {
            CurrentSceneType = SceneTypes.Game,
            CurrentSceneBiome = SceneBiomeTypes.HillsWoods
        };
    }

    [TestMethod]
    public void Balanced_AppliesTheOriginalShipPhysicsDefaults()
    {
        var physics = CreatePhysics(new GameSettingsState());

        Assert.AreEqual(0.9975f, physics.CoastingRetention);
        Assert.AreEqual(30f, physics.ThrustRampRate);
        Assert.AreEqual(9.6f, physics.ThrustSpeedMultiplier);
        Assert.AreEqual(9f, physics.GravityPullMultiplier);
    }

    [TestMethod]
    public void CoastingOptions_ProduceClearlyOrderedMomentumAfterThreeSeconds()
    {
        float shortMomentum = SimulateCoasting(FlightCoasting.Short, seconds: 3f);
        float normalMomentum = SimulateCoasting(FlightCoasting.Normal, seconds: 3f);
        float longMomentum = SimulateCoasting(FlightCoasting.Long, seconds: 3f);

        Assert.IsTrue(shortMomentum < normalMomentum && normalMomentum < longMomentum,
            $"Expected SHORT < NORMAL < LONG momentum, actual {shortMomentum:F2}, {normalMomentum:F2}, {longMomentum:F2}.");
        Assert.IsTrue(normalMomentum - shortMomentum > 5f,
            $"SHORT should feel materially slower than NORMAL after three seconds ({shortMomentum:F2} vs {normalMomentum:F2}).");
        Assert.IsTrue(longMomentum - normalMomentum > 5f,
            $"LONG should retain materially more speed than NORMAL after three seconds ({longMomentum:F2} vs {normalMomentum:F2}).");
    }

    [TestMethod]
    public void ThrustAccelerationOptions_ProduceClearlyOrderedSustainedAcceleration()
    {
        var soft = SimulateInitialThrust(FlightThrustResponse.Soft);
        var normal = SimulateInitialThrust(FlightThrustResponse.Normal);
        var quick = SimulateInitialThrust(FlightThrustResponse.Quick);

        Assert.IsTrue(soft.ForwardInertia < normal.ForwardInertia && normal.ForwardInertia < quick.ForwardInertia,
            $"Expected GENTLE < NORMAL < STRONG acceleration, actual inertia {soft.ForwardInertia:F2}, {normal.ForwardInertia:F2}, {quick.ForwardInertia:F2}.");
        Assert.IsTrue(quick.ForwardInertia - soft.ForwardInertia > 3f,
            $"Acceleration range should be clearly measurable, actual GENTLE {soft.ForwardInertia:F2}, STRONG {quick.ForwardInertia:F2}.");
    }

    [TestMethod]
    public void GravityOptions_ProduceClearlyOrderedFallResponse()
    {
        float light = SimulateFall(FlightGravityResponse.Light);
        float normal = SimulateFall(FlightGravityResponse.Normal);
        float strong = SimulateFall(FlightGravityResponse.Strong);

        Assert.IsTrue(light < normal && normal < strong,
            $"Expected LIGHT < NORMAL < STRONG fall speed, actual {light:F2}, {normal:F2}, {strong:F2}.");
        Assert.IsTrue(strong - light > 1f,
            $"Gravity range should create a measurable difference, actual LIGHT {light:F2}, STRONG {strong:F2}.");
    }

    [DataTestMethod]
    [DataRow(FlightCoasting.Short)]
    [DataRow(FlightCoasting.Normal)]
    [DataRow(FlightCoasting.Long)]
    public void EveryCoastingOption_RemainsFrameRateIndependent(FlightCoasting option)
    {
        float at60 = SimulateCoasting(option, 2f, fps: 60);
        float at90 = SimulateCoasting(option, 2f, fps: 90);

        Assert.AreEqual(at90, at60, 0.002f,
            $"{option} coasting should produce the same momentum at 60 and 90 FPS.");
    }

    private static float SimulateCoasting(FlightCoasting option, float seconds, int fps = 90)
    {
        var settings = new GameSettingsState { FlightCoastingSetting = option };
        var physics = CreatePhysics(settings);
        physics.InertiaX = 40f;
        float deltaTime = 1f / fps;

        for (int frame = 0; frame < (int)(seconds * fps); frame++)
            physics.ApplyFlightCoasting(deltaTime);

        return physics.InertiaX;
    }

    private static (float ThrustEffect, float ForwardInertia) SimulateInitialThrust(FlightThrustResponse option)
    {
        var settings = new GameSettingsState { FlightThrustResponseSetting = option };
        var physics = CreatePhysics(settings);

        // Measure long enough for the setting's ramp and thrust multiplier to
        // produce a sustained difference, while staying below top speed.
        for (int frame = 0; frame < 18; frame++)
            physics.CalculateThrustForces(10f, 90f, 0f, DeltaTime90);

        return (physics.ThrustEffect, MathF.Abs(physics.InertiaZ));
    }

    private static float SimulateFall(FlightGravityResponse option)
    {
        var settings = new GameSettingsState { FlightGravityResponseSetting = option };
        var physics = CreatePhysics(settings);
        physics.HoverElapsed = physics.HoverFloatDuration + physics.HoverRampDuration;

        for (int frame = 0; frame < 45; frame++)
            physics.ApplyFallGravity(rotationDegrees: 65f, DeltaTime90);

        return MathF.Abs(physics.InertiaY);
    }

    private static TheOmegaStrain.Gameplay.Physics.Physics CreatePhysics(GameSettingsState settings)
    {
        var physics = new TheOmegaStrain.Gameplay.Physics.Physics();
        ShipFlightPhysicsSettings.Apply(physics, settings);
        return physics;
    }
}
