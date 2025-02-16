
using _3dTesting._3dWorld;
using _3dRotations.World.Objects;
using static Domain._3dSpecificsImplementations;
using GameAiAndControls.Controls;

namespace _3dRotations.Scene.Scene1
{
    public class Scene1
    {
        Surface Surface = new();
        public void SetupScene1(_3dWorld world)
        {            
            //Add ship as first inhabitant
            var ship = Ship.CreateShip(Surface);
            //Test if this makes the ship tilt and rotate like it should
            ship.RotationOffsetY = 65;
            ship.Rotation = new Vector3 { };
            ship.Position = new Vector3 { };
            ship.ObjectName = "Ship";
            world.WorldInhabitants.Add(ship);

            //Add ship as first inhabitant
            var seeder = Seeder.CreateSeeder(Surface);
            //Initialize the seeder rotation
            seeder.Rotation = new Vector3 { };
            seeder.Position = new Vector3 { x = -150, y = 100, z = -500 };
            seeder.ObjectName = "Seeder";
            seeder.Movement = new SeederControls();            
            world.WorldInhabitants.Add(seeder);

            //Generate 2D map for the surface
            Surface.Create2DMap();
            //Get the surface viewport based on the global Map Position
            var surfaceObject = (_3dObject)Surface.GetSurfaceViewPort();
            surfaceObject.ObjectName = "Surface";
            //This position and rotation is for the onscreen object, not the map position
            surfaceObject.Position = new Vector3 { x = 0, y = 250, z = 300 };
            surfaceObject.Rotation = new Vector3 { x = 81, y = 0, z = 0 };                   
            //Add crashboxes to the surface, add more crashboxes to the surface for large objects (Mountains etc)
            surfaceObject.CrashBoxes = new System.Collections.Generic.List<System.Collections.Generic.List<Domain.IVector3>> { new System.Collections.Generic.List<Domain.IVector3> { new Vector3 { x = -625, y = 250, z = -625 }, new Vector3 { x = 625, y = 550, z = 625 } } };
            surfaceObject.Movement = new GroundControls();
            surfaceObject.ParentSurface = Surface;
            world.WorldInhabitants.Add(surfaceObject);
        }
    }
}
