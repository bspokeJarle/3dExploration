namespace CommonUtilities.CommonSetup
{
    public static class LandBasedObjectSetup
    {
        public static float GroundContactNudgeY = 6f;

        public static float GroundContactNudgeYScaled => GroundContactNudgeY * ScreenSetup.ScreenScaleY;
    }
}
