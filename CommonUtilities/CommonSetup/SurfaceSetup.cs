using static Domain._3dSpecificsImplementations;

namespace CommonUtilities.CommonSetup
{
    //TODO: Expand this as needed, common map properties can go here
    public static class SurfaceSetup
    {
        public const int surfaceWidth = 1350;
        public const int viewPortSize = surfaceWidth / tileSize;
        public const int tileSize = 75;
        public static Vector3 DefaultMapPosition  = new() { x = 95100, y = 0, z = 95200 };
    }
}
