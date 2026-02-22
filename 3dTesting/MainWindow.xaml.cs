using _3dTesting.Helpers;
using _3dTesting.MainWindowClasses;
using _3dTesting.Rendering;
using CommonUtilities.CommonGlobalState;
using Domain;
using System;
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

        // Overlay handlers
        private OverlayHandler _overlayHandler;
        private HudOverlayHandlerV2 _hudHandler;

        private bool isFading = false;
        private int Fps = 0;

        public MainWindow()
        {
            Logger.EnableFileLogging = true;
            Logger.ClearLog();

            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(HandleKeys);

            mainGrid = new Grid();
            Content = mainGrid;

            mainGrid.Children.Add(visualHost);
            worldRenderer = new WorldRenderer(visualHost);

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
            timer.Tick += (s, e) => Handle3dWorld();
            timer.Start();
            stopwatch.Start();
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

        private async void Handle3dWorld()
        {
            float dt = (float)timer.Interval.TotalSeconds;

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
            if (!isFading && world.WorldInhabitants.Count > 100)
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

            _ = Task.Run(() =>
            {
                if (pauseFrameCount >= limitFrameCount)
                    return;

                if (System.Threading.Interlocked.Exchange(ref _updateInProgress, 1) == 1)
                    return;

                try
                {
                    var screenCoordinates = new List<_Coordinates._2dTriangleMesh>();
                    var crashBoxCoordinates = new List<_Coordinates._2dTriangleMesh>();

                    gameWorldManager.UpdateWorld(world, ref screenCoordinates, ref crashBoxCoordinates);

                    if (!isFading)
                        Dispatcher.BeginInvoke(() => worldRenderer.RenderTriangles(screenCoordinates));
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _updateInProgress, 0);
                }
            });
        }
    }
}