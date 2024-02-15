using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static Domain._3dSpecificsImplementations;
using Colors = System.Windows.Media.Colors;

namespace _3dTesting
{
    public partial class MainWindow : Window
    {
        public enum Viewtypes { polygons, lines };
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
            ClearCanvasPolygons();

            //The Particle objects must be rotated as well, and then added to the rotated inhabitant in the end
            var particleObjectList = new List<_3dObject>();
            foreach (_3dObject inhabitant in activeWorld)
            {
                inhabitant.Movement?.MoveObject(inhabitant);                
                foreach (var part in inhabitant.ObjectParts)
                {
                    var RotatedInhabitant = new List<ITriangleMeshWithColor>();
                    //var startPosition = ParentObject?.ObjectParts.Single((part) => part.PartName == "JetMotor");
                    //var trajectory = ParentObject?.ObjectParts.Single((part) => part.PartName == "JetMotorDirectionGuide");
                    var JetMotor = new TriangleMeshWithColor();
                    var JetMotorDirectionGuide = new TriangleMeshWithColor();
                    RotatedInhabitant.AddRange(part.Triangles);

                    RotatedInhabitant = Rotate3d.RotateZMesh(RotatedInhabitant, inhabitant.Rotation.z);
                    RotatedInhabitant = Rotate3d.RotateYMesh(RotatedInhabitant, inhabitant.Rotation.y);
                    RotatedInhabitant = Rotate3d.RotateXMesh(RotatedInhabitant, inhabitant.Rotation.x);

                    if (part.PartName == "JetMotor")
                    {
                        JetMotor = RotatedInhabitant.First() as TriangleMeshWithColor;
                        inhabitant.Movement.SetStartGuideCoordinates(JetMotor, null);
                    }
                    if (part.PartName == "JetMotorDirectionGuide")
                    {
                        JetMotorDirectionGuide = RotatedInhabitant.First() as TriangleMeshWithColor;
                        inhabitant.Movement.SetStartGuideCoordinates(null, JetMotorDirectionGuide);
                    }

                    //If object has particles, add them to the object
                    if (inhabitant.Particles != null && inhabitant.Particles.Particles.Count > 0)
                    { 
                        var RotatedParticles = inhabitant.Particles.Particles.Select((particle) => new { particleTriangle = particle.ParticleTriangle ,particlePosition = particle.Position, particleRotation = particle.Rotation }).ToList();
                        foreach (var particle in RotatedParticles)
                        {
                            //Make a parent object for the particle
                            var particleObject = new _3dObject();
                            //Make a parent object part for the particle
                            var particleObjectPart = new List<I3dObjectPart>();
                            
                            //Rotate the partcle                            
                            var RotatedParticle = particle.particleTriangle;
                            RotatedParticle = Rotate3d.RotateZMesh(new List<ITriangleMeshWithColor> { RotatedParticle }, particle.particleRotation.z).First();
                            RotatedParticle = Rotate3d.RotateYMesh(new List<ITriangleMeshWithColor> { RotatedParticle }, particle.particleRotation.y).First();
                            RotatedParticle = Rotate3d.RotateXMesh(new List<ITriangleMeshWithColor> { RotatedParticle }, particle.particleRotation.x).First();
                            //Add the needed parts to the particle object
                            particleObjectPart.Add(new _3dObjectPart { Triangles = new List<ITriangleMeshWithColor> { RotatedParticle }, PartName="Particle", IsVisible = true });
                            //Add the parts to the particle object
                            particleObject.ObjectParts = particleObjectPart;
                            particleObject.Position = particle.particlePosition;
                            particleObject.Rotation = particle.particleRotation;
                            //Add the particle object to the list of particle objects
                            particleObjectList.Add(particleObject);
                        }
                    }
                    part.Triangles = RotatedInhabitant;
                }
            }
            //add the particles to the active world if there are any particles
            activeWorld.AddRange(particleObjectList);

            //Calculate perspective and convert from 3d to 2d
            var ScreenCoordinates = From3dTo2d.convertTo2dFromObjects(activeWorld);

            var blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            foreach (var triangle in ScreenCoordinates.OrderBy(z => z.CalculatedZ))
            {
                if (triangle.CalculatedZ > 1000 || triangle.CalculatedZ < (-1000))
                    continue;

                var colorBrush = new SolidColorBrush();
                colorBrush.Color = Helpers.Colors.getShadeOfColorFromNormal(triangle.TriangleAngle, triangle.Color);

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
                    myPoly.Stroke = colorBrush;
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

        public void ClearCanvasPolygons()
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
