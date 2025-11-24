using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonUtilities.WeaponHelpers.WeaponHelpers;

namespace Domain
{
    public class _3dSpecificsImplementations
    {
        public class ActiveWeapon : IActiveWeapon
        {
            public I3dObject WeaponObject { get; set; }
            public IImpactStatus ImpactStatus { get; set; }
            public DateTime FiredTime { get; set; }
            public float Velocity { get; set; }
            public float Acceleration { get; set; }
            public IVector3 Trajectory { get; set; }
            public IVector3 StartWorldPosition { get; set; }
            public IVector3 CurrentWorldPosition { get; set; }
            public float DistanceTraveled { get; set; }
            public float MaxRange { get; set; }
            public double LifetimeSeconds { get; set; }
            public DateTime LastUpdateUtc { get; set; }
            public WeaponType WeaponType { get; set; }
            IVector3 IActiveWeapon.Velocity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            IVector3 IActiveWeapon.Acceleration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }

        public class _3dObject : I3dObject
        {
            public List<I3dObjectPart> ObjectParts { get; set; } = new();
            public int? RotationOffsetY { get; set; }
            public int? RotationOffsetX { get; set; }
            public int? RotationOffsetZ { get; set; }
            public IVector3? WorldPosition { get; set; }
            public IVector3? Rotation { get; set; }
            public IVector3? ObjectOffsets { get; set; }
            public IVector3? CrashboxOffsets { get; set; }
            public IObjectMovement? Movement { get; set; }
            public IParticles? Particles { get; set; }
            public List<List<IVector3>> CrashBoxes { get; set; }
            public IImpactStatus? ImpactStatus { get; set; }
            public int? Mass { get; set; }
            public string ObjectName { get; set; }
            public ISurface? ParentSurface { get; set; }
            public int? SurfaceBasedId { get; set; }
            public bool? CrashBoxDebugMode { get; set; }
            public IWeapon? WeaponSystems { get; set; }
        }
        public class _3dObjectPart : I3dObjectPart
        {
            public List<ITriangleMeshWithColor> Triangles { get; set; } = new();
            public string? PartName { get; set; }
            public bool IsVisible { get; set; }
        }

        public class ImpactStatus : IImpactStatus
        {
            public bool HasCrashed { get; set; }
            public string ObjectName { get; set; }
            public ImpactDirection? ImpactDirection { get; set; }
            public IParticle SourceParticle { get; set; }
            public int? ObjectHealth { get; set; } = 100;
            public bool HasExploded { get; set; }
        }

        public class Vector3 : IVector3
        {
            public Vector3(float xVal = 0, float yVal = 0, float zVal = 0)
            {
                this.x = xVal;
                this.y = yVal;
                this.z = zVal;
            }

            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
            public override string ToString() => $"(x={x:F2}, y={y:F2}, z={z:F2})";
        }

        public class TriangleMeshWithColor : TriangleMesh, ITriangleMeshWithColor
        {
            public string? Color { get; set; }
        }

        public class TriangleMesh : ITriangleMesh
        {
            public Domain.IVector3 normal1 { get; set; }
            public Domain.IVector3 normal2 { get; set; }
            public Domain.IVector3 normal3 { get; set; }
            public Domain.IVector3 vert1 { get; set; }
            public Domain.IVector3 vert2 { get; set; }
            public Domain.IVector3 vert3 { get; set; }
            public long? landBasedPosition { get; set; }
            public float angle { get; set; }
            public bool? noHidden { get; set; }

            public TriangleMesh()
            {
                normal1 = new Vector3();
                normal2 = new Vector3();
                normal3 = new Vector3();
                vert1 = new Vector3();
                vert2 = new Vector3();
                vert3 = new Vector3();
                landBasedPosition = 0;
                angle = 0;
            }
        }
    }
}
