
using _3dTesting._3dWorld;
using _3dRotations.World.Objects;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scene.Scene1
{
    public class Scene1
    {
        public void SetupScene1(_3dWorld world)
        {
            //Add ship as first inhabitant
            var ship = Ship.CreateShip();
            ship.Rotation = new Vector3 { x = 0, y = -180, z = -180 };
            ship.ObjectName = "Ship";
            world.WorldInhabitants.Add(ship);

            //Add the surface as second inhabitant, generate the surface
            var surface = Surface.CreateSurface();
            surface.ObjectName = "Surface";
            surface.Position = new Vector3 { x = 0, y = 250, z = 300 };
            surface.Rotation = new Vector3 { x = 85, y = 0, z = 0 };
            //TODO: Add movement to the surface when ship is moving
            //surface.Movement = new SurfaceMovement();
            surface.ObjectName = "Surface";
            //Add crashboxes to the surface, add more crashboxes to the surface for large objects (Mountains etc)
            surface.CrashBoxes = new System.Collections.Generic.List<System.Collections.Generic.List<Domain.IVector3>> { new System.Collections.Generic.List<Domain.IVector3> { new Vector3 { x = -625, y = 250, z = -625 }, new Vector3 { x = 625, y = 550, z = 625 } } };
            world.WorldInhabitants.Add(surface);
        }
    }
}
