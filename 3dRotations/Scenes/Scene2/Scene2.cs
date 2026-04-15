
using _3dRotations.Helpers;
using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.KamikazeDroneControls;
using GameAiAndControls.Controls.MotherShipSmallControls;
using GameAiAndControls.Controls.SeederControls;
using GameAiAndControls.Controls.SpaceSwanControls;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scene.Scene1
{
    public class Scene2:IScene
    {
        Surface Surface = new();

        public string SceneMusic { get; } = "music_battle";
        public SceneTypes SceneType { get; } = SceneTypes.Game;
        public ISceneDirector Director { get; } = new Scene2Director();
        public GameModes GameMode { get; } = GameModes.Playback;
        public float InfectionThresholdPercent { get; } = 10f;
        public int InfectionSpreadRate { get; } = 75;
        public int SeederOffscreenSpeedFactor { get; } = 10;
        public float LocalInfectionSpreadDelaySec { get; } = 8.0f;
        public float LocalInfectionSpreadRadius { get; } = 3500f;

        public void SetupScene(I3dWorld world)
        {
            var ws = SurfaceSetup.WorldScale;

            //Add ship as first inhabitant
            var ship = Ship.CreateShip(Surface);
            //Generate 2D map for the surface, maxtrees and maxhouses set
            Surface.Create2DMap(30000,15000,GameMode, "Scene2SurfaceRecording.retro");
            var weapons = new List<I3dObject> { Lazer.CreateLazer(Surface), Bullet.CreateBullet(Surface) };
            ship.Rotation = new Vector3 { };
            ship.WorldPosition = new Vector3 { };
            ship.ObjectName = "Ship";
            ship.ImpactStatus = new ImpactStatus { ObjectHealth = ShipSetup.DefaultShipHealth };
            ship.CrashBoxDebugMode = false;
            ship.WeaponSystems = new Weapons(weapons, ship.Movement!, ship);
            world.WorldInhabitants.Add(ship);

            // Guidance arrow — on-screen indicator pointing toward closest seeder
            var guidanceArrow = SeederGuidanceArrow.CreateSeederGuidanceArrow(Surface);
            guidanceArrow.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 200 };
            guidanceArrow.Rotation = new Vector3 { x = 70, y = 0, z = 90 };
            guidanceArrow.WorldPosition = new Vector3 { };
            guidanceArrow.ObjectName = "SeederGuidanceArrow";
            guidanceArrow.ImpactStatus = new ImpactStatus { };
            guidanceArrow.CrashBoxDebugMode = false;
            world.WorldInhabitants.Add(guidanceArrow);

            // Drones — waiting until the player has a Decoy powerup
            for (int i = 0; i < 6; i++)
            {
                var rmd = new Random();

                var kamikaze = KamikazeDrone.CreateKamikazeDrone(Surface);
                kamikaze.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-55000, 55000)) * ws, y = 0, z = (92000 + rmd.Next(-55000, 55000)) * ws };
                kamikaze.Rotation = new Vector3 { };
                kamikaze.ObjectOffsets = new Vector3 { x = 0, y = 150, z = 400 };
                kamikaze.ObjectName = "KamikazeDrone";
                kamikaze.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.KamikazeDroneHealth };
                kamikaze.CrashBoxDebugMode = false;
                kamikaze.WeaponSystems = null;
                kamikaze.HasPowerUp = false;
                kamikaze.IsActive = false;
                world.WorldInhabitants.Add(kamikaze);
                GameState.SurfaceState.AiObjects.Add(kamikaze);
            }

            // Seeders close to the player for immediate pressure
            for (int i = 0; i < 5; i++)
            {
                var rmd = new Random();

                var seeder = Seeder.CreateSeeder(Surface);
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-12000, 12000)) * ws, y = 0, z = (92000 + rmd.Next(-12000, 12000)) * ws };
                seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seeder.ObjectName = "Seeder";
                seeder.Movement = new SeederControls();
                seeder.CrashBoxDebugMode = false;
                seeder.ImpactStatus = new ImpactStatus { };
                seeder.HasPowerUp = false;
                world.WorldInhabitants.Add(seeder);
                GameState.SurfaceState.AiObjects.Add(seeder);
            }

            // Seeders spread further across the map
            for (int i = 0; i < 3; i++)
            {
                var rmd = new Random();

                var seeder = Seeder.CreateSeeder(Surface);
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-25000, 5000)) * ws, y = 0, z = (92000 + rmd.Next(-25000, 5000)) * ws };
                seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seeder.ObjectName = "Seeder";
                seeder.Movement = new SeederControls();
                seeder.CrashBoxDebugMode = false;
                seeder.ImpactStatus = new ImpactStatus { };
                seeder.HasPowerUp = false;
                world.WorldInhabitants.Add(seeder);
                GameState.SurfaceState.AiObjects.Add(seeder);
            }

            // Powerup seeders — unlock decoy and lazer
            for (int i = 0; i < 2; i++)
            {
                var rmd = new Random();
                var seederPowerup = Seeder.CreateSeeder(Surface);
                seederPowerup.Rotation = new Vector3 { };
                seederPowerup.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-15000, 15000)) * ws, y = 0, z = (92000 + rmd.Next(-15000, 15000)) * ws };
                seederPowerup.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seederPowerup.ObjectName = "Seeder";
                seederPowerup.Movement = new SeederControls();
                seederPowerup.CrashBoxDebugMode = false;
                seederPowerup.ImpactStatus = new ImpactStatus { };
                seederPowerup.HasPowerUp = true;
                world.WorldInhabitants.Add(seederPowerup);
                GameState.SurfaceState.AiObjects.Add(seederPowerup);
            }

            // Mothership — spawns inactive, enters when all seeders and drones are destroyed
            var motherShip = MotherShipSmall.CreateMotherShipSmall(Surface);
            motherShip.Rotation = new Vector3 { };
            motherShip.WorldPosition = new Vector3 { x = 95700 * ws, y = 0, z = 92000 * ws };
            motherShip.ObjectOffsets = new Vector3 { x = 0, y = -2500, z = 400 };
            motherShip.ObjectName = "MotherShipSmall";
            motherShip.Movement = new MotherShipSmallControls();
            motherShip.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.MotherShipSmallHealth };
            motherShip.CrashBoxDebugMode = false;
            motherShip.HasPowerUp = false;
            motherShip.IsActive = false;
            world.WorldInhabitants.Add(motherShip);
            GameState.SurfaceState.AiObjects.Add(motherShip);

            // SpaceSwans — passive wildlife
            for (int s = 0; s < 50; s++)
            {
                var rmdSwan = new Random();
                var spaceSwan = SpaceSwan.CreateSpaceSwan(Surface);
                spaceSwan.Rotation = new Vector3 { };
                spaceSwan.WorldPosition = new Vector3 { x = (95700 + rmdSwan.Next(-40000, 40000)) * ws, y = 0, z = (92000 + rmdSwan.Next(-40000, 40000)) * ws };
                spaceSwan.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                spaceSwan.ObjectName = "SpaceSwan";
                spaceSwan.Movement = new SpaceSwanControls();
                spaceSwan.CrashBoxDebugMode = false;
                spaceSwan.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.SpaceSwanHealth };
                spaceSwan.HasPowerUp = false;
                spaceSwan.IsActive = true;
                world.WorldInhabitants.Add(spaceSwan);
                GameState.SurfaceState.AiObjects.Add(spaceSwan);
            }

            //Get the surface viewport based on the global Map Position
            //Important: In a Scene, Surface should be amongst the first objects added to the world
            var surfaceObject = (_3dObject)Surface.GetSurfaceViewPort();
            surfaceObject.ObjectName = "Surface";
            //This position and rotation is for the onscreen object, not the map position
            surfaceObject.ObjectOffsets = new Vector3 { x = 105 * ScreenSetup.ScreenScaleX, y = 500 * ScreenSetup.ScreenScaleY, z = 400 };
            surfaceObject.Rotation = new Vector3 { x = 70, y = 0, z = 0 };
            surfaceObject.WorldPosition = new Vector3 { };
            //Crashboxes are added n the GetSurfaceViewPort method
            surfaceObject.Movement = new GroundControls();
            surfaceObject.ParentSurface = Surface;
            surfaceObject.ImpactStatus = new ImpactStatus { };
            surfaceObject.CrashBoxDebugMode = false;
            surfaceObject.CrashBoxesFollowRotation = false;
            world.WorldInhabitants.Add(surfaceObject);
            GameState.SurfaceState.SurfaceViewportObject = surfaceObject;

            var towerPlacements = SurfaceGeneration.FindTowerPlacements(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight());

            SurfaceGeneration.FlattenTerrainAroundTowers_ToHighlands(
                GameState.SurfaceState.Global2DMap,
                Surface.MaxHeight(),
                towerPlacements,
                writeDebugLogs: false
            );

            var towerIndex = 0;
            foreach (var towerPlacement in towerPlacements)
            {
                towerIndex++;

                var tower = Tower.CreateTower(Surface);
                tower.Rotation = new Vector3 { };
                tower.WorldPosition = new Vector3 { };
                tower.SurfaceBasedId = GameState.SurfaceState.Global2DMap[towerPlacement.y, towerPlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[towerPlacement.y, towerPlacement.x].hasLandbasedObject = true;
                tower.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 280 * ScreenSetup.ScreenScaleY, z = 300 };
                tower.ObjectName = "Tower";
                tower.Movement = new TowerControls();
                tower.CrashBoxDebugMode = false;
                tower.ImpactStatus = new ImpactStatus { };
                world.WorldInhabitants.Add(tower);
            }

            var treePlacements = SurfaceGeneration.FindTreePlacementAreas(GameState.SurfaceState.Global2DMap,Surface.GlobalMapSize(),Surface.TileSize(),Surface.MaxHeight(), 30000);
            var treeIndex = 0;
            foreach (var treePlacement in treePlacements)
            {
                treeIndex++;
                //Debug.WriteLine($"Tree placement: {treePlacement.x} {treePlacement.y}");
                //TODO: We need design more trees
                var tree = Tree.CreateTree(Surface);
                //The tree don't need a world position, it's a surface based object
                tree.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                tree.SurfaceBasedId = GameState.SurfaceState.Global2DMap[treePlacement.y, treePlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[treePlacement.y, treePlacement.x].hasLandbasedObject = true;
                //The offsets of landbased objects need to similar to that of the surface, apart from some fine tuning
                tree.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 425 * ScreenSetup.ScreenScaleY, z = 300 };
                //Crashbox offsets for Tree, counteract the object offsets
                //tree.CrashboxOffsets = new Vector3 { };
                tree.ObjectName = "Tree";
                tree.Movement = new TreeControls();
                tree.ImpactStatus = new ImpactStatus { };
                tree.CrashBoxDebugMode = false;
                if (tree.SurfaceBasedId>0) world.WorldInhabitants.Add(tree);
            }

            var housePlacements = SurfaceGeneration.FindHousePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.MaxHeight(), treePlacements, 15000);
            foreach (var housePlacement in housePlacements)
            {
                //Debug.WriteLine($"House placement: {housePlacement.x} {housePlacement.y}");

                var house = House.CreateHouse(Surface);
                //Initialize the house rotation
                //The tree don't need a world position, it's a surface based object
                house.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                //Find the surface based id for the tree
                house.SurfaceBasedId = GameState.SurfaceState.Global2DMap[housePlacement.y, housePlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[housePlacement.y, housePlacement.x].hasLandbasedObject = true;
                house.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 450 * ScreenSetup.ScreenScaleY, z = 300 };
                //TODO need to find the right offsets for house
                //house.CrashboxOffsets = new Vector3 { };
                house.ObjectName = "House";
                house.Movement = new HouseControls();
                house.ImpactStatus = new ImpactStatus { };
                house.CrashBoxDebugMode = false;
                if (house.SurfaceBasedId>0) world.WorldInhabitants.Add(house);
            }
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;

            o.Type = ScreenOverlayType.Intro;
            o.Anchor = ScreenOverlayAnchor.Top;

            o.Header = "RETROMESH // SECTOR BRIEFING";
            o.Title = "THE OMEGA STRAIN — PHASE II";

            o.Body =
                "NEREID outer colony TRITON-7 has gone dark.\n\n" +
                "Long-range telemetry confirms Omega Strain\n" +
                "has breached the quarantine perimeter.\n" +
                "Seeder count: TEN. Escort drones: SIX.\n" +
                "Infection spread rate: ACCELERATED.\n" +
                "Terrain: unknown — no prior survey data.\n\n" +
                "Bio-contamination tolerance: 10%.\n\n" +
                "REVISED DIRECTIVE:\n" +
                "Sterilize TRITON-7. Leave nothing behind.";

            o.Footer = "PRESS ANY KEY TO BEGIN DESCENT";

            o.ShowOverlay = true;
            o.AutoHide = false;
            o.AutoHideSeconds = 0f;

            o.DimStrength = 0.60f;
            o.PanelWidthRatio = 0.74f;
            o.PanelHeightRatio = 0.34f;

            o.ShowDebugOverlay = false;
        }
        public void SetupGameOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            GameState.ScreenOverlayState.Type = ScreenOverlayType.Game;
            GameState.ScreenOverlayState.SetGameOverlayPreset("Header", "The Omega Strain", "", "");
            GameState.ScreenOverlayState.ShowOverlay = false;
            GameState.ScreenOverlayState.ShowDebugOverlay = false;
        }

        public void SetupVideoOverlay(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
