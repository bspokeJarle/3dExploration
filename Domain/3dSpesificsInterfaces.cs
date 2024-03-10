using System;
using System.Collections.Generic;

namespace Domain
{
    public interface IObjectMovement
    {
        public I3dObject MoveObject(I3dObject theObject);
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord);
    }

    public interface I3dObject
    {
        public string ObjectName { get; set; }
        public List<I3dObjectPart> ObjectParts { get; set; }
        public IVector3? Position { get; set; }
        public IVector3? Rotation { get; set; }
        public IObjectMovement? Movement { get; set; }
        public IParticles? Particles { get; set; }
        //TODO: Might need to expand with metadata to differ between hits on different crashboxes
        public List<List<IVector3>> CrashBoxes { get; set; }
        public bool HasCrashed { get; set; }
        public int? Mass { get; set; }
        //todo add object relative properties, colors, ai etc    
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
        public void ReleaseParticles(ITriangleMeshWithColor Trajectory, ITriangleMeshWithColor StartPosition, IObjectMovement ParentShip);
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
        public IVector3? Rotation { get; set; }
        public IVector3? RotationSpeed { get; set; }
        public bool? NoShading { get; set; }

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
        public float angle { get; set; }
        public bool? noHidden { get; set; }
    }
}
