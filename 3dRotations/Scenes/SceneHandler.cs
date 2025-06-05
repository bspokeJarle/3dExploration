using _3dRotations.Scene.Scene1;
using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _3DWorld.Scene
{
    public class SceneHandler : ISceneHandler
    {
        // List of the available scenes for the game
        private List<IScene> scenes = new List<IScene> { new Scene1(), new Scene2() };
        private int currentSceneIndex = 0;

        public void SetupActiveScene(I3dWorld world)
        {
            // Setup the active scene
            var scene = scenes[currentSceneIndex];
            scene.SetupScene((_3dWorld)world);
        }

        public void ResetActiveScene(I3dWorld world)
        {
            var oldScene = scenes[currentSceneIndex];
            var newScene = (IScene?)Activator.CreateInstance(oldScene.GetType());

            // Ensure newScene is not null before assignment
            if (newScene != null)
            {
                scenes[currentSceneIndex] = newScene;
                newScene.SetupScene((_3dWorld)world);
            }
            else
            {
                throw new InvalidOperationException($"Failed to create a new instance of scene type {oldScene.GetType()}.");
            }
        }

        public void NextScene(I3dWorld world)
        {
            // Increment the scene index and wrap around if necessary
            currentSceneIndex = (currentSceneIndex + 1) % scenes.Count;
            SetupActiveScene(world);
        }
    }
}
