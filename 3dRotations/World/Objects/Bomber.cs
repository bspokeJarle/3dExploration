using _3dTesting._3dWorld;
using Domain;
using GameAiAndControls.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public class Bomber
    {
        public static _3dObject CreateBomber()
        {
            var upperTriangles = UpperTriangles();

            // Add orb as an inhabitant
            var ship = new _3dObject();
            if (upperTriangles == null) return ship;
            var bomberCrashBox = BomberCrashBoxes();
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "UpperPart", Triangles = upperTriangles, IsVisible = true });

            ship.Position = new Vector3 { x = 0, y = 0, z = 0 };
            ship.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            ship.Movement = new ShipControls();
            ship.Particles = new ParticlesAI();
            if (bomberCrashBox != null) ship.CrashBoxes = bomberCrashBox;
            return ship;
        }

        public static List<List<IVector3>>? BomberCrashBoxes()
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
    }
}
