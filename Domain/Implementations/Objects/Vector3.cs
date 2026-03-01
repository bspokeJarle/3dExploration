namespace Domain
{
    public partial class _3dSpecificsImplementations
    {
        public class Vector3 : IVector3
        {
            public Vector3(float xVal = 0, float yVal = 0, float zVal = 0)
            {
                x = xVal;
                y = yVal;
                z = zVal;
            }

            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
            public override string ToString() => $"(x={x:F2}, y={y:F2}, z={z:F2})";

            public static Vector3 operator -(Vector3 a, Vector3 b)
                => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);

            public static Vector3 operator +(Vector3 a, Vector3 b)
                => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);

            public static Vector3 operator *(Vector3 v, float s)
                => new Vector3(v.x * s, v.y * s, v.z * s);

            public static Vector3 operator *(float s, Vector3 v)
                => new Vector3(v.x * s, v.y * s, v.z * s);
        }
    }
}
