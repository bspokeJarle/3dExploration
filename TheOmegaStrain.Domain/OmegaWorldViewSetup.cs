using System;

namespace TheOmegaStrain.Domain
{
    /// <summary>
    /// Omega-specific world view angle. RetroMesh keeps its neutral default so
    /// other games can choose their own presentation without being affected.
    /// </summary>
    public static class OmegaWorldViewSetup
    {
        // The shipped/original presentation was authored and offset-tuned at 70°.
        public const float OriginalWorldPitchDegrees = 70f;

        // 63 degrees in the engine convention is approximately 27 degrees down
        // toward the ground and is the normal Omega view.
        public static float WorldPitchDegrees { get; private set; } = 63f;
        public static int WorldPitchDegreesInt => (int)MathF.Round(WorldPitchDegrees);

        public static float SurfacePitchDegrees => WorldPitchDegrees;
        public static float SurfaceFacingObjectPitchDegrees => WorldPitchDegrees;
        public static float CameraPitchDegrees => WorldPitchDegrees;
        public static int CameraPitchDegreesInt => WorldPitchDegreesInt;

        public static void ConfigurePitch(float pitchDegrees)
        {
            WorldPitchDegrees = Math.Clamp(pitchDegrees, 50f, 80f);
        }
    }
}
