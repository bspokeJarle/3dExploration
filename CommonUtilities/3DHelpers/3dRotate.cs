using Domain;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace CommonUtilities._3DHelpers
{
    public class _3dRotationCommon
    {
        private static readonly System.Numerics.Vector3 LightVector = new System.Numerics.Vector3(0, 0, 250);
        private static readonly System.Numerics.Vector3 LightDir = System.Numerics.Vector3.Normalize(LightVector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculateAngle(System.Numerics.Vector3 normal)
        {
            return System.Numerics.Vector3.Dot(LightDir, normal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private System.Numerics.Vector3 Rotate(System.Numerics.Vector3 v, float cosRes, float sinRes, char axis)
        {
            return axis switch
            {
                'X' => new System.Numerics.Vector3(v.X, v.Y * cosRes - v.Z * sinRes, v.Z * cosRes + v.Y * sinRes),
                'Y' => new System.Numerics.Vector3(v.X * cosRes + v.Z * sinRes, v.Y, v.Z * cosRes - v.X * sinRes),
                'Z' => new System.Numerics.Vector3(v.X * cosRes - v.Y * sinRes, v.Y * cosRes + v.X * sinRes, v.Z),
                _ => v
            };
        }

        private TriangleMeshWithColor RotateTriangle(TriangleMeshWithColor coord, float cosRes, float sinRes, char axis)
        {
            var rotated = new TriangleMeshWithColor
            {
                Color = coord.Color,
                noHidden = coord.noHidden,
                landBasedPosition = coord.landBasedPosition
            };

            RotateToDomain((Vector3)coord.vert1, (Vector3)rotated.vert1, cosRes, sinRes, axis);
            RotateToDomain((Vector3)coord.vert2, (Vector3)rotated.vert2, cosRes, sinRes, axis);
            RotateToDomain((Vector3)coord.vert3, (Vector3)rotated.vert3, cosRes, sinRes, axis);

            return CalculateNormalAndAngle(rotated);
        }

        private TriangleMeshWithColor CalculateNormalAndAngle(TriangleMeshWithColor coord)
        {
            var v1 = (Vector3)coord.vert1;
            var v2 = (Vector3)coord.vert2;
            var v3 = (Vector3)coord.vert3;

            float ux = v2.x - v1.x;
            float uy = v2.y - v1.y;
            float uz = v2.z - v1.z;

            float vx = v3.x - v1.x;
            float vy = v3.y - v1.y;
            float vz = v3.z - v1.z;

            float nx = uy * vz - uz * vy;
            float ny = uz * vx - ux * vz;
            float nz = ux * vy - uy * vx;

            float normalLength = MathF.Max(1e-6f, MathF.Sqrt(nx * nx + ny * ny + nz * nz));
            float invLength = 1f / normalLength;

            nx *= invLength;
            ny *= invLength;
            nz *= invLength;

            if (coord.normal1 is Vector3 normal)
            {
                normal.x = nx;
                normal.y = ny;
                normal.z = nz;
            }
            else
            {
                coord.normal1 = new Vector3 { x = nx, y = ny, z = nz };
            }

            coord.angle = (LightDir.X * nx) + (LightDir.Y * ny) + (LightDir.Z * nz);
            return coord;
        }

        private static Vector3 ConvertToDomainVector(System.Numerics.Vector3 v) =>
            new Vector3 { x = v.X, y = v.Y, z = v.Z };

        private static void RotateToDomainX(Vector3 v, Vector3 target, float cosRes, float sinRes)
        {
            target.x = v.x;
            target.y = v.y * cosRes - v.z * sinRes;
            target.z = v.z * cosRes + v.y * sinRes;
        }

        private static void RotateToDomainY(Vector3 v, Vector3 target, float cosRes, float sinRes)
        {
            target.x = v.x * cosRes + v.z * sinRes;
            target.y = v.y;
            target.z = v.z * cosRes - v.x * sinRes;
        }

        private static void RotateToDomainZ(Vector3 v, Vector3 target, float cosRes, float sinRes)
        {
            target.x = v.x * cosRes - v.y * sinRes;
            target.y = v.y * cosRes + v.x * sinRes;
            target.z = v.z;
        }

        private static void CopyToDomain(Vector3 v, Vector3 target)
        {
            target.x = v.x;
            target.y = v.y;
            target.z = v.z;
        }

        private static void RotateToDomain(Vector3 v, Vector3 target, float cosRes, float sinRes, char axis)
        {
            switch (axis)
            {
                case 'X':
                    RotateToDomainX(v, target, cosRes, sinRes);
                    break;
                case 'Y':
                    RotateToDomainY(v, target, cosRes, sinRes);
                    break;
                case 'Z':
                    RotateToDomainZ(v, target, cosRes, sinRes);
                    break;
                default:
                    CopyToDomain(v, target);
                    break;
            }
        }

        private static Vector3 RotateToDomain(Vector3 v, float cosRes, float sinRes, char axis)
        {
            var rotated = new Vector3();
            RotateToDomain(v, rotated, cosRes, sinRes, axis);
            return rotated;
        }

        // ✅ Backward Compatible Methods (Internally Call Optimized Code)
        public TriangleMeshWithColor RotateOnX(float cosRes, float sinRes, TriangleMeshWithColor coord) =>
            RotateTriangle(coord, cosRes, sinRes, 'X');

        public TriangleMeshWithColor RotateOnY(float cosRes, float sinRes, TriangleMeshWithColor coord) =>
            RotateTriangle(coord, cosRes, sinRes, 'Y');

        public TriangleMeshWithColor RotateOnZ(float cosRes, float sinRes, TriangleMeshWithColor coord) =>
            RotateTriangle(coord, cosRes, sinRes, 'Z');

        public List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, double angle, char axis)
        {
            var radian = Math.PI * angle / 180.0;
            var cosRes = (float)Math.Cos(radian);
            var sinRes = (float)Math.Sin(radian);

            var rotatedMesh = new List<ITriangleMeshWithColor>(mesh.Count);
            for (int i = 0; i < mesh.Count; i++)
            {
                var triangle = (TriangleMeshWithColor)mesh[i];
                if (triangle.vert1 == null || triangle.vert2 == null || triangle.vert3 == null)
                {
                    Console.WriteLine("Warning: Skipping uninitialized triangle");
                    continue;
                }
                rotatedMesh.Add(RotateTriangle(triangle, cosRes, sinRes, axis));
            }
            return rotatedMesh;
        }

        public List<ITriangleMeshWithColor> RotateXMesh(List<ITriangleMeshWithColor> X, double angle) =>
            RotateMesh(X, angle, 'X');

        public List<ITriangleMeshWithColor> RotateYMesh(List<ITriangleMeshWithColor> Y, double angle) =>
            RotateMesh(Y, angle, 'Y');

        public List<ITriangleMeshWithColor> RotateZMesh(List<ITriangleMeshWithColor> Z, double angle) =>
            RotateMesh(Z, angle, 'Z');

        public IVector3 RotatePoint(double angleInDegrees, IVector3 coord, char axis)
        {
            double radians = Math.PI * angleInDegrees / 180.0;
            float cosRes = (float)Math.Cos(radians);
            float sinRes = (float)Math.Sin(radians);

            return RotateToDomain((Vector3)coord, cosRes, sinRes, axis);
        }
    }
}
