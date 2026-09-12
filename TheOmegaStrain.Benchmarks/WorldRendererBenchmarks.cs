using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using BenchmarkDotNet.Attributes;
using TheOmegaStrain.Wpf;
using TheOmegaStrain.Wpf.Rendering;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Benchmarks;
[MemoryDiagnoser]
[InProcess]
[WarmupCount(3)]
[IterationCount(10)]
public class WorldRendererBenchmarks
{
    private Thread _uiThread = null !;
    private Dispatcher _dispatcher = null !;
    private DrawingVisualHost _host = null !;
    private WorldRenderer _renderer = null !;
    private List<ProjectedTriangleMesh> _triangles = null !;

    [Params(2048, 6000, 10000, 20000)]
    public int TriangleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _triangles = RendererBenchmarkTriangles.Create(TriangleCount);

        using var ready = new ManualResetEventSlim(false);
        _uiThread = new Thread(() =>
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        });
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Start();
        ready.Wait();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _dispatcher.Invoke(() =>
        {
            _host = new DrawingVisualHost();
            _renderer = new WorldRenderer(_host);
        });
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _dispatcher.Invoke(() =>
        {
            _host = null !;
            _renderer = null !;
        });
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dispatcher.InvokeShutdown();
        _uiThread.Join();
    }

    [Benchmark]
    public void RenderTriangles()
    {
        _dispatcher.Invoke(() => _renderer.RenderTriangles(_triangles));
    }
}
