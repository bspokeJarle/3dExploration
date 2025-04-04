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
    }
}
