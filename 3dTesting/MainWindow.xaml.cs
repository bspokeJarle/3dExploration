using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();
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

                    HandleParticles(inhabitant, particleObjectList, part, RotatedInhabitant);

                    part.Triangles = RotatedInhabitant;
                }
            }
            //add the particles to the active world if there are any particles
            if (particleObjectList.Count > 0) activeWorld.AddRange(particleObjectList);

            //send the rotated inhabitants for crashtesting
            HandleCrashboxes(activeWorld);

            //Calculate perspective and convert from 3d to 2d
            var ScreenCoordinates = From3dTo2d.convertTo2dFromObjects(activeWorld);
            RenderTriangles(ScreenCoordinates);
        }

        public void RenderTriangles(List<_2dTriangleMesh> ScreenCoordinates)
        {
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

        public void HandleCrashboxes(List<_3dObject> ActiveWorld)
        {
            //Create a list of objects with rotated crashboxes
            Dictionary<List<List<Vector3>>, _3dObject> CrashList = new Dictionary<List<List<Vector3>>, _3dObject>();
            //Go through all crashboxes, rotate them
            foreach (var inhabitant in ActiveWorld)
            {
                //Get the cos and sin results for the rotation on this inhabitant
                var cosResX = inhabitant.Rotation.x.ConvertFromAngleToCosSin();
                var cosResY = inhabitant.Rotation.y.ConvertFromAngleToCosSin();
                var cosResZ = inhabitant.Rotation.z.ConvertFromAngleToCosSin();

                if (inhabitant.CrashBoxes != null)
                {
                    var RotatatedCrashboxesInhabitant = new List<List<Vector3>>();
                    //Rotate the crashboxes on this inhabitant
                    foreach (var Crashbox in inhabitant.CrashBoxes)
                    {
                        var RotatedCrashBoxPoints = new List<Vector3>();
                        foreach (var point in Crashbox)
                        {
                            //Each crashbox has two point, minpoint and maxpoint
                            var RotatedCrashBox = new Vector3();
                            RotatedCrashBox = Rotate3d.RotatePointOnX(cosResX.CosRes, cosResX.SinRes, (Vector3)point);
                            RotatedCrashBox = Rotate3d.RotatePointOnY(cosResY.CosRes, cosResX.SinRes, RotatedCrashBox);
                            RotatedCrashBox = Rotate3d.RotatePointOnZ(cosResZ.CosRes, cosResX.SinRes, RotatedCrashBox);
                            //todo add position to the crashbox, ready for collision testing
                            RotatedCrashBox.x += inhabitant.Position.x;
                            RotatedCrashBox.y += inhabitant.Position.y;
                            RotatedCrashBox.z += inhabitant.Position.z;                            
                            RotatedCrashBoxPoints.Add(RotatedCrashBox);
                        }
                        RotatatedCrashboxesInhabitant.Add(RotatedCrashBoxPoints);
                    }
                    CrashList.Add(RotatatedCrashboxesInhabitant, inhabitant);
                }
            }
            //check for collision with other crashboxes
            foreach (var inhabitant in CrashList)
            {
                foreach (var otherInhabitant in CrashList)
                {
                    if (inhabitant.Value != otherInhabitant.Value)
                    {
                        foreach (var crashbox in inhabitant.Key)
                        {
                            foreach (var otherCrashbox in otherInhabitant.Key)
                            {
                                //If collision, set HasCrashed to true on inhabitants
                                if (_3dObjectHelpers.CheckCollisionBoxVsBox(crashbox, otherCrashbox))
                                {
                                    //Prevent particles from crashing into each other
                                    if (otherInhabitant.Value.ObjectName == "Particle" && inhabitant.Value.ObjectName == "Particle") continue;                                   
                                    //Object decides what to do when it has crashed
                                    //Todo check what type of object it is, and what to do when it has crashed
                                    inhabitant.Value.HasCrashed = true;
                                    otherInhabitant.Value.HasCrashed = true;                                    
                                }
                            }
                        }
                    }
                }
            }


        }


        public void HandleParticles(_3dObject inhabitant, List<_3dObject> particleObjectList, I3dObjectPart part, List<ITriangleMeshWithColor> RotatedInhabitant)
        {
            //If object has particles, add them to the object
            if (inhabitant.Particles != null && inhabitant.Particles.Particles.Count > 0)
            {
                var RotatedParticles = inhabitant.Particles.Particles.Select((particle) => new { particleTriangle = particle.ParticleTriangle, particlePosition = particle.Position, particleRotation = particle.Rotation }).ToList();
                foreach (var particle in RotatedParticles)
                {
                    //Make a parent object for the particle
                    var particleObject = new _3dObject();
                    particleObject.ObjectName = "Particle";
                    //Make a crashbox for the particle
                    particleObject.CrashBoxes = new List<List<IVector3>> { new() { new Vector3 { x = -5, y = -5, z = -5 }, new Vector3 { x = 5, y = 5, z = 5 } } };

                    //Make a parent object part for the particle
                    var particleObjectPart = new List<I3dObjectPart>();

                    //Rotate the partcle                            
                    var RotatedParticle = particle.particleTriangle;
                    RotatedParticle = Rotate3d.RotateZMesh(new List<ITriangleMeshWithColor> { RotatedParticle }, particle.particleRotation.z).First();
                    RotatedParticle = Rotate3d.RotateYMesh(new List<ITriangleMeshWithColor> { RotatedParticle }, particle.particleRotation.y).First();
                    RotatedParticle = Rotate3d.RotateXMesh(new List<ITriangleMeshWithColor> { RotatedParticle }, particle.particleRotation.x).First();
                    //Add the needed parts to the particle object
                    particleObjectPart.Add(new _3dObjectPart { Triangles = new List<ITriangleMeshWithColor> { RotatedParticle }, PartName = "Particle", IsVisible = true });
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
