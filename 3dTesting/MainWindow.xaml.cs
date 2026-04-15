using _3dTesting.Helpers;
using _3dTesting.MainWindowClasses;
using _3dTesting.MainWindowClasses.Overlays;
using _3dTesting.Rendering;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using CommonUtilities.Persistence;
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
        private int _minimapFrameSkip = 0;

        // Overlay handlers
        private OverlayManager _overlayManager;
        private MediaElement _videoOverlay;
        private string? _currentVideoClipPath;
        private bool _videoOverlayIsPlaying;
        private bool _videoEntrancePlayed;

        // MotherShip health bar (in-world overlay)
        private readonly Canvas _motherShipHealthBarCanvas;
        private readonly System.Windows.Shapes.Rectangle _motherShipHealthBarBg;
        private readonly System.Windows.Shapes.Rectangle _motherShipHealthBarFill;
        private const double MotherShipBarWidth = 16;
        private const double MotherShipBarHeight = 50;
        private const double MotherShipBarOffsetX = 140;

        // MotherShip ram warning (flashing reticle)
        private readonly Canvas _ramWarningCanvas;
        private readonly System.Windows.Shapes.Ellipse _ramWarningOuter;
        private readonly System.Windows.Shapes.Ellipse _ramWarningInner;
        private const double RamWarningSize = 100;

        // Aim assist target indicator (white reticle)
        private readonly Canvas _aimAssistCanvas;
        private readonly System.Windows.Shapes.Ellipse _aimAssistOuter;
        private readonly System.Windows.Shapes.Ellipse _aimAssistInner;
        private const double AimAssistIndicatorSize = 90;

        private bool isFading = false;
        private bool _isFadingIn = false;
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

            PersistenceSetup.Initialize();
            GameState.GamePlayState.PlayerName = PersistenceSetup.LoadLastPlayerName();

            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(HandleKeys);
            Closing += MainWindow_Closing;
            Loaded += Window_Loaded;

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
            _overlayManager = new OverlayManager(mainGrid);

            // MotherShip in-world health bar
            _motherShipHealthBarCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(_motherShipHealthBarCanvas, 10);

            _motherShipHealthBarBg = new System.Windows.Shapes.Rectangle
            {
                Width = MotherShipBarWidth,
                Height = MotherShipBarHeight,
                Fill = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
                Stroke = new SolidColorBrush(Color.FromArgb(200, 0, 255, 255)),
                StrokeThickness = 2
            };

            _motherShipHealthBarFill = new System.Windows.Shapes.Rectangle
            {
                Width = MotherShipBarWidth - 2,
                Height = MotherShipBarHeight - 2,
                Fill = Brushes.LimeGreen,
                Opacity = 0.9
            };

            _motherShipHealthBarCanvas.Children.Add(_motherShipHealthBarBg);
            _motherShipHealthBarCanvas.Children.Add(_motherShipHealthBarFill);
            mainGrid.Children.Add(_motherShipHealthBarCanvas);

            // MotherShip ram warning reticle
            _ramWarningCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(_ramWarningCanvas, 10);

            _ramWarningOuter = new System.Windows.Shapes.Ellipse
            {
                Width = RamWarningSize,
                Height = RamWarningSize,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 60, 30)),
                StrokeThickness = 3
            };

            _ramWarningInner = new System.Windows.Shapes.Ellipse
            {
                Width = RamWarningSize * 0.5,
                Height = RamWarningSize * 0.5,
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 40, 20)),
                Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 80, 40)),
                StrokeThickness = 2
            };

            _ramWarningCanvas.Children.Add(_ramWarningOuter);
            _ramWarningCanvas.Children.Add(_ramWarningInner);
            mainGrid.Children.Add(_ramWarningCanvas);

            // Aim assist target indicator (white concentric circles)
            _aimAssistCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(_aimAssistCanvas, 10);

            _aimAssistOuter = new System.Windows.Shapes.Ellipse
            {
                Width = AimAssistIndicatorSize,
                Height = AimAssistIndicatorSize,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                StrokeThickness = 3
            };

            _aimAssistInner = new System.Windows.Shapes.Ellipse
            {
                Width = AimAssistIndicatorSize * 0.5,
                Height = AimAssistIndicatorSize * 0.5,
                Fill = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                StrokeThickness = 2
            };

            _aimAssistCanvas.Children.Add(_aimAssistOuter);
            _aimAssistCanvas.Children.Add(_aimAssistInner);
            mainGrid.Children.Add(_aimAssistCanvas);

            timer.Interval = TimeSpan.FromMilliseconds(8);
            CompositionTarget.Rendering += Handle3dWorldRendering;
            stopwatch.Start();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int w = (int)ActualWidth;
            int h = (int)ActualHeight;
            if (w > 0 && h > 0)
                ScreenSetup.Initialize(w, h);
        }

        private void HandleKeys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // During name entry, Escape goes back to intro instead of quitting
                if (GameState.ScreenOverlayState.Type == ScreenOverlayType.NameEntry
                    && GameState.ScreenOverlayState.ShowOverlay)
                {
                    world.SceneHandler.HandleKeyPress(e, world);
                    return;
                }
                Application.Current.Shutdown();
            }

            if (e.Key == Key.LeftCtrl)
            {
                isPaused = !isPaused;
                world.IsPaused = isPaused;
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
            if (steps > 3) steps = 3;

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

            if (pauseFrameCount < limitFrameCount && gameWorldManager.FadeInWorld)
            {
                // Clear immediately so concurrent async Handle3dWorld calls
                // from the same render tick don't start overlapping FadeInAsync animations
                gameWorldManager.FadeInWorld = false;
                if (!isFading)
                {
                    // FadeOut was skipped (e.g., ship exploded before the fadeout delay elapsed)
                    // Snap overlay to opaque before fading in
                    FadeOverlay.Visibility = Visibility.Visible;
                    FadeOverlay.Opacity = 1;
                    isFading = true;
                }
                _isFadingIn = true;
                await FadeInAsync(1.5f);
                _isFadingIn = false;
                isFading = false;
                fadeOutTrigged = DateTime.MinValue;
            }

            if (pauseFrameCount <= limitFrameCount && gameWorldManager.FadeOutWorld && !isFading)
            {
                isFading = true;
                await FadeOutAsync(1.0f);
                gameWorldManager.FadeOutWorld = false;
                gameWorldManager.SceneResetReady = true;
                fadeOutTrigged = DateTime.MinValue;
            }

            // Overlay update + render (UI layer)
            // FadeOverlay (Z=MaxValue) covers everything during fade,
            // so overlay state progresses naturally — no need to suppress ShowOverlay
            if (GameState.ScreenOverlayState != null)
            {
                GameState.ScreenOverlayState.Update(dt);

                UpdateVideoOverlay();

                double w = ActualWidth > 0 ? ActualWidth : Width;
                double h = ActualHeight > 0 ? ActualHeight : Height;
                if (w <= 0) w = 1920;
                if (h <= 0) h = 1080;

                var gameplay = GameState.GamePlayState;
                int triangles = worldRenderer.GetRenderingTriangleCount();

                _overlayManager.Update(GameState.ScreenOverlayState, gameplay, w, h, Fps, triangles);

                // MotherShip in-world health bar
                UpdateMotherShipHealthBar(gameplay);

                // MotherShip ram warning reticle
                UpdateMotherShipRamWarning(gameplay);

                // Aim assist target indicator
                UpdateAimAssistIndicator(gameplay);
            }

            world.SceneHandler.UpdateFrame(world);

            frameCount++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                Fps = frameCount;
                FpsText.Text = $"FPS: {frameCount} Triangles:{worldRenderer.GetRenderingTriangleCount()} {gameWorldManager.DebugMessage}";
                frameCount = 0;
                stopwatch.Restart();
            }

            // Minimap -> HUD slot only
            if (!isFading && world.WorldInhabitants.Count > 50 && ++_minimapFrameSkip >= 3)
            {
                _minimapFrameSkip = 0;
                if (GameState.SurfaceState.GlobalMapPosition != null)
                {
                    GameHelpers.UpdateDirtyTilesInMap(GameState.SurfaceState.GlobalMapBitmap);

                    // Crop the source bitmap, draw markers on the copy, display it.
                    // Markers never touch the source bitmap — no save/restore needed.
                    GameHelpers.UpdateMapOverlayWithMarkers(
                        _overlayManager.GetMinimapImage(),
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

                    if (!isFading || _isFadingIn)
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

        private void UpdateMotherShipHealthBar(GamePlayState gameplay)
        {
            if (gameplay == null || !gameplay.ShowMotherShipHealthBar || !gameplay.MotherShipIsOnScreen)
            {
                _motherShipHealthBarCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            if (GameState.ScreenOverlayState.Type != ScreenOverlayType.Game || isFading)
            {
                _motherShipHealthBarCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            _motherShipHealthBarCanvas.Visibility = Visibility.Visible;

            float pct = Math.Clamp(gameplay.MotherShipHealthPercent, 0f, 1f);

            // Position the bar to the right of the mothership's screen center
            double barX = gameplay.MotherShipScreenX + MotherShipBarOffsetX;
            double maxBarY = ScreenSetup.screenSizeY * 0.83 - MotherShipBarHeight / 2;
            double barY = Math.Min(gameplay.MotherShipScreenY - MotherShipBarHeight / 2, maxBarY);

            Canvas.SetLeft(_motherShipHealthBarBg, barX);
            Canvas.SetTop(_motherShipHealthBarBg, barY);

            // Fill grows from bottom to top
            double fillH = (MotherShipBarHeight - 2) * pct;
            _motherShipHealthBarFill.Height = fillH;
            Canvas.SetLeft(_motherShipHealthBarFill, barX + 1);
            Canvas.SetTop(_motherShipHealthBarFill, barY + 1 + (MotherShipBarHeight - 2 - fillH));

            // Color: green at 100%, yellow at 50%, red at 0%
            byte r, g;
            if (pct > 0.5f)
            {
                float t = (1f - pct) * 2f;
                r = (byte)(255 * t);
                g = 255;
            }
            else
            {
                float t = pct * 2f;
                r = 255;
                g = (byte)(255 * t);
            }

            _motherShipHealthBarFill.Fill = new SolidColorBrush(Color.FromArgb(220, r, g, 0));
        }

        private void UpdateMotherShipRamWarning(GamePlayState gameplay)
        {
            if (gameplay == null || !gameplay.MotherShipRamWarningActive || isFading)
            {
                _ramWarningCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            if (GameState.ScreenOverlayState.Type != ScreenOverlayType.Game)
            {
                _ramWarningCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            _ramWarningCanvas.Visibility = Visibility.Visible;

            double cx = gameplay.MotherShipRamWarningScreenX;
            double cy = gameplay.MotherShipRamWarningScreenY;

            Canvas.SetLeft(_ramWarningOuter, cx - RamWarningSize / 2);
            Canvas.SetTop(_ramWarningOuter, cy - RamWarningSize / 2);

            double innerSize = RamWarningSize * 0.5;
            Canvas.SetLeft(_ramWarningInner, cx - innerSize / 2);
            Canvas.SetTop(_ramWarningInner, cy - innerSize / 2);
        }

        private void UpdateAimAssistIndicator(GamePlayState gameplay)
        {
            if (gameplay == null || !gameplay.AimAssistTargetActive || isFading)
            {
                _aimAssistCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            if (GameState.ScreenOverlayState.Type != ScreenOverlayType.Game)
            {
                _aimAssistCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            // Flash the indicator using a sine wave (toggles opacity)
            double phase = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) % 600.0 / 600.0;
            double alpha = 0.5 + 0.5 * Math.Sin(phase * Math.PI * 2);
            byte outerAlpha = (byte)(200 * alpha);
            byte innerAlpha = (byte)(60 * alpha);
            byte innerStrokeAlpha = (byte)(160 * alpha);

            _aimAssistOuter.Stroke = new SolidColorBrush(Color.FromArgb(outerAlpha, 255, 255, 255));
            _aimAssistInner.Fill = new SolidColorBrush(Color.FromArgb(innerAlpha, 255, 255, 255));
            _aimAssistInner.Stroke = new SolidColorBrush(Color.FromArgb(innerStrokeAlpha, 255, 255, 255));

            _aimAssistCanvas.Visibility = Visibility.Visible;

            double cx = gameplay.AimAssistTargetScreenX;
            double cy = gameplay.AimAssistTargetScreenY;

            Canvas.SetLeft(_aimAssistOuter, cx - AimAssistIndicatorSize / 2);
            Canvas.SetTop(_aimAssistOuter, cy - AimAssistIndicatorSize / 2);

            double innerSize = AimAssistIndicatorSize * 0.5;
            Canvas.SetLeft(_aimAssistInner, cx - innerSize / 2);
            Canvas.SetTop(_aimAssistInner, cy - innerSize / 2);
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