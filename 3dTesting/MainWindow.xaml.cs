using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
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
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting
{
    public partial class MainWindow : Window
    {
        public enum Viewtypes {polygons , lines};
        public _3dTo2d From3dTo2d = new();
        public _3dRotate Rotate3d = new();
        public _3dWorld._3dWorld world = new();
        public Viewtypes ViewType = Viewtypes.polygons;       
        public List<TriangleMesh> modelCoordinates;

        public MainWindow()
        {            
            InitializeComponent();
            CompositionTarget.Rendering += Handle3dWorld;
            
        }        

        public void Handle3dWorld(object sender, EventArgs e)
        {
            //Create new list of objects to prevent changing the original objects
            var activeWorld = _3dObjectHelpers.DeepCopy3dObjects(world.WorldInhabitants);
            clearCanvasPolygons();
            
            foreach (_3dObject inhabitant in activeWorld)
            {
                inhabitant.Movement?.MoveObject(inhabitant);
                foreach(var part in inhabitant.ObjectParts)
                {
                    var RotatedInhabitant = new List<ITriangleMeshWithColor>();
                    RotatedInhabitant.AddRange(part.Triangles);
                    RotatedInhabitant = Rotate3d.RotateZMesh(RotatedInhabitant, inhabitant.Rotation.z);
                    RotatedInhabitant = Rotate3d.RotateYMesh(RotatedInhabitant, inhabitant.Rotation.y);
                    RotatedInhabitant = Rotate3d.RotateXMesh(RotatedInhabitant, inhabitant.Rotation.x);
                    part.Triangles = RotatedInhabitant;
                }
            }

            //Calculate perspective and convert from 3d to 2d
            var ScreenCoordinates = From3dTo2d.convertTo2dFromObjects(activeWorld);

            var blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;
            
            foreach (var triangle in ScreenCoordinates.OrderBy(z=>z.CalculatedZ))
            {
                var colorBrush = new SolidColorBrush();
                colorBrush.Color = Helpers.Colors.getShadeOfColorFromNormal(triangle.TriangleAngle,triangle.Color);

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
                    myPoly.Fill = colorBrush;
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
