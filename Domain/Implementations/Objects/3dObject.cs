using System.Collections.Generic;

namespace Domain
{
    public partial class _3dSpecificsImplementations
    {
        public class _3dObject : I3dObject
        {
            public required int ObjectId { get; set; }
            public List<I3dObjectPart> ObjectParts { get; set; } = new();
            public int? RotationOffsetY { get; set; }
            public int? RotationOffsetX { get; set; }
            public int? RotationOffsetZ { get; set; }
            public IVector3? WorldPosition { get; set; }
            public IVector3? Rotation { get; set; }
            public IVector3? ObjectOffsets { get; set; }
            public IObjectMovement? Movement { get; set; }
            public IParticles? Particles { get; set; }
            public List<List<IVector3>> CrashBoxes { get; set; }
            public List<string?>? CrashBoxNames { get; set; }
            public bool CrashBoxesFollowRotation { get; set; } = true;
            public IImpactStatus? ImpactStatus { get; set; }
            public int? Mass { get; set; }
            public string ObjectName { get; set; }
            public ISurface? ParentSurface { get; set; }
            public int? SurfaceBasedId { get; set; }
            public bool? CrashBoxDebugMode { get; set; }
            public IWeapon? WeaponSystems { get; set; }
            public IVector3? CalculatedCrashOffset { get; set; }
            public bool IsOnScreen { get; set; } = false;
            public bool HasShadow { get; set; } = false;
            public bool HasPowerUp { get; set; } = false;
        }
    }
}
