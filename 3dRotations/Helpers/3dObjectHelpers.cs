﻿using _3dTesting._3dWorld;
using Domain;
using GameAiAndControls.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Documents;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class _3dObjectHelpers
    {
 /*       public static Vector3 FindWorldPosition(_3dObject inhabitant)
        {
            //Absolute world position in the map for static and dynamic objects
            if (inhabitant.SurfaceBasedId > 0)
            {
                var surfaceTriangle = inhabitant?.ParentSurface?.RotatedSurfaceTriangles.Find(tri => tri.landBasedPosition == inhabitant.SurfaceBasedId) as TriangleMeshWithColor;
                //Logger.Log($"[FindWorldPosition - Surfacebased position] {inhabitant.ObjectName} {inhabitant.SurfaceBasedId} X:{surfaceTriangle.vert1.x} Y:{surfaceTriangle.vert1.y} Z:{surfaceTriangle.vert1.z} ");
                return MapIdToWorldPosition((int)inhabitant.SurfaceBasedId, inhabitant.ParentSurface.GlobalMapSize(), inhabitant.ParentSurface.TileSize(), surfaceTriangle);
            }
            if (inhabitant.ObjectName == "Ship")
            {
                return GetCenterWorldPosition(
                    inhabitant.ParentSurface.GlobalMapPosition,
                    inhabitant.ParentSurface.GlobalMapPosition,
                    inhabitant.ParentSurface.SurfaceWidth(),
                    inhabitant.ParentSurface.TileSize(),
                    (Vector3)inhabitant.Position);
            }
            return new Vector3
            {
                //TODO: Remove the position adding here, should only be world position
                x = inhabitant.ParentSurface.GlobalMapPosition.x + inhabitant.Position.x,
                y = inhabitant.ParentSurface.GlobalMapPosition.y + inhabitant.Position.y,
                z = inhabitant.ParentSurface.GlobalMapPosition.z + inhabitant.Position.z
            };
        }*/

       /* public static Vector3 MapIdToWorldPosition(int mapId, int mapSize, int tileSize, TriangleMeshWithColor surfaceTriangle)
        {
            //Map SurfaceBasedId to world position
            int zeroBasedId = mapId - 1;
            int row = zeroBasedId / mapSize;
            int col = zeroBasedId % mapSize;

            //The Y position needs to come from the surface triangle + the position y offsets, on X and Z position offsets comes from the object then is added to the calculation
            //TODO: Remove test offset
            return new Vector3(col * tileSize, surfaceTriangle.vert1.y + 500, row * tileSize);
        }*/

        public static Vector3 GetCenterWorldPosition(Vector3 globalMapPosition, Vector3 localSurfacePosition, int screenPixels, int tileSize, Vector3 position)
        {
            float tilesPerScreen = screenPixels / (float)tileSize;
            float halfTiles = tilesPerScreen / 2f;

            return new Vector3
            {
                x = globalMapPosition.x + halfTiles * tileSize,
                //TODO:
                //Use an offset on +300 to make the ship able to crash with surface... Why the offset though?
                y = position.y + 300,
                z = globalMapPosition.z + halfTiles * tileSize
            };
        }

        public static IVector3 GetLocalWorldPosition(this _3dObject inhabitant)
        {
            var globalMapPosition = inhabitant.ParentSurface.GlobalMapPosition;
            //Some objects will always be in location, they have no world position, just return
            if (inhabitant.WorldPosition.x == 0 && inhabitant.WorldPosition.y == 0 && inhabitant.WorldPosition.z == 0) return null;
            //Some objects fly around, they have this world position, so they appear when you are at that location in the map
            var localWorldPosition = new Vector3
            {
                x = globalMapPosition.x - inhabitant.WorldPosition.x,
                y = globalMapPosition.y - inhabitant.WorldPosition.y,
                z = globalMapPosition.z - inhabitant.WorldPosition.z
            };
            return localWorldPosition;
        }
        public static bool CheckInhabitantVisibility(this _3dObject inhabitant)
        {
            // 1. Land-based check
            if (inhabitant.SurfaceBasedId > 0 && inhabitant.ParentSurface?.RotatedSurfaceTriangles != null)
            {
                bool isOnCurrentSurface = inhabitant.ParentSurface.RotatedSurfaceTriangles
                    .Any(t => t.landBasedPosition == inhabitant.SurfaceBasedId);

                return isOnCurrentSurface;
            }

            // 2. Always-visible (onscreen) objects — world position (0, 0, 0)
            if (inhabitant.WorldPosition.x == 0 &&
                inhabitant.WorldPosition.y == 0 &&
                inhabitant.WorldPosition.z == 0)
            {
                return true;
            }

            // 3. Distance-based visibility check
            var globalMapPosition = inhabitant.ParentSurface.GlobalMapPosition;
            var inhabitantPosition = inhabitant.WorldPosition;

            float distance = (float)GetDistance(globalMapPosition, (Vector3)inhabitantPosition);

            return Math.Abs(distance) <= 1400;
        }


        public static double GetDistance(Vector3 point1, Vector3 point2)
        {
            float dx = point1.x - point2.x;
            float dy = point1.y - point2.y;
            float dz = point1.z - point2.z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /*public static void CenterCrashBoxAt(List<Vector3> crashBox, IVector3 targetPosition)
        {
            if (crashBox == null || crashBox.Count == 0 || targetPosition == null)
                return;

            IVector3 boxCenter = GetCrashBoxGeometricCenter(crashBox, snapToBottomY: true);

            float shiftX = targetPosition.x - boxCenter.x;
            float shiftY = targetPosition.y - boxCenter.y;
            float shiftZ = targetPosition.z - boxCenter.z;

            for (int i = 0; i < crashBox.Count; i++)
            {
                var point = crashBox[i];
                crashBox[i] = new Vector3
                {
                    x = point.x + shiftX,
                    y = point.y + shiftY,
                    z = point.z + shiftZ
                };
            }
        }*/
        /*
        public static IVector3 GetCrashBoxGeometricCenter(List<Vector3> crashBox, bool snapToBottomY = false)
        {
            float sumX = 0, sumY = 0, sumZ = 0;
            float minY = float.MaxValue;

            foreach (var p in crashBox)
            {
                sumX += p.x;
                sumY += p.y;
                sumZ += p.z;
                if (snapToBottomY)
                    minY = Math.Min(minY, p.y);
            }

            int count = crashBox.Count;
            if (count == 0) return new Vector3();

            return new Vector3
            {
                x = sumX / count,
                y = snapToBottomY ? minY : sumY / count,
                z = sumZ / count
            };
        }
        */

        public static void CenterObjectAt(I3dObject obj, IVector3 targetPosition)
        {
            if (obj == null || targetPosition == null)
                return;

            //Use offset from specified in the Scene
            // Center the object at the bottom, meaning place it on top
            IVector3 objectCenter = GetObjectGeometricCenter(obj,true);

            // Compute the shift required
            float shiftX = targetPosition.x - objectCenter.x;
            float shiftY = targetPosition.y - objectCenter.y;
            float shiftZ = targetPosition.z - objectCenter.z;

            foreach (var part in obj.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    triangle.vert1.x += shiftX;
                    triangle.vert1.y += shiftY;
                    triangle.vert1.z += shiftZ;

                    triangle.vert2.x += shiftX;
                    triangle.vert2.y += shiftY;
                    triangle.vert2.z += shiftZ;

                    triangle.vert3.x += shiftX;
                    triangle.vert3.y += shiftY;
                    triangle.vert3.z += shiftZ;
                }
            }
        }

        public static IVector3 GetObjectGeometricCenter(I3dObject obj, bool snapToBottomY = false)
        {
            float sumX = 0, sumY = 0, sumZ = 0;
            int count = 0;
            float minY = float.MaxValue;

            foreach (var part in obj.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    var vertices = new[] { triangle.vert1, triangle.vert2, triangle.vert3 };

                    foreach (var v in vertices)
                    {
                        sumX += v.x;
                        sumY += v.y;
                        sumZ += v.z;
                        count++;

                        if (snapToBottomY)
                            minY = Math.Min(minY, v.y);
                    }
                }
            }

            if (count == 0) return new Vector3();

            return new Vector3
            {
                x = sumX / count,
                y = snapToBottomY ? minY : sumY / count,
                z = sumZ / count
            };
        }

        /*public static TriangleMeshWithColor MapFromVector3ToTMesh(this Vector3 coord)
        {
            return new TriangleMeshWithColor { vert1 = coord, vert2 = coord, vert3 = coord };
        }*/
        public struct CosSin
        {
            public float CosRes { get; set; }
            public float SinRes { get; set; }
        }
        public static CosSin ConvertFromAngleToCosSin(this float angle)
        {
            var radian = Math.PI * angle / 180.0;
            var sinRes = Math.Sin(radian);
            var cosRes = Math.Cos(radian);
            return new CosSin { CosRes = (float)cosRes, SinRes = (float)sinRes };
        }

        public static bool CheckCollisionBoxVsBox(List<Vector3> boxA, List<Vector3> boxB)
        {
            var minA = new Vector3(boxA.Min(p => p.x), boxA.Min(p => p.y), boxA.Min(p => p.z));
            var maxA = new Vector3(boxA.Max(p => p.x), boxA.Max(p => p.y), boxA.Max(p => p.z));

            var minB = new Vector3(boxB.Min(p => p.x), boxB.Min(p => p.y), boxB.Min(p => p.z));
            var maxB = new Vector3(boxB.Max(p => p.x), boxB.Max(p => p.y), boxB.Max(p => p.z));

            bool overlapX = maxA.x >= minB.x && minA.x <= maxB.x;
            bool overlapY = maxA.y >= minB.y && minA.y <= maxB.y;
            bool overlapZ = maxA.z >= minB.z && minA.z <= maxB.z;

            return overlapX && overlapY && overlapZ;
        }

        public static float GetDeepestZ(ITriangleMeshWithColor triangle)
        {
            if (triangle.vert1.z <= triangle.vert2.z && triangle.vert1.z <= triangle.vert3.z) return triangle.vert1.z;
            if (triangle.vert2.z <= triangle.vert1.z && triangle.vert2.z <= triangle.vert3.z) return triangle.vert2.z;
            if (triangle.vert3.z <= triangle.vert1.z && triangle.vert3.z <= triangle.vert2.z) return triangle.vert3.z;
            return 0;
        }
        public static List<ITriangleMeshWithColor> ConvertToTrianglesWithColor(List<TriangleMesh> triangles, string color)
        {
            var triangleswithcolor = new List<ITriangleMeshWithColor>();
            foreach (var triangle in triangles)
            {
                triangleswithcolor.Add(new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                    vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                    vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                    normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                    normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                    normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                    angle = triangle.angle,
                    Color = color
                });
            }
            return triangleswithcolor;
        }

        public static List<TriangleMeshWithColor> DeepCopyTriangles(List<TriangleMeshWithColor> originalList)
        {
            List<TriangleMeshWithColor> copiedList = new List<TriangleMeshWithColor>();

            foreach (var original in originalList)
            {
                var copy = new TriangleMeshWithColor
                {
                    Color = original.Color,
                    normal1 = new Vector3 { x = original.normal1.x, y = original.normal1.y, z = original.normal1.z },
                    normal2 = new Vector3 { x = original.normal2.x, y = original.normal2.y, z = original.normal2.z },
                    normal3 = new Vector3 { x = original.normal3.x, y = original.normal3.y, z = original.normal3.z },
                    vert1 = new Vector3 { x = original.vert1.x, y = original.vert1.y, z = original.vert1.z },
                    vert2 = new Vector3 { x = original.vert2.x, y = original.vert2.y, z = original.vert2.z },
                    vert3 = new Vector3 { x = original.vert3.x, y = original.vert3.y, z = original.vert3.z },
                    landBasedPosition = original.landBasedPosition,
                    angle = original.angle,
                    noHidden = original.noHidden
                };

                copiedList.Add(copy);
            }

            return copiedList;
        }

        public static ParticlesAI DeepCopyParticlesAI(ParticlesAI original)
        {
            var copy = new ParticlesAI
            {
                ParentShip = original.ParentShip, // Keeping reference, change if deep copy is needed
                Visible = original.Visible,
                Particles = DeepCopyParticles(original.Particles)
            };

            return copy;
        }

        public static List<IParticle> DeepCopyParticles(List<IParticle> originalParticles)
        {
            List<IParticle> copiedParticles = new List<IParticle>();

            foreach (var original in originalParticles)
            {
                var copy = new Particle // Ensure `Particle` is the concrete class implementing `IParticle`
                {
                    ParticleTriangle = new TriangleMeshWithColor
                    {
                        Color = original.ParticleTriangle.Color,
                        normal1 = new Vector3(original.ParticleTriangle.normal1.x, original.ParticleTriangle.normal1.y, original.ParticleTriangle.normal1.z),
                        normal2 = new Vector3(original.ParticleTriangle.normal2.x, original.ParticleTriangle.normal2.y, original.ParticleTriangle.normal2.z),
                        normal3 = new Vector3(original.ParticleTriangle.normal3.x, original.ParticleTriangle.normal3.y, original.ParticleTriangle.normal3.z),
                        vert1 = new Vector3(original.ParticleTriangle.vert1.x, original.ParticleTriangle.vert1.y, original.ParticleTriangle.vert1.z),
                        vert2 = new Vector3(original.ParticleTriangle.vert2.x, original.ParticleTriangle.vert2.y, original.ParticleTriangle.vert2.z),
                        vert3 = new Vector3(original.ParticleTriangle.vert3.x, original.ParticleTriangle.vert3.y, original.ParticleTriangle.vert3.z),
                        landBasedPosition = original.ParticleTriangle.landBasedPosition,
                        angle = original.ParticleTriangle.angle,
                        noHidden = original.ParticleTriangle.noHidden
                    },
                    Velocity = new Vector3(original.Velocity.x, original.Velocity.y, original.Velocity.z),
                    Acceleration = new Vector3(original.Acceleration.x, original.Acceleration.y, original.Acceleration.z),
                    VariedStart = original.VariedStart,
                    Life = original.Life,
                    Size = original.Size,
                    Color = original.Color,
                    BirthTime = original.BirthTime,
                    IsRotated = original.IsRotated,
                    Position = new Vector3(original.Position.x, original.Position.y, original.Position.z),
                    WorldPosition = new Vector3(original.WorldPosition.x, original.WorldPosition.y, original.WorldPosition.z),
                    Rotation = original.Rotation != null ? new Vector3(original.Rotation.x, original.Rotation.y, original.Rotation.z) : null,
                    RotationSpeed = original.RotationSpeed != null ? new Vector3(original.RotationSpeed.x, original.RotationSpeed.y, original.RotationSpeed.z) : null,
                    NoShading = original.NoShading,
                    Visible = original.Visible
                };

                copiedParticles.Add(copy);
            }

            return copiedParticles;
        }

        public static List<_3dObject> DeepCopy3dObjects(List<_3dObject> inhabitants)
        {
            return inhabitants
                .Where(i => i.CheckInhabitantVisibility())
                .Select(inhabitant =>
                {
                    var objectParts = inhabitant.ObjectParts.Select(part =>
                    {
                        var triangles = part.Triangles.Select(triangle => new TriangleMeshWithColor
                        {
                            vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                            vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                            vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                            normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                            normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                            normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                            landBasedPosition = triangle.landBasedPosition,
                            angle = triangle.angle,
                            Color = triangle.Color,
                            noHidden = triangle.noHidden
                        }).ToList();

                        return new _3dObjectPart
                        {
                            PartName = part.PartName,
                            Triangles = triangles.Select(t => (ITriangleMeshWithColor)t).ToList(),
                            IsVisible = part.IsVisible
                        };
                    }).ToList();

                    return new _3dObject
                    {
                        Position = new Vector3 { x = inhabitant.Position.x, y = inhabitant.Position.y, z = inhabitant.Position.z },
                        Rotation = new Vector3 { x = inhabitant.Rotation.x, y = inhabitant.Rotation.y, z = inhabitant.Rotation.z },
                        WorldPosition = new Vector3 { x = inhabitant.WorldPosition.x, y = inhabitant.WorldPosition.y, z = inhabitant.WorldPosition.z },
                        ObjectParts = objectParts.Cast<I3dObjectPart>().ToList(),
                        Movement = inhabitant.Movement,
                        Particles = inhabitant.Particles,
                        CrashBoxes = inhabitant.CrashBoxes
                          .Select(innerList => innerList
                              .Select(v => new Vector3 { x = v.x, y = v.y, z = v.z } as IVector3)
                              .ToList())
                          .ToList(),
                        HasCrashed = inhabitant.HasCrashed,
                        Mass = inhabitant.Mass,
                        ObjectName = inhabitant.ObjectName,
                        ParentSurface = inhabitant.ParentSurface,
                        RotationOffsetX = inhabitant.RotationOffsetX,
                        RotationOffsetY = inhabitant.RotationOffsetY,
                        RotationOffsetZ = inhabitant.RotationOffsetZ,
                        SurfaceBasedId = inhabitant.SurfaceBasedId
                    };
                }).ToList();
        }
    }  
}
