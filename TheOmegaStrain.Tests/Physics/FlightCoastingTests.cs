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
        GameState.SettingsState = new GameSettingsState();
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

    [TestMethod]
    public void ShipApplyGravity_LowFlightInertiaCoastsTwoSecondsWithoutLosingAltitude()
    {
        GameState.SettingsState.FlightCoastingSetting = FlightCoasting.Short;
        var surface = new Surface();
        var ship = Ship.CreateShip(surface);
        var controls = (ShipControls)ship.Movement!;
        controls.ParentObject = ship;
        controls.ThrustOn = false;
        controls.Thrust = 0f;
        controls.Physics.InertiaX = 20f;

        float startScreenY = ship.ObjectOffsets.y;
        float startAltitude = GameState.SurfaceState.GlobalMapPosition.y;
        int hoverFrames = (int)(2f / GameState.GameplayBaselineDeltaTime) - 1;

        for (int frame = 0; frame < hoverFrames; frame++)
            controls.ApplyGravity(GameState.GameplayBaselineDeltaTime);

        Assert.AreEqual(startScreenY, ship.ObjectOffsets.y, 0.001f);
        Assert.AreEqual(startAltitude, GameState.SurfaceState.GlobalMapPosition.y, 0.001f);
        Assert.IsTrue(controls.Physics.InertiaX > 0f,
            "Horizontal coasting should continue while altitude is held.");
    }

    [TestMethod]
    public void ShipApplyGravity_AfterHoverRampsSettleInGradually()
    {
        GameState.SettingsState.FlightCoastingSetting = FlightCoasting.Short;
        var surface = new Surface();
        var ship = Ship.CreateShip(surface);
        var controls = (ShipControls)ship.Movement!;
        controls.ParentObject = ship;
        controls.ThrustOn = false;
        controls.Thrust = 0f;
        controls.Physics.HoverElapsed = 2f;

        float startAltitude = GameState.SurfaceState.GlobalMapPosition.y;
        controls.ApplyGravity(GameState.GameplayBaselineDeltaTime);
        float firstFrameDrop = startAltitude - GameState.SurfaceState.GlobalMapPosition.y;

        for (int frame = 0; frame < 90; frame++)
            controls.ApplyGravity(GameState.GameplayBaselineDeltaTime);

        float laterFrameStart = GameState.SurfaceState.GlobalMapPosition.y;
        controls.ApplyGravity(GameState.GameplayBaselineDeltaTime);
        float laterFrameDrop = laterFrameStart - GameState.SurfaceState.GlobalMapPosition.y;

        Assert.IsTrue(firstFrameDrop >= 0f);
        Assert.IsTrue(firstFrameDrop < laterFrameDrop,
            $"Settle should fade in after hover; first={firstFrameDrop:F4}, later={laterFrameDrop:F4}.");
    }

    private static TheOmegaStrain.Gameplay.Physics.Physics CreateMovingPhysics() => new()
    {
        InertiaX = 40f,
        InertiaZ = -20f
    };
}
