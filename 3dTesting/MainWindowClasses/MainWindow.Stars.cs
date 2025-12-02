using _3dRotations.World.Objects;
using Domain;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTesting.MainWindowClasses
{
    using NumericsVector3 = System.Numerics.Vector3;
    using EngineVector3 = _3dSpecificsImplementations.Vector3;
    using static Domain._3dSpecificsImplementations;

    public class StarFieldHandler
    {
        private readonly Random random = new();

        // For debugging vi holder dette lavt fortsatt.
        private const int maxStarCount = 20;

        // Stjerner bør culles rett utenfor synlig område.
        private const float despawnRadius = 2200f;

        // Synlig område: ca -2000..+2000, sweet spot -1500..+1500.
        private const float visibleMinXY = -1500f;
        private const float visibleMaxXY = 1500f;

        // Z: foran kamera (inn i skjermen) = negativ Z.
        // Vi sier at “horisont” er ca. -1800, og vi vil ikke nærmere enn -500.
        private const float visibleMinZ = -2000f; // lengst unna
        private const float visibleMaxZ = -500f;  // nærmest kamera

        // "Edges" of the local starfield in each axis (screen-ish coordinates).
        private const float spawnEdgeY = 3500f;   // top/bottom of view
        private const float spawnEdgeX = 4500f;   // left/right of view
        private const float spawnEdgeZ = 5500f;   // deep "horizon" / depth

        // How much we spread stars along the secondary axes.
        private const float verticalSpreadX = 4000f;
        private const float verticalSpreadZ = 4000f;

        private const float horizontalSpreadY = 3000f;
        private const float horizontalSpreadZ = 4000f;

        private const float depthSpreadX = 4000f;
        private const float depthSpreadY = 3000f;

        private readonly List<_3dObject> stars = new();

        private ISurface ParentSurface { get; }

        /// <summary>
        /// Last known world position of the surface.
        /// Used to estimate movement direction between frames.
        /// </summary>
        private IVector3? PriorWorldPosition { get; set; }

        // Debug toggle. Set to true when you want to inspect behaviour in logs.
        private bool enableLogging = true;

        public StarFieldHandler(ISurface surface)
        {
            ParentSurface = surface;
            PriorWorldPosition = surface?.GlobalMapPosition;
        }

        public bool HasStars()
        {
            return stars != null && stars.Count > 0;
        }

        /// <summary>
        /// Optional external switch if you want to enable/disable logging at runtime.
        /// </summary>
        public void SetLogging(bool enabled)
        {
            enableLogging = enabled;
        }

        /// <summary>
        /// Call this once per frame (after surface/ship are updated)
        /// to maintain the starfield.
        /// - Culls stars that moved too far away,
        /// - Spawns new stars up to maxStarCount when surface is "low enough".
        /// </summary>
        public void GenerateStarfield()
        {
            if (ParentSurface == null)
                return;

            var currentWorldPos = ParentSurface.GlobalMapPosition;

            // Always cull stars that are too far from center.
            CullFarStars();

            if (enableLogging)
            {
                Logger.Log(
                    $"[StarField] Frame start: Surface=({currentWorldPos.x:0.0}, {currentWorldPos.y:0.0}, {currentWorldPos.z:0.0}), " +
                    $"Stars={stars.Count}"
                );
            }

            // If the surface is not "low enough" we do not spawn new stars.
            if (currentWorldPos.y <= 250)
            {
                if (enableLogging)
                {
                    Logger.Log($"[StarField] Surface Y={currentWorldPos.y:0.0} <= 250 -> no new stars this frame.");
                }

                PriorWorldPosition = currentWorldPos;
                return;
            }

            // Ensure we have a prior position to compare with.
            if (PriorWorldPosition == null)
            {
                PriorWorldPosition = currentWorldPos;
            }

            // If we already have enough stars, just update PriorWorldPosition and exit.
            if (stars.Count >= maxStarCount)
            {
                if (enableLogging)
                {
                    Logger.Log($"[StarField] Stars already at max ({maxStarCount}). No spawn this frame.");
                }

                PriorWorldPosition = currentWorldPos;

                // For debugging: log a snapshot of current star positions.
                DebugLogStarPositions("Frame (no spawn)");

                return;
            }

            // Spawn new stars up to maxStarCount.
            for (int i = stars.Count; i < maxStarCount; i++)
            {
                var offset = FindRandomPosition(currentWorldPos);

                var star = Star.CreateStar(ParentSurface, offset);

                stars.Add(star);

                if (enableLogging)
                {
                    Logger.Log(
                        $"[StarField] Spawned star #{stars.Count} at local offset " +
                        $"({offset.x:0.0}, {offset.y:0.0}, {offset.z:0.0})"
                    );
                }
            }

            // Store current position for next frame's movement estimation.
            PriorWorldPosition = currentWorldPos;

            // For debugging: log a snapshot of current star positions.
            DebugLogStarPositions("Frame (after spawn)");
        }

        /// <summary>
        /// Using prior world position vs the new world position of the surface
        /// to decide which screen edge to spawn the star from.
        ///
        /// - Dominant Y movement: spawn at top/bottom edge.
        /// - Dominant Z movement: spawn deep in the horizon.
        /// - Dominant X movement: spawn left/right.
        ///
        /// Coordinates are local around (0,0,0) = center of the screen.
        /// </summary>
        public IVector3 FindRandomPosition(IVector3 newWorldPosition)
        {
            // Default: hvis vi ikke har bevegelse, sleng en stjerne tilfeldig
            // et sted foran kamera, innenfor sweet spot.
            NumericsVector3 spawn = new NumericsVector3(
                RandomRange(visibleMinXY, visibleMaxXY),
                RandomRange(visibleMinXY, visibleMaxXY),
                RandomRange(visibleMinZ, visibleMaxZ)
            );

            string mode = "Default";

            if (PriorWorldPosition != null)
            {
                NumericsVector3 prev = ToNumerics(PriorWorldPosition);
                NumericsVector3 curr = ToNumerics(newWorldPosition);
                NumericsVector3 delta = curr - prev;

                if (delta.LengthSquared() > 0.0001f)
                {
                    float absX = MathF.Abs(delta.X);
                    float absY = MathF.Abs(delta.Y);
                    float absZ = MathF.Abs(delta.Z);

                    if (absY >= absX && absY >= absZ)
                    {
                        // DOMINERENDE VERTIKAL BEVEGELSE
                        // -Y = oppover, +Y = nedover i ditt system.
                        float signY = MathF.Sign(delta.Y);

                        // Hvis dette føles feil vei, bytt til: -signY.
                        float y = signY > 0
                            ? visibleMaxXY   // nedre kant
                            : visibleMinXY;  // øvre kant

                        float x = RandomRange(visibleMinXY, visibleMaxXY);
                        float z = RandomRange(visibleMinZ, visibleMaxZ);

                        spawn = new NumericsVector3(x, y, z);
                        mode = "VerticalEdge";
                    }
                    else if (absZ >= absX && absZ >= absY)
                    {
                        // DOMINERENDE FREM/BAK
                        // Uansett retning på delta.Z vil vi ha stjernene foran kamera:
                        // dvs. negativ Z i sweet spot.
                        float z = RandomRange(visibleMinZ, visibleMinZ * 0.7f); // litt "dypere" i midten

                        float x = RandomRange(visibleMinXY, visibleMaxXY);
                        float y = RandomRange(visibleMinXY, visibleMaxXY);

                        spawn = new NumericsVector3(x, y, z);
                        mode = "DepthEdge";
                    }
                    else
                    {
                        // DOMINERENDE SIDEVEIS
                        float signX = MathF.Sign(delta.X);

                        float x = signX > 0
                            ? visibleMaxXY   // høyre kant
                            : visibleMinXY;  // venstre kant

                        float y = RandomRange(visibleMinXY, visibleMaxXY);
                        float z = RandomRange(visibleMinZ, visibleMaxZ);

                        spawn = new NumericsVector3(x, y, z);
                        mode = "HorizontalEdge";
                    }

                    if (enableLogging)
                    {
                        Logger.Log(
                            $"[StarField] FindRandomPosition mode={mode}, delta=({delta.X:0.0}, {delta.Y:0.0}, {delta.Z:0.0}), " +
                            $"spawn=({spawn.X:0.0}, {spawn.Y:0.0}, {spawn.Z:0.0})"
                        );
                    }
                }
                else if (enableLogging)
                {
                    Logger.Log("[StarField] FindRandomPosition: delta almost zero, using default random inside sweet spot.");
                }
            }
            else if (enableLogging)
            {
                Logger.Log("[StarField] FindRandomPosition: no PriorWorldPosition, using default random inside sweet spot.");
            }

            return new EngineVector3
            {
                x = spawn.X,
                y = spawn.Y,
                z = spawn.Z
            };
        }


        /// <summary>
        /// Returns the current star list so the caller can add them to WorldInhabitants, etc.
        /// </summary>
        public List<_3dObject> GetStars()
        {
            if (stars.Count == 0) return null;
            return stars;
        }

        /// <summary>
        /// Removes any stars that are too far from the origin (0,0,0) in local space.
        /// This is a cheap approximation for "off-screen" for a starfield centered around the camera.
        /// </summary>
        private void CullFarStars()
        {
            float maxDistSq = despawnRadius * despawnRadius;

            for (int i = stars.Count - 1; i >= 0; i--)
            {
                var p = stars[i].ObjectOffsets;
                float dx = p.x;
                float dy = p.y;
                float dz = p.z;
                float distSq = dx * dx + dy * dy + dz * dz;

                if (distSq > maxDistSq)
                {
                    if (enableLogging)
                    {
                        Logger.Log(
                            $"[StarField] Culling star at offsets=({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), distSq={distSq:0.0} (>{maxDistSq:0.0})"
                        );
                    }

                    stars.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Helper: convert engine IVector3 to System.Numerics.Vector3 for math operations.
        /// </summary>
        private static NumericsVector3 ToNumerics(IVector3 v)
        {
            return new NumericsVector3(v.x, v.y, v.z);
        }

        private float RandomRange(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// For debugging: logs positions of all stars in the current frame.
        /// </summary>
        private void DebugLogStarPositions(string context)
        {
            if (!enableLogging)
                return;

            if (stars.Count == 0)
            {
                Logger.Log($"[StarField] {context}: No stars.");
                return;
            }

            Logger.Log($"[StarField] {context}: Stars={stars.Count}");

            for (int i = 0; i < stars.Count; i++)
            {
                var p = stars[i].ObjectOffsets;
                Logger.Log(
                    $"[StarField]   Star[{i}] offsets=({p.x:0.0}, {p.y:0.0}, {p.z:0.0})"
                );
            }
        }

        /// <summary>
        /// Helper: random unit vector on a sphere.
        /// (Not used in the edge logic anymore, but kept for future experiments.)
        /// </summary>
        private NumericsVector3 GetRandomDirectionOnSphere()
        {
            float x = (float)(random.NextDouble() * 2.0 - 1.0);
            float y = (float)(random.NextDouble() * 2.0 - 1.0);
            float z = (float)(random.NextDouble() * 2.0 - 1.0);

            var v = new NumericsVector3(x, y, z);
            float lenSq = v.LengthSquared();

            if (lenSq < 0.0001f)
                return new NumericsVector3(0, 0, -1); // fallback

            return NumericsVector3.Normalize(v);
        }
    }
}
