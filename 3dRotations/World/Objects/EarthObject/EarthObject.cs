using System;
using System.Collections.Generic;
using System.Globalization;
using CommonUtilities.CommonGlobalState;
using Domain;
using static Domain._3dSpecificsImplementations;
using MathF = System.MathF;

namespace _3dRotations.World.Objects.EarthObject
{
    public static class EarthObject
    {
        private const float RenderDepth = 520f;
        private const float RenderYOffset = 0f;
        private const float CrashboxRadius = 200f;
        private const float CrashboxScale = 1.04f;

        private const int StarCount = 150;
        private const float StarFieldRadius = 280f;   // distance from globe centre
        private const float StarSize = 5f;

        private static readonly string[] StarColors = { "FFFFFF", "FFF7CC", "CCE5FF", "FFD8D8", "E6FFE6" };
        private static readonly Random Rng = new Random(42);

        public static _3dObject CreateEarth()
        {
            var parts = new List<I3dObjectPart>
            {
                CreatePart("EarthGlobe", ParseTriangles(EarthModelData.EarthTrianglesData))
            };

            // Add stars as extra parts so they rotate with the globe
            for (int i = 0; i < StarCount; i++)
                parts.Add(CreateStarPart(i));

            var obj = new _3dObject
            {
                ObjectId = GameState.ObjectIdCounter++,
                ObjectName = "Earth",
                ObjectParts = parts,
                CrashBoxes = BuildCrashBoxes(CrashboxRadius, CrashboxScale),
                Rotation = new Vector3 { x = 70f, y = 0f, z = 90f },
                WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f },
                ObjectOffsets = new Vector3
                {
                    x = 0f,
                    y = RenderYOffset,
                    z = RenderDepth
                },
                IsActive = true
            };

            obj.ImpactStatus = new ImpactStatus();
            return obj;
        }

        private static _3dObjectPart CreateStarPart(int index)
        {
            // Evenly distribute stars across the sphere surface using the Fibonacci sphere method
            float goldenAngle = MathF.PI * (3f - MathF.Sqrt(5f));
            float t = (index + 0.5f) / StarCount;
            float inclination = MathF.Acos(1f - 2f * t);
            float azimuth = goldenAngle * index;

            float r = StarFieldRadius;
            float cx = r * MathF.Sin(inclination) * MathF.Cos(azimuth);
            float cy = r * MathF.Sin(inclination) * MathF.Sin(azimuth);
            float cz = r * MathF.Cos(inclination);

            // Random size variation and color
            float size = StarSize * (0.5f + (float)Rng.NextDouble() * 1.0f);
            string color = StarColors[Rng.Next(StarColors.Length)];

            var tris = BuildStarTriangles(cx, cy, cz, size, color);

            return new _3dObjectPart
            {
                PartName = $"Star_{index}",
                Triangles = tris,
                IsVisible = true
            };
        }

        private static List<ITriangleMeshWithColor> BuildStarTriangles(float cx, float cy, float cz, float size, string color)
        {
            var tris = new List<ITriangleMeshWithColor>();
            float h = size * 0.5f;
            float w = size * 0.12f;

            // Two crossing quads (XY plane and XZ plane) for a sparkle at (cx,cy,cz)
            // XY quad arms
            tris.Add(MakeStarTri(
                new Vector3 { x = cx - w, y = cy - h, z = cz },
                new Vector3 { x = cx + w, y = cy - h, z = cz },
                new Vector3 { x = cx,     y = cy + h, z = cz },
                color));
            tris.Add(MakeStarTri(
                new Vector3 { x = cx - h, y = cy - w, z = cz },
                new Vector3 { x = cx + h, y = cy - w, z = cz },
                new Vector3 { x = cx,     y = cy + w, z = cz },
                color));

            return tris;
        }

        private static TriangleMeshWithColor MakeStarTri(Vector3 v1, Vector3 v2, Vector3 v3, string color)
        {
            return new TriangleMeshWithColor
            {
                Color = color,
                vert1 = v1,
                vert2 = v2,
                vert3 = v3,
                noHidden = true
            };
        }

        private static List<ITriangleMeshWithColor> ParseTriangles(string data)
        {
            var result = new List<ITriangleMeshWithColor>(EarthModelData.TriangleCount);
            var lines = data.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                var colorSplit = line.Split('|', StringSplitOptions.TrimEntries);
                if (colorSplit.Length != 2)
                    throw new FormatException($"Invalid Earth triangle data line: {line}");

                var vertices = colorSplit[0].Split(';', StringSplitOptions.TrimEntries);
                if (vertices.Length != 3)
                    throw new FormatException($"Invalid Earth triangle vertex count: {line}");

                AddTriangle(
                    result,
                    ParseVertex(vertices[0]),
                    ParseVertex(vertices[1]),
                    ParseVertex(vertices[2]),
                    $"#{colorSplit[1]}");
            }

            return result;
        }

        private static Vector3 ParseVertex(string value)
        {
            var parts = value.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
                throw new FormatException($"Invalid Earth vertex data: {value}");

            return new Vector3
            {
                x = float.Parse(parts[0], CultureInfo.InvariantCulture),
                y = float.Parse(parts[1], CultureInfo.InvariantCulture),
                z = float.Parse(parts[2], CultureInfo.InvariantCulture)
            };
        }

        private static void AddTriangle(List<ITriangleMeshWithColor> result, Vector3 v1, Vector3 v2, Vector3 v3, string color)
        {
            var normal = CalculateNormal(v1, v2, v3);
            var center = new Vector3
            {
                x = (v1.x + v2.x + v3.x) / 3f,
                y = (v1.y + v2.y + v3.y) / 3f,
                z = (v1.z + v2.z + v3.z) / 3f
            };

            float outwardDot = (normal.x * center.x) + (normal.y * center.y) + (normal.z * center.z);
            if (outwardDot < 0f)
            {
                (v2, v3) = (v3, v2);
                normal.x = -normal.x;
                normal.y = -normal.y;
                normal.z = -normal.z;
            }

            result.Add(new TriangleMeshWithColor
            {
                Color = color,
                vert1 = v1,
                vert2 = v2,
                vert3 = v3,
                normal1 = normal,
                normal2 = normal,
                normal3 = normal
            });
        }

        private static Vector3 CalculateNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var edge1 = new Vector3 { x = v2.x - v1.x, y = v2.y - v1.y, z = v2.z - v1.z };
            var edge2 = new Vector3 { x = v3.x - v1.x, y = v3.y - v1.y, z = v3.z - v1.z };
            var normal = new Vector3
            {
                x = edge1.y * edge2.z - edge1.z * edge2.y,
                y = edge1.z * edge2.x - edge1.x * edge2.z,
                z = edge1.x * edge2.y - edge1.y * edge2.x
            };

            float len = MathF.Sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
            if (len > 0f)
            {
                normal.x /= len;
                normal.y /= len;
                normal.z /= len;
            }

            return normal;
        }

        private static List<List<IVector3>> BuildCrashBoxes(float radius, float scale)
        {
            float h = radius * scale;
            var box = new List<IVector3>
            {
                new Vector3 { x = -h, y = -h, z = -h },
                new Vector3 { x =  h, y = -h, z = -h },
                new Vector3 { x =  h, y =  h, z = -h },
                new Vector3 { x = -h, y =  h, z = -h },
                new Vector3 { x = -h, y = -h, z =  h },
                new Vector3 { x =  h, y = -h, z =  h },
                new Vector3 { x =  h, y =  h, z =  h },
                new Vector3 { x = -h, y =  h, z =  h }
            };

            return new List<List<IVector3>> { box };
        }

        private static _3dObjectPart CreatePart(string name, List<ITriangleMeshWithColor> triangles)
        {
            return new _3dObjectPart
            {
                PartName = name,
                Triangles = triangles,
                IsVisible = true
            };
        }
    }
}
