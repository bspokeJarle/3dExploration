using _3dRotations.World.Objects;
using _3dRotations.World.Objects.EarthObject;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using System;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scenes.Outro
{
    public class Outro : IScene
    {
        public GameModes GameMode { get; } = GameModes.Live;
        public string SceneMusic { get; } = "music_outro";
        public SceneTypes SceneType { get; } = SceneTypes.Outro;
        public SceneBiomeTypes SceneBiome { get; } = SceneBiomeTypes.HillsWoods;

        private static readonly Random _rng = new Random(77);

        public void SetupScene(I3dWorld world)
        {
            var earth = EarthObject.CreateEarth();
            earth.Movement = new EarthObjectControls();
            world.WorldInhabitants.Add(earth);

            var ship = Ship.CreateShip(null, new OutroShipControls());
            ship.ObjectName = "Ship";
            ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };
            ship.ObjectOffsets = OutroShipControls.CreateInitialOffset();
            ship.Rotation = OutroShipControls.CreateInitialRotation();
            ship.ImpactStatus = new ImpactStatus { ObjectHealth = ShipSetup.DefaultShipHealth };
            ship.CrashBoxDebugMode = false;
            ship.WeaponSystems = null;
            ship.ParentSurface = null;
            ship.HasShadow = false;
            world.WorldInhabitants.Add(ship);

            // White icy asteroid — enters from top-left, travels down-right
            var asteroid1 = AsteroidObject.CreateAsteroid(
                colorPalette: new[] { "E8E8E8", "FFFFFF", "C8D8E8", "B0C8D8" },
                size: 16f,
                startOffsetX: -ScreenSetup.screenSizeX * 0.6f,
                startOffsetY: -ScreenSetup.screenSizeY * 0.5f,
                depth: 480f,
                rng: _rng);
            var ctrl1 = new AsteroidControls(new Random(13), 480f, startImmediately: true);
            ctrl1.ForceDirection(directionRight: true, directionDown: true);
            asteroid1.Movement = ctrl1;
            world.WorldInhabitants.Add(asteroid1);

            // Fiery yellow-red asteroid — enters from top-right, travels down-left
            var asteroid2 = AsteroidObject.CreateAsteroid(
                colorPalette: new[] { "FF8800", "FF4400", "FFCC00", "CC3300", "FF6600" },
                size: 14f,
                startOffsetX:  ScreenSetup.screenSizeX * 0.6f,
                startOffsetY: -ScreenSetup.screenSizeY * 0.3f,
                depth: 460f,
                rng: _rng);
            var ctrl2 = new AsteroidControls(new Random(37), 460f, startImmediately: true);
            ctrl2.ForceDirection(directionRight: false, directionDown: true);
            asteroid2.Movement = ctrl2;
            world.WorldInhabitants.Add(asteroid2);
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;
            o.Type = ScreenOverlayType.Outro;
            o.ShowOverlay = false;
            o.AutoHide = false;
            o.ShowDebugOverlay = false;
        }

        public void SetupGameOverlay() { }

        public void SetupVideoOverlay(string fileName) { }
    }
}
