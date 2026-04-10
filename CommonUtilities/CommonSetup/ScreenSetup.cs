namespace CommonUtilities.CommonSetup
{
    //TODO: Expand this as needed, common Screen properties can go here
    public static class ScreenSetup
    {
        public const int perspectiveAdjustment = 1500;
        public const int defaultObjectZoom = 2;
        public const int targetFps = 90;

        public const int screenSizeX = 1500;
        public const int screenSizeY = 1024;

        // Depth / view-distance constants
        public const float RenderFarZ = 2000f;
        public const float RenderNearZ = -2000f;
        public const float ObjectVisibilityDistance = 2000f;
    }
}
