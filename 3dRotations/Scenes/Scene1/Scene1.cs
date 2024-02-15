
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
            world.WorldInhabitants.Add(ship);

            //Add the surface as second inhabitant, generate the surface
            var surface = Surface.CreateSurface();
            surface.Position = new Vector3 { x = 0, y = 200, z = 300 };
            surface.Rotation = new Vector3 { x = 85, y = 0, z = 0 };
            world.WorldInhabitants.Add(surface);
        }
    }
}
