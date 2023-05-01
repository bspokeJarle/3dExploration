using STL_Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace _3dTesting._3dWorld.Scene
{
    public class Setup
    {
        //Setup the scene to put into the world
        public Setup(_3dWorld world) { 
            //todo the ability to setup multiple scenes/levels/planets etc, for now just one scene

            //Add orb as an inhabitant
            var orb = new _3dObject();
            var modelReader = new STLReader("C:\\Users\\kh979\\Documents\\Privat\\Bspoke prosjekter\\3dProsjekt\\3dProsjekt\\3dTesting\\3d objects\\div\\complexorb.stl");
            orb.Triangles = modelReader.ReadFile().ToList();
            orb.Position = new Vector3 { x = -200, y = 0, z = 0 };
            orb.Rotation = new Vector3 { x = 0, y = -180, z = -180 };
            world.WorldInhabitants.Add(orb);

            //Add cube as an inhabitant
            var cube = new _3dObject();
            modelReader = new STLReader("C:\\Users\\kh979\\Documents\\Privat\\Bspoke prosjekter\\3dProsjekt\\3dProsjekt\\3dTesting\\3d objects\\div\\3dcube.stl");
            cube.Triangles = modelReader.ReadFile().ToList();
            cube.Position = new Vector3 { x = 400, y = -0, z = -0 };
            cube.Rotation = new Vector3 { x = 0, y = -180, z = -180 };
            world.WorldInhabitants.Add(cube);

        }
    }
}
