using _3dRotations.World.Objects;
using Domain;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    using NumericsVector3 = System.Numerics.Vector3;
    using EngineVector3 = _3dSpecificsImplementations.Vector3;

    public class StarFieldHandler
    {
        private readonly Random random = new();

        // For debugging / tuning. Increase when you're happy with visuals.
        private const int maxStarCount = 40;

        // Stars are culled if they move too far from screen center in local space.
        private const float despawnRadius = 2200f;

        // Visible area in local space (sweet spot ~ -1500..+1500).
        private const float SpawnXMin = -1500f;
        private const float SpawnXMax = 1500f;
        private const float SpawnYMin = -1500f;
        private const float SpawnYMax = 1500f;

        // Z: in front of camera (into screen) = NEGATIVE Z in your system.
        // Horizon area:
        private const float SpawnZFar = -2000f; // farthest
        private const float SpawnZNear = -800f; // closest

        private readonly List<_3dObject> stars = new();

        private ISurface ParentSurface { get; }

        /// <summary>
        /// Last known world position of the surface.
        /// Used to estimate movement direction between frames.
        /// </summary>
        private IVector3? PriorWorldPosition { get; set; }

        private bool enableLogging = false;

        public StarFieldHandler(ISurface surface)
        {
            ParentSurface = surface;
            PriorWorldPosition = surface?.GlobalMapPosition;
        }

        public void SetLogging(bool enabled)
        {
            enableLogging = enabled;
        }

        public bool HasStars()
        {
            return stars != null && stars.Count > 0;
        }

        /// <summary>
        /// Clear all current stars and reset state.
        /// Useful when resetting level, switching scenes, or when surface becomes visible again.
        /// </summary>
        public void ClearStars()
        {
            stars.Clear();
            // Optional: we can also reset PriorWorldPosition so movement-based spawning
            // doesn't get a huge delta after a long pause.
            PriorWorldPosition = ParentSurface?.GlobalMapPosition;

            if (enableLogging)
            {
                Logger.Log("[StarField] ClearStars() called. Stars list cleared and PriorWorldPosition reset.");
            }
        }

        /// <summary>
        /// Call this once per frame (after Surface/Ship are updated).
        /// - Culls stars that moved too far away.
        /// - Spawns new stars up to maxStarCount, based on movement direction.
        /// </summary>
        public void GenerateStarfield()
        {
            if (ParentSurface == null)
                return;

            var currentWorldPos = ParentSurface.GlobalMapPosition;

            // Cull stars that moved far outside the visible area.
            CullFarStars();

            if (enableLogging)
            {
                Logger.Log(
                    $"[StarField] Frame start: Surface=({currentWorldPos.x:0.0}, {currentWorldPos.y:0.0}, {currentWorldPos.z:0.0}), " +
                    $"Stars={stars.Count}"
                );
            }

            // Do not spawn stars if the surface is "too close" (high up).
            if (currentWorldPos.y <= 250)
            {
                if (enableLogging)
                    Logger.Log($"[StarField] Surface Y={currentWorldPos.y:0.0} <= 250 -> no new stars this frame.");

                PriorWorldPosition = currentWorldPos;
                return;
            }

            if (PriorWorldPosition == null)
            {
                PriorWorldPosition = currentWorldPos;
            }

            if (stars.Count >= maxStarCount)
            {
                if (enableLogging)
                {
                    Logger.Log($"[StarField] Stars already at max ({maxStarCount}). No spawn this frame.");
                    DebugLogStarPositions("Frame (no spawn)");
                }

                PriorWorldPosition = currentWorldPos;
                return;
            }

            // Spawn new stars up to maxStarCount.
            for (int i = stars.Count; i < maxStarCount; i++)
            {
                var offset = FindRandomPosition(currentWorldPos);

                // randomOffset becomes ObjectOffsets on the star.
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

            PriorWorldPosition = currentWorldPos;

            DebugLogStarPositions("Frame (after spawn)");
        }

        /// <summary>
        /// Find a random local position to spawn a star at,
        /// weighted by movement direction (Y/Z/X).
        /// 
        /// - Mostly vertical movement -> more stars from top/bottom.
        /// - Mostly depth movement  -> more stars deep in the horizon.
        /// - Mostly horizontal     -> more stars from left/right edge.
        /// </summary>
        public IVector3 FindRandomPosition(IVector3 newWorldPosition)
        {
            // Default: random within sweet spot in front of camera,
            // if we don't have prior position or have almost no movement.
            var spawn = new NumericsVector3(
                RandomRange(SpawnXMin, SpawnXMax),
                RandomRange(SpawnYMin, SpawnYMax),
                RandomRange(SpawnZFar, SpawnZNear) // negative Z = in front of camera
            );

            if (PriorWorldPosition != null)
            {
                var prev = ToNumerics(PriorWorldPosition);
                var curr = ToNumerics(newWorldPosition);
                var delta = curr - prev;

                float absX = MathF.Abs(delta.X);
                float absY = MathF.Abs(delta.Y);
                float absZ = MathF.Abs(delta.Z);

                float sum = absX + absY + absZ;

                if (sum > 0.0001f)
                {
                    // Weighted choice of "edge" based on how much movement we have in each axis.
                    float r = (float)(random.NextDouble() * sum);

                    bool useVertical = r < absY;
                    bool useDepth = !useVertical && (r < absY + absZ);
                    bool useHorizontal = !useVertical && !useDepth;

                    if (useVertical)
                    {
                        // Vertical component -> top/bottom edge.
                        // In your system: -Y = up, +Y = down.
                        float signY = MathF.Sign(delta.Y);

                        // If this feels inverted visually, just swap SpawnYMin/SpawnYMax here.
                        float yEdge = signY > 0 ? SpawnYMax : SpawnYMin;

                        float x = RandomRange(SpawnXMin, SpawnXMax);
                        float z = RandomRange(SpawnZFar, SpawnZNear);

                        spawn = new NumericsVector3(x, yEdge, z);
                    }
                    else if (useDepth)
                    {
                        // Forward/backward movement -> spawn deep in the horizon.
                        // Regardless of sign of delta.Z, we want stars IN FRONT of camera:
                        // negative Z in a deeper interval.
                        float z = RandomRange(SpawnZFar, SpawnZFar * 0.7f);

                        float x = RandomRange(SpawnXMin, SpawnXMax);
                        float y = RandomRange(SpawnYMin, SpawnYMax);

                        spawn = new NumericsVector3(x, y, z);
                    }
                    else // useHorizontal
                    {
                        // Sideways movement -> left/right edge.
                        float signX = MathF.Sign(delta.X);

                        float xEdge = signX > 0 ? SpawnXMax : SpawnXMin;
                        float y = RandomRange(SpawnYMin, SpawnYMax);
                        float z = RandomRange(SpawnZFar, SpawnZNear);

                        spawn = new NumericsVector3(xEdge, y, z);
                    }

                    if (enableLogging)
                    {
                        Logger.Log(
                            $"[StarField] FindRandomPosition: " +
                            $"delta=({delta.X:0.0}, {delta.Y:0.0}, {delta.Z:0.0}), " +
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

            // Return as engine-Vector3 (ObjectOffsets).
            return new EngineVector3
            {
                x = spawn.X,
                y = spawn.Y,
                z = spawn.Z
            };
        }

        /// <summary>
        /// Return current stars so caller can push them into WorldInhabitants, etc.
        /// </summary>
        public List<_3dObject> GetStars()
        {
            if (stars.Count == 0) return null;
            return stars;
        }

        /// <summary>
        /// Remove stars that are too far away (cheap off-screen approximation).
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
                            $"[StarField] Culling star at offsets=({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), " +
                            $"distSq={distSq:0.0} (>{maxDistSq:0.0})"
                        );
                    }

                    stars.RemoveAt(i);
                }
            }
        }

        private static NumericsVector3 ToNumerics(IVector3 v)
        {
            return new NumericsVector3(v.x, v.y, v.z);
        }

        private float RandomRange(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }

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
    }
}
