using _3dRotations.Scene.Scene1;
using _3dRotations.Scenes.Intro;
using _3dRotations.Scenes.Outro;
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
        private List<IScene> scenes = new List<IScene> { new Intro(), new Scene1(), new Scene2(), new Outro() };
        private int currentSceneIndex = 0;
        private const bool enableLogging = false;
        private const int SceneAdvanceDelayFrames = 5;
        private bool _pendingSceneAdvance = false;
        private int _pendingSceneAdvanceFramesLeft = 0;
        private int? _targetSceneIndex = null;
        private SavedGameState? _pendingSavedState = null;

        public IScene GetActiveScene() => scenes[currentSceneIndex];

        // -----------------------------------------------------------------
        // Scene lifecycle
        // -----------------------------------------------------------------

        public void SetupActiveScene(I3dWorld world)
        {
            var scene = GetActiveScene();
            scene.SetupSceneOverlay();
            ClearVideoOverlay();
            scene.SetupScene((_3dWorld)world);
            ApplySceneSettings(scene);
            InitializeDirector(scene, world);
            ValidateGameSceneSetup(scene, world);
        }

        public void ResetActiveScene(I3dWorld world)
        {
            if (enableLogging) Logger.Log("Scenehandler: ResetActiveScene");

            DisposeDirector();
            var newScene = CreateFreshScene();
            if (newScene == null)
                throw new InvalidOperationException($"Failed to create a new instance of scene type {GetActiveScene().GetType()}.");

            var gps = GameState.GamePlayState;
            bool hadCheckpoint = gps.HasCheckpoint;
            var snapshot = hadCheckpoint ? gps.CaptureCheckpointSnapshot() : default;

            ClearVideoOverlay();
            gps.ResetForNewGame();
            ResetSurfaceState();

            scenes[currentSceneIndex] = newScene;
            GameState.ScreenOverlayState.HardHide();
            newScene.SetupGameOverlay();
            newScene.SetupScene((_3dWorld)world);
            ApplySceneSettings(newScene);

            if (hadCheckpoint)
            {
                TrimEnemies(world, "Seeder", snapshot.SeedersRemaining);
                TrimEnemies(world, "KamikazeDrone", snapshot.DronesRemaining);
                TrimEnemies(world, "MotherShipSmall", snapshot.MotherShipsRemaining);
                gps.ApplyCheckpointRestart(snapshot);

                if (enableLogging) Logger.Log($"Scenehandler: Checkpoint restored. Score={gps.Score} Lives={gps.Lives} Kills={gps.TotalKills}");
            }
        }

        public void NextScene(I3dWorld world)
        {
            if (enableLogging) Logger.Log($"Scenehandler: NextScene :{GameState.ScreenOverlayState.ShowOverlay} ");

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

            currentSceneIndex = (currentSceneIndex + 1) % scenes.Count;

            // Game completed — delete save so the next playthrough starts fresh
            if (currentSceneIndex == 0)
            {
                try { GameStatePersistence.DeleteSave(gps.PlayerName); } catch { }
            }

            // Persist scene progress so the player can resume from here
            gps.SceneIndex = currentSceneIndex;
            try { GameStatePersistence.SaveGameState(); } catch { }

            ClearVideoOverlay();
            gps.ResetForNewGame();
            ResetSurfaceState();
            SetupActiveScene(world);

            // Carry forward score, stats, and powerups into game scenes
            var nextScene = GetActiveScene();
            if (nextScene.SceneType == SceneTypes.Game)
            {
                gps.Score = prevScore;
                gps.TotalKills = prevKills;
                gps.TotalShotsFired = prevShots;
                gps.TotalDeaths = prevDeaths;
                gps.PowerUpsCollected = prevPowerUps;
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
            SetupActiveScene(world);

            // Restore score and combat stats from saved game so the player builds upon them
            if (_pendingSavedState != null)
            {
                var gps = GameState.GamePlayState;
                gps.Score = _pendingSavedState.Score;
                gps.TotalKills = _pendingSavedState.TotalKills;
                gps.TotalShotsFired = _pendingSavedState.TotalShotsFired;
                gps.TotalDeaths = _pendingSavedState.TotalDeaths;
                gps.PowerUpsCollected = _pendingSavedState.PowerUpsCollected;
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
                return;
            }

            if (scene.SceneType == SceneTypes.Intro)
            {
                HandleIntroKey(overlay, k);
                return;
            }

            if (scene.SceneType == SceneTypes.Game)
                HandleGameKey(k, scene, overlay);
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
                        overlay.NameEntryValidationMessage = ">> CALLSIGN ALREADY IN USE — CHOOSE ANOTHER";
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

                    // Skip to saved scene only if it's a Game scene ahead of current
                    if (saved.SceneIndex > currentSceneIndex + 1 &&
                        saved.SceneIndex < scenes.Count &&
                        scenes[saved.SceneIndex].SceneType == SceneTypes.Game)
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
            if (enableLogging) Logger.Log($"Scenehandler: Keypress during Intro ShowOverlay: {overlay.ShowOverlay} ", "General");

            // Page navigation with arrow keys
            if (overlay.HasMultiplePages)
            {
                if (k.Key == Key.Right || k.Key == Key.D)
                {
                    overlay.NextPage();
                    return;
                }
                if (k.Key == Key.Left || k.Key == Key.A)
                {
                    overlay.PreviousPage();
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
                        return;
                    }
                    if (k.Key == Key.Left || k.Key == Key.A)
                    {
                        overlay.PreviousPage();
                        return;
                    }
                }

                if (enableLogging) Logger.Log($"Scenehandler: Game keypress. Overlay Type={overlay.Type} Show={overlay.ShowOverlay}", "General");
                scene.SetupGameOverlay();
            }
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

        private IScene? CreateFreshScene() =>
            (IScene?)Activator.CreateInstance(GetActiveScene().GetType());

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
        }

        private static void ApplySceneSettings(IScene scene)
        {
            var gps = GameState.GamePlayState;
            gps.InfectionCriticalMass = scene.InfectionThresholdPercent;
            gps.InfectionSpreadRate = scene.InfectionSpreadRate;
            gps.SeederOffscreenSpeedFactor = scene.SeederOffscreenSpeedFactor;
            gps.LocalInfectionSpreadDelaySec = scene.LocalInfectionSpreadDelaySec;
            gps.LocalInfectionSpreadRadius = scene.LocalInfectionSpreadRadius;
        }

        private void InitializeDirector(IScene scene, I3dWorld world)
        {
            scene.Director?.Initialize(world.EventBus!, world);
        }

        private void DisposeDirector()
        {
            GetActiveScene().Director?.Dispose();
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