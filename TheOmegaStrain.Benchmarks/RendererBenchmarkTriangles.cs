using System.Collections.Generic;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Benchmarks;

internal static class RendererBenchmarkTriangles
{
    internal const int RenderWidth = 1920;
    internal const int RenderHeight = 1080;

    internal static List<ProjectedTriangleMesh> Create(int count)
    {
        var triangles = new List<ProjectedTriangleMesh>(count);
        for (int i = 0; i < count; i++)
        {
            int column = i % 64;
            int row = (i / 64) % 36;
            int x = column * 30;
            int y = row * 30;
            triangles.Add(new ProjectedTriangleMesh
            {
                X1 = x,
                Y1 = y,
                X2 = x + 24,
                Y2 = y + 4,
                X3 = x + 8,
                Y3 = y + 24,
                CalculatedZ = i % 2000,
                TriangleAngle = 0.5f,
                Color = "FFFFFF",
                Opacity = 1f,
                Rhw1 = 1f,
                Rhw2 = 1f,
                Rhw3 = 1f,
                PartName = "Benchmark"
            });
        }

        return triangles;
    }
}
