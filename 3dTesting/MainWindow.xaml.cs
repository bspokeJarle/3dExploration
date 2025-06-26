using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using _3dTesting.Helpers;
using _3dTesting.MainWindowClasses;
using _3dTesting.Rendering;
using System.Collections.Generic;
using System.Windows.Media.Animation;

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
        public BitmapSource surfaceMapBitmap;
        private Image mapOverlay;
        private System.Windows.Shapes.Rectangle healthRectangle;
        private bool isPaused = false;

        public MainWindow()
        {
            //Turn this on when debugging
            Logger.EnableFileLogging = true;
            Logger.ClearLog();

            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

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
            mapOverlay = new Image
            {
                Width = 200,
                Height = 200,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0.7
            };

            var yellowBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0));
            //Update the Ship statistics
            healthRectangle = new System.Windows.Shapes.Rectangle
            {
                Stroke = yellowBrush,
                Fill = yellowBrush,
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 50

            };
            mainGrid.Children.Add(healthRectangle);
            mainGrid.Children.Add(mapOverlay);
            mainGrid.Children.Add(FpsText);

            surfaceMapBitmap = world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapBitmap;

            timer.Interval = TimeSpan.FromMilliseconds(8);
            timer.Tick += (s, e) => Handle3dWorld();
            timer.Start();
            stopwatch.Start();

        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();
            //This is the pause the game
            if (e.Key == Key.LeftCtrl)
                isPaused = !isPaused;
        }

        private bool isFading = false;
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
            //TODO: Should wait until we actually have the new Scene
            if (!isPaused && gameWorldManager.FadeInWorld && isFading && world.WorldInhabitants.Count>100)
            {
                await FadeInAsync(1.5f);
                gameWorldManager.FadeInWorld = false;
                isFading = false;
            }

            if (!isPaused && gameWorldManager.FadeOutWorld && !isFading)
            {
                isFading = true;
                await FadeOutAsync(1.0f);
                gameWorldManager.FadeOutWorld = false;
            }

            frameCount++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                Dispatcher.Invoke(() =>
                    FpsText.Text = $"FPS: {frameCount} Triangles:{worldRenderer.GetRenderingTriangleCount()} { gameWorldManager.DebugMessage }"
                ) ;
                frameCount = 0;
                stopwatch.Restart();
            }

            //Show health etc for main ship
            GameHelpers.UpdateShipStatistics(healthRectangle, (Domain._3dSpecificsImplementations._3dObject)world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Ship"));

            GameHelpers.UpdateMapOverlay(mapOverlay, surfaceMapBitmap,
                Convert.ToInt32(world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapPosition.x),
                Convert.ToInt32(world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapPosition.z));

            _ = Task.Run(() =>
            {
                if (isPaused)
                {
                    return;
                }

                var screenCoordinates = new List<_Coordinates._2dTriangleMesh>();
                var crashBoxCoordinates = new List<_Coordinates._2dTriangleMesh>();
                gameWorldManager.UpdateWorld(world, ref screenCoordinates, ref crashBoxCoordinates);
                //Check if there are any crashboxes to debug
                if (crashBoxCoordinates.Count > 0) screenCoordinates.AddRange(crashBoxCoordinates);
                if (!isFading) Dispatcher.Invoke(() => worldRenderer.RenderTriangles(screenCoordinates));
                //Check if there are any crashboxes to debug
            });
        }
    }
}
