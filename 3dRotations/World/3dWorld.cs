using _3DWorld.Scene;
using Domain;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting._3dWorld
{
    //This class will contain all the objects in the world and the world itself
    public class _3dWorld : I3dWorld
    {
        //Global class to hold all the global variables and methods
        public List<I3dObject> WorldInhabitants { get; set; } = new List<I3dObject>();
        //SceneHandler to handle the scenes in the game
        public ISceneHandler SceneHandler { get; set; } = new SceneHandler();

        public _3dWorld()
        {
            //Initialize the world with Scene1 (should be Intro later)
            SceneHandler.SetupActiveScene(this);
        }
    }

}
