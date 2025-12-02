using _3dTesting._3dWorld;
using _3dTesting.Helpers;
using Domain;
using GameAiAndControls.Ai;
using GameAiAndControls.Controls;
using System.Collections.Generic;
using System.Net.Security;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public static class Star
    {
        /// <summary>
        /// Creates a small 3D star object with a minimal number of triangles.
        /// The star is centered around local origin (0,0,0).
        /// Position and rotation should be applied via ObjectOffsets and Rotation when spawning.
        /// </summary>
        public static _3dObject CreateStar(ISurface parentSurface,IVector3 randomOffset, float size = 4f, string colorHex = "FFFFFF")
        {
            var random = new System.Random();
            var starTriangles = BuildStarGeometry(size, colorHex);

            var star = new _3dObject();

            // If something goes wrong, just return an empty object (safe fallback).
            if (starTriangles == null || starTriangles.Count == 0)
                return star;

            star.ObjectParts.Add(new _3dObjectPart
            {
                PartName = "Star_Core",
                Triangles = starTriangles,
                IsVisible = true
            });

            // Placement offset is randomized per star instance.
            star.ObjectOffsets = randomOffset;
            //Fixed for now
            star.Rotation = new Vector3
            {
                x = (float)(random.NextDouble() * 360.0),
                y = (float)(random.NextDouble() * 360.0),
                z = (float)(random.NextDouble() * 360.0)
            };
            //WorldPosition is set to the parent surface's global position.
            star.WorldPosition = parentSurface.GlobalMapPosition;

            // Stars do not need crashboxes – they will not participate in collision.
            star.CrashBoxes = new List<List<IVector3>>();

            // ParentSurface can be null if the star is not tied to any surface.
            star.ParentSurface = parentSurface;
            star.Movement = new StarsControl();
            return star;
        }

        /// <summary>
        /// Builds a small 3D "spark" / star-shape using a few triangles around origin.
        /// This keeps the triangle count low (well below 10) and is cheap to render.
        /// </summary>
        private static List<ITriangleMeshWithColor> BuildStarGeometry(float size, string colorHex)
        {
            var tris = new List<ITriangleMeshWithColor>();

            // We create three crossed quads (XY, XZ, YZ planes), each split into 2 triangles.
            // Total: 3 quads * 2 triangles = 6 triangles.
            // This gives a simple volumetric star that is visible from most angles.

            float s = size;

            // --- Quad 1: XZ-plane (flat in Y) ---
            var q1_v1 = new Vector3 { x = -s, y = 0f, z = -s };
            var q1_v2 = new Vector3 { x = s, y = 0f, z = -s };
            var q1_v3 = new Vector3 { x = s, y = 0f, z = s };
            var q1_v4 = new Vector3 { x = -s, y = 0f, z = s };

            tris.Add(new TriangleMeshWithColor { Color = colorHex, vert1 = q1_v1, vert2 = q1_v2, vert3 = q1_v3 });
            tris.Add(new TriangleMeshWithColor { Color = colorHex, vert1 = q1_v1, vert2 = q1_v3, vert3 = q1_v4 });

            // --- Quad 2: YZ-plane (flat in X) ---
            var q2_v1 = new Vector3 { x = 0f, y = -s, z = -s };
            var q2_v2 = new Vector3 { x = 0f, y = s, z = -s };
            var q2_v3 = new Vector3 { x = 0f, y = s, z = s };
            var q2_v4 = new Vector3 { x = 0f, y = -s, z = s };

            tris.Add(new TriangleMeshWithColor { Color = colorHex, vert1 = q2_v1, vert2 = q2_v2, vert3 = q2_v3 });
            tris.Add(new TriangleMeshWithColor { Color = colorHex, vert1 = q2_v1, vert2 = q2_v3, vert3 = q2_v4 });

            // --- Quad 3: XY-plane (flat in Z) ---
            var q3_v1 = new Vector3 { x = -s, y = -s, z = 0f };
            var q3_v2 = new Vector3 { x = s, y = -s, z = 0f };
            var q3_v3 = new Vector3 { x = s, y = s, z = 0f };
            var q3_v4 = new Vector3 { x = -s, y = s, z = 0f };

            tris.Add(new TriangleMeshWithColor { Color = colorHex, vert1 = q3_v1, vert2 = q3_v2, vert3 = q3_v3 });
            tris.Add(new TriangleMeshWithColor { Color = colorHex, vert1 = q3_v1, vert2 = q3_v3, vert3 = q3_v4 });

            return tris;
        }
    }
}
