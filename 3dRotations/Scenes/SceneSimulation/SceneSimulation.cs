using _3dRotations.Helpers;
using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.KamikazeDroneControls;
using GameAiAndControls.Controls.MotherShipMediumControls;
using GameAiAndControls.Controls.SeederControls;
using GameAiAndControls.Controls.SpaceSwanControls;
using GameAiAndControls.Controls.ZeppelinBomberControls;
using GameAiAndControls.Controls.JumpingFishControls;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scenes.SceneSimulation
{
    public class SceneSimulation : IScene
    {
        Surface Surface = new();

        private static readonly SceneBiomeTypes[] BiomeCycle = new[]
        {
            SceneBiomeTypes.HillsWoods,
            SceneBiomeTypes.Rainforrest,
            SceneBiomeTypes.Desert,
            SceneBiomeTypes.Winter,
        };

        private readonly SceneBiomeTypes _biome;
        private readonly int _simulationRound;

        // Derived scaling values based on round
        private readonly int _seeders;
        private readonly int _drones;
        private readonly int _bombers;
        private readonly int _powerUpSeeders;
        private readonly float _infectionThreshold;
        private readonly int _infectionSpreadRate;
        private readonly int _offscreenSpeedFactor;
        private readonly float _spreadDelaySec;
        private readonly float _spreadRadius;
        private readonly float _motherShipAggression;
        private readonly bool _useLargeMotherShip;

        public string SceneMusic { get; } = "music_battle";
        public SceneTypes SceneType { get; } = SceneTypes.Simulation;
        public SceneBiomeTypes SceneBiome { get; }
        public GameModes GameMode { get; } = GameModes.Live;
        public ISceneDirector Director { get; }

        // IScene infection/difficulty properties driven by simulation round
        public float InfectionThresholdPercent { get; }
        public int InfectionSpreadRate { get; }
        public int SeederOffscreenSpeedFactor { get; }
        public float LocalInfectionSpreadDelaySec { get; }
        public float LocalInfectionSpreadRadius { get; }
        public float MotherShipSmallAggression { get; }
        public float MotherShipMediumAggression { get; }
        public float MotherShipLargeAggression { get; }

        public SceneSimulation()
        {
            _simulationRound = GameState.GamePlayState.SimulationRound;

            // Pick biome pseudo-randomly from round seed so each round feels different
            var rndBiome = new Random(_simulationRound * 7919 + 42);
            _biome = BiomeCycle[rndBiome.Next(BiomeCycle.Length)];
            SceneBiome = _biome;

            // Scale enemies with round: base values + round increment, capped at sane maxima
            int round = _simulationRound;
            _seeders = Math.Min(8 + round * 3, 40);
            _powerUpSeeders = Math.Max(2, Math.Min(2 + round, 8));
            _drones = Math.Min(12 + round * 2, 40);
            _bombers = Math.Min(5 + round, 15);

            // Use large mothership from round 2 onwards, toggle between medium/large
            _useLargeMotherShip = round >= 2;

            // Infection gets progressively more brutal
            _infectionThreshold = Math.Max(1.0f, 3.5f - round * 0.15f);
            _infectionSpreadRate = Math.Min(200 + round * 40, 800);
            _offscreenSpeedFactor = Math.Min(16 + round * 1, 30);
            _spreadDelaySec = Math.Max(0.4f, 2.0f - round * 0.12f);
            _spreadRadius = Math.Min(5500f + round * 200f, 10000f);
            _motherShipAggression = Math.Min(1.20f + round * 0.08f, 2.2f);

            // Assign to interface properties
            InfectionThresholdPercent = _infectionThreshold;
            InfectionSpreadRate = _infectionSpreadRate;
            SeederOffscreenSpeedFactor = _offscreenSpeedFactor;
            LocalInfectionSpreadDelaySec = _spreadDelaySec;
            LocalInfectionSpreadRadius = _spreadRadius;
            MotherShipSmallAggression = _motherShipAggression;
            MotherShipMediumAggression = _motherShipAggression;
            MotherShipLargeAggression = _motherShipAggression;

            Director = new SceneSimulationDirector();
        }

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

            SpawnJumpingFish(world);

            // Zeppelin Bombers
            for (int b = 0; b < _bombers; b++)
            {
                var rmd = new Random();
                var bomber = ZeppelinBomber.CreateZeppelinBomber(Surface);
                bomber.Rotation = new Vector3 { };
                bomber.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-40000, 40000)) * ws, y = 0, z = (92000 + rmd.Next(-40000, 40000)) * ws };
                bomber.ObjectOffsets = new Vector3 { x = 0, y = -50, z = 400 };
                bomber.ObjectName = "ZeppelinBomber";
                bomber.Movement = new ZeppelinBomberControls();
                bomber.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.ZeppelinBomberHealth };
                bomber.CrashBoxDebugMode = false;
                bomber.IsActive = true;
                world.WorldInhabitants.Add(bomber);
                GameState.SurfaceState.AiObjects.Add(bomber);
            }

            // Kamikaze Drones — inactive until Decoy unlocked
            for (int i = 0; i < _drones; i++)
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

            // Seeders — close group
            int nearSeeders = _seeders / 2;
            int farSeeders = _seeders - nearSeeders;

            for (int i = 0; i < nearSeeders; i++)
            {
                var rmd = new Random();
                var seeder = Seeder.CreateSeeder(Surface);
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-20000, 20000)) * ws, y = 0, z = (92000 + rmd.Next(-20000, 20000)) * ws };
                seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seeder.ObjectName = "Seeder";
                seeder.Movement = new SeederControls();
                seeder.CrashBoxDebugMode = false;
                seeder.ImpactStatus = new ImpactStatus { };
                seeder.HasPowerUp = false;
                world.WorldInhabitants.Add(seeder);
                GameState.SurfaceState.AiObjects.Add(seeder);
            }

            for (int i = 0; i < farSeeders; i++)
            {
                var rmd = new Random();
                var seeder = Seeder.CreateSeeder(Surface);
                seeder.Rotation = new Vector3 { };
                seeder.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-50000, 30000)) * ws, y = 0, z = (92000 + rmd.Next(-50000, 30000)) * ws };
                seeder.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seeder.ObjectName = "Seeder";
                seeder.Movement = new SeederControls();
                seeder.CrashBoxDebugMode = false;
                seeder.ImpactStatus = new ImpactStatus { };
                seeder.HasPowerUp = false;
                world.WorldInhabitants.Add(seeder);
                GameState.SurfaceState.AiObjects.Add(seeder);
            }

            // PowerUp seeders
            for (int i = 0; i < _powerUpSeeders; i++)
            {
                var rmd = new Random();
                var seederPu = Seeder.CreateSeeder(Surface);
                seederPu.Rotation = new Vector3 { };
                seederPu.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-35000, 35000)) * ws, y = 0, z = (92000 + rmd.Next(-35000, 35000)) * ws };
                seederPu.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 600 };
                seederPu.ObjectName = "Seeder";
                seederPu.Movement = new SeederControls();
                seederPu.CrashBoxDebugMode = false;
                seederPu.ImpactStatus = new ImpactStatus { };
                seederPu.HasPowerUp = true;
                world.WorldInhabitants.Add(seederPu);
                GameState.SurfaceState.AiObjects.Add(seederPu);
            }

            // MotherShip — Large from round 2+, Medium for rounds 0-1
            if (_useLargeMotherShip)
            {
                var motherShipLarge = MotherShipLarge.CreateMotherShipLarge(Surface);
                motherShipLarge.Rotation = new Vector3 { };
                motherShipLarge.WorldPosition = new Vector3 { x = 95700 * ws, y = 0, z = 88000 * ws };
                motherShipLarge.ObjectOffsets = new Vector3 { x = 0, y = -1500, z = 400 };
                motherShipLarge.ObjectName = "MotherShipLarge";
                motherShipLarge.Movement = new MotherShipLargeControls();
                var lazer = Lazer.CreateLazer(Surface, scaleMultiplier: 2.5f);
                lazer.CrashBoxDebugMode = false;
                motherShipLarge.WeaponSystems = new Weapons(new List<I3dObject> { lazer }, motherShipLarge.Movement!, (_3dObject)motherShipLarge)
                {
                    ShowAimAssist = false,
                    FireAsEnemyWeapon = true,
                    EnemyLazerName = "EnemyLazerLarge"
                };
                motherShipLarge.ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.MotherShipLargeHealth };
                motherShipLarge.CrashBoxDebugMode = false;
                motherShipLarge.HasPowerUp = false;
                motherShipLarge.IsActive = false;
                world.WorldInhabitants.Add(motherShipLarge);
                GameState.SurfaceState.AiObjects.Add(motherShipLarge);
            }
            else
            {
                var motherShipMedium = MotherShipMedium.CreateMotherShipMedium(Surface);
                motherShipMedium.Rotation = new Vector3 { };
                motherShipMedium.WorldPosition = new Vector3 { x = 95700 * ws, y = 0, z = 90000 * ws };
                motherShipMedium.ObjectOffsets = new Vector3 { x = 0, y = -1500, z = 400 };
                motherShipMedium.ObjectName = "MotherShipMedium";
                motherShipMedium.Movement = new MotherShipMediumControls();
                var lazer = Lazer.CreateLazer(Surface, scaleMultiplier: 2.0f);
                lazer.CrashBoxDebugMode = false;
                motherShipMedium.WeaponSystems = new Weapons(new List<I3dObject> { lazer }, motherShipMedium.Movement!, (_3dObject)motherShipMedium)
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
            }

            // SpaceSwans — passive wildlife
            for (int s = 0; s < 50; s++)
            {
                var rmd = new Random();
                var spaceSwan = SpaceSwan.CreateSpaceSwan(Surface);
                spaceSwan.Rotation = new Vector3 { };
                spaceSwan.WorldPosition = new Vector3 { x = (95700 + rmd.Next(-40000, 40000)) * ws, y = 0, z = (92000 + rmd.Next(-40000, 40000)) * ws };
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

            var towerPlacements = SurfaceGeneration.FindTowerPlacements(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight());
            SurfaceGeneration.FlattenTerrainAroundTowers_ToHighlands(
                GameState.SurfaceState.Global2DMap,
                Surface.MaxHeight(),
                towerPlacements,
                writeDebugLogs: false);

            foreach (var towerPlacement in towerPlacements)
            {
                var tower = Tower.CreateTower(Surface);
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

            var treePlacements = SurfaceGeneration.FindTreePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.TileSize(), Surface.MaxHeight(), 30000);
            SurfaceGeneration.FlattenTerrainAroundPlacements(GameState.SurfaceState.Global2DMap, Surface.MaxHeight(), treePlacements, radius: 0);
            foreach (var treePlacement in treePlacements)
            {
                var tree = Tree.CreateTree(Surface);
                tree.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                tree.SurfaceBasedId = GameState.SurfaceState.Global2DMap[treePlacement.y, treePlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[treePlacement.y, treePlacement.x].hasLandbasedObject = true;
                tree.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 425 * ScreenSetup.ScreenScaleY, z = 400 };
                tree.ObjectName = "Tree";
                tree.Movement = new TreeControls();
                tree.ImpactStatus = new ImpactStatus { };
                tree.CrashBoxDebugMode = false;
                if (tree.SurfaceBasedId > 0) world.WorldInhabitants.Add(tree);
            }

            var housePlacements = SurfaceGeneration.FindHousePlacementAreas(GameState.SurfaceState.Global2DMap, Surface.GlobalMapSize(), Surface.MaxHeight(), treePlacements, 15000);
            SurfaceGeneration.FlattenTerrainAroundPlacements(GameState.SurfaceState.Global2DMap, Surface.MaxHeight(), housePlacements, radius: 1);
            foreach (var housePlacement in housePlacements)
            {
                var house = House.CreateHouse(Surface);
                house.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                house.SurfaceBasedId = GameState.SurfaceState.Global2DMap[housePlacement.y, housePlacement.x].mapId;
                GameState.SurfaceState.Global2DMap[housePlacement.y, housePlacement.x].hasLandbasedObject = true;
                house.ObjectOffsets = new Vector3 { x = 75 * ScreenSetup.ScreenScaleX, y = 450 * ScreenSetup.ScreenScaleY, z = 400 };
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

            int round = _simulationRound;
            string roundLabel = round == 0 ? "INITIAL" : $"ROUND {round + 1}";
            int totalEnemies = _seeders + _powerUpSeeders + _drones + _bombers + 1;
            string motherShipClass = _useLargeMotherShip ? "LARGE-CLASS WAR CARRIER" : "MEDIUM-CLASS CARRIER";
            string biomeName = _biome switch
            {
                SceneBiomeTypes.Rainforrest => "JUNGLE WORLD",
                SceneBiomeTypes.Desert      => "DESERT WORLD",
                SceneBiomeTypes.Winter      => "FROZEN WORLD",
                _                           => "TEMPERATE WORLD"
            };

            o.Header = "RETROMESH // SIMULATION";
            o.Title = $"COMBAT SIMULATOR \u2014 {roundLabel}";

            o.Body =
                $"The galaxy has been cleared \u2014 but the war is not over.\\n\\n" +
                $"A new infection wave is imminent. Train now.\\n" +
                $"Simulation type: {biomeName}\\n\\n" +
                $"ENEMY COUNT: {totalEnemies} units total\\n" +
                $"  Seeders:          {_seeders + _powerUpSeeders} (incl. {_powerUpSeeders} power-up carriers)\\n" +
                $"  Kamikaze Drones:  {_drones}\\n" +
                $"  Zeppelin Bombers: {_bombers}\\n" +
                $"  MotherShip:       1 \u00d7 {motherShipClass}\\n\\n" +
                $"Infection tolerance: {_infectionThreshold:F1}%  |  Spread delay: {_spreadDelaySec:F1}s\\n\\n" +
                "DIRECTIVE:\\n" +
                "Survive. Score high. Defend your rank on the leaderboard.";

            o.Footer = "PRESS ANY KEY TO ENTER THE SIMULATION";

            o.ShowOverlay = true;
            o.AutoHide = false;
            o.AutoHideSeconds = 0f;

            o.DimStrength = 0.65f;
            o.PanelWidthRatio = 0.80f;
            o.PanelHeightRatio = 0.40f;

            o.ShowDebugOverlay = false;
        }

        public void SetupGameOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            GameState.ScreenOverlayState.Type = ScreenOverlayType.Game;
            int round = _simulationRound;
            string roundLabel = round == 0 ? "Simulation" : $"Simulation R{round + 1}";
            GameState.ScreenOverlayState.SetGameOverlayPreset("Header", roundLabel, "", "");
            GameState.ScreenOverlayState.ShowOverlay = false;
            GameState.ScreenOverlayState.ShowDebugOverlay = false;
        }

        public void SetupVideoOverlay(string fileName)
        {
            throw new NotImplementedException();
        }

        private void SpawnJumpingFish(I3dWorld world)
        {
            var fishJumpAreas = GameState.SurfaceState.FishJumpAreas;
            if (fishJumpAreas == null || fishJumpAreas.Count == 0)
                return;

            int fishCount = Math.Min(100, fishJumpAreas.Count);
            int tileSize = Surface.TileSize();
            for (int i = 0; i < fishCount; i++)
            {
                int areaIndex = (int)MathF.Floor(i * fishJumpAreas.Count / (float)fishCount);
                var area = fishJumpAreas[areaIndex];
                float jumpSpan = Math.Min(tileSize * 2f, Math.Max(tileSize, (area.EndTileX - area.StartTileX - 1) * tileSize));
                float baseOffsetX = 75 * ScreenSetup.ScreenScaleX;
                float minPathOffsetX = baseOffsetX + ((area.StartTileX - area.CenterTileX) * tileSize);
                float maxPathOffsetX = baseOffsetX + ((area.EndTileX - area.CenterTileX) * tileSize);

                var jumpingFish = JumpingFish.CreateJumpingFish(Surface);
                jumpingFish.Rotation = new Vector3 { x = 70, y = 0, z = 0 };
                jumpingFish.WorldPosition = new Vector3 { };
                jumpingFish.SurfaceBasedId = GameState.SurfaceState.Global2DMap[area.CenterTileZ, area.CenterTileX].mapId;
                jumpingFish.ObjectOffsets = new Vector3
                {
                    x = baseOffsetX,
                    y = 500 * ScreenSetup.ScreenScaleY,
                    z = 400
                };
                jumpingFish.ObjectName = "JumpingFish";
                jumpingFish.Movement = new JumpingFishControls(jumpSpan, minPathOffsetX, maxPathOffsetX);
                jumpingFish.ImpactStatus = new ImpactStatus { };
                jumpingFish.CrashBoxDebugMode = false;
                jumpingFish.CrashBoxes = new List<List<IVector3>>();
                jumpingFish.IsActive = true;
                world.WorldInhabitants.Add(jumpingFish);
            }
        }
    }
}
