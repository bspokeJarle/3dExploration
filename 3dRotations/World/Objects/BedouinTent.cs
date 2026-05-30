using _3dTesting.Helpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAiAndControls.Controls;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public static class BedouinTent
    {
        public static _3dObject CreateBedouinTent(ISurface parentSurface)
        {
            var tent = new _3dObject
            {
                ObjectId = GameState.ObjectIdCounter++,
                ObjectName = "BedouinTent",
                HasShadow = true,
                ObjectOffsets = new Vector3 { },
                Rotation = new Vector3 { x = 0, y = 0, z = 0 },
                ParentSurface = parentSurface,
                Movement = new BedouinTentControls(),
                ShadowOffset = new Vector3 { x = -9f, y = 0f, z = -8f }
            };

            AddPart(tent, "TentCanvas", TentCanvas(), true);
            AddPart(tent, "TentDarkSide", TentDarkSide(), true);
            AddPart(tent, "TentEntrance", TentEntrance(), true);
            AddPart(tent, "TentPoles", TentPoles(), true);

            tent.CrashBoxes = new List<List<IVector3>>
            {
                _3dObjectHelpers.GenerateCrashBoxCorners(
                    new Vector3 { x = -24f, y = -14f, z = 1f },
                    new Vector3 { x = 24f, y = 14f, z = 22f })
            };
            tent.CrashBoxNames = new List<string?> { "BedouinTentBody" };

            _3dObjectHelpers.AddCustomShadowPart(tent, TentShadow());

            return tent;
        }

        private static void AddPart(_3dObject obj, string name, List<ITriangleMeshWithColor> triangles, bool visible)
        {
            obj.ObjectParts.Add(new _3dObjectPart
            {
                PartName = name,
                Triangles = triangles,
                IsVisible = visible
            });
        }

        private static List<ITriangleMeshWithColor> TentCanvas()
        {
            const string light = "B98B58";
            const string mid = "8F633C";
            const string seam = "6F472B";

            var backLeft = new Vector3 { x = -30f, y = 18f, z = 0f };
            var backRight = new Vector3 { x = 30f, y = 18f, z = 0f };
            var frontLeft = new Vector3 { x = -34f, y = -18f, z = 0f };
            var frontRight = new Vector3 { x = 34f, y = -18f, z = 0f };
            var ridgeBack = new Vector3 { x = 0f, y = 16f, z = 27f };
            var ridgeFront = new Vector3 { x = 0f, y = -16f, z = 27f };

            return new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor { Color = light, vert1 = frontLeft, vert2 = backLeft, vert3 = ridgeBack },
                new TriangleMeshWithColor { Color = light, vert1 = frontLeft, vert2 = ridgeBack, vert3 = ridgeFront },
                new TriangleMeshWithColor { Color = mid, vert1 = ridgeFront, vert2 = ridgeBack, vert3 = backRight },
                new TriangleMeshWithColor { Color = mid, vert1 = ridgeFront, vert2 = backRight, vert3 = frontRight },
                new TriangleMeshWithColor { Color = seam, vert1 = ridgeFront, vert2 = frontLeft, vert3 = frontRight },
                new TriangleMeshWithColor { Color = seam, vert1 = ridgeBack, vert2 = backRight, vert3 = backLeft }
            };
        }

        private static List<ITriangleMeshWithColor> TentDarkSide()
        {
            const string dark = "5A3925";
            return new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor
                {
                    Color = dark,
                    vert1 = new Vector3 { x = -8f, y = -18.5f, z = 0f },
                    vert2 = new Vector3 { x = 8f, y = -18.5f, z = 0f },
                    vert3 = new Vector3 { x = 0f, y = -18.5f, z = 18f }
                }
            };
        }

        private static List<ITriangleMeshWithColor> TentEntrance()
        {
            const string flap = "C99A62";
            return new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor
                {
                    Color = flap,
                    vert1 = new Vector3 { x = -18f, y = -19f, z = 0f },
                    vert2 = new Vector3 { x = -8f, y = -18.5f, z = 0f },
                    vert3 = new Vector3 { x = 0f, y = -18.5f, z = 18f }
                },
                new TriangleMeshWithColor
                {
                    Color = flap,
                    vert1 = new Vector3 { x = 8f, y = -18.5f, z = 0f },
                    vert2 = new Vector3 { x = 18f, y = -19f, z = 0f },
                    vert3 = new Vector3 { x = 0f, y = -18.5f, z = 18f }
                }
            };
        }

        private static List<ITriangleMeshWithColor> TentPoles()
        {
            const string pole = "3F2A1D";
            return new List<ITriangleMeshWithColor>
            {
                AddPole(-3f, -18f, 0f, -1f, -18f, 28f, pole),
                AddPole(3f, -18f, 0f, 1f, -18f, 28f, pole),
                AddPole(-3f, 18f, 0f, -1f, 18f, 28f, pole),
                AddPole(3f, 18f, 0f, 1f, 18f, 28f, pole)
            };
        }

        private static TriangleMeshWithColor AddPole(float x1, float y1, float z1, float x2, float y2, float z2, string color)
        {
            return new TriangleMeshWithColor
            {
                Color = color,
                vert1 = new Vector3 { x = x1, y = y1, z = z1 },
                vert2 = new Vector3 { x = x2, y = y2, z = z2 },
                vert3 = new Vector3 { x = x1 + 2f, y = y1, z = z1 }
            };
        }

        private static List<ITriangleMeshWithColor> TentShadow()
        {
            const string sc = _3dObjectHelpers.ShadowColorHex;
            var a = new Vector3 { x = -34f, y = -19f, z = 0f };
            var b = new Vector3 { x = 34f, y = -19f, z = 0f };
            var c = new Vector3 { x = 30f, y = 18f, z = 0f };
            var d = new Vector3 { x = -30f, y = 18f, z = 0f };
            var ridge = new Vector3 { x = 0f, y = 0f, z = 27f };

            return new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor { Color = sc, vert1 = a, vert2 = b, vert3 = ridge },
                new TriangleMeshWithColor { Color = sc, vert1 = b, vert2 = c, vert3 = ridge },
                new TriangleMeshWithColor { Color = sc, vert1 = c, vert2 = d, vert3 = ridge },
                new TriangleMeshWithColor { Color = sc, vert1 = d, vert2 = a, vert3 = ridge }
            };
        }
    }
}
