using _3dTesting._3dWorld;
using Domain;
using GameAiAndControls.Ai;
using GameAiAndControls.Controls;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public static class Ship
    {
        public static _3dObject CreateShip(ISurface parentSurface)
        {
            //var modelReader = new STLReader("C:\\Users\\kh979\\Documents\\Privat\\Bspoke prosjekter\\3dProsjekt\\3dProsjekt\\3dTesting\\3d objects\\div\\complexorb.stl");
            //var triangles = _3dObjectHelpers.ConvertToTrianglesWithColor(modelReader.ReadFile().ToList(), "FF6644");           

            var upperTriangles = UpperTriangles();
            var lowerTriangles = LowerTriangles();
            var rearTriangles = RearTriangles();
            var jetMotorTriangle = JetMotorTriangle();
            var jetMotorDirectionGuide = JetMotorDirectionGuide();
            var shipCrashBox = ShipCrashBoxes();

            // Add orb as an inhabitant
            var ship = new _3dObject();
            if (upperTriangles==null||lowerTriangles==null||rearTriangles==null) return ship;
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "UpperPart", Triangles = upperTriangles, IsVisible=true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "LowerPart", Triangles = lowerTriangles, IsVisible = true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "RearPart", Triangles = rearTriangles, IsVisible = true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "JetMotor", Triangles = jetMotorTriangle!, IsVisible = true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "JetMotorDirectionGuide", Triangles = jetMotorDirectionGuide!, IsVisible = false });

            ship.Position = new Vector3 { };
            ship.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            ship.Movement = new ShipControls();    
            ship.Particles = new ParticlesAI();
            ship.ParentSurface = parentSurface;
            if (shipCrashBox != null) ship.CrashBoxes = shipCrashBox;
            return ship; 
        }

        public static List<List<IVector3>>? ShipCrashBoxes()
        {
            //List of crash boxes for the ship, min, max
            return new List<List<IVector3>>
            {
                //TODO: Maybe use three crashboxes in time, for now only one
                new List<IVector3>
                {
                    new Vector3 { x = -65, y = -45, z = -45 },
                    new Vector3 { x = 65, y = 45, z = 45 },
                }
            };
        }

        public static List<ITriangleMeshWithColor>? UpperTriangles()
        {
            var upper = new List<ITriangleMeshWithColor>
            {
                //Three upper triangles
                new TriangleMeshWithColor { Color = "007700", vert1 = { x = -50, y = -50, z = 0 }, vert2 = { x = 0, y = 50, z = 25 }, vert3 = { x = -65, y = 45, z = 0 } },                
                new TriangleMeshWithColor { Color = "00ff00", vert1 = { x = 0, y = 50, z = 25 }, vert2 = { x = -50, y = -50, z = 0 }, vert3 = { x = 50, y = -50, z = 0 } },
                new TriangleMeshWithColor { Color = "007700", vert1 = { x = 0, y = 50, z = 25 }, vert2 =  { x = 50, y = -50, z = 0 } , vert3 = { x = 65, y = 45, z = 0 } },
            };
            return upper;
        }
        public static List<ITriangleMeshWithColor>? LowerTriangles()
        {
            var lower = new List<ITriangleMeshWithColor>
            {
                //Six bottom triangles
                new TriangleMeshWithColor { Color = "007799", vert1 = { x = -65, y = 45, z = 0 } , vert2 = { x = 0, y = 50, z = -25 }, vert3 = { x = -50, y = -50, z = 0 } },                
                new TriangleMeshWithColor { Color = "007799", vert1 = { x = 50, y = -50, z = 0 }, vert2 = { x = 0, y = 50, z = -25 } , vert3 = { x = 65, y = 45, z = 0 } },

                new TriangleMeshWithColor { Color = "00ff99", vert1 = { x = -50, y = -50, z = 0 } , vert2 = { x = -25, y = 0, z = -12 } , vert3 = { x = 0, y = -50, z = 0 } },
                new TriangleMeshWithColor { Color = "00ff99", vert1 = { x = 0, y = -50, z = 0 } , vert2 = { x = 25, y = 0, z = -12 } , vert3 = { x = 50, y = -50, z = 0 } },
                new TriangleMeshWithColor { Color = "00ff99", vert1 = { x = -25, y = 0, z = -12 } , vert2 = { x = 25, y = 0, z = -12 } , vert3 = { x = 0, y = -50, z = 0 }},                
            };
            return lower;
        }
        public static List<ITriangleMeshWithColor>? JetMotorTriangle()
        {
            var jet = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor { Color = "ffff00", vert1 = { x = 25, y = 0, z = -12 }, vert2 = { x = -25, y = 0, z = -12 }, vert3 = { x = 0, y = 50, z = -25 } },
            };
            return jet;
        }

        public static List<ITriangleMeshWithColor>? JetMotorDirectionGuide()
        {
            var jet = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor { Color = "ffffff", vert1 = { x = 12, y = 0, z = -100 }, vert2 = { x = -12, y = 0, z = -100 }, vert3 = { x = 0, y = 50, z = -100 } },
            };
            return jet;
        }

        public static List<ITriangleMeshWithColor>? RearTriangles()
        {
            var rear = new List<ITriangleMeshWithColor>
            {
                //Four back triangles
                new TriangleMeshWithColor { Color = "ff0000", vert1 = { x = 0, y = 50, z = -25 }, vert2 = { x = 0, y = 70, z = 0 }, vert3 =  { x = 65, y = 45, z = 0 } },
                new TriangleMeshWithColor { Color = "ff0000", vert1 = { x = -65, y = 45, z = 0 } , vert2 = { x = 0, y = 70, z = 0 }, vert3 = { x = 0, y = 50, z = -25 } },

                new TriangleMeshWithColor { Color = "ff0000", vert1 = { x = 0, y = 70, z = 0 }, vert2 = { x = 0, y = 50, z = 25 }, vert3 =  { x = 65, y = 45, z = 0 } },
                new TriangleMeshWithColor { Color = "ff0000", vert1 = { x = -65, y = 45, z = 0 } , vert2 = { x = 0, y = 50, z = 25 }, vert3 = { x = 0, y = 70, z = 0 } },
            };
            return rear;
        }
    }
}
