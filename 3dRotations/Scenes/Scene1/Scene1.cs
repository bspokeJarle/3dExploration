
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
            var orb = Ship.CreateShip();
            orb.Position = new Vector3 { x = 100, y = 0, z = 800 };
            orb.Rotation = new Vector3 { x = 0, y = -180, z = -180 };
            world.WorldInhabitants.Add(orb);

            /*//Add ship as first inhabitant
            var test1 = TestObjects.CreateTestObject();
            test1.Position = new Vector3 { x = -100, y = 0, z = 800 };
            test1.Rotation = new Vector3 { x = 0, y = -180, z = -180 };
            world.WorldInhabitants.Add(test1);

            //Add ship as first inhabitant
            var test2 = TestObjects.CreateTestObject();
            test2.Position = new Vector3 { x = 200, y = 0, z = 800 };
            test2.Rotation = new Vector3 { x = 0, y = -180, z = -180 };
            world.WorldInhabitants.Add(test2);*/
        }
    }
}
