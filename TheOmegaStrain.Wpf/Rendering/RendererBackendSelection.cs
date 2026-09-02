using System;

namespace TheOmegaStrain.Wpf.Rendering;

public static class RendererBackendSelection
{
    public static bool UseDirect3D11()
    {
        var rendererOverride = Environment.GetEnvironmentVariable("OMEGASTRAIN_RENDERER");
        return !string.Equals(rendererOverride, "wpf", StringComparison.OrdinalIgnoreCase);
    }
}
