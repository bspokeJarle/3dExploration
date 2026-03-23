using System.Collections.Generic;

namespace Domain
{
    public interface I3dObject
    {
        int ObjectId { get; set; }
        string ObjectName { get; set; }
        int? RotationOffsetX { get; set; }
        int? RotationOffsetY { get; set; }
        int? RotationOffsetZ { get; set; }
        IVector3? WorldPosition { get; set; }
        List<I3dObjectPart> ObjectParts { get; set; }
        IVector3? ObjectOffsets { get; set; }
        IVector3? Rotation { get; set; }
        IObjectMovement? Movement { get; set; }
        IParticles? Particles { get; set; }
        IWeapon? WeaponSystems { get; set; }
        List<List<IVector3>> CrashBoxes { get; set; }
        bool CrashBoxesFollowRotation { get; set; }
        IImpactStatus? ImpactStatus { get; set; }
        int? Mass { get; set; }
        ISurface? ParentSurface { get; set; }
        int? SurfaceBasedId { get; set; }
        bool? CrashBoxDebugMode { get; set; }
        IVector3? CalculatedCrashOffset { get; set; }
        bool IsOnScreen { get; set; }
        bool HasShadow { get; set; }
    }
}
