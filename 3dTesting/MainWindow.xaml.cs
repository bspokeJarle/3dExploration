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
using _3dTesting._3dWorld;
using System.Collections.Generic;
using _3dRotations.World.Objects;

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

        private void Handle3dWorld()
        {
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
            GameHelpers.UpdateShipStatistics(healthRectangle, world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Ship"));

            GameHelpers.UpdateMapOverlay(mapOverlay, surfaceMapBitmap,
                Convert.ToInt32(world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapPosition.x),
                Convert.ToInt32(world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapPosition.z));

            Task.Run(() =>
            {
                if (isPaused)
                {
                    return;
                }
                
                var screenCoordinates = new List<_Coordinates._2dTriangleMesh>();
                var crashBoxCoordinates = new List<_Coordinates._2dTriangleMesh>();
                gameWorldManager.UpdateWorld(world,ref screenCoordinates, ref crashBoxCoordinates);
                //Check if there are any crashboxes to debug
                if (crashBoxCoordinates.Count > 0) screenCoordinates.AddRange(crashBoxCoordinates);
                Dispatcher.Invoke(() => worldRenderer.RenderTriangles(screenCoordinates));
                //Check if there are any crashboxes to debug
            });
        }
    }
}
