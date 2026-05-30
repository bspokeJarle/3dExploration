using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls.MotherShipMediumControls;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;
using _3dRotations.Helpers;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.ZeppelinBomberControls;
using GameAiAndControls.Controls.KamikazeDroneControls;
using GameAiAndControls.Controls.SeederControls;
using GameAiAndControls.Controls.SpaceSwanControls;
using System;

namespace _3dRotations.Scene.Scene6
{
    public class Scene6 : IScene
    {
        Surface Surface = new();

        public string SceneMusic { get; } = "music_kanpai";
        public SceneTypes SceneType { get; } = SceneTypes.Game;
        public SceneBiomeTypes SceneBiome { get; } = SceneBiomeTypes.Desert;
        public ISceneDirector Director { get; } = new Scene6Director();
        public GameModes GameMode { get; } = GameModes.Playback;

        public float InfectionThresholdPercent { get; } = 11.5f;
        public int InfectionSpreadRate { get; } = 8;
        public int SeederOffscreenSpeedFactor { get; } = 20;
        public float LocalInfectionSpreadDelaySec { get; } = 1.8f;
        public float LocalInfectionSpreadRadius { get; } = 5800f;
        public float MotherShipMediumAggression { get; } = 1.35f;

        public void SetupScene(I3dWorld world)
        {
            var ws = SurfaceSetup.WorldScale;

            var ship = Ship.CreateShip(Surface);
            Surface.Create2DMap(30000, 15000, GameMode, "Scene6SurfaceRecording_20260526_220746.retro");
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

            for (int b = 0; b < 6; b++)
            {
                var rmdBomber = new Random();
                var bomber = ZeppelinBomber.CreateZeppelinBomber(Surface);
                bomber.Rotation = new Vector3 { };
                bomber.WorldPosition = new Vector3 { x = (95700 + rmdBomber.Next(-40000, 40000)) * ws, y = 0, z = (92000 + rmdBomber.Next(-40000, 40000)) * ws };
                bomber.ObjectOffsets = new Vector3 { x = 0, y = -50, z = 400 };
                bomber.ObjectName = "ZeppelinBomber";
                bomber.Movement = new ZeppelinBomberControls();
                bomber.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.ZeppelinBomberHealth };
                bomber.CrashBoxDebugMode = false;
                bomber.IsActive = true;
                world.WorldInhabitants.Add(bomber);
                GameState.SurfaceState.AiObjects.Add(bomber);
            }

            for (int i = 0; i < 14; i++)
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

            foreach (var seederPosition in SeederPlacementHelpers.CreateRingSeederPositions(
                         count: 17,
                         center: GameState.SurfaceState.GlobalMapPosition,
                         seed: 6061,
                         nearSeederCount: 5,
                         firstRingRadius: 7500f,
                         ringRadiusStep: 11500f))
            {
                AddSeeder(world, seederPosition, hasPowerUp: false);
            }

            foreach (var seederPosition in SeederPlacementHelpers.CreateRandomSeederPositions(
                         count: 4,
                         center: GameState.SurfaceState.GlobalMapPosition,
                         seed: 6062,
                         minRadius: 16000f,
                         maxRadius: 42000f))
            {
                AddSeeder(world, seederPosition, hasPowerUp: true);
            }

            var motherShipMedium = MotherShipMedium.CreateMotherShipMedium(Surface);
            motherShipMedium.Rotation = new Vector3 { };
            motherShipMedium.WorldPosition = new Vector3 { x = 95700 * ws, y = 0, z = 90000 * ws };
            motherShipMedium.ObjectOffsets = new Vector3 { x = 0, y = -1500, z = 400 };
            motherShipMedium.ObjectName = "MotherShipMedium";
            motherShipMedium.Movement = new MotherShipMediumControls();
            var motherShipMediumLazer = Lazer.CreateLazer(Surface, scaleMultiplier: 2.0f);
            motherShipMediumLazer.CrashBoxDebugMode = false;
            var motherShipMediumWeapons = new List<I3dObject> { motherShipMediumLazer };
            motherShipMedium.WeaponSystems = new Weapons(motherShipMediumWeapons, motherShipMedium.Movement!, (_3dObject)motherShipMedium)
            {
                ShowAimAssist = false,
                FireAsEnemyWeapon = true,
                EnemyLazerName = "EnemyLazerMedium"
            };
            motherShipMedium.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.MotherShipMediumHealth };
            motherShipMedium.CrashBoxDebugMode = false;
            motherShipMedium.HasPowerUp = false;
            motherShipMedium.IsActive = false;
            world.WorldInhabitants.Add(motherShipMedium);
            GameState.SurfaceState.AiObjects.Add(motherShipMedium);

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

            var surfaceObject = (_3dObject)Surface.GetSurfaceViewPort();
            surfaceObject.ObjectName = "Surface";
            surfaceObject.ObjectOffsets = new Vector3 { x = 105 * ScreenSetup.ScreenScaleX, y = 500 * ScreenSetup.ScreenScaleY, z = 400 };
            surfaceObject.Rotation = new Vector3 { x = 70, y = 0, z = 0 };
            surfaceObject.WorldPosition = new Vector3 { };
            surfaceObject.Movement = new GroundControls();
            surfaceObject.ParentSurface = Surface;
            surfaceObject.ImpactStatus = new ImpactStatus { };
            surfaceObject.CrashBoxDebugMode = false;
            surfaceObject.CrashBoxesFollowRotation = false;
            world.WorldInhabitants.Add(surfaceObject);
            GameState.SurfaceState.SurfaceViewportObject = surfaceObject;
            world.WorldInhabitants.Add(SandEmitter.CreateSandEmitter(Surface));

            var towerPlacements = SurfaceGeneration.FindTowerPlacements(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight());
            AddGuaranteedStartTowerPlacements(towerPlacements);
            SurfaceGeneration.FlattenTerrainAroundTowers_ToHighlands(
                GameState.SurfaceState.Global2DMap,
                Surface.MaxHeight(),
                towerPlacements,
                writeDebugLogs: false
            );

            foreach (var towerPlacement in towerPlacements)
            {
                var tower = DesertTower.CreateDesertTower(Surface);
                tower.Rotation = new Vector3 { };
                tower.WorldPosition = new Vector3 { };
                tower.SurfaceBasedId = GameState.SurfaceState.Global2DMap[towerPlacement.y, towerPlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[towerPlacement.y, towerPlacement.x].hasLandbasedObject = true;
                tower.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 280 * ScreenSetup.ScreenScaleY, z = 400 };
                tower.ObjectName = "Tower";
                tower.Movement = new TowerControls();
                tower.CrashBoxDebugMode = false;
                tower.ImpactStatus = new ImpactStatus { };
                world.WorldInhabitants.Add(tower);
            }

            var rockPlacements = SurfaceGeneration.FindTreePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight(), 12000);
            AddGuaranteedStartRockPlacements(rockPlacements);
            SurfaceGeneration.FlattenTerrainAroundPlacements(GameState.SurfaceState.Global2DMap, Surface.MaxHeight(), rockPlacements, radius: 1);
            foreach (var rockPlacement in rockPlacements)
            {
                if (GameState.SurfaceState.Global2DMap[rockPlacement.y, rockPlacement.x].hasLandbasedObject)
                    continue;

                var rocks = DesertRockFormation.CreateDesertRockFormation(Surface);
                rocks.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                rocks.SurfaceBasedId = GameState.SurfaceState.Global2DMap[rockPlacement.y, rockPlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[rockPlacement.y, rockPlacement.x].hasLandbasedObject = true;
                rocks.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 455 * ScreenSetup.ScreenScaleY, z = 400 };
                rocks.ObjectName = "DesertRockFormation";
                rocks.Movement = new DesertRockControls();
                rocks.ImpactStatus = new ImpactStatus { };
                rocks.CrashBoxDebugMode = false;
                if (rocks.SurfaceBasedId > 0) world.WorldInhabitants.Add(rocks);
            }

            var cactusPlacements = SurfaceGeneration.FindTreePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight(), 30000);
            AddGuaranteedStartCactusPlacements(cactusPlacements);
            SurfaceGeneration.FlattenTerrainAroundPlacements(GameState.SurfaceState.Global2DMap, Surface.MaxHeight(), cactusPlacements, radius: 1);
            foreach (var cactusPlacement in cactusPlacements)
            {
                var cactus = Cactus.CreateCactus(Surface);
                cactus.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                cactus.SurfaceBasedId = GameState.SurfaceState.Global2DMap[cactusPlacement.y, cactusPlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[cactusPlacement.y, cactusPlacement.x].hasLandbasedObject = true;
                cactus.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 425 * ScreenSetup.ScreenScaleY, z = 400 };
                cactus.ObjectName = "Cactus";
                cactus.Movement = new CactusControls();
                cactus.ImpactStatus = new ImpactStatus { };
                cactus.CrashBoxDebugMode = false;
                if (cactus.SurfaceBasedId > 0) world.WorldInhabitants.Add(cactus);
            }
        }

        private void AddSeeder(I3dWorld world, Vector3 worldPosition, bool hasPowerUp)
        {
            var seeder = Seeder.CreateSeeder(Surface);
            seeder.Rotation = new Vector3 { };
            seeder.WorldPosition = worldPosition;
            seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
            seeder.ObjectName = "Seeder";
            seeder.Movement = new SeederControls();
            seeder.CrashBoxDebugMode = false;
            seeder.ImpactStatus = new ImpactStatus { };
            seeder.HasPowerUp = hasPowerUp;
            world.WorldInhabitants.Add(seeder);
            GameState.SurfaceState.AiObjects.Add(seeder);
        }

        private void AddGuaranteedStartRockPlacements(List<(int x, int y, int height)> rockPlacements)
        {
            var map = GameState.SurfaceState.Global2DMap;
            if (map == null)
                return;

            int tileSize = Surface.TileSize();
            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);
            int startX = ((int)(GameState.SurfaceState.GlobalMapPosition.x / tileSize)) % sizeX;
            int startY = ((int)(GameState.SurfaceState.GlobalMapPosition.z / tileSize)) % sizeY;
            if (startX < 0) startX += sizeX;
            if (startY < 0) startY += sizeY;

            int maxHeight = Surface.MaxHeight();
            int coastCutoff = Math.Max(1, (int)Math.Ceiling(maxHeight * 0.15));
            var used = new HashSet<(int x, int y)>();
            foreach (var placement in rockPlacements)
                used.Add((placement.x, placement.y));

            (int dx, int dy)[] nearStartOffsets =
            {
                (5, 7),
                (11, 9),
                (16, 11)
            };

            foreach (var (dx, dy) in nearStartOffsets)
            {
                int x = (startX + dx) % sizeX;
                int y = (startY + dy) % sizeY;
                TryAddRockPlacementNear(x, y);
            }

            void TryAddRockPlacementNear(int targetX, int targetY)
            {
                const int searchRadius = 5;
                for (int radius = 0; radius <= searchRadius; radius++)
                {
                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            if (Math.Abs(ox) != radius && Math.Abs(oy) != radius)
                                continue;

                            int x = (targetX + ox + sizeX) % sizeX;
                            int y = (targetY + oy + sizeY) % sizeY;
                            if (TryAddRockPlacement(x, y))
                                return;
                        }
                    }
                }
            }

            bool TryAddRockPlacement(int x, int y)
            {
                if (x <= 1 || y <= 1 || x >= sizeX - 2 || y >= sizeY - 2)
                    return false;
                if (used.Contains((x, y)))
                    return false;

                var tile = map[y, x];
                if (tile.hasLandbasedObject)
                    return false;
                if (tile.mapDepth <= coastCutoff)
                    return false;

                used.Add((x, y));
                rockPlacements.Insert(0, (x, y, tile.mapDepth));
                return true;
            }
        }

        private void AddGuaranteedStartTowerPlacements(List<(int x, int y, int height)> towerPlacements)
        {
            var map = GameState.SurfaceState.Global2DMap;
            if (map == null)
                return;

            int tileSize = Surface.TileSize();
            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);
            int startX = ((int)(GameState.SurfaceState.GlobalMapPosition.x / tileSize)) % sizeX;
            int startY = ((int)(GameState.SurfaceState.GlobalMapPosition.z / tileSize)) % sizeY;
            if (startX < 0) startX += sizeX;
            if (startY < 0) startY += sizeY;

            int maxHeight = Surface.MaxHeight();
            int coastCutoff = Math.Max(1, (int)Math.Ceiling(maxHeight * 0.15));
            int highlandsMin = Math.Max(coastCutoff + 1, (int)(maxHeight * 0.40));
            var used = new HashSet<(int x, int y)>();
            foreach (var placement in towerPlacements)
                used.Add((placement.x, placement.y));

            (int dx, int dy)[] nearStartOffsets =
            {
                (7, 3),
                (15, 6)
            };

            foreach (var (dx, dy) in nearStartOffsets)
            {
                int x = (startX + dx) % sizeX;
                int y = (startY + dy) % sizeY;
                TryAddTowerPlacementNear(x, y);
            }

            void TryAddTowerPlacementNear(int targetX, int targetY)
            {
                const int searchRadius = 5;
                for (int radius = 0; radius <= searchRadius; radius++)
                {
                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            if (Math.Abs(ox) != radius && Math.Abs(oy) != radius)
                                continue;

                            int x = (targetX + ox + sizeX) % sizeX;
                            int y = (targetY + oy + sizeY) % sizeY;
                            if (TryAddTowerPlacement(x, y))
                                return;
                        }
                    }
                }
            }

            bool TryAddTowerPlacement(int x, int y)
            {
                if (x <= 2 || y <= 2 || x >= sizeX - 3 || y >= sizeY - 3)
                    return false;
                if (used.Contains((x, y)))
                    return false;

                var tile = map[y, x];
                if (tile.hasLandbasedObject)
                    return false;
                if (tile.mapDepth <= coastCutoff)
                    return false;

                int height = Math.Max(tile.mapDepth, highlandsMin);
                used.Add((x, y));
                towerPlacements.Insert(0, (x, y, height));
                return true;
            }
        }

        private void AddGuaranteedStartCactusPlacements(List<(int x, int y, int height)> cactusPlacements)
        {
            var map = GameState.SurfaceState.Global2DMap;
            if (map == null)
                return;

            int tileSize = Surface.TileSize();
            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);
            int startX = ((int)(GameState.SurfaceState.GlobalMapPosition.x / tileSize)) % sizeX;
            int startY = ((int)(GameState.SurfaceState.GlobalMapPosition.z / tileSize)) % sizeY;
            if (startX < 0) startX += sizeX;
            if (startY < 0) startY += sizeY;

            int maxHeight = Surface.MaxHeight();
            int coastCutoff = Math.Max(1, (int)Math.Ceiling(maxHeight * 0.15));
            var used = new HashSet<(int x, int y)>();
            foreach (var placement in cactusPlacements)
                used.Add((placement.x, placement.y));

            (int dx, int dy)[] nearStartOffsets =
            {
                (4, 4),
                (10, 5),
                (14, 8),
                (6, 11),
                (12, 13)
            };

            foreach (var (dx, dy) in nearStartOffsets)
            {
                int x = (startX + dx) % sizeX;
                int y = (startY + dy) % sizeY;
                TryAddCactusPlacementNear(x, y);
            }

            void TryAddCactusPlacementNear(int targetX, int targetY)
            {
                const int searchRadius = 4;
                for (int radius = 0; radius <= searchRadius; radius++)
                {
                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            if (Math.Abs(ox) != radius && Math.Abs(oy) != radius)
                                continue;

                            int x = (targetX + ox + sizeX) % sizeX;
                            int y = (targetY + oy + sizeY) % sizeY;
                            if (TryAddCactusPlacement(x, y))
                                return;
                        }
                    }
                }
            }

            bool TryAddCactusPlacement(int x, int y)
            {
                if (x <= 1 || y <= 1 || x >= sizeX - 2 || y >= sizeY - 2)
                    return false;
                if (used.Contains((x, y)))
                    return false;

                var tile = map[y, x];
                if (tile.hasLandbasedObject)
                    return false;
                if (tile.mapDepth <= coastCutoff)
                    return false;

                used.Add((x, y));
                cactusPlacements.Insert(0, (x, y, tile.mapDepth));
                return true;
            }
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;

            o.Type = ScreenOverlayType.Intro;
            o.Anchor = ScreenOverlayAnchor.Top;
            o.Header = "RETROMESH // SECTOR BRIEFING";
            o.Title = "PLANET ARIDUS - PHASE VI";
            o.Body =
                "Descending on ARIDUS - arid desert world, surface temperature extreme.\n\n" +
                "Sand contamination identified as Omega Strain vector.\n" +
                "Twenty-one seeders detected across dune fields.\n" +
                "Kamikaze escort: FOURTEEN units. Bomber wing: SIX.\n" +
                "Spread delay: 1.8 seconds. Bio-tolerance: 11.5%.\n\n" +
                "DIRECTIVE:\n" +
                "Destroy all seeders before the desert biome collapses entirely.";
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
            GameState.ScreenOverlayState.SetGameOverlayPreset("Header", "Planet Aridus", "", "");
            GameState.ScreenOverlayState.ShowOverlay = false;
            GameState.ScreenOverlayState.ShowDebugOverlay = false;
        }

        public void SetupVideoOverlay(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
