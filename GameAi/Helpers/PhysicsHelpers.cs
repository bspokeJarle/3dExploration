using Domain;
using System;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Helpers
{
    public class PhysicsHelpers
    {
        public static IVector3 Subtract(IVector3 a, IVector3 b)
        {
            return new Vector3(
                a.x - b.x,
                a.y - b.y,
                a.z - b.z
            );
        }

        public static IVector3 Add(IVector3 a, IVector3 b)
        {
            return new Vector3(
                a.x + b.x,
                a.y + b.y,
                a.z + b.z
            );
        }

        public static IVector3 Multiply(IVector3 v, float scalar)
        {
            return new Vector3(
                v.x * scalar,
                v.y * scalar,
                v.z * scalar
            );
        }

        public static IVector3 Normalize(IVector3 v)
        {
            float length = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            if (length == 0) return new Vector3(0, 0, 0);
            return new Vector3(v.x / length, v.y / length, v.z / length);
        }

        public static double GetLength(Vector3 point1, Vector3 point2)
        {
            float dx = point1.x - point2.x;
            float dy = point1.y - point2.y;
            float dz = point1.z - point2.z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public static float Dot(IVector3 a, IVector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static float Length(IVector3 v)
        {
            return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        public static IVector3 ClampMagnitude(IVector3 v, float maxLength)
        {
            float length = Length(v);
            if (length > maxLength)
            {
                float scale = maxLength / length;
                return Multiply(v, scale);
            }
            return v;
        }
        public static IVector3 ReflectVelocity(IVector3 velocity, IVector3 normal, float bounceFactor)
        {
            float dot = Dot(velocity, normal);
            var reflected = Subtract(velocity, Multiply(normal, 2 * dot));
            return Multiply(reflected, bounceFactor);
        }

        public static IVector3 GetTriangleCenter(TriangleMeshWithColor tri)
        {
            return new Vector3(
                (tri.vert1.x + tri.vert2.x + tri.vert3.x) / 3f,
                (tri.vert1.y + tri.vert2.y + tri.vert3.y) / 3f,
                (tri.vert1.z + tri.vert2.z + tri.vert3.z) / 3f
            );
        }

        private static readonly Random _random = new();

        public static IVector3 RandomUnitVector()
        {
            float x = (float)(_random.NextDouble() * 2 - 1);
            float y = (float)(_random.NextDouble() * 2 - 1);
            float z = (float)(_random.NextDouble() * 2 - 1);
            var vec = new Vector3(x, y, z);
            return Normalize(vec);
        }

        public static IVector3 RotateAroundAxis(IVector3 point, IVector3 axis, float angleDegrees, IVector3 origin)
        {
            float angleRad = angleDegrees * (MathF.PI / 180f);
            axis = Normalize(axis);

            // Move so the rotationpoint is at the origin
            IVector3 translated = Subtract(point,origin);

            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);

            IVector3 rotated = new Vector3
            {
                x = (cos + (1 - cos) * axis.x * axis.x) * translated.x +
                    ((1 - cos) * axis.x * axis.y - axis.z * sin) * translated.y +
                    ((1 - cos) * axis.x * axis.z + axis.y * sin) * translated.z,

                y = ((1 - cos) * axis.y * axis.x + axis.z * sin) * translated.x +
                    (cos + (1 - cos) * axis.y * axis.y) * translated.y +
                    ((1 - cos) * axis.y * axis.z - axis.x * sin) * translated.z,

                z = ((1 - cos) * axis.z * axis.x - axis.y * sin) * translated.x +
                    ((1 - cos) * axis.z * axis.y + axis.x * sin) * translated.y +
                    (cos + (1 - cos) * axis.z * axis.z) * translated.z
            };

            return Add(rotated,origin);
        }

        public static IVector3 CalculateTriangleGeometryCenter(I3dObject obj)
        {
            float sumX = 0f, sumY = 0f, sumZ = 0f;
            int count = 0;

            foreach (var part in obj.ObjectParts)
            {
                foreach (var tri in part.Triangles)
                {
                    var c = PhysicsHelpers.GetTriangleCenter((TriangleMeshWithColor)tri);
                    sumX += c.x;
                    sumY += c.y;
                    sumZ += c.z;
                    count++;
                }
            }

            if (count == 0) return new Vector3(0, 0, 0);
            return new Vector3(sumX / count, sumY / count, sumZ / count);
        }

        public static class RandomHelper
        {
            private static readonly Random _random = new();

            public static float Float(float min, float max)
            {
                return (float)(_random.NextDouble() * (max - min) + min);
            }
        }

    }
}
