using _3dRotations.Scene.Scene1;
using _3dRotations.Scenes.Intro;
using _3dRotations.Scenes.Outro;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
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
        // List of the available scenes for the game
        private List<IScene> scenes = new List<IScene> { new Intro(), new Scene1(), new Scene2(), new Outro() };
        private int currentSceneIndex = 1;
        private const bool enableLogging = false;
        private const int SceneAdvanceDelayFrames = 5;
        private bool _pendingSceneAdvance = false;
        private int _pendingSceneAdvanceFramesLeft = 0;

        public IScene GetActiveScene()
        {
            return scenes[currentSceneIndex];
        }

        public void SetupActiveScene(I3dWorld world)
        {
            // Setup the active scene (overlay + world objects)
            var scene = GetActiveScene();
            scene.SetupSceneOverlay();
            GameState.ScreenOverlayState.ShowVideoOverlay = false;
            GameState.ScreenOverlayState.VideoClipPath = string.Empty;
            scene.SetupScene((_3dWorld)world);
            GameState.GamePlayState.InfectionCriticalMass = scene.InfectionThresholdPercent;
            GameState.GamePlayState.InfectionSpreadRate = scene.InfectionSpreadRate;
            GameState.GamePlayState.SeederOffscreenSpeedFactor = scene.SeederOffscreenSpeedFactor;
            GameState.GamePlayState.LocalInfectionSpreadDelaySec = scene.LocalInfectionSpreadDelaySec;
            GameState.GamePlayState.LocalInfectionSpreadRadius = scene.LocalInfectionSpreadRadius;

            ValidateGameSceneSetup(scene, world);
        }

        public void ResetActiveScene(I3dWorld world)
        {
            if (enableLogging) Logger.Log("Scenehandler: ResetActiveScene");
            var oldScene = GetActiveScene();
            var newScene = (IScene?)Activator.CreateInstance(oldScene.GetType());

            if (newScene != null)
            {
                GameState.ScreenOverlayState.ShowVideoOverlay = false;
                GameState.ScreenOverlayState.VideoClipPath = string.Empty;
                GameState.GamePlayState.ResetForNewGame();
                GameState.SurfaceState.GlobalMapBitmap = null;
                GameState.SurfaceState.SurfaceViewportObject = null;
                GameState.SurfaceState.GlobalMapPosition = new _3dSpecificsImplementations.Vector3
                {
                    x = SurfaceSetup.DefaultMapPosition.x,
                    y = SurfaceSetup.DefaultMapPosition.y,
                    z = SurfaceSetup.DefaultMapPosition.z
                };
                GameState.SurfaceState.ScreenEcoMetas = new ScreenEcoMeta[MapSetup.screensPrMap, MapSetup.screensPrMap];
                scenes[currentSceneIndex] = newScene;
                GameState.ScreenOverlayState.HardHide();
                newScene.SetupGameOverlay();

                newScene.SetupScene((_3dWorld)world);
                GameState.GamePlayState.InfectionCriticalMass = newScene.InfectionThresholdPercent;
                GameState.GamePlayState.InfectionSpreadRate = newScene.InfectionSpreadRate;
                GameState.GamePlayState.SeederOffscreenSpeedFactor = newScene.SeederOffscreenSpeedFactor;
                GameState.GamePlayState.LocalInfectionSpreadDelaySec = newScene.LocalInfectionSpreadDelaySec;
                GameState.GamePlayState.LocalInfectionSpreadRadius = newScene.LocalInfectionSpreadRadius;
            }
            else
            {
                throw new InvalidOperationException($"Failed to create a new instance of scene type {oldScene.GetType()}.");
            }
        }

        public void HandleKeyPress(KeyEventArgs k, I3dWorld world)
        {
            var scene = GetActiveScene();
            if (GameState.ScreenOverlayState.ShowOverlay == false) return;

            // Intro scene: any key continues
            if (scene.SceneType == SceneTypes.Intro)
            {
                if (enableLogging) Logger.Log($"Scenehandler: Keypress during Intro ShowOverlay: {GameState.ScreenOverlayState.ShowOverlay} ", "General");

                GameState.ScreenOverlayState.HardHide();
                GameState.ScreenOverlayState.ShowVideoOverlay = false;
                GameState.ScreenOverlayState.VideoClipPath = string.Empty;
                _pendingSceneAdvance = true;
                _pendingSceneAdvanceFramesLeft = SceneAdvanceDelayFrames;
                return;
            }

            // Game scene
            if (scene.SceneType == SceneTypes.Game)
            {
                // Only react if the scene intro overlay is actually showing
                if (GameState.ScreenOverlayState.Type == ScreenOverlayType.Intro &&
                    GameState.ScreenOverlayState.ShowOverlay)
                {
                    if (enableLogging) Logger.Log($"Scenehandler: Game keypress. Overlay Type={GameState.ScreenOverlayState.Type} Show={GameState.ScreenOverlayState.ShowOverlay}", "General");
                    scene.SetupGameOverlay(); // SetupGameOverlay must set Type=Game and ShowOverlay=false
                }
                // Otherwise: let the rest of the game handle the key (do nothing here)
            }
        }

        public void NextScene(I3dWorld world)
        {
            if (enableLogging) Logger.Log($"Scenehandler: NextScene :{GameState.ScreenOverlayState.ShowOverlay} ");
            // Increment the scene index and wrap around if necessary
            currentSceneIndex = (currentSceneIndex + 1) % scenes.Count;
            GameState.ScreenOverlayState.ShowVideoOverlay = false;
            GameState.ScreenOverlayState.VideoClipPath = string.Empty;
            GameState.GamePlayState.ResetForNewGame();
            GameState.SurfaceState.GlobalMapBitmap = null;
            GameState.SurfaceState.SurfaceViewportObject = null;
            GameState.SurfaceState.GlobalMapPosition = new _3dSpecificsImplementations.Vector3
            {
                x = SurfaceSetup.DefaultMapPosition.x,
                y = SurfaceSetup.DefaultMapPosition.y,
                z = SurfaceSetup.DefaultMapPosition.z
            };
            GameState.SurfaceState.ScreenEcoMetas = new ScreenEcoMeta[MapSetup.screensPrMap, MapSetup.screensPrMap];
            SetupActiveScene(world);
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
            currentSceneIndex = (currentSceneIndex + 1) % scenes.Count;
            SetupActiveScene(world);
        }

        /// <summary>
        /// Validates that a Game-type scene has set up all critical state.
        /// Runs after SetupScene so missing fields are caught during development.
        /// </summary>
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