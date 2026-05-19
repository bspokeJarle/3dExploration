using CommonUtilities.CommonGlobalState;
using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects.EarthObject
{
    public static class AsteroidObject
    {
        public static _3dObject CreateAsteroid(string[] colorPalette, float size, float startOffsetX, float startOffsetY, float depth, Random rng)
        {
            var tris = BuildAsteroidGeometry(colorPalette, size, rng);

            var obj = new _3dObject
            {
                ObjectId = GameState.ObjectIdCounter++,
                ObjectName = "Asteroid",
                ObjectOffsets = new Vector3 { x = startOffsetX, y = startOffsetY, z = depth },
                Rotation = new Vector3 { x = 70f, y = 0f, z = 90f },
                WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f },
                CrashBoxes = new List<List<IVector3>>(),
                IsActive = true
            };

            obj.ObjectParts.Add(new _3dObjectPart
            {
                PartName = "AsteroidBody",
                Triangles = tris,
                IsVisible = true
            });

            obj.ImpactStatus = new ImpactStatus();
            return obj;
        }

        private static List<ITriangleMeshWithColor> BuildAsteroidGeometry(string[] palette, float size, Random rng)
        {
            float j = size * 0.28f;
            float Jit() => (float)(rng.NextDouble() - 0.5) * j * 2f;

            // Jittered octahedron — gives a lumpy rock shape
            var top    = new Vector3 { x = Jit(),       y = Jit(),       z =  size + Jit() };
            var bottom = new Vector3 { x = Jit(),       y = Jit(),       z = -size + Jit() };
            var left   = new Vector3 { x = -size + Jit(), y = Jit(),     z = Jit() };
            var right  = new Vector3 { x =  size + Jit(), y = Jit(),     z = Jit() };
            var front  = new Vector3 { x = Jit(),       y =  size + Jit(), z = Jit() };
            var back   = new Vector3 { x = Jit(),       y = -size + Jit(), z = Jit() };

            string C() => palette[rng.Next(palette.Length)];

            return new List<ITriangleMeshWithColor>
            {
                Tri(top,    right, front, C()),
                Tri(top,    front, left,  C()),
                Tri(top,    left,  back,  C()),
                Tri(top,    back,  right, C()),
                Tri(bottom, front, right, C()),
                Tri(bottom, left,  front, C()),
                Tri(bottom, back,  left,  C()),
                Tri(bottom, right, back,  C()),
            };
        }

        private static TriangleMeshWithColor Tri(Vector3 v1, Vector3 v2, Vector3 v3, string color) =>
            new TriangleMeshWithColor { Color = color, vert1 = v1, vert2 = v2, vert3 = v3, noHidden = true };
    }
}
