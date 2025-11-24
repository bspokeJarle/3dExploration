using Domain;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace CommonUtilities._3DHelpers
{
    public class _3dRotationCommon
    {
        private static readonly System.Numerics.Vector3 LightVector = new System.Numerics.Vector3(0, 0, 250);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculateAngle(System.Numerics.Vector3 normal)
        {
            var lightDir = System.Numerics.Vector3.Normalize(LightVector);
            return System.Numerics.Vector3.Dot(lightDir, normal);
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
            return CalculateNormalAndAngle(new TriangleMeshWithColor
            {
                vert1 = RotateToDomain((Vector3)coord.vert1, cosRes, sinRes, axis),
                vert2 = RotateToDomain((Vector3)coord.vert2, cosRes, sinRes, axis),
                vert3 = RotateToDomain((Vector3)coord.vert3, cosRes, sinRes, axis),
                Color = coord.Color,
                noHidden = coord.noHidden,
                landBasedPosition = coord.landBasedPosition
            });
        }

        private TriangleMeshWithColor CalculateNormalAndAngle(TriangleMeshWithColor coord)
        {
            var U = ConvertToSystemNumerics((Vector3)coord.vert2) - ConvertToSystemNumerics((Vector3)coord.vert1);
            var V = ConvertToSystemNumerics((Vector3)coord.vert3) - ConvertToSystemNumerics((Vector3)coord.vert1);
            var normal = System.Numerics.Vector3.Cross(U, V);
            var normalLength = MathF.Max(1e-6f, normal.Length());

            normal /= normalLength;
            coord.normal1 = ConvertToDomainVector(normal);
            coord.angle = CalculateAngle(normal);
            return coord;
        }

        // Convert Domain.Vector3 <-> System.Numerics.Vector3
        private static System.Numerics.Vector3 ConvertToSystemNumerics(Vector3 v) =>
            new System.Numerics.Vector3(v.x, v.y, v.z);

        private static Vector3 ConvertToDomainVector(System.Numerics.Vector3 v) =>
            new Vector3 { x = v.X, y = v.Y, z = v.Z };

        private static Vector3 RotateToDomain(Vector3 v, float cosRes, float sinRes, char axis)
        {
            var rotated = axis switch
            {
                'X' => new System.Numerics.Vector3(v.x, v.y * cosRes - v.z * sinRes, v.z * cosRes + v.y * sinRes),
                'Y' => new System.Numerics.Vector3(v.x * cosRes + v.z * sinRes, v.y, v.z * cosRes - v.x * sinRes),
                'Z' => new System.Numerics.Vector3(v.x * cosRes - v.y * sinRes, v.y * cosRes + v.x * sinRes, v.z),
                _ => new System.Numerics.Vector3(v.x, v.y, v.z)
            };
            return ConvertToDomainVector(rotated);
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

            var rotatedMesh = new List<ITriangleMeshWithColor>();
            foreach (TriangleMeshWithColor triangle in mesh)
            {
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
