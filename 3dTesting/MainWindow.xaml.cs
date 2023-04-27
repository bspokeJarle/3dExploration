using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using STL_Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace _3dTesting
{
    public partial class MainWindow : Window
    {
        public enum Viewtypes {polygons , lines};
        public _3dTo2d From3dTo2d = new();
        public _3dRotate Rotate3d = new();
        private _3dWorld._3dWorld world = new();
        public double rotateAngleX = 0;
        public double rotateAngleY = -180;
        public double rotateAngleZ = -180;
        public Viewtypes ViewType = Viewtypes.polygons;       
        public List<TriangleMesh> modelCoordinates;



        public MainWindow()
        {            
            InitializeComponent();
            var modelReader = new STLReader("C:\\Users\\kh979\\Documents\\Privat\\Bspoke prosjekter\\3dProsjekt\\3dProsjekt\\3dTesting\\3d objects\\div\\complexorb.stl");
            modelCoordinates = modelReader.ReadFile().ToList();                        
            CompositionTarget.Rendering += Handle3dWorld;
        }        

        public void Handle3dWorld(object sender, EventArgs e)
        {
            rotateAngleY += 0.5;
            rotateAngleX += 0.8;
            rotateAngleZ += 0.5;
            if (rotateAngleY >= 360) rotateAngleY = 0;
            if (rotateAngleX >= 360) rotateAngleX = 0;
            if (rotateAngleZ >= 360) rotateAngleZ = 0;
            clearCanvasPolygons();

            var RotatedCoordinates = new List<TriangleMesh>();            
            RotatedCoordinates = modelCoordinates;
            RotatedCoordinates = Rotate3d.RotateZMesh(RotatedCoordinates, rotateAngleZ);
            RotatedCoordinates = Rotate3d.RotateYMesh(RotatedCoordinates, rotateAngleY);
            RotatedCoordinates = Rotate3d.RotateXMesh(RotatedCoordinates, rotateAngleX);

            //Calculate perspective
            var ScreenCoordinates = From3dTo2d.convertTo2d(RotatedCoordinates);
          
            var blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;
            
            foreach (var triangle in ScreenCoordinates.OrderBy(z=>z.CalculatedZ))
            {
                var yellowBrush = new SolidColorBrush();
                yellowBrush.Color = Helpers.Colors.getGrayColorFromNormal(triangle.TriangleAngle);
                //Debug.WriteLine("Angle:"+triangle.TriangleAngle+" Color:"+yellowBrush.Color);

                var myPoly = new Polygon();
                if (ViewType == Viewtypes.polygons)
                {
                    PointCollection pointCollection = new()
                    {
                        new Point{X=triangle.X1,Y=triangle.Y1},
                        new Point{X=triangle.X2,Y=triangle.Y2},
                        new Point{X=triangle.X3,Y=triangle.Y3}
                    };

                    myPoly.Points = pointCollection;
                    myPoly.Stroke = blackBrush;
                    myPoly.Fill = yellowBrush;
                    myPoly.StrokeThickness = 1;

                    MyCanvas.Children.Add(myPoly);
                }

                if (ViewType == Viewtypes.lines)
                {
                    PointCollection pointCollection = new()
                    {
                    new Point{X=triangle.X1,Y=triangle.Y1},
                    new Point{X=triangle.X2,Y=triangle.Y2},
                    new Point{X=triangle.X3,Y=triangle.Y3}
                };

                    myPoly.Points = pointCollection;
                    myPoly.Stroke = blackBrush;
                    myPoly.StrokeThickness = 1;

                    MyCanvas.Children.Add(myPoly);
                }
            }            
        }

        private void RotateY_Click(object sender, RoutedEventArgs e)
        {
            rotateAngleY+=25;            
            clearCanvasPolygons();            
        }

        private void RotateX_Click(object sender, RoutedEventArgs e)
        {
            rotateAngleX+=25;
            clearCanvasPolygons();            
        }

        private void RotateZ_Click(object sender, RoutedEventArgs e)
        {
            rotateAngleZ+=25;            
            clearCanvasPolygons();                    
        }

        public void clearCanvasPolygons()
        {            
            var buttons = new List<Button>();
            foreach (Button b in MyCanvas.Children.OfType<Button>())
            {
                buttons.Add(b);
            }
            MyCanvas.Children.Clear();
            foreach (Button b in buttons)
            {
                MyCanvas.Children.Add(b);
            }           
        }

        private void Polygons_Click(object sender, RoutedEventArgs e)
        {
            ViewType = Viewtypes.polygons;            
            clearCanvasPolygons();                 
        }

        private void Lines_Click(object sender, RoutedEventArgs e)
        {
            ViewType = Viewtypes.lines;            
            clearCanvasPolygons();                   
        }
    }
}
