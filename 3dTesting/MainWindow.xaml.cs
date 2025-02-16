using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
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
            mainGrid.Children.Add(mapOverlay);
            mainGrid.Children.Add(FpsText);

            surfaceMapBitmap = world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapBitmap;

            timer.Interval = TimeSpan.FromMilliseconds(12);
            timer.Tick += (s, e) => Handle3dWorld();
            timer.Start();
            stopwatch.Start();
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();
        }

        private void Handle3dWorld()
        {
            frameCount++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                Dispatcher.Invoke(() => FpsText.Text = $"FPS: {frameCount} Triangles:{worldRenderer.GetRenderingTriangleCount()}");
                frameCount = 0;
                stopwatch.Restart();
            }

            GameHelpers.UpdateMapOverlay(mapOverlay, surfaceMapBitmap,
                Convert.ToInt16(world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapPosition.x),
                Convert.ToInt16(world.WorldInhabitants.FirstOrDefault(z => z.ObjectName == "Surface")?.ParentSurface?.GlobalMapPosition.z));

            Task.Run(() =>
            {
                var screenCoordinates = gameWorldManager.UpdateWorld(world);
                Dispatcher.Invoke(() => worldRenderer.RenderTriangles(screenCoordinates));
            });
        }
    }
}
