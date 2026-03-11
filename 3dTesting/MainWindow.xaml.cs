using _3dTesting.Helpers;
using _3dTesting.MainWindowClasses;
using _3dTesting.Rendering;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace _3dTesting
{
    public class DrawingVisualHost : FrameworkElement
    {
        private VisualCollection visuals;

        public DrawingVisualHost()
        {
            visuals = new VisualCollection(this);
        }

        public void AddVisual(DrawingVisual visual)
        {
            visuals.Clear();
            visuals.Add(visual);
        }

        protected override int VisualChildrenCount => visuals.Count;
        protected override Visual GetVisualChild(int index) => visuals[index];
    }

    public partial class MainWindow : Window
    {
        private const bool enableLogging = false;
        private DrawingVisualHost visualHost = new DrawingVisualHost();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private int frameCount = 0;

        private Grid mainGrid;
        private GameWorldManager gameWorldManager = new GameWorldManager();
        private WorldRenderer worldRenderer;

        public _3dWorld._3dWorld world = new();
        public MediaPlayer player = new MediaPlayer();

        private bool isPaused = false;
        private int pauseFrameCount = 0;
        private const int limitFrameCount = 10;
        private DateTime fadeOutTrigged = DateTime.MinValue;
        private int _updateInProgress = 0;
        private long _lastTickTimestamp = 0;

        // Overlay handlers
        private OverlayHandler _overlayHandler;
        private HudOverlayHandlerV2 _hudHandler;
        private MediaElement _videoOverlay;
        private string? _currentVideoClipPath;
        private bool _videoOverlayIsPlaying;
        private bool _videoEntrancePlayed;

        private bool isFading = false;
        private int Fps = 0;

        private const int TargetFps = ScreenSetup.targetFps;
        private readonly double targetFrameIntervalMs = 1000.0 / TargetFps;
        private long _lastFrameTick = 0;
        private double _tickAccumulatorMs = 0;

        public MainWindow()
        {
            Logger.EnableFileLogging = true;
            Logger.ClearLog();
            GameState.SurfaceState.RecordingFps = ScreenSetup.targetFps;

            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(HandleKeys);
            Closing += MainWindow_Closing;

            mainGrid = new Grid();
            Content = mainGrid;

            mainGrid.Children.Add(visualHost);
            worldRenderer = new WorldRenderer(visualHost);

            _videoOverlay = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                IsMuted = true,
                Stretch = Stretch.UniformToFill,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _videoOverlay.MediaEnded += VideoOverlayOnMediaEnded;
            _videoOverlay.RenderTransform = new TranslateTransform(0, 0);
            _videoOverlay.RenderTransformOrigin = new Point(0.5, 0.5);
            Panel.SetZIndex(_videoOverlay, 1);
            mainGrid.Children.Add(_videoOverlay);

            FadeOverlay = new System.Windows.Shapes.Rectangle
            {
                Fill = Brushes.Black,
                Opacity = 0,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Panel.SetZIndex(FadeOverlay, int.MaxValue);
            mainGrid.Children.Add(FadeOverlay);

            FpsText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 20,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };
            mainGrid.Children.Add(FpsText);

            //surfaceMapBitmap = GameState.SurfaceState.GlobalMapBitmap;

            // Overlay handlers must be put in the grid
            _overlayHandler = new OverlayHandler(mainGrid);
            _hudHandler = new HudOverlayHandlerV2(mainGrid);

            timer.Interval = TimeSpan.FromMilliseconds(8);
            CompositionTarget.Rendering += Handle3dWorldRendering;
            stopwatch.Start();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void HandleKeys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();

            if (e.Key == Key.LeftCtrl)
            {
                isPaused = !isPaused;
                world.IsPaused = !isPaused;
                pauseFrameCount = 0;
            }
            //Send keys to Scenehandler to handle scene switches and overlay switches
            world.SceneHandler.HandleKeyPress(e,world);
        }

        public async Task FadeOutAsync(float durationSeconds = 1.0f)
        {
            FadeOverlay.Visibility = Visibility.Visible;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                FillBehavior = FillBehavior.HoldEnd
            };

            var tcs = new TaskCompletionSource<bool>();
            animation.Completed += (s, e) => tcs.SetResult(true);
            FadeOverlay.BeginAnimation(UIElement.OpacityProperty, animation);

            await tcs.Task;
        }

        public async Task FadeInAsync(float durationSeconds = 1.0f)
        {
            FadeOverlay.Visibility = Visibility.Visible;
            FadeOverlay.Opacity = 1;

            var animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                FillBehavior = FillBehavior.Stop
            };

            var tcs = new TaskCompletionSource<bool>();
            animation.Completed += (s, e) =>
            {
                FadeOverlay.Visibility = Visibility.Collapsed;
                FadeOverlay.Opacity = 0;
                tcs.SetResult(true);
            };

            FadeOverlay.BeginAnimation(UIElement.OpacityProperty, animation);

            await tcs.Task;
        }

        private void Handle3dWorldRendering(object? sender, EventArgs e)
        {
            var nowTicks = Stopwatch.GetTimestamp();
            if (_lastFrameTick == 0)
            {
                _lastFrameTick = nowTicks;
                return;
            }

            var elapsedMs = (nowTicks - _lastFrameTick) * 1000.0 / Stopwatch.Frequency;
            _lastFrameTick = nowTicks;
            _tickAccumulatorMs += elapsedMs;

            if (_tickAccumulatorMs < targetFrameIntervalMs)
                return;

            int steps = (int)Math.Floor(_tickAccumulatorMs / targetFrameIntervalMs);
            if (steps > 5) steps = 5;

            _tickAccumulatorMs -= steps * targetFrameIntervalMs;

            for (int i = 0; i < steps; i++)
            {
                Handle3dWorld(targetFrameIntervalMs / 1000.0);
            }
        }

        private async void Handle3dWorld(double dtSeconds)
        {
            if (Logger.EnableFileLogging)
            {
                var nowTicks = Stopwatch.GetTimestamp();
                if (_lastTickTimestamp != 0)
                {
                    var tickMs = (nowTicks - _lastTickTimestamp) * 1000.0 / Stopwatch.Frequency;
                    if (enableLogging) Logger.Log($"[Tick] dtMs={tickMs:0.###}");
                }
                _lastTickTimestamp = nowTicks;
            }

            float dt = (float)dtSeconds;

            if (GameState.ScreenOverlayState.ShowDebugOverlay == false)
                FpsText.Visibility = Visibility.Collapsed;
            else
                FpsText.Visibility = Visibility.Visible;

            if (world.IsPaused)
                pauseFrameCount++;

            if (pauseFrameCount < limitFrameCount && gameWorldManager.FadeInWorld && isFading && world.WorldInhabitants.Count > 100)
            {
                await FadeInAsync(1.5f);
                gameWorldManager.FadeInWorld = false;
                isFading = false;
            }

            if (pauseFrameCount <= limitFrameCount && gameWorldManager.FadeOutWorld && !isFading)
            {
                if (fadeOutTrigged == DateTime.MinValue)
                {
                    fadeOutTrigged = DateTime.Now;
                    return;
                }

                if (DateTime.Now >= fadeOutTrigged.AddSeconds(1.2))
                {
                    isFading = true;
                    await FadeOutAsync(1.0f);
                    gameWorldManager.FadeOutWorld = false;
                    fadeOutTrigged = DateTime.MinValue;
                }
            }

            // Overlay update + render (UI layer)
            if (GameState.ScreenOverlayState != null)
            {
                // FadeOverlay wins – avoid double-dim
                if (isFading || FadeOverlay.Visibility == Visibility.Visible)
                {
                    // keep existing behavior
                    GameState.ScreenOverlayState.ShowOverlay = false;
                }

                GameState.ScreenOverlayState.Update(dt);

                UpdateVideoOverlay();

                double w = ActualWidth > 0 ? ActualWidth : Width;
                double h = ActualHeight > 0 ? ActualHeight : Height;
                if (w <= 0) w = 1920;
                if (h <= 0) h = 1080;

                _overlayHandler.Update(GameState.ScreenOverlayState, w, h);

                // HUD V2: update each tick (it hides itself when ShowOverlay==true)
                var gameplay = GameState.GamePlayState;

                int triangles = worldRenderer.GetRenderingTriangleCount();
                _hudHandler.Update(GameState.ScreenOverlayState, gameplay, w, h, Fps, triangles);
            }

            frameCount++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                Fps = frameCount;
                Dispatcher.Invoke(() =>
                    FpsText.Text = $"FPS: {frameCount} Triangles:{worldRenderer.GetRenderingTriangleCount()} {gameWorldManager.DebugMessage}"
                );
                frameCount = 0;
                stopwatch.Restart();
            }

            // Minimap -> HUD slot only
            if (!isFading && world.WorldInhabitants.Count > 50)
            {
                 if (GameState.SurfaceState.GlobalMapPosition != null)
                {
                    GameHelpers.UpdateDirtyTilesInMap(GameState.SurfaceState.GlobalMapBitmap);

                    GameHelpers.UpdateMapOverlay(
                        _hudHandler.GetMinimapImage(),
                        GameState.SurfaceState.GlobalMapBitmap,
                        Convert.ToInt32(GameState.SurfaceState.GlobalMapPosition.x),
                        Convert.ToInt32(GameState.SurfaceState.GlobalMapPosition.z)
                    );
                }
            }

            if (pauseFrameCount >= limitFrameCount)
                return;

            if (System.Threading.Interlocked.Exchange(ref _updateInProgress, 1) == 1)
                return;

            _ = Task.Run(() =>
            {
                long startTicks = Logger.EnableFileLogging ? Stopwatch.GetTimestamp() : 0;
                try
                {
                    var screenCoordinates = new List<_Coordinates._2dTriangleMesh>();
                    var crashBoxCoordinates = new List<_Coordinates._2dTriangleMesh>();

                    gameWorldManager.UpdateWorld(world, ref screenCoordinates, ref crashBoxCoordinates);

                    if (enableLogging)
                    {
                        var elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
                        Logger.Log($"[UpdateWorld] ms={elapsedMs:0.###}");
                    }

                    if (!isFading)
                        Dispatcher.BeginInvoke(() => worldRenderer.RenderTriangles(screenCoordinates));
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _updateInProgress, 0);
                }
            });
        }

        private void UpdateVideoOverlay()
        {
            var state = GameState.ScreenOverlayState;
            if (state == null)
                return;

            if (state.ShowVideoOverlay && !string.IsNullOrWhiteSpace(state.VideoClipPath))
            {
                var resolvedPath = ResolveVideoPath(state.VideoClipPath);
                if (!string.Equals(_currentVideoClipPath, resolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _videoOverlay.Stop();
                    _videoOverlay.Source = new Uri(resolvedPath, UriKind.Absolute);
                    _currentVideoClipPath = resolvedPath;
                    _videoOverlayIsPlaying = false;
                    _videoEntrancePlayed = false;
                }

                double w = ActualWidth > 0 ? ActualWidth : Width;
                double h = ActualHeight > 0 ? ActualHeight : Height;
                if (w <= 0) w = 1920;
                if (h <= 0) h = 1080;

                _videoOverlay.Width = w;
                _videoOverlay.Height = h;
                _videoOverlay.Visibility = Visibility.Visible;

                if (!_videoEntrancePlayed)
                {
                    StartVideoEntranceAnimation(h);
                    _videoEntrancePlayed = true;
                }

                if (!_videoOverlayIsPlaying && _videoOverlay.Source != null)
                {
                    _videoOverlay.Play();
                    _videoOverlayIsPlaying = true;
                }
            }
            else
            {
                if (_videoOverlay.Visibility != Visibility.Collapsed)
                {
                    _videoOverlay.Stop();
                    _videoOverlay.Visibility = Visibility.Collapsed;
                }

                _currentVideoClipPath = null;
                _videoOverlayIsPlaying = false;
                _videoEntrancePlayed = false;
            }
        }

    private void VideoOverlayOnMediaEnded(object? sender, RoutedEventArgs e)
    {
        if (_videoOverlay.Visibility != Visibility.Visible)
            return;

        _videoOverlay.Position = TimeSpan.Zero;
        _videoOverlay.Play();
    }

        private static string ResolveVideoPath(string clipPath)
        {
            if (Path.IsPathRooted(clipPath))
                return clipPath;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, clipPath);
        }

        private void StartVideoEntranceAnimation(double screenHeight)
        {
            if (_videoOverlay.RenderTransform is not TranslateTransform translate)
            {
                translate = new TranslateTransform(0, 0);
                _videoOverlay.RenderTransform = translate;
            }

            var startOffset = screenHeight * 1.25;
            translate.Y = startOffset;

            var animation = new DoubleAnimation
            {
                From = startOffset,
                To = 0,
                Duration = TimeSpan.FromSeconds(3.0),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            translate.BeginAnimation(TranslateTransform.YProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }
    }
}