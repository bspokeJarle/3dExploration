using _3dTesting._3dWorld.Scene;
using _3dTesting._Coordinates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Media3D;

namespace _3dTesting._3dWorld
{
    //This class will contain all the objects in the world and the world itself
    public class _3dWorld
    {
        public List<_3dObject> WorldInhabitants = new();
        //Todo setup the map
        public _3dWorld()
        {
            var WorldSetup = new Setup(this);
            Debug.WriteLine(WorldInhabitants.Count);
        }
    }

}
