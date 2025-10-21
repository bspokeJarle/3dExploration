using _3dTesting._3dWorld;
using _3dTesting.Helpers;
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
            var topCannonCrashBox = TopCannonCrashBoxes();
            var cannon = TopCannonTriangles();
            var crashBoxes = new List<List<IVector3>>();
            if (shipCrashBox != null) crashBoxes.AddRange(shipCrashBox);
            if (topCannonCrashBox != null) crashBoxes.AddRange(topCannonCrashBox);

            // Add orb as an inhabitant
            var ship = new _3dObject();
            if (upperTriangles==null||lowerTriangles==null||rearTriangles==null) return ship;
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "UpperPart", Triangles = upperTriangles, IsVisible=true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "LowerPart", Triangles = lowerTriangles, IsVisible = true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "RearPart", Triangles = rearTriangles, IsVisible = true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "JetMotor", Triangles = jetMotorTriangle!, IsVisible = true });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "JetMotorDirectionGuide", Triangles = jetMotorDirectionGuide!, IsVisible = false });
            ship.ObjectParts.Add(new _3dObjectPart { PartName = "TopCannon", Triangles = cannon!, IsVisible = true });

            ship.ObjectOffsets = new Vector3 { };
            ship.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            ship.Movement = new ShipControls();    
            ship.Particles = new ParticlesAI();
            ship.ParentSurface = parentSurface;
            if (shipCrashBox != null) ship.CrashBoxes = crashBoxes;
            return ship; 
        }

        public static List<List<IVector3>>? ShipCrashBoxes()
        {
            var min = new Vector3 { x = -75, y = -50, z = -45 };
            var max = new Vector3 { x = 75, y = 50, z = 45 };

            return new List<List<IVector3>>
            {
                _3dObjectHelpers.GenerateCrashBoxCorners(min, max)
            };
        }

        public static List<List<IVector3>>? TopCannonCrashBoxes()
        {
            var min = new Vector3 { x = -7, y = -45, z = 18 };
            var max = new Vector3 { x = 7, y = 20, z = 38 };

            return new List<List<IVector3>>
            {
                _3dObjectHelpers.GenerateCrashBoxCorners(min, max)
            };
        }

        public static List<ITriangleMeshWithColor>? TopCannonTriangles()
        {
            var tris = new List<ITriangleMeshWithColor>();

            var back = new Vector3 { x = 0f, y = 20f, z = 28f }; // tykk ende
            var mid = new Vector3 { x = 0f, y = -10f, z = 28f }; // mellomring
            var front = new Vector3 { x = 0f, y = -45f, z = 28f }; // tynn ende

            float rxB = 10f, ryB = 5f;
            float rxM = 6f, ryM = 3.5f;
            float rxF = 3f, ryF = 2f;

            float dx = front.x - back.x, dy = front.y - back.y, dz = front.z - back.z;
            float len = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-4f) return tris;
            float wx = dx / len, wy = dy / len, wz = dz / len;

            float tx = 1f, ty = 0f, tz = 0f;
            if (System.Math.Abs(wx) > 0.9f) { tx = 0f; ty = 1f; tz = 0f; }

            float ux = wy * tz - wz * ty;
            float uy = wz * tx - wx * tz;
            float uz = wx * ty - wy * tx;
            float ulen = (float)System.Math.Sqrt(ux * ux + uy * uy + uz * uz);
            ux /= ulen; uy /= ulen; uz /= ulen;

            float vx = wy * uz - wz * uy;
            float vy = wz * ux - wx * uz;
            float vz = wx * uy - wy * ux;
            if (vz > 0f) { vx = -vx; vy = -vy; vz = -vz; }

            float[] ca = { 1f, 0.70710678f, 0f, -0.70710678f, -1f, -0.70710678f, 0f, 0.70710678f };
            float[] sa = { 0f, -0.70710678f, -1f, -0.70710678f, 0f, 0.70710678f, 1f, 0.70710678f };

            Vector3[] B = new Vector3[8];
            Vector3[] M = new Vector3[8];
            Vector3[] F = new Vector3[8];

            for (int i = 0; i < 8; i++)
            {
                B[i] = new Vector3
                {
                    x = back.x + ux * (rxB * ca[i]) + vx * (ryB * sa[i]),
                    y = back.y + uy * (rxB * ca[i]) + vy * (ryB * sa[i]),
                    z = back.z + uz * (rxB * ca[i]) + vz * (ryB * sa[i])
                };
                M[i] = new Vector3
                {
                    x = mid.x + ux * (rxM * ca[i]) + vx * (ryM * sa[i]),
                    y = mid.y + uy * (rxM * ca[i]) + vy * (ryM * sa[i]),
                    z = mid.z + uz * (rxM * ca[i]) + vz * (ryM * sa[i])
                };
                F[i] = new Vector3
                {
                    x = front.x + ux * (rxF * ca[i]) + vx * (ryF * sa[i]),
                    y = front.y + uy * (rxF * ca[i]) + vy * (ryF * sa[i]),
                    z = front.z + uz * (rxF * ca[i]) + vz * (ryF * sa[i])
                };
            }

            string[] backToMidColors = { "8A8A8A", "777777" }; // litt lys/mørk
            string[] midToFrontColors = { "909090", "7A7A7A" };
            string frontCapColor = "B8B8B8";
            string mountColor = "445A77";   // festebrakett: egen (stålblå) farge

            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                string col = backToMidColors[i % 2];
                tris.Add(new TriangleMeshWithColor { Color = col, vert1 = B[i], vert2 = B[j], vert3 = M[j] });
                tris.Add(new TriangleMeshWithColor { Color = col, vert1 = B[i], vert2 = M[j], vert3 = M[i] });
            }

            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                string col = midToFrontColors[i % 2];
                tris.Add(new TriangleMeshWithColor { Color = col, vert1 = M[i], vert2 = M[j], vert3 = F[j] });
                tris.Add(new TriangleMeshWithColor { Color = col, vert1 = M[i], vert2 = F[j], vert3 = F[i] });
            }

            var Fc = new Vector3 { x = front.x + wx * 0.2f, y = front.y + wy * 0.2f, z = front.z + wz * 0.2f };
            int[] idx = { 0, 2, 4, 6 };
            for (int k = 0; k < 4; k++)
            {
                int a = idx[k], b = idx[(k + 1) % 4];
                tris.Add(new TriangleMeshWithColor { Color = frontCapColor, vert1 = F[a], vert2 = F[b], vert3 = Fc });
            }

            float footHalfX = 6f;   // bredde/2 langs u
            float footUp = 2f;   // opp mot kanon (langs -v)
            float footDown = 4.5f; // ned mot skrog (langs +v)
            float footAhead = 6f;   // litt mot front (langs w)

            var T1 = new Vector3 { x = back.x + ux * (+footHalfX) + vx * (-footUp), y = back.y + uy * (+footHalfX) + vy * (-footUp), z = back.z + uz * (+footHalfX) + vz * (-footUp) };
            var T2 = new Vector3 { x = back.x + ux * (-footHalfX) + vx * (-footUp), y = back.y + uy * (-footHalfX) + vy * (-footUp), z = back.z + uz * (-footHalfX) + vz * (-footUp) };
            var T3 = new Vector3 { x = back.x + wx * (footAhead) + vx * (-footUp), y = back.y + wy * (footAhead) + vy * (-footUp), z = back.z + wz * (footAhead) + vz * (-footUp) };

            var B1p = new Vector3 { x = T1.x + vx * (footDown + footUp), y = T1.y + vy * (footDown + footUp), z = T1.z + vz * (footDown + footUp) };
            var B2p = new Vector3 { x = T2.x + vx * (footDown + footUp), y = T2.y + vy * (footDown + footUp), z = T2.z + vz * (footDown + footUp) };
            var B3p = new Vector3 { x = T3.x + vx * (footDown + footUp), y = T3.y + vy * (footDown + footUp), z = T3.z + vz * (footDown + footUp) };

            // Tre sideflater (2 tri hver) + to ender (1 tri hver) = 8 tri
            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T1, vert2 = T2, vert3 = B2p });
            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T1, vert2 = B2p, vert3 = B1p });

            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T2, vert2 = T3, vert3 = B3p });
            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T2, vert2 = B3p, vert3 = B2p });

            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T3, vert2 = T1, vert3 = B1p });
            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T3, vert2 = B1p, vert3 = B3p });

            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = T1, vert2 = T3, vert3 = T2 });   // topp
            tris.Add(new TriangleMeshWithColor { Color = mountColor, vert1 = B1p, vert2 = B2p, vert3 = B3p }); // bunn

            return tris;
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
