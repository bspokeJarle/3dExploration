using _3dTesting._3dWorld;
using _3dTesting.Helpers;
using Domain;
using GameAiAndControls.Controls;
using STL_Tools;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public class Seeder
    {
        public static _3dObject CreateSeeder(ISurface parentSurface)
        {
            var modelReader = new STLReader("C:\\Users\\jarle\\Documents\\Privat\\Bspoke prosjekter\\3dProsjekt\\3dProsjekt\\3dTesting\\3d objects\\div\\seeder_ball.stl");
            var seederTriangles = _3dObjectHelpers.ConvertToTrianglesWithColor(modelReader.ReadFile().ToList(), "FF6644");
            var whiteTriangles = new List<ITriangleMeshWithColor>();
            var blueTriangles = new List<ITriangleMeshWithColor>();

            var indx = 0;
            foreach (var triangle in seederTriangles)
            {                
                if (indx == 0) whiteTriangles.Add(new TriangleMeshWithColor { vert1 = triangle.vert1, vert2 = triangle.vert2, vert3 = triangle.vert3, Color = "990077" });
                if (indx == 1) blueTriangles.Add(new TriangleMeshWithColor { vert1 = triangle.vert1, vert2 = triangle.vert2, vert3 = triangle.vert3, Color = "0000ff" });
                indx++;
                if (indx>1) indx = 0;
            }

            // Add orb as an inhabitant
            var seeder = new _3dObject();
            var seederCrashBox = SeederCrashBoxes();
            var seederGuide = ParticlesDirectionGuide();
            var seederStartGuide = ParticlesStartGuide();

            if (seederGuide != null) seeder.ObjectParts.Add(new _3dObjectPart { PartName = "SeederParticlesGuide", Triangles = seederGuide, IsVisible = false });
            if (seederStartGuide != null) seeder.ObjectParts.Add(new _3dObjectPart { PartName = "SeederParticlesStartGuide", Triangles = seederStartGuide, IsVisible = false });

            if (whiteTriangles != null) seeder.ObjectParts.Add(new _3dObjectPart { PartName = "SeederWhite", Triangles = whiteTriangles, IsVisible = true });
            if (blueTriangles != null) seeder.ObjectParts.Add(new _3dObjectPart { PartName = "SeederBlue", Triangles = blueTriangles, IsVisible = true });
            
            seeder.Movement = new SeederControls();
            seeder.Particles = new ParticlesAI();
            if (seederCrashBox != null) seeder.CrashBoxes = seederCrashBox;           
            return seeder;
        }

        public static List<ITriangleMeshWithColor>? ParticlesDirectionGuide()
        {
            var direction = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor { noHidden = true, Color = "ffffff", vert1 = { x = 12, y = -10, z = -100 }, vert2 = { x = -12, y = -10, z = -100 }, vert3 = { x = 0, y = -20, z = -100 } },
            };
            return direction;
        }
        public static List<ITriangleMeshWithColor>? ParticlesStartGuide()
        {
            var direction = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor { noHidden = true, Color = "ffffff", vert1 = { x = 12, y = -10, z = -10 }, vert2 = { x = -12, y = -10, z = -10 }, vert3 = { x = 0, y = -20, z = -10 } },
            };
            return direction;
        }

        public static List<List<IVector3>>? SeederCrashBoxes()
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
    }
}
