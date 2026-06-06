namespace CommonUtilities.CommonSetup
{
    public static class LandBasedObjectSetup
    {
        public static float GroundContactNudgeY = 6f;
        public const float SurfaceFootprintOffsetY = 500f;

        public static float GroundContactNudgeYScaled => GroundContactNudgeY * ScreenSetup.ScreenScaleY;
        public static float SurfaceFootprintOffsetYScaled => SurfaceFootprintOffsetY * ScreenSetup.ScreenScaleY;
        public static float NudgedSurfaceFootprintOffsetYScaled => (SurfaceFootprintOffsetY - GroundContactNudgeY) * ScreenSetup.ScreenScaleY;
    }
}
