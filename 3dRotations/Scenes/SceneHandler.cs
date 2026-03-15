using _3dRotations.Scene.Scene1;
using _3dRotations.Scenes.Intro;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace _3DWorld.Scene
{
    public class SceneHandler : ISceneHandler
    {
        // List of the available scenes for the game
        private List<IScene> scenes = new List<IScene> { new Intro(), new Scene1(), new Scene2() };
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
                scenes[currentSceneIndex] = newScene;
                GameState.ScreenOverlayState.HardHide();
                newScene.SetupGameOverlay();
                
                newScene.SetupScene((_3dWorld)world);
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
    }
}