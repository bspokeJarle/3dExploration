using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class ParticleManager
    {
        private readonly _3dRotationCommon Rotate3d = new();

        private const string ShadowColor = "000000";

        // Surface-projection constants for shadows
        private const float BaseProjectedScale = 2.4f;
        private const float AltitudeShrinkFactor = 0.003f;
        private const float MinProjectedScale = 0.6f;
        private const float SurfaceFlattenY = 0.3f;
        private const float ShadowOffsetX = -20f;

        public void HandleParticles(_3dObject inhabitant, List<_3dObject> particleObjectList)
        {
            if (inhabitant.Particles?.Particles is null || inhabitant.Particles.Particles.Count == 0)
                return;

            List<Particle> particles;
            lock (inhabitant.Particles)
            {
                particles = inhabitant.Particles.Particles.OfType<Particle>().Where(p => p.Visible).ToList();
            }

            var surfaceObj = GameState.SurfaceState.SurfaceViewportObject;
            float surfaceY = surfaceObj?.ObjectOffsets?.y ?? 500f;
            float surfaceZ = surfaceObj?.ObjectOffsets?.z ?? 400f;
            float surfaceX = surfaceObj?.ObjectOffsets?.x ?? 0f;

            // Tile cache for ground projection (same approach as ObjectShadowManager free-flying path)
            var rotatedTiles = inhabitant.ParentSurface?.RotatedSurfaceTriangles;

            foreach (var particle in particles)
            {
                var particleTriangle = RotateParticle(particle.ParticleTriangle, particle.Rotation as Vector3);

                float particleOffsetX = inhabitant.ObjectOffsets.x + particle.Position.x;
                float particleOffsetY = inhabitant.ObjectOffsets.y + particle.Position.y;
                float particleOffsetZ = inhabitant.ObjectOffsets.z + particle.Position.z;

                // Original particle — rendered as its actual colored triangle in 3D space
                particleObjectList.Add(new _3dObject
                {
                    ObjectId = GameState.ObjectIdCounter++,
                    ObjectName = "Particle",
                    WorldPosition = particle.WorldPosition,
                    ParentSurface = inhabitant.ParentSurface,
                    ObjectParts = new List<I3dObjectPart>
                    {
                        new _3dObjectPart { Triangles = new List<ITriangleMeshWithColor> { particleTriangle }, PartName = "Particle", IsVisible = true }
                    },
                    ObjectOffsets = new Vector3
                    {
                        x = particleOffsetX,
                        y = particleOffsetY,
                        z = particleOffsetZ
                    },
                    CrashBoxes = CreateCrashBoxFromTriangle(particleTriangle),
                    ImpactStatus = new ImpactStatus { HasCrashed = false, SourceParticle = particle, HasExploded = false, ObjectName = inhabitant.ObjectName },
                    Rotation = particle.Rotation
                });

                // Black shadow — surface-projected flattened mark on the ground.
                // The shadow belongs to the GROUND (parented to the surface's ObjectOffsets),
                // not to the particle. Geometry is placed in surface-local space at the
                // ground tile under the particle's X position. Y here = depth (forward into screen).
                float altitude = MathF.Max(0f, particle.Position.y * -1f);
                float projScale = MathF.Max(MinProjectedScale, BaseProjectedScale - altitude * AltitudeShrinkFactor);

                var shadowTriangle = new TriangleMeshWithColor
                {
                    Color = ShadowColor,
                    vert1 = new Vector3 { x = particleTriangle.vert1.x * projScale, y = particleTriangle.vert1.y * projScale * SurfaceFlattenY, z = 0 },
                    vert2 = new Vector3 { x = particleTriangle.vert2.x * projScale, y = particleTriangle.vert2.y * projScale * SurfaceFlattenY, z = 0 },
                    vert3 = new Vector3 { x = particleTriangle.vert3.x * projScale, y = particleTriangle.vert3.y * projScale * SurfaceFlattenY, z = 0 },
                    noHidden = true
                };

                // Find the ground Y (depth) and Z (vertical) under this particle in surface-local space,
                // by interpolating between the two tile centers bracketing the particle's X.
                float targetX = particleOffsetX - surfaceX;
                float groundLocalX = targetX;
                float groundLocalY = 0f;
                float groundLocalZ = 0f;
                bool grounded = false;
                if (rotatedTiles != null && rotatedTiles.Count > 0)
                {
                    float leftX = float.MinValue, rightX = float.MaxValue;
                    float leftY = 0, rightY = 0;
                    float leftZ = 0, rightZ = 0;
                    for (int i = 0; i < rotatedTiles.Count; i++)
                    {
                        var tile = rotatedTiles[i];
                        float cx = (tile.vert1.x + tile.vert2.x + tile.vert3.x) / 3f;
                        float cy = (tile.vert1.y + tile.vert2.y + tile.vert3.y) / 3f;
                        float cz = (tile.vert1.z + tile.vert2.z + tile.vert3.z) / 3f;
                        if (cx <= targetX && cx > leftX) { leftX = cx; leftY = cy; leftZ = cz; }
                        if (cx >= targetX && cx < rightX) { rightX = cx; rightY = cy; rightZ = cz; }
                    }
                    if (!(leftX == float.MinValue && rightX == float.MaxValue))
                    {
                        if (leftX == float.MinValue) { leftX = rightX; leftY = rightY; leftZ = rightZ; }
                        if (rightX == float.MaxValue) { rightX = leftX; rightY = leftY; rightZ = leftZ; }
                        float t = (rightX - leftX) != 0 ? (targetX - leftX) / (rightX - leftX) : 0f;
                        groundLocalY = leftY + (rightY - leftY) * t;
                        groundLocalZ = leftZ + (rightZ - leftZ) * t;
                        grounded = true;
                    }
                }

                // Bake ground anchor into the shadow geometry (surface-local space).
                // The shadow object is then parented to the surface's ObjectOffsets so it
                // scrolls with the terrain instead of following the particle.
                shadowTriangle.vert1.x += groundLocalX + ShadowOffsetX;
                shadowTriangle.vert2.x += groundLocalX + ShadowOffsetX;
                shadowTriangle.vert3.x += groundLocalX + ShadowOffsetX;
                shadowTriangle.vert1.y += groundLocalY;
                shadowTriangle.vert2.y += groundLocalY;
                shadowTriangle.vert3.y += groundLocalY;
                shadowTriangle.vert1.z += groundLocalZ;
                shadowTriangle.vert2.z += groundLocalZ;
                shadowTriangle.vert3.z += groundLocalZ;

                if (!grounded)
                    continue;

                particleObjectList.Add(new _3dObject
                {
                    ObjectId = GameState.ObjectIdCounter++,
                    ObjectName = "ParticleShadow",
                    WorldPosition = new Vector3(),
                    ParentSurface = inhabitant.ParentSurface,
                    ObjectParts = new List<I3dObjectPart>
                    {
                        new _3dObjectPart { Triangles = new List<ITriangleMeshWithColor> { shadowTriangle }, PartName = "ParticleShadow", IsVisible = true }
                    },
                    ObjectOffsets = new Vector3
                    {
                        x = surfaceObj.ObjectOffsets.x,
                        y = surfaceObj.ObjectOffsets.y,
                        z = surfaceObj.ObjectOffsets.z
                    },
                    Rotation = new Vector3 { x = 70, y = 0, z = 0 }
                });
            }
        }

        //Creates a crashbox from a triangle
        private List<List<IVector3>> CreateCrashBoxFromTriangle(ITriangleMeshWithColor triangle)
        {
            float minX = MathF.Min(triangle.vert1.x, MathF.Min(triangle.vert2.x, triangle.vert3.x));
            float maxX = MathF.Max(triangle.vert1.x, MathF.Max(triangle.vert2.x, triangle.vert3.x));

            float minY = MathF.Min(triangle.vert1.y, MathF.Min(triangle.vert2.y, triangle.vert3.y));
            float maxY = MathF.Max(triangle.vert1.y, MathF.Max(triangle.vert2.y, triangle.vert3.y));

            float minZ = MathF.Min(triangle.vert1.z, MathF.Min(triangle.vert2.z, triangle.vert3.z));
            float maxZ = MathF.Max(triangle.vert1.z, MathF.Max(triangle.vert2.z, triangle.vert3.z));

            return new List<List<IVector3>>
            {
                new List<IVector3>
                {
                    new Vector3 { x = minX, y = minY, z = minZ },
                    new Vector3 { x = maxX, y = maxY, z = maxZ }
                }
            };
        }

        private ITriangleMeshWithColor RotateParticle(ITriangleMeshWithColor particleTriangle, Vector3 rotation)
        {
            return Rotate3d.RotateXMesh(
                Rotate3d.RotateYMesh(
                    Rotate3d.RotateZMesh(new List<ITriangleMeshWithColor> { particleTriangle }, rotation.z),
                    rotation.y
                ),
                rotation.x
            ).First();
        }
    }
}
