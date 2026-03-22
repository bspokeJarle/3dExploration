using _3dRotations.Helpers;
using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.SeederControls;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scene.Scene1
{
    public class Scene1 : IScene
    {
        Surface Surface = new();

        public string SceneMusic { get; } = "music_flight";
        public SceneTypes SceneType { get; } = SceneTypes.Game;

        public GameModes GameMode { get; } = GameModes.Playback;

        public void SetupScene(I3dWorld world)
        {          
            //Add ship as first inhabitant
            var ship = Ship.CreateShip(Surface);
            //Generate 2D map for the surface, maxtrees and maxhouses set
            Surface.Create2DMap(30000,15000, GameMode, "Scene1SurfaceRecording.retro");
            var weapons = new List<I3dObject> { Lazer.CreateLazer(Surface) };
            ship.Rotation = new Vector3 { };
            ship.WorldPosition = new Vector3 { };
            ship.ObjectName = "Ship";
            ship.ImpactStatus = new ImpactStatus { ObjectHealth = ShipSetup.DefaultShipHealth };
            ship.CrashBoxDebugMode = false;
            ship.WeaponSystems = new Weapons(weapons, ship.Movement!, ship);
            world.WorldInhabitants.Add(ship);

            for (int i = 0; i < 10; i++)
            {
                var rmd = new Random();

                //Add ship as first inhabitant
                var kamikaze = KamikazeDrone.CreateKamikazeDrone(Surface);
                kamikaze.WorldPosition = new Vector3 { x = 95700 + rmd.Next(-15000, 15000), y = 0, z = 92000 + rmd.Next(-15000, 15000) };
                kamikaze.Rotation = new Vector3 { };
                kamikaze.ObjectOffsets = new Vector3 { x = 0, y = 150, z = 400 };
                kamikaze.ObjectName = "KamikazeDrone";
                kamikaze.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.KamikazeDroneHealth };
                kamikaze.CrashBoxDebugMode = false;
                kamikaze.WeaponSystems = null;
                world.WorldInhabitants.Add(kamikaze);
                GameState.SurfaceState.AiObjects.Add(kamikaze);
            }


            for (int i = 0; i < 40; i++)
            {
                var rmd = new Random();

                var seeder = Seeder.CreateSeeder(Surface);
                //Initialize the seeder rotation
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = 95700 + rmd.Next(-30000, 30000), y = 0, z = 92000 + rmd.Next(-30000, 30000)};
                seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seeder.ObjectName = "Seeder";
                seeder.Movement = new SeederControls();
                seeder.CrashBoxDebugMode = false;
                seeder.ImpactStatus = new ImpactStatus { };
                world.WorldInhabitants.Add(seeder);
                GameState.SurfaceState.AiObjects.Add(seeder);
            }

            //Get the surface viewport based on the global Map Position
            //Important: In a Scene, Surface should be amongst the first objects added to the world
            var surfaceObject = (_3dObject)Surface.GetSurfaceViewPort();
            surfaceObject.ObjectName = "Surface";
            //This position and rotation is for the onscreen object, not the map position
            surfaceObject.ObjectOffsets = new Vector3 { x = 40, y = 500, z = 300 };
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

            //For a more natural look, flatten the area around the towers
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
                //Initialize the seeder rotation
                tower.Rotation = new Vector3 { };
                tower.WorldPosition = new Vector3 { };
                tower.SurfaceBasedId = GameState.SurfaceState.Global2DMap[towerPlacement.y, towerPlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[towerPlacement.y, towerPlacement.x].hasLandbasedObject = true;

                //The offsets of landbased objects need to similar to that of the surface, apart from some fine tuning
                tower.ObjectOffsets = new Vector3 { x = 40, y =280, z = 300 };
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
                tree.ObjectOffsets = new Vector3 { x = 40, y = 430, z = 300 };
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

                house.ObjectOffsets = new Vector3 { x = 40, y = 450, z = 300 };
                house.ObjectName = "House";
                house.Movement = new HouseControls();
                house.ImpactStatus = new ImpactStatus { };
                house.CrashBoxDebugMode = false;
                if (house.SurfaceBasedId>0) world.WorldInhabitants.Add(house);
            }
        }

        public void SetupGameOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            GameState.ScreenOverlayState.Type = ScreenOverlayType.Game;
            GameState.ScreenOverlayState.SetGameOverlayPreset("Header", "The Omega Strain", "", "");
            GameState.ScreenOverlayState.ShowOverlay = false;
            GameState.ScreenOverlayState.ShowDebugOverlay = false;
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;

            o.Type = ScreenOverlayType.Intro;
            o.Anchor = ScreenOverlayAnchor.Top;

            o.Header = "RETROMESH // BOOT SEQUENCE";
            o.Title = "THE OMEGA STRAIN";

            o.Body =
                "Year 2147.\n\n" +
                "Signal received from the NEREID perimeter colonies.\n" +
                "Biological anomaly confirmed.\n" +
                "Designation: OMEGA STRAIN.\n\n" +
                "Seeder activity detected across multiple sectors.\n" +
                "Infection rate: ACCELERATING.\n" +
                "Containment probability: 12%.\n\n" +
                "PRIMARY DIRECTIVE:\n" +
                "Eliminate Seeders before Critical Mass.";

            o.Footer = "PRESS ANY KEY TO INITIATE PROTOCOL";

            // Scene intro overlay should be visible until player input
            o.ShowOverlay = true;
            o.AutoHide = false;
            o.AutoHideSeconds = 0f;

            // Cinematic / readability
            o.DimStrength = 0.60f;
            o.PanelWidthRatio = 0.74f;
            o.PanelHeightRatio = 0.34f;

            // Hide debug overlay during intro
            o.ShowDebugOverlay = false;
        }

        public void SetupVideoOverlay(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
