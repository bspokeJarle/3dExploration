using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using static Domain._3dSpecificsImplementations;

namespace Domain
{
    public struct SurfaceData
    {
        public int mapDepth;
        public int mapId;
    }

    public interface IPhysics
    {
        float Mass { get; set; }
        IVector3 Velocity { get; set; }
        float Thrust { get; set; }
        float Friction { get; set; }
        float MaxSpeed { get; set; }
        float MaxThrust { get; set; }
        float GravityStrength { get; set; }
        IVector3 GravitySource { get; set; }
        IVector3 Acceleration { get; set; }
        int BounceCooldownFrames { get; set; }
        float BounceHeightMultiplier { get; set; }
        IVector3 ApplyDragForce(IVector3 currentPosition, float deltaTime);
        IVector3 ApplyForces(IVector3 currentPosition, float deltaTime);
        IVector3 ApplyGravityForce(IVector3 currentPosition, float deltaTime);
        IVector3 ApplyThrust(IVector3 currentPosition, IVector3 direction, float deltaTime);
        IVector3 ApplyRotationDragForce(IVector3 rotationVector);
        void Bounce(Vector3 normal, ImpactDirection? direction);
        void TiltStabilization(ref IVector3 tiltState);
        I3dObject ExplodeObject(I3dObject explodingObject, DateTime deltaTime); 
    }

    public interface ISurface
    {
        public Vector3 GlobalMapPosition { get; set; }
        public Vector3 GlobalMapRotation { get; set; }
        public SurfaceData[,]? Global2DMap { get; set; }
        public BitmapSource GlobalMapBitmap { get; set; }

        public int SurfaceWidth();
        public int GlobalMapSize();
        public int ViewPortSize();
        public int TileSize();
        public int MaxHeight();

        public List<ITriangleMeshWithColor> RotatedSurfaceTriangles { get; set; }
        public I3dObject GetSurfaceViewPort();

        public void Create2DMap(int? maxTrees, int? maxHouses);
    }

    public interface IObjectMovement
    {
        public I3dObject MoveObject(I3dObject theObject);
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord);
        public IPhysics Physics { get; set; }
    }

    public interface I3dObject
    {
        public string ObjectName { get; set; }
        public int? RotationOffsetX { get; set; }
        public int? RotationOffsetY { get; set; }
        public int? RotationOffsetZ { get; set; }
        public IVector3? WorldPosition { get; set; }
        public List<I3dObjectPart> ObjectParts { get; set; }
        public IVector3? ObjectOffsets { get; set; }
        public IVector3? CrashboxOffsets { get; set; }
        public IVector3? Rotation { get; set; }
        public IObjectMovement? Movement { get; set; }
        public IParticles? Particles { get; set; }
        public List<List<IVector3>> CrashBoxes { get; set; }
        public IImpactStatus? ImpactStatus { get; set; }
        public int? Mass { get; set; }
        public ISurface? ParentSurface { get; set; }
        public int? SurfaceBasedId { get; set; }
    }

    public interface IImpactStatus
    {
        public bool HasCrashed { get; set; }
        public string ObjectName { get; set; }
        public ImpactDirection? ImpactDirection { get; set; }
        //Particles are temporary objects, so need a referance to the original object to give notice of impact
        public IParticle? SourceParticle { get; set; }

    }

    public enum ImpactDirection
    {
        Top,
        Bottom,
        Left,
        Right,
        Center
    }

    public interface I3dObjectPart
    {
        public List<ITriangleMeshWithColor> Triangles { get; set; }
        public string? PartName { get; set; }
        public bool IsVisible { get; set; }
    }
    public interface IVector3
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    public interface IParticles
    {
        public IObjectMovement ParentShip { get; set; }
        public List<IParticle> Particles { get; set; }
        public void ReleaseParticles(ITriangleMeshWithColor Trajectory, ITriangleMeshWithColor StartPosition, IVector3 WorldPosition, IObjectMovement ParentShip, int Thrust);
        public void MoveParticles();
    }
    public interface IParticle
    {
        public ITriangleMeshWithColor ParticleTriangle { get; set; }
        public IVector3 Velocity { get; set; }
        public IVector3 Acceleration { get; set; }
        public Int64 VariedStart { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        public string Color { get; set; }
        public DateTime BirthTime { get; set; }
        public bool IsRotated { get; set; }
        public IVector3 Position { get; set; }
        //Where in the world is this specific particle
        public IVector3 WorldPosition { get; set; }
        //Where is the actual Map Position of the ship
        public IVector3? Rotation { get; set; }
        public IVector3? RotationSpeed { get; set; }
        public bool? NoShading { get; set; }
        public bool Visible { get; set; }
        public IImpactStatus? ImpactStatus { get; set; }
        public IPhysics? Physics { get; set; }
    }
    public interface ITriangleMeshWithColor : ITriangleMesh
    {
        public string? Color { get; set; }
    }

    public interface ITriangleMesh
    {
        public IVector3 normal1 { get; set; }
        public IVector3 normal2 { get; set; }
        public IVector3 normal3 { get; set; }
        public IVector3 vert1 { get; set; }
        public IVector3 vert2 { get; set; }
        public IVector3 vert3 { get; set; }
        public long? landBasedPosition { get; set; }
        public float angle { get; set; }
        public bool? noHidden { get; set; }
    }
}
