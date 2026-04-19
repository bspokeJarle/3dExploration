using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class ObjectShadowManager
    {
        // =====================================================================
        // TUNING KNOBS — all live values grouped here so they are easy to tweak
        // from the debugger / Immediate window without rebuilding. Change any
        // of these at runtime (e.g. `ObjectShadowManager.TowerShadowNudgeX = -7;`)
        // and the next frame's shadows pick up the new value.
        //
        // Quick map:
        //   ShadowColor              - silhouette fill color ("000000" = black)
        //   StaticOffsetX/Y/Z        - pre-projection push for ship / free-flying
        //                              (tower-like branch ignores these)
        //   BaseScale                - overall silhouette size multiplier
        //   FreeFlyingShadowScale    - extra size boost for airborne enemies
        //   AltitudeShrinkFactor     - how fast shadow shrinks as object climbs
        //   MinScale                 - lower clamp for the shrink
        //   TowerShadowSurfaceLift   - pull tower/tree shadow toward camera (-Y)
        //                              so it doesn't z-fight with the tile
        //   UniversalShadowLift      - small surface-local Y lift for all shadows
        //   ShadowSlopeX / SlopeY    - planar-projection light direction
        //                              (-X = lean left, -Y = fall behind)
        //   VertexStretchBoost       - how strongly tall silhouettes elongate
        //   TowerShadowNudgeX/Y/Z    - tower-like branch fine-tune (matched tile)
        // =====================================================================
        public static string ShadowColor = "000000";

        public static float StaticOffsetX = -30f;
        public static float StaticOffsetY = -40f;
        public static float StaticOffsetZ = 0f;

        public static float BaseScale = 1.0f;
        public static float FreeFlyingShadowScale = 1.8f;
        public static float AltitudeShrinkFactor = 0.002f;
        public static float MinScale = 0.2f;

        public static float TowerShadowSurfaceLift = 10f;
        public static float UniversalShadowLift = 10f;

        // Tower-like per-axis nudge applied AFTER the matched-tile anchor, so
        // the tower trunk and its shadow line up visually. These are the values
        // you've been iterating on — tweak freely.
        public static float TowerShadowNudgeX = -8f; // +right / -left in surface-local X
        public static float TowerShadowNudgeY = 0f;  // +down-screen / -up-screen (after tilt)
        public static float TowerShadowNudgeZ = 0f; // +further in / -closer along scroll axis

        // Per-vertex projection stretch. Base silhouette verts (z=0) stay put;
        // upper verts (z>0) are displaced along the light direction by
        // slope * boost. Larger = longer cast shadow.
        public static float VertexStretchBoost = 1.2f;

        // Global directional light (a "sun") used for proper planar projection of the
        // pre-built Shadow silhouette onto the ground plane (surface-local z = 0).
        //
        // Projection: given light direction L = (Lx, Ly, Lz) with Lz < 0 (light shining
        // downward), a model vertex v projects onto the ground plane via
        //     v' = v - L * (v.z / Lz)
        // which in component form becomes
        //     v'.x = v.x + v.z * ShadowSlopeX   where ShadowSlopeX = -Lx / Lz
        //     v'.y = v.y + v.z * ShadowSlopeY   where ShadowSlopeY = -Ly / Lz
        //     v'.z = 0
        // This is applied UNIFORMLY to every object's Shadow part — no per-type skew,
        // no flatten/zScale fudge factors. Verts at z=0 stay anchored at the base;
        // verts at z=H land at (x + H*slopeX, y + H*slopeY, 0).
        //
        // With Lx=0.35, Ly=0, Lz=-1 the sun leans slightly to the right and straight
        // down in surface-local space, so every shadow falls the same short distance
        // to the right of its base, regardless of screen position.
        // Shadow projection slopes. A model vertex at height z projects onto the
        // ground plane at model-space offsets (z*ShadowSlopeX, z*ShadowSlopeY, 0).
        // Those offsets are then rotated by the surface tilt (X=70°), so the
        // shadow-space Y offset translates to screen (-Y + Z) on the ground plane:
        //   +Y offset (model)  -> in front of the object  (toward camera, down on screen)
        //   -Y offset (model)  -> BEHIND the object       (away from camera, up on screen)
        //   +X offset (model)  -> to the right of the object
        //   -X offset (model)  -> to the left of the object
        //
        // We want the tip of a tall object's shadow to fall BEHIND and slightly to
        // one side — i.e. negative Y and a small X component. That means the light
        // source is above, behind the camera's shoulder, shining forward and down.
        public static float ShadowSlopeX = -0.15f; // shadow leans slightly to the left
        public static float ShadowSlopeY = -0.55f; // shadow falls behind (away from camera)

        // Surface tilt. The ground plane is rotated X=70° (so tiles lean toward the
        // camera). Shadow triangles are built in surface-local space using tile
        // coordinates that are ALREADY rotated, so we must bake that same tilt into
        // the projected silhouette offsets and leave the shadow object's own
        // Rotation at zero — otherwise LiveGameLoop rotates the (base + offset)
        // vertex a SECOND time and the silhouette pops back up off the ground.
        private const float SurfaceTiltDegrees = 70f;
        private static readonly float SurfaceTiltCos = MathF.Cos(SurfaceTiltDegrees * MathF.PI / 180f);
        private static readonly float SurfaceTiltSin = MathF.Sin(SurfaceTiltDegrees * MathF.PI / 180f);

        /// <summary>
        /// Creates a black flattened shadow projected onto the surface.
        /// The shadow shares the surface's ObjectOffsets so it scrolls with the terrain.
        /// Shadow geometry is translated to the nearest tile center in surface-local space.
        /// </summary>
        public void HandleObjectShadow(_3dObject inhabitant, List<_3dObject> shadowList)
        {
            if (!inhabitant.HasShadow)
                return;

            var surfaceObj = GameState.SurfaceState.SurfaceViewportObject;
            if (surfaceObj?.ObjectOffsets == null)
                return;

            var rotatedTiles = inhabitant.ParentSurface?.RotatedSurfaceTriangles;
            if (rotatedTiles == null || rotatedTiles.Count == 0)
                return;

            // Object X position relative to surface in surface-local space.
            //
            // Flying enemies (seeders, drones, bomber, swan...) move via WorldPosition
            // — the AI only updates WorldPosition, not ObjectOffsets.x/z. The renderer
            // places them at screen X = screenCenter - localWorld.x + ObjectOffsets.x
            // (see ObjectPlacementHelpers.TryGetRenderPosition).
            //
            // Tile vertices live in surface-local space (they're rotated but carry
            // the surface's own ObjectOffsets in screen-space), so surface-local X
            // for any object equals:
            //     objectScreenX - surfaceScreenX
            // where objectScreenX = -localWorld.x + ObjectOffsets.x and
            //       surfaceScreenX = surface.ObjectOffsets.x.
            //
            // For objects without a WorldPosition (player ship, towers), localWorld
            // is null and objectScreenX collapses to ObjectOffsets.x.
            var localWorld = inhabitant.GetLocalWorldPosition();
            float objScreenX = (localWorld != null ? -localWorld.x : 0f)
                               + (inhabitant.ObjectOffsets?.x ?? 0f);
            float objScreenY = (localWorld != null ? -localWorld.y : 0f)
                               + (inhabitant.ObjectOffsets?.y ?? 0f);
            // Z (vertical on screen) is the scroll axis; tiles are laid out in the
            // X–Z plane. localWorld.z uses +Z forward (see GetLocalWorldPosition),
            // so object screen Z = +localWorld.z + ObjectOffsets.z.
            float objScreenZ = (localWorld != null ? localWorld.z : 0f)
                               + (inhabitant.ObjectOffsets?.z ?? 0f);
            float surfScreenX = surfaceObj.ObjectOffsets.x;
            float surfScreenY = surfaceObj.ObjectOffsets.y;
            float surfScreenZ = surfaceObj.ObjectOffsets.z;
            float targetX = objScreenX - surfScreenX;
            float targetZ = objScreenZ - surfScreenZ;

            // Classify object once (avoid repeated string allocations / contains checks).
            // IMPORTANT: use exact equality for "Ship" — substring match would also
            // catch names like "MotherShipSmall" and wrongly route it to the ship
            // branch (which anchors to the frontmost platform tile). Tower classifier
            // stays substring-based because it's genuinely tower-like whenever the
            // name contains "tower" or a SurfaceBasedId is set.
            string name = inhabitant.ObjectName ?? string.Empty;
            bool isShip = name.Equals("Ship", StringComparison.OrdinalIgnoreCase);
            bool isTowerLike = !isShip && (inhabitant.SurfaceBasedId != null
                                           || name.IndexOf("tower", StringComparison.OrdinalIgnoreCase) >= 0);

            // Compute only the shadow base actually needed for this object type.
            // All *BaseX/Y/Z are in SURFACE-LOCAL space because the shadow _3dObject
            // is parented to surface.ObjectOffsets; the renderer adds surfaceX/Y/Z.
            // targetX = objScreenX - surfScreenX is the object's X in surface space.
            float shadowBaseX = targetX;
            float shadowBaseY;
            float shadowBaseZ = 0f;

            if (isShip)
            {
                // Frontmost tile (smallest |tileCenterY|)
                float platformFrontTileY = 0f;
                float platformFrontTileZ = 0f;
                float minAbsY = float.MaxValue;
                for (int i = 0; i < rotatedTiles.Count; i++)
                {
                    var tile = rotatedTiles[i];
                    float tileCenterY = (tile.vert1.y + tile.vert2.y + tile.vert3.y) / 3f;
                    float absY = MathF.Abs(tileCenterY);
                    if (absY < minAbsY)
                    {
                        minAbsY = absY;
                        platformFrontTileY = tileCenterY;
                        platformFrontTileZ = (tile.vert1.z + tile.vert2.z + tile.vert3.z) / 3f;
                    }
                }
                shadowBaseY = platformFrontTileY;
                shadowBaseZ = platformFrontTileZ;
            }
            else if (isTowerLike)
            {
                // Direct tile lookup by SurfaceBasedId (O(N) scan, single pass, no closure)
                ITriangleMeshWithColor matchedTile = null;
                if (inhabitant.SurfaceBasedId != null)
                {
                    long sid = (long)inhabitant.SurfaceBasedId;
                    for (int i = 0; i < rotatedTiles.Count; i++)
                    {
                        var t = rotatedTiles[i];
                        if (t.landBasedPosition.HasValue && t.landBasedPosition.Value == sid)
                        {
                            matchedTile = t;
                            break;
                        }
                    }
                }

                if (matchedTile != null)
                {
                    // Learn from the ship branch: tile provides ground Y and Z
                    // (so the shadow sits on the surface), but X is shifted by
                    // the OO.x delta between the tower and the surface so the
                    // shadow lines up under the tower's visible trunk. Same
                    // trick the ship uses via targetX.
                    float tileCenterX = (matchedTile.vert1.x + matchedTile.vert2.x + matchedTile.vert3.x) / 3f;
                    shadowBaseX = tileCenterX + targetX + TowerShadowNudgeX;
                    shadowBaseY = (matchedTile.vert1.y + matchedTile.vert2.y + matchedTile.vert3.y) / 3f
                                  - TowerShadowSurfaceLift + TowerShadowNudgeY;
                    shadowBaseZ = (matchedTile.vert1.z + matchedTile.vert2.z + matchedTile.vert3.z) / 3f
                                  + TowerShadowNudgeZ;
                }
                else
                {
                    // Fallback: tile with center X closest to object's X
                    float nearestTileY = 0f;
                    float minDistX = float.MaxValue;
                    for (int i = 0; i < rotatedTiles.Count; i++)
                    {
                        var tile = rotatedTiles[i];
                        float tileCenterX = (tile.vert1.x + tile.vert2.x + tile.vert3.x) / 3f;
                        float dx = MathF.Abs(tileCenterX - targetX);
                        if (dx < minDistX)
                        {
                            minDistX = dx;
                            nearestTileY = (tile.vert1.y + tile.vert2.y + tile.vert3.y) / 3f;
                        }
                    }
                    shadowBaseY = nearestTileY;
                }
            }
            else
            {
                // Free-flying: mirror the ship branch but take ONLY Y (ground depth)
                // from the nearest tile. X and Z come straight from the object, so
                // the shadow tracks the object's continuous world-space position and
                // does NOT snap/jump as the nearest tile changes from frame to frame.
                //   - X from the OBJECT (lateral, continuous)
                //   - Z from the OBJECT (scroll axis, continuous)
                //   - Y from the nearest TILE (ground surface depth under the object)
                shadowBaseX = targetX;
                shadowBaseZ = targetZ;

                float bestTileY = 0f;
                float bestDistSq = float.MaxValue;
                bool found = false;
                for (int i = 0; i < rotatedTiles.Count; i++)
                {
                    var tile = rotatedTiles[i];
                    float tileCenterX = (tile.vert1.x + tile.vert2.x + tile.vert3.x) / 3f;
                    float tileCenterZ = (tile.vert1.z + tile.vert2.z + tile.vert3.z) / 3f;
                    float dx = tileCenterX - targetX;
                    float dz = tileCenterZ - targetZ;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < bestDistSq)
                    {
                        bestDistSq = d2;
                        bestTileY = (tile.vert1.y + tile.vert2.y + tile.vert3.y) / 3f;
                        found = true;
                    }
                }
                if (!found) return;

                shadowBaseY = bestTileY;
            }

            // Altitude for scaling: gap between object screen Y and surface screen Y.
            // Free-flying objects get a larger base scale so their shadow reads
            // clearly even at altitude; altitude shrink still applies so the
            // shadow shrinks as the object climbs.
            float altitude = MathF.Max(0f, surfScreenY - objScreenY);
            float baseScale = (isShip || isTowerLike) ? BaseScale : BaseScale * FreeFlyingShadowScale;
            float scale = MathF.Max(MinScale, baseScale - altitude * AltitudeShrinkFactor);

            // Planar projection (see ShadowSlopeX/Y comment at top of file). One
            // formula for every object type: the silhouette is projected onto the
            // ground plane along the global light direction, then translated to
            // shadowBaseX/Y/Z (which has already been chosen per-object type).

            var shadowParts = new List<I3dObjectPart>(1);

            // Performance: only objects with a pre-built low-poly "Shadow" part
            // (IsVisible = false, added at object creation) get a shadow. No
            // fallback to projecting full meshes — that cost is forbidden.
            I3dObjectPart simplifiedShadowPart = null;
            for (int i = 0; i < inhabitant.ObjectParts.Count; i++)
            {
                if (inhabitant.ObjectParts[i].PartName == "Shadow")
                {
                    simplifiedShadowPart = inhabitant.ObjectParts[i];
                    break;
                }
            }

            if (simplifiedShadowPart == null
                || simplifiedShadowPart.Triangles == null
                || simplifiedShadowPart.Triangles.Count == 0)
                return;

            // Static ShadowOffsetX/Y was a pre-projection fudge; with proper planar
            // projection the shadow direction comes entirely from ShadowSlopeX/Y.
            // Keep a small static push only for ship/free-flying (where it provides
            // the ground anchor below the on-screen model); zero it for tower-like.
            float shadowOffsetX = isTowerLike ? 0f : StaticOffsetX;
            float shadowOffsetY = isTowerLike ? 0f : StaticOffsetY;

            // All shadows are parented to the surface's ObjectOffsets so they
            // scroll and depth-sort with the terrain. The ship/tower-like/
            // free-flying branches above have already baked any OO delta into
            // shadowBaseX (via targetX), so no special parenting is needed.
            Vector3 shadowObjectOffsets = new Vector3
            {
                x = surfaceObj.ObjectOffsets.x,
                y = surfaceObj.ObjectOffsets.y,
                z = surfaceObj.ObjectOffsets.z
            };

            // Per-object fine-tuning. Any object can set ShadowOffset (in
            // surface-local X/Y/Z) to nudge its shadow anchor. Positive X = right,
            // positive Y = up-screen (further from camera after the tilt),
            // positive Z = farther up the scroll axis. Keep values small —
            // typically a few units — for subtle alignment corrections.
            if (inhabitant.ShadowOffset != null)
            {
                shadowBaseX += inhabitant.ShadowOffset.x;
                shadowBaseY += inhabitant.ShadowOffset.y;
                shadowBaseZ += inhabitant.ShadowOffset.z;
            }

            // Per-vertex projection stretch. The base of the silhouette (z=0
            // vertices) stays anchored at shadowBase so the shadow starts AT the
            // object; only the upper vertices (z>0) are displaced along the
            // light direction. Larger VertexStretchBoost = longer cast shadow.
            float vStretchX = ShadowSlopeX * VertexStretchBoost;
            float vStretchY = ShadowSlopeY * VertexStretchBoost;

            {
                var part = simplifiedShadowPart;

                var shadowTriangles = new List<ITriangleMeshWithColor>(part.Triangles.Count);

                for (int i = 0; i < part.Triangles.Count; i++)
                {
                    var tri = part.Triangles[i];

                    // 1. Project each vertex onto the model-space ground plane (z=0)
                    //    along the global light direction, using the boosted slopes
                    //    so tall silhouettes (tower/tree prisms) actually stretch.
                    //    Verts at z=0 stay put; verts at z=H land at
                    //    (x + H*vStretchX, y + H*vStretchY, 0).
                    float p1x = tri.vert1.x + tri.vert1.z * vStretchX;
                    float p1y = tri.vert1.y + tri.vert1.z * vStretchY;
                    float p2x = tri.vert2.x + tri.vert2.z * vStretchX;
                    float p2y = tri.vert2.y + tri.vert2.z * vStretchY;
                    float p3x = tri.vert3.x + tri.vert3.z * vStretchX;
                    float p3y = tri.vert3.y + tri.vert3.z * vStretchY;

                    // 2. Rotate that flat silhouette by the surface tilt (X = 70°)
                    //    so it lies in the tilted ground plane. A point (x, y, 0)
                    //    rotated about X becomes (x, y*cos, y*sin).
                    //    The silhouette is scaled, then added to shadowBase (which
                    //    comes from the already-rotated tile mesh). shadow.Rotation
                    //    is (0,0,0) so LiveGameLoop does NOT rotate these again.
                    float sx1 = p1x * scale;
                    float sy1 = p1y * scale;
                    float sx2 = p2x * scale;
                    float sy2 = p2y * scale;
                    float sx3 = p3x * scale;
                    float sy3 = p3y * scale;

                    shadowTriangles.Add(new TriangleMeshWithColor
                    {
                        Color = ShadowColor,
                        vert1 = new Vector3
                        {
                            x = shadowBaseX + sx1 + shadowOffsetX,
                            y = shadowBaseY + sy1 * SurfaceTiltCos + shadowOffsetY,
                            z = shadowBaseZ + sy1 * SurfaceTiltSin + StaticOffsetZ
                        },
                        vert2 = new Vector3
                        {
                            x = shadowBaseX + sx2 + shadowOffsetX,
                            y = shadowBaseY + sy2 * SurfaceTiltCos + shadowOffsetY,
                            z = shadowBaseZ + sy2 * SurfaceTiltSin + StaticOffsetZ
                        },
                        vert3 = new Vector3
                        {
                            x = shadowBaseX + sx3 + shadowOffsetX,
                            y = shadowBaseY + sy3 * SurfaceTiltCos + shadowOffsetY,
                            z = shadowBaseZ + sy3 * SurfaceTiltSin + StaticOffsetZ
                        },
                        noHidden = true
                    });
                }

                shadowParts.Add(new _3dObjectPart
                {
                    PartName = "ObjectShadow",
                    Triangles = shadowTriangles,
                    IsVisible = true
                });
            }

            // Shadow uses the surface's ObjectOffsets and WorldPosition so it scrolls with the terrain
            shadowList.Add(new _3dObject
            {
                ObjectId = GameState.ObjectIdCounter++,
                ObjectName = "ObjectShadow",
                WorldPosition = new Vector3(),
                ParentSurface = inhabitant.ParentSurface,
                ObjectParts = shadowParts,
                ObjectOffsets = shadowObjectOffsets,
                Rotation = new Vector3 { x = 0, y = 0, z = 0 }
            });
        }
    }
}
