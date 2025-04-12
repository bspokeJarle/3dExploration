using _3dTesting._3dWorld;
using Domain;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public static class Tree
    {
        private static float trunkRadius = 2.5f; // Defined trunk radius
        private static float trunkHeight = 12f;
        private static float foliageBaseRadius = 8.5f; // Increased foliage mass
        private static float foliageHeight = 3.5f; // Squashed foliage into a denser form

        public static _3dObject CreateTree(ISurface parentSurface)
        {
            var trunkTriangles = TrunkTriangles();
            var foliageTriangles = FoliageTriangles();
            var treeCrashBox = TreeCrashBoxes();
            var tree = new _3dObject();

            if (trunkTriangles != null)
                tree.ObjectParts.Add(new _3dObjectPart { PartName = "TreeTrunk", Triangles = trunkTriangles, IsVisible = true });

            if (foliageTriangles != null)
                tree.ObjectParts.Add(new _3dObjectPart { PartName = "TreeFoliage", Triangles = foliageTriangles, IsVisible = true });

            tree.ObjectOffsets = new Vector3 { };
            tree.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            tree.ParentSurface = parentSurface;

            tree.CrashBoxes = treeCrashBox;
            return tree;
        }

        public static List<ITriangleMeshWithColor>? TrunkTriangles()
        {
            var trunk = new List<ITriangleMeshWithColor>();
            int trunkSegments = 10;
            float radius = 5;
            float height = 15;
            string[] trunkColors = { "8B4513", "A0522D", "6D4C41" }; // Different shades of brown

            for (int i = 0; i < trunkSegments; i++)
            {
                float angle1 = (float)(i * (2 * System.Math.PI / trunkSegments));
                float angle2 = (float)((i + 1) * (2 * System.Math.PI / trunkSegments));
                string color = trunkColors[i % trunkColors.Length];

                var v1 = new Vector3 { x = radius * System.MathF.Cos(angle1), y = radius * System.MathF.Sin(angle1), z = 0 };
                var v2 = new Vector3 { x = radius * System.MathF.Cos(angle2), y = radius * System.MathF.Sin(angle2), z = 0 };
                var v3 = new Vector3 { x = radius * System.MathF.Cos(angle1), y = radius * System.MathF.Sin(angle1), z = height };
                var v4 = new Vector3 { x = radius * System.MathF.Cos(angle2), y = radius * System.MathF.Sin(angle2), z = height };

                trunk.Add(new TriangleMeshWithColor { Color = color, vert1 = v1, vert2 = v2, vert3 = v3 });
                trunk.Add(new TriangleMeshWithColor { Color = color, vert1 = v2, vert2 = v4, vert3 = v3 });
            }
            return trunk;
        }

        public static List<ITriangleMeshWithColor>? FoliageTriangles()
        {
            var foliage = new List<ITriangleMeshWithColor>();
            int foliageLayers = 4;
            float baseRadius = 24;
            float heightStep = 8;
            float topHeight = 45;
            float overlap = 4;
            float initialFoliageHeight = 15;
            string[] foliageColors = { "007700", "008800", "009900" }; // Different shades of green

            for (int layer = 0; layer < foliageLayers; layer++)
            {
                float layerHeight = initialFoliageHeight + (layer * (heightStep - 2));
                float layerRadius = baseRadius * (1.0f - (layer * 0.3f));
                int segments = 10 + (layer * 3);
                string color = foliageColors[layer % foliageColors.Length];

                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (float)(i * (2 * System.Math.PI / segments));
                    float angle2 = (float)((i + 1) * (2 * System.Math.PI / segments));

                    var v1 = new Vector3 { x = layerRadius * System.MathF.Cos(angle1), y = layerRadius * System.MathF.Sin(angle1), z = layerHeight - overlap };
                    var v2 = new Vector3 { x = layerRadius * System.MathF.Cos(angle2), y = layerRadius * System.MathF.Sin(angle2), z = layerHeight - overlap };
                    var tip = new Vector3 { x = 0, y = 0, z = layerHeight + heightStep };

                    foliage.Add(new TriangleMeshWithColor { Color = color, vert1 = v1, vert2 = v2, vert3 = tip });
                }
            }
            return foliage;
        }

        public static List<List<IVector3>>? TreeCrashBoxes()
        {
            float trunkExpand = trunkRadius * 2f;
            float foliageExpand = foliageBaseRadius * 2f;
            float foliageHeightExpanded = foliageHeight * 2f;

            return new List<List<IVector3>>
            {
                new List<IVector3>
                {
                    new Vector3 { x = -trunkExpand, y = -trunkExpand, z = 0 },
                    new Vector3 { x = trunkExpand, y = trunkExpand, z = trunkHeight / 2 }
                },
                new List<IVector3>
                {
                    new Vector3 { x = -trunkExpand - 1, y = -trunkExpand - 1, z = trunkHeight / 2 },
                    new Vector3 { x = trunkExpand + 1, y = trunkExpand + 1, z = trunkHeight }
                },
                new List<IVector3>
                {
                    new Vector3 { x = -foliageExpand, y = -foliageExpand, z = trunkHeight },
                    new Vector3 { x = foliageExpand, y = foliageExpand, z = trunkHeight + foliageHeightExpanded }
                },
                new List<IVector3>
                {
                    new Vector3 { x = -foliageExpand / 2, y = -foliageExpand / 2, z = trunkHeight + foliageHeightExpanded },
                    new Vector3 { x = foliageExpand / 2, y = foliageExpand / 2, z = trunkHeight + foliageHeightExpanded * 1.5f }
                }
            };
        }
    }
}
