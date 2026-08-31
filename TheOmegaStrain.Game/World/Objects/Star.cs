using TheOmegaStrain.Game.Helpers;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.OmegaEngineAdapters;
using TheOmegaStrain.Domain;
using System.Collections.Generic;

namespace TheOmegaStrain.Game.World.Objects
{
    public static class Star
    {
        // Stars must have real pixel size. The old 0.2f value produced sub-pixel geometry
        // (0.4px arms) that only stayed visible because the WPF renderer stroked every
        // triangle with a 1px pen. Direct3D fills triangles without a stroke, so sub-pixel
        // stars vanish or flicker. At 1.2f the arms span ~4.8px with ~1.15px thickness,
        // which reads as a small cross-shaped star rather than a blob.
        private static readonly float ZoomRatio = 1.2f;

        // Simple color palette for stars (can be tweaked to taste)
        // White, warm white, slightly blue-white, slightly reddish, slightly greenish
        private static readonly string[] StarColors =
        {
            "FFFFFF", // pure white
            "FFF7CC", // warm white / yellowish
            "CCE5FF", // cold blue-white
            "FFD8D8", // soft reddish
            "E6FFE6"  // faint greenish
        };

        // One shared Random instance for all stars (avoids “same seed” problems)
        private static readonly System.Random random = new System.Random();

        /// <summary>
        /// Creates a small 3D star object with a minimal number of triangles.
        /// The star is centered around local origin (0,0,0).
        /// StarFieldHandler owns world-space placement and fading.
        /// </summary>
        public static OmegaObject3D CreateStar(
            ISurface parentSurface,
            IVector3 randomOffset,
            float size = 4f,
            string colorHex = "FFFFFF")
        {
            // Pick a random color from the star palette
            var chosenColor = GetRandomStarColor();

            var starTriangles = BuildStarGeometry(size, chosenColor);
            OmegaObject3DHelpers.ApplyScaleToTriangles(starTriangles, ZoomRatio);
            starTriangles = ApplyRandomOrientation(starTriangles);

            var star = new OmegaObject3D { ObjectId = GameState.ObjectIdCounter++ };

            // If something goes wrong, just return an empty object (safe fallback).
            if (starTriangles == null || starTriangles.Count == 0)
                return star;

            star.ObjectParts.Add(new OmegaObjectPart3D
            {
                PartName = "Star_Core",
                Triangles = starTriangles,
                IsVisible = true
            });

            // StarFieldHandler normally supplies a zero offset and places stars via WorldPosition.
            star.ObjectOffsets = randomOffset;

            // Rotation is baked into the mesh above, so this must stay zero. Stars are added to
            // the render list *after* LiveGameLoop deep-copies the world, which means
            // RotateObjectGeometry would re-rotate the very same triangles every frame and the
            // angle would accumulate - that is what made the starfield spin and shimmer.
            star.Rotation = new Vector3
            {
                x = 0f,
                y = 0f,
                z = 0f
            };

            var surfacePos = GameState.SurfaceState.GlobalMapPosition;
            star.WorldPosition = new Vector3
            {
                x = surfacePos.x,
                y = surfacePos.y,
                z = surfacePos.z
            };

            // Stars do not need crashboxes – they will not participate in collision.
            star.CrashBoxes = new List<List<IVector3>>();

            // ParentSurface can be null if the star is not tied to any surface.
            star.ParentSurface = parentSurface;
            star.Movement = null;

            return star;
        }

        /// <summary>
        /// Bakes a fixed random orientation into the star mesh. This is done once at creation
        /// instead of via <c>Rotation</c>, because stars bypass the per-frame deep copy and would
        /// otherwise have the same rotation re-applied to the same triangles on every frame.
        /// Axis order matches the frame transformer (Z then Y then X).
        /// </summary>
        private static List<ITriangleMeshWithColorAndTexture> ApplyRandomOrientation(
            List<ITriangleMeshWithColorAndTexture> triangles)
        {
            var rotate = new OmegaMeshRotation();
            var rotated = rotate.RotateMesh(triangles, random.NextDouble() * 360.0, 'Z');
            rotated = rotate.RotateMesh(rotated, random.NextDouble() * 360.0, 'Y');
            return rotate.RotateMesh(rotated, random.NextDouble() * 360.0, 'X');
        }

        private static string GetRandomStarColor()
        {
            int index = random.Next(StarColors.Length);
            return StarColors[index];
        }

        /// <summary>
        /// Builds a small 3D "spark" / star shape using a few triangles around origin.
        /// This keeps the triangle count low (well below 10) and is cheap to render.
        /// </summary>
        private static List<ITriangleMeshWithColorAndTexture> BuildStarGeometry(float size, string colorHex)
        {
            var tris = new List<ITriangleMeshWithColorAndTexture>();

            // Three / four crossing “arms” as long, thin triangles in the XY-plane
            int armCount = 4;          // set to 3 if you want to test a 3-armed star
            float halfLength = size * 0.38f;
            float halfWidth = size * 0.2f;  // wider base relative to length = softer, less spiky arms

            var center = new Vector3 { x = 0f, y = 0f, z = 0f };

            for (int i = 0; i < armCount; i++)
            {
                float angle = (float)(2.0 * System.Math.PI * i / armCount);

                float dx = (float)System.Math.Cos(angle);
                float dy = (float)System.Math.Sin(angle);

                var tip = new Vector3
                {
                    x = dx * halfLength,
                    y = dy * halfLength,
                    z = 0f
                };

                // Perpendicular vector for arm width near the center
                float px = -dy;
                float py = dx;

                var baseLeft = new Vector3
                {
                    x = center.x + px * halfWidth,
                    y = center.y + py * halfWidth,
                    z = 0f
                };

                var baseRight = new Vector3
                {
                    x = center.x - px * halfWidth,
                    y = center.y - py * halfWidth,
                    z = 0f
                };

                tris.Add(new TriangleMeshWithColor
                {
                    Color = colorHex,
                    vert1 = baseLeft,
                    vert2 = tip,
                    vert3 = baseRight,
                    noHidden = true
                });
            }

            return tris;
        }
    }
}
