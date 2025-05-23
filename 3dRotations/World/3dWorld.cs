﻿using _3dRotations.World.Objects;
using _3DWorld.Scene;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Media3D;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting._3dWorld
{
    //This class will contain all the objects in the world and the world itself
    public class _3dWorld
    {
        //Global class to hold all the global variables and methods
        public List<_3dObject> WorldInhabitants = new();
        //Todo setup the map
        public _3dWorld()
        {
            var WorldSetup = new Setup(this);
            Debug.WriteLine(WorldInhabitants.Count);
        }
    }

}
