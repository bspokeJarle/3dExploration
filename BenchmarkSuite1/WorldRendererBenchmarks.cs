using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using _3dTesting;
using _3dTesting.Rendering;
using _3dTesting._Coordinates;

namespace _3dTesting.Benchmarks;
[CPUUsageDiagnoser]
public class WorldRendererBenchmarks
{
    private const int TriangleCount = 2048;
    private DrawingVisualHost _host = null !;
    private WorldRenderer _renderer = null !;
    private List<_2dTriangleMesh> _triangles = null !;
    [GlobalSetup]
    public void Setup()
    {
        _triangles = new List<_2dTriangleMesh>(TriangleCount);
        for (int i = 0; i < TriangleCount; i++)
        {
            _triangles.Add(new _2dTriangleMesh { X1 = i, Y1 = i + 1, X2 = i + 2, Y2 = i + 3, X3 = i + 4, Y3 = i + 5, CalculatedZ = i % 2000, TriangleAngle = 0.5f, Color = "FFFFFF", PartName = i % 2 == 0 ? "CrashBox-Test" : "Ship" });
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _host = new DrawingVisualHost();
        _renderer = new WorldRenderer(_host);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _host = null !;
        _renderer = null !;
    }

    [Benchmark]
    public void RenderTriangles()
    {
        _renderer.RenderTriangles(_triangles);
    }
}