using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using BenchmarkDotNet.Attributes;
using RetroMesh.Rendering.Direct3D11;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Benchmarks;

/// <summary>
/// Measures the complete Direct3D 11 render path, including vertex preparation,
/// upload, draw calls and an immediate (non-VSync) swap-chain presentation.
/// VSync is deliberately disabled so the result reports renderer capacity rather
/// than the refresh interval of the monitor running the benchmark.
/// </summary>
[MemoryDiagnoser]
[InProcess]
[WarmupCount(5)]
[IterationCount(15)]
public class Direct3D11RendererBenchmarks
{
    private Thread _uiThread = null!;
    private Form _form = null!;
    private Panel _panel = null!;
    private Direct3D11ProjectedTriangleRenderer _renderer = null!;
    private List<ProjectedTriangleMesh> _triangles = null!;

    [Params(2048, 6000, 10000, 20000)]
    public int TriangleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _triangles = RendererBenchmarkTriangles.Create(TriangleCount);

        using var ready = new ManualResetEventSlim(false);
        Exception? initializationFailure = null;
        _uiThread = new Thread(() =>
        {
            try
            {
                _form = new Form
                {
                    ClientSize = new System.Drawing.Size(
                        RendererBenchmarkTriangles.RenderWidth,
                        RendererBenchmarkTriangles.RenderHeight),
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new System.Drawing.Point(-32000, -32000)
                };
                _panel = new Panel { Dock = DockStyle.Fill };
                _form.Controls.Add(_panel);
                _form.Show();
                _panel.CreateControl();

                _renderer = new Direct3D11ProjectedTriangleRenderer(
                    _panel.Handle,
                    RendererBenchmarkTriangles.RenderWidth,
                    RendererBenchmarkTriangles.RenderHeight)
                {
                    VerticalSync = false
                };
                _renderer.SetProjectionSize(
                    RendererBenchmarkTriangles.RenderWidth,
                    RendererBenchmarkTriangles.RenderHeight);
            }
            catch (Exception ex)
            {
                initializationFailure = ex;
            }
            finally
            {
                ready.Set();
            }

            if (initializationFailure == null)
                Application.Run(_form);
        });
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Start();
        ready.Wait();

        if (initializationFailure != null)
            throw new InvalidOperationException("Direct3D 11 benchmark initialization failed.", initializationFailure);

        // Allocate and upload the vertex buffer before timing steady-state frames.
        _panel.Invoke(() => _renderer.RenderTriangles(_triangles));
    }

    [Benchmark]
    public void RenderTriangles()
    {
        // The game also renders through its UI thread. Keep that dispatch in the
        // measurement so this remains representative of the shipping call path.
        _panel.Invoke(() => _renderer.RenderTriangles(_triangles));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_panel is not null && !_panel.IsDisposed)
        {
            _panel.Invoke(() =>
            {
                _renderer.Dispose();
                _form.Close();
            });
        }

        _uiThread?.Join();
    }
}
