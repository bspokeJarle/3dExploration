using _3dRotations.Helpers;
using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.ZeppelinBomberControls;
using GameAiAndControls.Controls.KamikazeDroneControls;
using GameAiAndControls.Controls.MotherShipSmallControls;
using GameAiAndControls.Controls.SeederControls;
using GameAiAndControls.Controls.SpaceSwanControls;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scene.Scene4
{
    public class Scene4 : IScene
    {
        Surface Surface = new();

        public string SceneMusic { get; } = "music_battle";
        public SceneTypes SceneType { get; } = SceneTypes.Game;
        public ISceneDirector Director { get; } = new Scene4Director();
        public GameModes GameMode { get; } = GameModes.Live;
        public float InfectionThresholdPercent { get; } = 6f;
        public int InfectionSpreadRate { get; } = 150;
        public int SeederOffscreenSpeedFactor { get; } = 14;
        public float LocalInfectionSpreadDelaySec { get; } = 4.0f;
        public float LocalInfectionSpreadRadius { get; } = 4500f;

        public void SetupScene(I3dWorld world)
        {
            var ws = SurfaceSetup.WorldScale;

            var ship = Ship.CreateShip(Surface);
            Surface.Create2DMap(30000, 15000, GameMode, null);
            var weapons = new List<I3dObject> { Lazer.CreateLazer(Surface), Bullet.CreateBullet(Surface) };
            ship.Rotation = new Vector3 { };
            ship.WorldPosition = new Vector3 { };
            ship.ObjectName = "Ship";
            ship.ImpactStatus = new ImpactStatus { ObjectHealth = ShipSetup.DefaultShipHealth };
            ship.CrashBoxDebugMode = false;
            ship.WeaponSystems = new Weapons(weapons, ship.Movement!, ship);
            world.WorldInhabitants.Add(ship);

            var guidanceArrow = SeederGuidanceArrow.CreateSeederGuidanceArrow(Surface);
            guidanceArrow.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 200 };
            guidanceArrow.Rotation = new Vector3 { x = 70, y = 0, z = 90 };
            guidanceArrow.WorldPosition = new Vector3 { };
            guidanceArrow.ObjectName = "SeederGuidanceArrow";
            guidanceArrow.ImpactStatus = new ImpactStatus { };
            guidanceArrow.CrashBoxDebugMode = false;
            world.WorldInhabitants.Add(guidanceArrow);

            // ZeppelinBombers — 3 bombers
            for (int b = 0; b < 3; b++)
            {
                var rmdBomber = new Random();
                var bomber = ZeppelinBomber.CreateZeppelinBomber(Surface);
                bomber.Rotation = new Vector3 { };
                bomber.WorldPosition = new Vector3 { x = (95700 + rmdBomber.Next(-40000, 40000)) * ws, y = 0, z = (92000 + rmdBomber.Next(-40000, 40000)) * ws };
                bomber.ObjectOffsets = new Vector3 { x = 0, y = 150, z = 400 };
                bomber.ObjectName = "ZeppelinBomber";
                bomber.Movement = new ZeppelinBomberControls();
                bomber.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.ZeppelinBomberHealth };
                bomber.CrashBoxDebugMode = false;
                bomber.IsActive = true;
                world.WorldInhabitants.Add(bomber);
                GameState.SurfaceState.AiObjects.Add(bomber);
            }

            // Drones — waiting until the player has a Decoy powerup
            for (int i = 0; i < 10; i++)
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
            for (int i = 0; i < 7; i++)
            {
                var rmd = new Random();

                var seeder = Seeder.CreateSeeder(Surface);
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-15000, 15000)) * ws, y = 0, z = (92000 + rmd.Next(-15000, 15000)) * ws };
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
            for (int i = 0; i < 5; i++)
            {
                var rmd = new Random();

                var seeder = Seeder.CreateSeeder(Surface);
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-35000, 15000)) * ws, y = 0, z = (92000 + rmd.Next(-35000, 15000)) * ws };
                seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seeder.ObjectName = "Seeder";
                seeder.Movement = new SeederControls();
                seeder.CrashBoxDebugMode = false;
                seeder.ImpactStatus = new ImpactStatus { };
                seeder.HasPowerUp = false;
                world.WorldInhabitants.Add(seeder);
                GameState.SurfaceState.AiObjects.Add(seeder);
            }

            // Powerup seeders
            for (int i = 0; i < 3; i++)
            {
                var rmd = new Random();
                var seederPowerup = Seeder.CreateSeeder(Surface);
                seederPowerup.Rotation = new Vector3 { };
                seederPowerup.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-25000, 25000)) * ws, y = 0, z = (92000 + rmd.Next(-25000, 25000)) * ws };
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

            // Surface
            var surfaceObject = (_3dObject)Surface.GetSurfaceViewPort();
            surfaceObject.ObjectName = "Surface";
            surfaceObject.ObjectOffsets = new Vector3 { x = 105 * ScreenSetup.ScreenScaleX, y = 500 * ScreenSetup.ScreenScaleY, z = 300 };
            surfaceObject.Rotation = new Vector3 { x = 70, y = 0, z = 0 };
            surfaceObject.WorldPosition = new Vector3 { };
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

            var treePlacements = SurfaceGeneration.FindTreePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight(), 30000);
            var treeIndex = 0;
            foreach (var treePlacement in treePlacements)
            {
                treeIndex++;
                var tree = Tree.CreateTree(Surface);
                tree.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                tree.SurfaceBasedId = GameState.SurfaceState.Global2DMap[treePlacement.y, treePlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[treePlacement.y, treePlacement.x].hasLandbasedObject = true;
                tree.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 425 * ScreenSetup.ScreenScaleY, z = 300 };
                tree.ObjectName = "Tree";
                tree.Movement = new TreeControls();
                tree.ImpactStatus = new ImpactStatus { };
                tree.CrashBoxDebugMode = false;
                if (tree.SurfaceBasedId > 0) world.WorldInhabitants.Add(tree);
            }

            var housePlacements = SurfaceGeneration.FindHousePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.MaxHeight(), treePlacements, 15000);
            foreach (var housePlacement in housePlacements)
            {
                var house = House.CreateHouse(Surface);
                house.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                house.SurfaceBasedId = GameState.SurfaceState.Global2DMap[housePlacement.y, housePlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[housePlacement.y, housePlacement.x].hasLandbasedObject = true;
                house.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 450 * ScreenSetup.ScreenScaleY, z = 300 };
                house.ObjectName = "House";
                house.Movement = new HouseControls();
                house.ImpactStatus = new ImpactStatus { };
                house.CrashBoxDebugMode = false;
                if (house.SurfaceBasedId > 0) world.WorldInhabitants.Add(house);
            }
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;

            o.Type = ScreenOverlayType.Intro;
            o.Anchor = ScreenOverlayAnchor.Top;

            o.Header = "RETROMESH // SECTOR BRIEFING";
            o.Title = "THE OMEGA STRAIN — PHASE IV";

            o.Body =
                "Mining station KEPLER-22b reports critical breach.\n\n" +
                "Omega Strain has mutated. Fifteen seeders confirmed.\n" +
                "Escort drones: TEN. Formation pattern: aggressive.\n" +
                "Bomber squadron: THREE units in orbit.\n" +
                "Infection spread rate: EXTREME.\n" +
                "Local cascade delay: MINIMAL.\n\n" +
                "Bio-contamination tolerance: 6%.\n\n" +
                "DIRECTIVE:\n" +
                "Purge the station. Accept no losses.";

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
