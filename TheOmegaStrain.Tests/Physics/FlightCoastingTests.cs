using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.World.Objects;
using TheOmegaStrain.Gameplay.Controls;
using RetroMesh.Engine;

namespace TheOmegaStrain.Tests.Physics;

[DoNotParallelize]
[TestClass]
public class FlightCoastingTests
{
    [TestInitialize]
    public void Setup()
    {
        ScreenSetup.Initialize(1500, 1024);
        GameState.DeltaTime = GameState.GameplayBaselineDeltaTime;
        GameState.GamePlayState = new GamePlayState
        {
            CurrentSceneType = SceneTypes.Game,
            CurrentSceneBiome = SceneBiomeTypes.HillsWoods
        };
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3 { x = 1000f, y = 100f, z = 1000f }
        };
    }

    [TestMethod]
    public void ApplyFlightCoasting_SlowsMomentumGraduallyAndFrameRateIndependently()
    {
        var at90Fps = CreateMovingPhysics();
        var at60Fps = CreateMovingPhysics();

        for (int i = 0; i < 90; i++)
            at90Fps.ApplyFlightCoasting(1f / 90f);
        for (int i = 0; i < 60; i++)
            at60Fps.ApplyFlightCoasting(1f / 60f);

        Assert.IsTrue(at90Fps.InertiaX is > 30f and < 40f,
            $"One second of coasting should retain substantial momentum, actual {at90Fps.InertiaX:F2}.");
        Assert.AreEqual(at90Fps.InertiaX, at60Fps.InertiaX, 0.001f);
        Assert.AreEqual(at90Fps.InertiaZ, at60Fps.InertiaZ, 0.001f);
    }

    [TestMethod]
    public void ShipApplyGravity_ContinuesHorizontalTravelWithoutThrust()
    {
        var surface = new Surface();
        var ship = Ship.CreateShip(surface);
        var controls = (ShipControls)ship.Movement!;
        controls.ParentObject = ship;
        controls.ThrustOn = false;
        controls.Thrust = 0f;
        controls.Physics.InertiaX = 20f;
        controls.Physics.InertiaZ = -10f;
        float startX = GameState.SurfaceState.GlobalMapPosition.x;
        float startZ = GameState.SurfaceState.GlobalMapPosition.z;

        controls.ApplyGravity(GameState.GameplayBaselineDeltaTime);

        Assert.IsTrue(GameState.SurfaceState.GlobalMapPosition.x > startX);
        Assert.IsTrue(GameState.SurfaceState.GlobalMapPosition.z < startZ);
        Assert.IsTrue(controls.Physics.InertiaX is > 19f and < 20f);
        Assert.IsTrue(controls.Physics.InertiaZ is > -10f and < -9f);
    }

    private static TheOmegaStrain.Gameplay.Physics.Physics CreateMovingPhysics() => new()
    {
        InertiaX = 40f,
        InertiaZ = -20f
    };
}
