using _3dRotations.Scene.Scene1;
using _3dRotations.Scene.Scene3;
using _3dRotations.Scene.Scene4;
using _3dRotations.Scene.Scene5;
using _3dRotations.Scene.Scene6;
using _3dRotations.Scene.Scene7;
using _3dRotations.Scene.Scene8;
using _3dRotations.Scenes.Intro;
using _3dRotations.Scenes.Outro;
using _3dRotations.Scenes.SceneSimulation;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using CommonUtilities.Persistence;
using Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using static Domain._3dSpecificsImplementations;

namespace _3DWorld.Scene
{
    public class SceneHandler : ISceneHandler
    {
        private List<IScene> scenes = new List<IScene> { new Intro(), new Scene1(), new Scene2(), new Scene3(), new Scene4(), new Scene5(), new Scene6(), new Scene7(), new Scene8(), new Outro(), new SceneSimulation() };
        private int currentSceneIndex = 0;
        private const bool enableLogging = false;
        private const int SceneAdvanceDelayFrames = 5;
        private bool _pendingSceneAdvance = false;
        private int _pendingSceneAdvanceFramesLeft = 0;
        private int? _targetSceneIndex = null;
        private bool _pendingNextScene = false;
        private SavedGameState? _pendingSavedState = null;

        public SceneHandler()
        {
            ApplySceneIndexOverrideFromGameState();
        }

        public IScene GetActiveScene() => scenes[currentSceneIndex];

        // -----------------------------------------------------------------
        // Scene lifecycle
        // -----------------------------------------------------------------

        public void SetupActiveScene(I3dWorld world)
        {
            PersistenceSetup.Initialize();
            ApplySceneIndexOverrideFromGameState();
            ResetSurfaceState();
            var scene = GetActiveScene();
            scene.SetupSceneOverlay();
            ClearVideoOverlay();
            ApplySceneSettings(scene);
            scene.SetupScene((_3dWorld)world);
            InitializeDirector(scene, world);
            ValidateGameSceneSetup(scene, world);
        }

        public void ResetActiveScene(I3dWorld world)
        {
            if (Logger.ShouldLog(enableLogging)) Logger.Log("Scenehandler: ResetActiveScene");

            DisposeDirector();
            var newScene = CreateFreshScene();
            if (newScene == null)
                throw new InvalidOperationException($"Failed to create a new instance of scene type {GetActiveScene().GetType()}.");

            var gps = GameState.GamePlayState;

            // Submit highscore and save progress before the state is reset
            try { HighscoreService.SubmitFromGamePlay(gps); } catch { }
            try { GameStatePersistence.SaveGameState(); } catch { }

            bool hadCheckpoint = gps.HasCheckpoint;
            var snapshot = hadCheckpoint ? gps.CaptureCheckpointSnapshot() : default;

            // Capture cumulative stats before reset so they survive when there is no checkpoint
            long prevScore = gps.Score;
            int prevLives = gps.Lives;
            int prevKills = gps.TotalKills;
            int prevShots = gps.TotalShotsFired;
            int prevDeaths = gps.TotalDeaths;
            int prevPowerUps = gps.PowerUpsCollected;

            ClearVideoOverlay();
            gps.ResetForNewGame();
            ResetSurfaceState();

            scenes[currentSceneIndex] = newScene;
            GameState.ScreenOverlayState.HardHide();
            newScene.SetupGameOverlay();
            ApplySceneSettings(newScene);
            newScene.SetupScene((_3dWorld)world);
            ApplySceneSettings(newScene);

            if (hadCheckpoint)
            {
                int restoredMotherShips = ResolveRestoredMotherShipCount(snapshot.MotherShipsRemaining);

                TrimEnemies(world, "Seeder", snapshot.SeedersRemaining);
                TrimEnemies(world, "KamikazeDrone", snapshot.DronesRemaining);
                TrimEnemies(world, "MotherShipSmall", restoredMotherShips);
                TrimEnemies(world, "MotherShipMedium", restoredMotherShips);
                TrimEnemies(world, "MotherShipLarge", restoredMotherShips);
                gps.ApplyCheckpointRestart(snapshot);

                var aiObjs = GameState.SurfaceState.AiObjects;

                // If checkpoint was taken during mothership phase (all seeders/drones cleared),
                // restore mothership combat immediately after reset.
                if (snapshot.SeedersRemaining == 0 && snapshot.DronesRemaining == 0)
                {
                    for (int i = 0; i < aiObjs.Count; i++)
                    {
                        if ((aiObjs[i].ObjectName == "MotherShipSmall" || aiObjs[i].ObjectName == "MotherShipMedium" || aiObjs[i].ObjectName == "MotherShipLarge") && !aiObjs[i].IsActive)
                            aiObjs[i].IsActive = true;
                    }
                }

                // If decoy was unlocked at checkpoint time, restore active drone phase too.
                if (gps.IsDecoyUnlocked)
                {
                    for (int i = 0; i < aiObjs.Count; i++)
                    {
                        if (aiObjs[i].ObjectName == "KamikazeDrone" && !aiObjs[i].IsActive)
                            aiObjs[i].IsActive = true;
                    }
                }

                if (Logger.ShouldLog(enableLogging)) Logger.Log($"Scenehandler: Checkpoint restored. Score={gps.Score} Lives={gps.Lives} Kills={gps.TotalKills}");
            }
            else
            {
                // No checkpoint — preserve accumulated stats with death penalty
                gps.Score = prevScore;
                gps.Lives = Math.Max(0, prevLives - 1);
                gps.TotalKills = prevKills;
                gps.TotalShotsFired = prevShots;
                gps.TotalDeaths = prevDeaths + 1;
                gps.PowerUpsCollected = prevPowerUps;

                if (Logger.ShouldLog(enableLogging)) Logger.Log($"Scenehandler: No checkpoint — stats preserved. Score={gps.Score} Lives={gps.Lives} Kills={gps.TotalKills}");
            }

            InitializeDirector(newScene, world);
        }

        public void NextScene(I3dWorld world)
        {
            if (Logger.ShouldLog(enableLogging)) Logger.Log($"Scenehandler: NextScene :{GameState.ScreenOverlayState.ShowOverlay} ");

            DisposeDirector();
            var gps = GameState.GamePlayState;

            // Capture cumulative stats before reset
            long prevScore = gps.Score;
            int prevKills = gps.TotalKills;
            int prevShots = gps.TotalShotsFired;
            int prevDeaths = gps.TotalDeaths;
            int prevPowerUps = gps.PowerUpsCollected;

            // Submit highscore before the state is reset
            try
            {
                HighscoreService.SubmitFromGamePlay(gps);
            }
            catch { }

            var currentScene = GetActiveScene();
            bool isOutro = currentScene.SceneType == SceneTypes.Outro;
            bool isSimulation = currentScene.SceneType == SceneTypes.Simulation;

            if (isOutro || isSimulation)
            {
                // After Outro or completing a Simulation round, always go to Simulation
                gps.SimulationRound++;

                // Replace the simulation slot with a fresh instance for the new round
                int simIndex = scenes.FindIndex(s => s.SceneType == SceneTypes.Simulation);
                if (simIndex < 0) simIndex = scenes.Count - 1;
                scenes[simIndex] = new SceneSimulation();
                currentSceneIndex = simIndex;

                // Clear all objects from the previous scene (Outro ship, asteroids,
                // surface, landing pad, astronaut, fireworks, particles) so nothing
                // bleeds through into the new Simulation scene.
                world.WorldInhabitants.Clear();
                if (GameState.SurfaceState.AiObjects != null)
                    GameState.SurfaceState.AiObjects.Clear();

                ClearVideoOverlay();
                gps.ResetForNewGame();
                // Keep SimulationRound — it was incremented above and ResetForNewGame does not touch it
                gps.SceneIndex = currentSceneIndex;
                ResetSurfaceState();
                SetupActiveScene(world);

                // Carry forward score and stats into simulation
                gps.Score = prevScore;
                gps.TotalKills = prevKills;
                gps.TotalShotsFired = prevShots;
                gps.TotalDeaths = prevDeaths;
                gps.PowerUpsCollected = prevPowerUps;

                try { GameStatePersistence.SaveGameState(); } catch { }
                return;
            }

            currentSceneIndex = (currentSceneIndex + 1) % scenes.Count;

            // Game completed normally (wrapped to 0 or Outro index) — delete save
            if (currentSceneIndex == 0)
            {
                try { GameStatePersistence.DeleteSave(gps.PlayerName); } catch { }
            }

            ClearVideoOverlay();
            gps.ResetForNewGame();
            gps.SceneIndex = currentSceneIndex;
            ResetSurfaceState();
            SetupActiveScene(world);

            // Carry forward score, stats, and powerups into game and simulation scenes
            var nextScene = GetActiveScene();
            if (nextScene.SceneType == SceneTypes.Game || nextScene.SceneType == SceneTypes.Simulation)
            {
                gps.Score = prevScore;
                gps.TotalKills = prevKills;
                gps.TotalShotsFired = prevShots;
                gps.TotalDeaths = prevDeaths;
                gps.PowerUpsCollected = prevPowerUps;
            }

            // Persist the freshly-entered scene after ResetForNewGame so an old
            // scene checkpoint is not saved under the new scene index.
            if (currentSceneIndex != 0)
            {
                try { GameStatePersistence.SaveGameState(); } catch { }
            }
        }

        public void UpdateFrame(I3dWorld world)
        {
            if (!_pendingSceneAdvance)
                return;

            if (_pendingSceneAdvanceFramesLeft > 0)
            {
                _pendingSceneAdvanceFramesLeft--;
                return;
            }

            _pendingSceneAdvance = false;

            if (_pendingNextScene)
            {
                _pendingNextScene = false;
                NextScene(world);
                return;
            }

            if (_targetSceneIndex.HasValue)
            {
                currentSceneIndex = _targetSceneIndex.Value;
                _targetSceneIndex = null;
            }
            else
            {
                currentSceneIndex = (currentSceneIndex + 1) % scenes.Count;
            }

            GameState.GamePlayState.SceneIndex = currentSceneIndex;
            ResetSurfaceState();
            SetupActiveScene(world);

            // Restore score and combat stats from saved game so the player builds upon them
            if (_pendingSavedState != null)
            {
                var gps = GameState.GamePlayState;
                gps.Score = _pendingSavedState.Score;
                gps.SimulationRound = _pendingSavedState.SimulationRound;
                gps.CurrentSceneBiome = _pendingSavedState.SceneBiome;
                gps.TotalKills = _pendingSavedState.TotalKills;
                gps.TotalShotsFired = _pendingSavedState.TotalShotsFired;
                gps.TotalDeaths = _pendingSavedState.TotalDeaths;
                gps.PowerUpsCollected = _pendingSavedState.PowerUpsCollected;

                // If loading into the simulation slot, rebuild it for the restored round
                if (GetActiveScene().SceneType == SceneTypes.Simulation)
                {
                    int simIndex = scenes.FindIndex(s => s.SceneType == SceneTypes.Simulation);
                    if (simIndex >= 0)
                    {
                        scenes[simIndex] = new SceneSimulation();
                        ResetSurfaceState();
                        SetupActiveScene(world);
                    }
                }

                // If there's a checkpoint, trim enemies and restore full checkpoint state
                bool shouldRestoreCheckpoint =
                    _pendingSavedState.HasCheckpoint &&
                    SavedCheckpointMatchesActiveScene(_pendingSavedState);

                if (shouldRestoreCheckpoint)
                {
                    GameStatePersistence.RestoreToGamePlayState(_pendingSavedState);
                    ApplySceneSettings(GetActiveScene());

                    var snapshot = gps.CaptureCheckpointSnapshot();
                    ApplyCheckpointSnapshotToCurrentState(gps, snapshot);
                    int restoredMotherShips = ResolveRestoredMotherShipCount(snapshot.MotherShipsRemaining);

                    TrimEnemies(world, "Seeder", snapshot.SeedersRemaining);
                    TrimEnemies(world, "KamikazeDrone", snapshot.DronesRemaining);
                    TrimEnemies(world, "MotherShipSmall", restoredMotherShips);
                    TrimEnemies(world, "MotherShipMedium", restoredMotherShips);
                    TrimEnemies(world, "MotherShipLarge", restoredMotherShips);

                    // Activate enemies that should already be active at this checkpoint
                    if (snapshot.SeedersRemaining == 0 && snapshot.DronesRemaining == 0)
                    {
                        ActivateMotherShips(world);
                    }

                    // Re-initialize director so it sees the trimmed enemy state
                    var scene = GetActiveScene();
                    scene.Director?.Initialize(world.EventBus!, world);
                }
                else if (_pendingSavedState.HasCheckpoint)
                {
                    ClearCheckpointState(gps);
                }

                // Always apply decoy-driven drone activation when loading a saved game,
                // even when there is no checkpoint snapshot.
                if (gps.IsDecoyUnlocked)
                {
                    var aiObjs = GameState.SurfaceState.AiObjects;
                    for (int i = 0; i < aiObjs.Count; i++)
                    {
                        if (aiObjs[i].ObjectName == "KamikazeDrone" && !aiObjs[i].IsActive)
                            aiObjs[i].IsActive = true;
                    }
                }

                _pendingSavedState = null;
            }
        }

        // -----------------------------------------------------------------
        // Key handling
        // -----------------------------------------------------------------

        public void HandleKeyPress(KeyEventArgs k, I3dWorld world)
        {
            var scene = GetActiveScene();
            var overlay = GameState.ScreenOverlayState;

            if (overlay.Type == ScreenOverlayType.NameEntry && overlay.ShowOverlay)
            {
                HandleNameEntryKey(k, scene, overlay);
                return;
            }

            if (!overlay.ShowOverlay)
            {
                if (scene.SceneType == SceneTypes.Intro)
                    SkipLogoCube(world, scene);
                else if ((scene.SceneType == SceneTypes.Game || scene.SceneType == SceneTypes.Simulation) && k.Key == Key.X)
                    ReturnToIntro(world);
                return;
            }

            if (scene.SceneType == SceneTypes.Intro)
            {
                HandleIntroKey(overlay, k);
                return;
            }

            if (scene.SceneType == SceneTypes.Outro && overlay.Type == ScreenOverlayType.Outro)
            {
                // Page navigation with arrow keys; any other key deploys to the Simulation scene.
                if (overlay.HasMultiplePages)
                {
                    if (k.Key == Key.Right || k.Key == Key.D)
                    {
                        overlay.NextPage();
                        RefreshCurrentHighscorePage(overlay);
                        return;
                    }
                    if (k.Key == Key.Left || k.Key == Key.A)
                    {
                        overlay.PreviousPage();
                        RefreshCurrentHighscorePage(overlay);
                        return;
                    }
                }

                overlay.HardHide();
                _pendingNextScene = true;
                _pendingSceneAdvance = true;
                _pendingSceneAdvanceFramesLeft = SceneAdvanceDelayFrames;
                return;
            }

            if (scene.SceneType == SceneTypes.Game || scene.SceneType == SceneTypes.Simulation)
            {
                if (k.Key == Key.X)
                {
                    ReturnToIntro(world);
                    return;
                }
                HandleGameKey(k, scene, overlay);
            }
        }

        private void HandleNameEntryKey(KeyEventArgs k, IScene scene, ScreenOverlayState overlay)
        {
            if (k.Key == Key.Escape)
            {
                overlay.HardHide();
                scene.SetupSceneOverlay();
                return;
            }

            if (k.Key == Key.Return || k.Key == Key.Enter)
            {
                var name = overlay.NameEntryBuffer.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    overlay.NameEntryValidationMessage = ">> CALLSIGN CANNOT BE EMPTY";
                    return;
                }

                var priorName = PersistenceSetup.LoadLastPlayerName();
                bool isOwnName = string.Equals(name, priorName, StringComparison.OrdinalIgnoreCase)
                              || PersistenceSetup.HasPlayerSaveFile(name);

                if (!isOwnName)
                {
                    var highscores = HighscoreService.LoadLocalHighscores();
                    bool taken = highscores.Entries.Exists(e =>
                        string.Equals(e.PlayerName, name, StringComparison.OrdinalIgnoreCase));
                    if (taken)
                    {
                        overlay.NameEntryValidationMessage = ">> CALLSIGN ALREADY IN USE - CHOOSE ANOTHER";
                        return;
                    }
                }

                overlay.IsNameConfirmed = true;
                GameState.GamePlayState.PlayerName = name;
                PersistenceSetup.SaveLastPlayerName(name);

                // Check for saved scene progress for this player
                var saved = GameStatePersistence.LoadGameState(name);
                if (saved != null)
                {
                    // Always restore score and stats so the player builds upon them
                    _pendingSavedState = saved;

                    // Skip to saved scene only if it's a playable scene ahead of current
                    if (CanTargetSavedScene(saved))
                    {
                        _targetSceneIndex = saved.SceneIndex;
                    }
                }

                overlay.HardHide();
                _pendingSceneAdvance = true;
                _pendingSceneAdvanceFramesLeft = SceneAdvanceDelayFrames;
                return;
            }

            overlay.ProcessNameEntryKey(k.Key);
        }

        private void HandleIntroKey(ScreenOverlayState overlay, KeyEventArgs k)
        {
            if (Logger.ShouldLog(enableLogging)) Logger.Log($"Scenehandler: Keypress during Intro ShowOverlay: {overlay.ShowOverlay} ", "General");

            // Page navigation with arrow keys
            if (overlay.HasMultiplePages)
            {
                if (k.Key == Key.Right || k.Key == Key.D)
                {
                    overlay.NextPage();
                    RefreshCurrentHighscorePage(overlay);
                    return;
                }
                if (k.Key == Key.Left || k.Key == Key.A)
                {
                    overlay.PreviousPage();
                    RefreshCurrentHighscorePage(overlay);
                    return;
                }
            }

            overlay.HardHide();
            ClearVideoOverlay();

            var lastPlayer = PersistenceSetup.LoadLastPlayerName();
            overlay.SetNameEntryPreset(lastPlayer);
        }

        private static void HandleGameKey(KeyEventArgs k, IScene scene, ScreenOverlayState overlay)
        {
            if (overlay.Type == ScreenOverlayType.Intro && overlay.ShowOverlay)
            {
                // Page navigation with arrow keys
                if (overlay.HasMultiplePages)
                {
                    if (k.Key == Key.Right || k.Key == Key.D)
                    {
                        overlay.NextPage();
                        RefreshCurrentHighscorePage(overlay);
                        return;
                    }
                    if (k.Key == Key.Left || k.Key == Key.A)
                    {
                        overlay.PreviousPage();
                        RefreshCurrentHighscorePage(overlay);
                        return;
                    }
                }

                if (Logger.ShouldLog(enableLogging)) Logger.Log($"Scenehandler: Game keypress. Overlay Type={overlay.Type} Show={overlay.ShowOverlay}", "General");
                scene.SetupGameOverlay();
            }
        }

        private static void RefreshCurrentHighscorePage(ScreenOverlayState overlay)
        {
            HighscoreOverlayFormatter.RefreshCurrentPageIfHighscorePage(overlay);
        }

        // -----------------------------------------------------------------
        // Shared helpers
        // -----------------------------------------------------------------

        private static void SkipLogoCube(I3dWorld world, IScene scene)
        {
            var inhabitants = world.WorldInhabitants;
            for (int i = inhabitants.Count - 1; i >= 0; i--)
            {
                if (inhabitants[i].ObjectName == "LogoCube")
                {
                    inhabitants.RemoveAt(i);
                    break;
                }
            }

            GameState.ScreenOverlayState.ShowOverlay = true;
            scene.SetupVideoOverlay("introclip.mp4");
        }

        private bool CanTargetSavedScene(SavedGameState saved)
        {
            if (saved.SceneIndex <= currentSceneIndex + 1 || saved.SceneIndex >= scenes.Count)
                return false;

            var sceneType = scenes[saved.SceneIndex].SceneType;
            return sceneType == SceneTypes.Game || sceneType == SceneTypes.Simulation;
        }

        private IScene? CreateFreshScene() =>
            (IScene?)Activator.CreateInstance(GetActiveScene().GetType());

        private void ReturnToIntro(I3dWorld world)
        {
            DisposeDirector();
            var gps = GameState.GamePlayState;
            try { HighscoreService.SubmitFromGamePlay(gps); } catch { }
            try { GameStatePersistence.SaveGameState(); } catch { }

            // Clear all game objects so nothing from the current scene bleeds through
            world.WorldInhabitants.Clear();
            if (GameState.SurfaceState.AiObjects != null)
                GameState.SurfaceState.AiObjects.Clear();

            currentSceneIndex = 0;
            ClearVideoOverlay();
            gps.ResetForNewGame();
            gps.SceneIndex = 0;
            ResetSurfaceState();

            // Replace the intro scene instance so SkipLogoCube is set before SetupScene runs
            var introScene = new Intro { SkipLogoCube = true };
            scenes[0] = introScene;

            introScene.SetupSceneOverlay();
            ApplySceneSettings(introScene);
            introScene.SetupScene((_3dWorld)world);
        }

        private static void ClearVideoOverlay()
        {
            GameState.ScreenOverlayState.ShowVideoOverlay = false;
            GameState.ScreenOverlayState.VideoClipPath = string.Empty;
        }

        private static void ResetSurfaceState()
        {
            GameState.SurfaceState.GlobalMapBitmap = null;
            GameState.SurfaceState.SurfaceViewportObject = null;
            GameState.SurfaceState.GlobalMapPosition = new Vector3
            {
                x = SurfaceSetup.DefaultMapPosition.x,
                y = SurfaceSetup.DefaultMapPosition.y,
                z = SurfaceSetup.DefaultMapPosition.z
            };
            GameState.SurfaceState.ScreenEcoMetas = new ScreenEcoMeta[MapSetup.screensPrMap, MapSetup.screensPrMap];
            GameState.SurfaceState.SceneBiome = SceneBiomeTypes.HillsWoods;
            GameState.WeatherVisualState.ClearLightningFlash();
        }

        private static void ApplySceneSettings(IScene scene)
        {
            var gps = GameState.GamePlayState;
            gps.InfectionCriticalMass = scene.InfectionThresholdPercent;
            gps.InfectionSpreadRate = scene.InfectionSpreadRate;
            gps.SeederOffscreenSpeedFactor = scene.SeederOffscreenSpeedFactor;
            gps.LocalInfectionSpreadDelaySec = scene.LocalInfectionSpreadDelaySec;
            gps.LocalInfectionSpreadRadius = scene.LocalInfectionSpreadRadius;
            gps.MotherShipSmallAggression = scene.MotherShipSmallAggression;
            gps.MotherShipMediumAggression = scene.MotherShipMediumAggression;
            gps.MotherShipLargeAggression = scene.MotherShipLargeAggression;
            gps.CurrentSceneBiome = scene.SceneBiome;
            GameState.SurfaceState.SceneBiome = scene.SceneBiome;
        }

        private void InitializeDirector(IScene scene, I3dWorld world)
        {
            scene.Director?.Initialize(world.EventBus!, world);
        }

        private void DisposeDirector()
        {
            GetActiveScene().Director?.Dispose();
        }

        private void ApplySceneIndexOverrideFromGameState()
        {
            var gamePlayState = GameState.GamePlayState;
            if (gamePlayState == null || !CanUseSceneIndexAsStartupOverride(gamePlayState))
                return;

            var requestedSceneIndex = gamePlayState.SceneIndex;
            if (requestedSceneIndex < 0 || requestedSceneIndex >= scenes.Count)
                return;

            currentSceneIndex = requestedSceneIndex;
        }

        private static bool CanUseSceneIndexAsStartupOverride(GamePlayState gamePlayState)
        {
            return string.IsNullOrWhiteSpace(gamePlayState.PlayerName)
                && gamePlayState.Score == 0
                && gamePlayState.TotalKills == 0
                && gamePlayState.TotalShotsFired == 0
                && gamePlayState.TotalDeaths == 0
                && gamePlayState.PowerUpsCollected == 0
                && !gamePlayState.HasCheckpoint;
        }

        // -----------------------------------------------------------------
        // Enemy trimming (checkpoint restore)
        // -----------------------------------------------------------------

        /// <summary>
        /// Removes excess enemies of a given type so the count matches the checkpoint
        /// remaining count. Removes non-powerup enemies first.
        /// Drones and MotherShips are only counted when active (matching UpdateHudState).
        /// </summary>
        private static void TrimEnemies(I3dWorld world, string enemyName, int targetRemaining)
        {
            var inhabitants = world.WorldInhabitants;

            int current = 0;
            for (int i = 0; i < inhabitants.Count; i++)
            {
                if (inhabitants[i].ObjectName == enemyName)
                    current++;
            }

            int toRemove = current - targetRemaining;
            if (toRemove <= 0) return;

            // First pass: non-powerup enemies (from end)
            for (int i = inhabitants.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                var obj = inhabitants[i];
                if (obj.ObjectName == enemyName && !obj.HasPowerUp)
                {
                    RemoveEnemyAt(world, i, obj.ObjectId);
                    toRemove--;
                }
            }

            // Second pass: powerup enemies if still needed
            for (int i = inhabitants.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                var obj = inhabitants[i];
                if (obj.ObjectName == enemyName && obj.HasPowerUp)
                {
                    RemoveEnemyAt(world, i, obj.ObjectId);
                    toRemove--;
                }
            }
        }

        private static void RemoveEnemyAt(I3dWorld world, int inhabitantIndex, int objectId)
        {
            world.WorldInhabitants.RemoveAt(inhabitantIndex);

            var aiObjects = GameState.SurfaceState.AiObjects;
            for (int j = aiObjects.Count - 1; j >= 0; j--)
            {
                if (aiObjects[j].ObjectId == objectId)
                {
                    aiObjects.RemoveAt(j);
                    break;
                }
            }
        }

        private static bool SavedCheckpointMatchesActiveScene(SavedGameState saved)
        {
            int sceneSeeders = CountSceneAi("Seeder");
            int sceneDrones = CountSceneAi("KamikazeDrone");
            int sceneMotherShips = CountSceneMotherShips();

            if (!CheckpointInitialMatchesScene(saved.CheckpointInitialSeeders, sceneSeeders))
                return false;
            if (!CheckpointInitialMatchesScene(saved.CheckpointInitialDrones, sceneDrones))
                return false;
            if (!CheckpointInitialMatchesScene(saved.CheckpointInitialMotherShips, sceneMotherShips))
                return false;

            return true;
        }

        private static bool CheckpointInitialMatchesScene(int checkpointInitial, int sceneInitial)
        {
            return checkpointInitial <= 0 || sceneInitial <= 0 || checkpointInitial == sceneInitial;
        }

        private static int CountSceneAi(string objectName)
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null) return 0;

            int count = 0;
            for (int i = 0; i < aiObjects.Count; i++)
            {
                if (aiObjects[i].ObjectName == objectName)
                    count++;
            }

            return count;
        }

        private static int ResolveRestoredMotherShipCount(int checkpointMotherShipsRemaining)
        {
            int sceneMotherShips = CountSceneMotherShips();
            if (sceneMotherShips <= 0)
                return 0;

            if (checkpointMotherShipsRemaining > 0)
                return Math.Min(checkpointMotherShipsRemaining, sceneMotherShips);

            // Keep scene mothership candidates even when old checkpoint saves have 0 remaining,
            // otherwise late-phase activation can never happen.
            return sceneMotherShips;
        }

        private static int CountSceneMotherShips()
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null) return 0;

            int count = 0;
            for (int i = 0; i < aiObjects.Count; i++)
            {
                var objectName = aiObjects[i].ObjectName;
                if (objectName == "MotherShipSmall" ||
                    objectName == "MotherShipMedium" ||
                    objectName == "MotherShipLarge")
                    count++;
            }

            return count;
        }

        private static void ActivateMotherShips(I3dWorld world)
        {
            for (int i = 0; i < world.WorldInhabitants.Count; i++)
            {
                if (IsMotherShipName(world.WorldInhabitants[i].ObjectName))
                    world.WorldInhabitants[i].IsActive = true;
            }

            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null)
                return;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                if (IsMotherShipName(aiObjects[i].ObjectName))
                    aiObjects[i].IsActive = true;
            }
        }

        private static bool IsMotherShipName(string? objectName)
        {
            return objectName == "MotherShipSmall" ||
                   objectName == "MotherShipMedium" ||
                   objectName == "MotherShipLarge";
        }

        private static void ClearCheckpointState(GamePlayState gps)
        {
            gps.HasCheckpoint = false;
            gps.CheckpointScore = 0;
            gps.CheckpointLives = 3;
            gps.CheckpointHealth = 100f;
            gps.CheckpointPowerUpsCollected = 0;
            gps.CheckpointSeedersRemaining = 0;
            gps.CheckpointDronesRemaining = 0;
            gps.CheckpointMotherShipsRemaining = 0;
            gps.CheckpointTotalShotsFired = 0;
            gps.CheckpointTotalKills = 0;
            gps.CheckpointTotalDeaths = 0;
            gps.CheckpointInfectionLevel = 0f;
            gps.CheckpointWaveNumber = 1;
            gps.CheckpointInitialSeeders = 0;
            gps.CheckpointInitialDrones = 0;
            gps.CheckpointInitialMotherShips = 0;
        }

        private static void ApplyCheckpointSnapshotToCurrentState(
            GamePlayState gps,
            GamePlayState.CheckpointSnapshot snapshot)
        {
            gps.Score = snapshot.Score;
            gps.Lives = snapshot.Lives;
            gps.Health = snapshot.Health;
            gps.PowerUpsCollected = snapshot.PowerUpsCollected;
            gps.SeedersRemaining = snapshot.SeedersRemaining;
            gps.DronesRemaining = snapshot.DronesRemaining;
            gps.MotherShipsRemaining = snapshot.MotherShipsRemaining;
            gps.TotalShotsFired = snapshot.TotalShotsFired;
            gps.TotalKills = snapshot.TotalKills;
            gps.TotalDeaths = snapshot.TotalDeaths;
            gps.InfectionLevel = snapshot.InfectionLevel;
            gps.WaveNumber = snapshot.WaveNumber;
            gps.InitialSeeders = snapshot.InitialSeeders;
            gps.InitialDrones = snapshot.InitialDrones;
            gps.InitialMotherShips = snapshot.InitialMotherShips;
        }

        // -----------------------------------------------------------------
        // Validation (DEBUG only)
        // -----------------------------------------------------------------

        [Conditional("DEBUG")]
        private static void ValidateGameSceneSetup(IScene scene, I3dWorld world)
        {
            if (scene.SceneType != SceneTypes.Game) return;

            var inhabitants = world.WorldInhabitants;
            var ship = inhabitants.FirstOrDefault(o => o.ObjectName == "Ship");

            Debug.Assert(ship != null,
                $"[{scene.GetType().Name}] No Ship found in WorldInhabitants.");
            Debug.Assert(ship?.WeaponSystems != null,
                $"[{scene.GetType().Name}] Ship.WeaponSystems is null — weapons not assigned.");
            Debug.Assert(ship?.ImpactStatus?.ObjectHealth > 0,
                $"[{scene.GetType().Name}] Ship.ImpactStatus.ObjectHealth is 0 — ship has no health.");
            Debug.Assert(GameState.SurfaceState.SurfaceViewportObject != null,
                $"[{scene.GetType().Name}] SurfaceViewportObject is null — surface not stored in GameState.");
            Debug.Assert(GameState.SurfaceState.AiObjects.Count > 0,
                $"[{scene.GetType().Name}] No AI objects — scene has no enemies.");
            Debug.Assert(inhabitants.Any(o => o.ObjectName == "SeederGuidanceArrow"),
                $"[{scene.GetType().Name}] No SeederGuidanceArrow — guidance arrow missing.");
        }
    }
}
