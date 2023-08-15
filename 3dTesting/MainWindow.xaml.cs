using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using STL_Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using _3dTesting.Helpers;
using Colors = System.Windows.Media.Colors;

namespace _3dTesting
{
    public partial class MainWindow : Window
    {
        public enum Viewtypes {polygons , lines};
        public _3dTo2d From3dTo2d = new();
        public _3dRotate Rotate3d = new();
        private _3dWorld._3dWorld world = new();
        public Viewtypes ViewType = Viewtypes.polygons;       
        public List<TriangleMesh> modelCoordinates;

        public MainWindow()
        {            
            InitializeComponent();
            CompositionTarget.Rendering += Handle3dWorld;
        }        

        public void Handle3dWorld(object sender, EventArgs e)
        {
            //Rotate the objects in the world
            //todo temp solution, should be done in the world class
            world.WorldInhabitants[0].Rotation.x += (float)0.5;
            world.WorldInhabitants[0].Rotation.y += (float)0.8;
            world.WorldInhabitants[0].Rotation.z += (float)0.5;
            if (world.WorldInhabitants[0].Rotation.x >= 360) world.WorldInhabitants[0].Rotation.x = 0;
            if (world.WorldInhabitants[0].Rotation.y >= 360) world.WorldInhabitants[0].Rotation.y = 0;
            if (world.WorldInhabitants[0].Rotation.z >= 360) world.WorldInhabitants[0].Rotation.z = 0;

            world.WorldInhabitants[1].Rotation.x += (float)0.3;
            world.WorldInhabitants[1].Rotation.y += (float)0.6;
            world.WorldInhabitants[1].Rotation.z += (float)0.7;
            if (world.WorldInhabitants[1].Rotation.x >= 360) world.WorldInhabitants[1].Rotation.x = 0;
            if (world.WorldInhabitants[1].Rotation.y >= 360) world.WorldInhabitants[1].Rotation.y = 0;
            if (world.WorldInhabitants[1].Rotation.z >= 360) world.WorldInhabitants[1].Rotation.z = 0;

            world.WorldInhabitants[2].Rotation.x += (float)0.4;
            world.WorldInhabitants[2].Rotation.y += (float)0.6;
            world.WorldInhabitants[2].Rotation.z += (float)0.7;
            if (world.WorldInhabitants[2].Rotation.x >= 360) world.WorldInhabitants[2].Rotation.x = 0;
            if (world.WorldInhabitants[2].Rotation.y >= 360) world.WorldInhabitants[2].Rotation.y = 0;
            if (world.WorldInhabitants[2].Rotation.z >= 360) world.WorldInhabitants[2].Rotation.z = 0;

            //Create new list of objects to prevent changing the original objects
            var activeWorld = _3dObjectHelpers.DeepCopy3dObjects(world.WorldInhabitants);
            clearCanvasPolygons();
            
            foreach (_3dObject inhabitant in activeWorld)
            {
                var RotatedInhabitant = new List<TriangleMesh>();
                RotatedInhabitant.AddRange(inhabitant.Triangles);
                RotatedInhabitant = Rotate3d.RotateZMesh(RotatedInhabitant, inhabitant.Rotation.z);
                RotatedInhabitant = Rotate3d.RotateYMesh(RotatedInhabitant, inhabitant.Rotation.y);
                RotatedInhabitant = Rotate3d.RotateXMesh(RotatedInhabitant, inhabitant.Rotation.x);
                inhabitant.Triangles = RotatedInhabitant;                
            }

            //Calculate perspective and convert from 3d to 2d
            var ScreenCoordinates = From3dTo2d.convertTo2dFromObjects(activeWorld);

            var blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;
            
            foreach (var triangle in ScreenCoordinates.OrderBy(z=>z.CalculatedZ))
            {
                var yellowBrush = new SolidColorBrush();
                yellowBrush.Color = Helpers.Colors.getGrayColorFromNormal(triangle.TriangleAngle);                

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
    }
}
