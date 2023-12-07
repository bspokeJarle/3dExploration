using _3dTesting._3dWorld;
using _3dRotations.Scene.Scene1;

namespace _3DWorld.Scene
{
    public class Setup
    {
        //Setup the scene to put into the world
        public Setup(_3dWorld world)
        {
            //todo the ability to setup multiple scenes/levels/planets etc, for now just one scene
            var scene1 = new Scene1();
            scene1.SetupScene1(world);
        }
    }
}
