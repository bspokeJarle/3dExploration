using _3dRotations.World.Objects;
using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    using NumericsVector3 = System.Numerics.Vector3;
    using EngineVector3 = _3dSpecificsImplementations.Vector3;

    public class StarFieldHandler
    {
        private readonly Random random = new();

        // Max number of stars we want at any time.
        private const int maxStarCount = 60;

        // Stars that move outside this world radius are recycled.
        private const float despawnRadius = 1500f;

        // Local visible spawn area around camera (relative offsets).
        private const float SpawnXMin = -1200f;
        private const float SpawnXMax = 1200f;
        private const float SpawnYMin = -750f;
        private const float SpawnYMax = 750f;

        // Do not spawn new stars if surface is closer than this to the "camera" on Y.
        private const int GroundDistanceY = 250;

        // Z: in front of camera (into the screen) = NEGATIVE Z.
        private const float SpawnZFar = -1000f; // furthest away
        private const float SpawnZNear = -200f; // closest

        private readonly List<_3dObject> stars = new();

        // Our own base world position for each star.
        // This is the world position snapshot from Surface when the star is spawned/recycled.
        // starWorld = baseWorld + offset, and this baseWorld stays until the star is recycled.
        private readonly List<EngineVector3> starBaseWorldPositions = new();

        public ISurface ParentSurface { get; set; }

        /// <summary>
        /// Previous world position of Surface.
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
        /// Clears all stars and resets state.
        /// Used on scene restart / respawn / when surface becomes visible again.
        /// </summary>
        public void ClearStars()
        {
            stars.Clear();
            starBaseWorldPositions.Clear();
            PriorWorldPosition = ParentSurface?.GlobalMapPosition;

            if (enableLogging)
            {
                Logger.Log("[StarField] ClearStars() called. Stars list and baseWorldPositions cleared, PriorWorldPosition reset.");
            }
        }

        /// <summary>
        /// Called once per frame (after Surface/Ship is updated).
        /// - Recycles stars that are too far away.
        /// - Spawns new stars until we hit maxStarCount.
        /// </summary>
        public void GenerateStarfield()
        {
            if (ParentSurface == null)
                return;

            /*// If surface is too close to the camera, skip spawning.
            if (ParentSurface.GlobalMapPosition.y < GroundDistanceY)
                return;*/

            var currentWorldPos = ParentSurface.GlobalMapPosition;

            // 1) Recycle stars that moved too far away (no deletion).
            RecycleFarStars(currentWorldPos);

            if (enableLogging)
            {
                Logger.Log(
                    $"[StarField] Frame start: Surface=({currentWorldPos.x:0.0}, {currentWorldPos.y:0.0}, {currentWorldPos.z:0.0}), " +
                    $"Stars={stars.Count}"
                );
            }

            // 2) Do not spawn new stars if the surface is too close to "camera".
            if (currentWorldPos.y <= GroundDistanceY)
            {
                if (enableLogging)
                    Logger.Log($"[StarField] Surface Y={currentWorldPos.y:0.0} <= {GroundDistanceY} -> no new stars this frame.");

                PriorWorldPosition = currentWorldPos;
                ClearStars();
                return;
            }

            // First frame: set prior position.
            if (PriorWorldPosition == null)
            {
                PriorWorldPosition = currentWorldPos;
            }

            // 3) Fill up to maxStarCount (but not more).
            if (stars.Count < maxStarCount)
            {
                for (int i = stars.Count; i < maxStarCount; i++)
                {
                    // Offset is where we want the star relative to the direction we are flying.
                    var offset = FindRandomPosition(currentWorldPos);

                    // Snapshot of surface world position when this star is spawned.
                    var baseWorld = new EngineVector3
                    {
                        x = currentWorldPos.x,
                        y = currentWorldPos.y,
                        z = currentWorldPos.z
                    };

                    // Final world position for this star (this is what should feel "fixed"
                    // in world space until the star is recycled).
                    var starWorld = new EngineVector3
                    {
                        x = baseWorld.x + offset.x,
                        y = baseWorld.y + offset.y,
                        z = baseWorld.z + offset.z
                    };

                    // Create the star object. We keep offset as local data,
                    // but also give the star its own world position.
                    var star = Star.CreateStar(ParentSurface, offset);

                    // Ensure the 3d object has its own independent world position.
                    star.WorldPosition = starWorld;

                    stars.Add(star);
                    starBaseWorldPositions.Add(baseWorld);

                    if (enableLogging)
                    {
                        Logger.Log(
                            $"[StarField] Spawned star #{stars.Count - 1} " +
                            $"Offset=({offset.x:0.0}, {offset.y:0.0}, {offset.z:0.0}), " +
                            $"BaseWorld=({baseWorld.x:0.0}, {baseWorld.y:0.0}, {baseWorld.z:0.0}), " +
                            $"StarWorld=({starWorld.x:0.0}, {starWorld.y:0.0}, {starWorld.z:0.0})"
                        );
                    }
                }

                DebugLogStarPositions("Frame (after spawn+recycle)");
            }
            else
            {
                // We have enough stars; only recycling.
                DebugLogStarPositions("Frame (recycle only)");
            }

            PriorWorldPosition = currentWorldPos;
        }

        /// <summary>
        /// Finds a random local position to spawn a star at,
        /// weighted by movement direction (Y/Z/X).
        /// - Mostly vertical movement  -> top/bottom edge.
        /// - Mostly depth movement    -> far into the horizon.
        /// - Mostly horizontal        -> left/right edge.
        /// Also enforces a minimum vertical distance from the ground on Y for all stars.
        /// </summary>
        public IVector3 FindRandomPosition(IVector3 newWorldPosition)
        {
            // Default: random inside sweet spot in front of camera,
            // if we have no previous position or almost no movement.
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
                    // Choose edge based on how much movement we have on each axis.
                    float r = (float)(random.NextDouble() * sum);

                    bool useVertical = r < absY;
                    bool useDepth = !useVertical && (r < absY + absZ);
                    bool useHorizontal = !useVertical && !useDepth;

                    if (useVertical)
                    {
                        // Vertical component -> top/bottom edge.
                        // Coordinate system: -Y = up, +Y = down.
                        float signY = MathF.Sign(delta.Y);
                        float yEdge = signY > 0 ? SpawnYMax : SpawnYMin;

                        float x = RandomRange(SpawnXMin, SpawnXMax);
                        float z = RandomRange(SpawnZFar, SpawnZNear);

                        spawn = new NumericsVector3(x, yEdge, z);
                    }
                    else if (useDepth)
                    {
                        // Forward/backward -> spawn far into the horizon.
                        float z = RandomRange(SpawnZFar, SpawnZFar * 0.7f);
                        float x = RandomRange(SpawnXMin, SpawnXMax);
                        float y = RandomRange(SpawnYMin, SpawnYMax);

                        spawn = new NumericsVector3(x, y, z);
                    }
                    else // useHorizontal
                    {
                        // Sideways -> left/right edge.
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

            // Return as engine Vector3 (ObjectOffsets).
            return new EngineVector3
            {
                x = spawn.X,
                y = spawn.Y,
                z = spawn.Z
            };
        }



        /// <summary>
        /// Returns current stars so GameWorldManager can add them to WorldInhabitants.
        /// </summary>
        public List<_3dObject> GetStars()
        {
            if (stars.Count == 0) return null;
            return stars;
        }

        /// <summary>
        /// Recycles stars that are too far away in WORLD SPACE.
        /// We use our own baseWorld list so stars do not "follow" Surface automatically.
        /// Each star effectively has: world = baseWorld + offset.
        /// </summary>
        private void RecycleFarStars(IVector3 surfaceWorld)
        {
            if (ParentSurface == null)
                return;

            float maxDistSq = despawnRadius * despawnRadius;

            // Center we measure from – today: Surface.world (camera is effectively above this).
            IVector3 centerWorld = surfaceWorld;

            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];

                // Get our own base world snapshot for this star.
                EngineVector3 baseWorld;
                if (i < starBaseWorldPositions.Count)
                {
                    baseWorld = starBaseWorldPositions[i];
                }
                else
                {
                    // Fallback if something got out of sync.
                    baseWorld = new EngineVector3
                    {
                        x = surfaceWorld.x,
                        y = surfaceWorld.y,
                        z = surfaceWorld.z
                    };
                    if (enableLogging)
                    {
                        Logger.Log($"[StarField] WARNING: baseWorld missing for star[{i}], using surfaceWorld as fallback.");
                    }
                }

                var offsets = star.ObjectOffsets;

                // Final world position for the star (this is what should stay until we recycle it).
                var starWorld = new EngineVector3
                {
                    x = baseWorld.x + offsets.x,
                    y = baseWorld.y + offsets.y,
                    z = baseWorld.z + offsets.z
                };

                // Keep the 3d object in sync with our calculated world position.
                star.WorldPosition = starWorld;

                float dx = starWorld.x - centerWorld.x;
                float dy = starWorld.y - centerWorld.y;
                float dz = starWorld.z - centerWorld.z;

                float distSq = dx * dx + dy * dy + dz * dz;
                float dist = MathF.Sqrt(distSq);

                bool shouldRecycle = distSq > maxDistSq;

                if (enableLogging)
                {
                    Logger.Log(
                        $"[StarField] STAR[{i}] Offsets=({offsets.x:0.0}, {offsets.y:0.0}, {offsets.z:0.0}) " +
                        $"BaseWorldPos=({baseWorld.x:0.0}, {baseWorld.y:0.0}, {baseWorld.z:0.0}) " +
                        $"StarWorldPos=({starWorld.x:0.0}, {starWorld.y:0.0}, {starWorld.z:0.0}) " +
                        $"Dist={dist:0.0} Max={despawnRadius:0.0} -> {(shouldRecycle ? "RECYCLE" : "OK")}"
                    );
                }

                if (shouldRecycle)
                {
                    // New offset based on current movement direction.
                    var newOffset = FindRandomPosition(centerWorld);

                    // New base world snapshot at the moment of recycle.
                    var newBaseWorld = new EngineVector3
                    {
                        x = centerWorld.x,
                        y = centerWorld.y,
                        z = centerWorld.z
                    };

                    // New final world position for this recycled star.
                    var newStarWorld = new EngineVector3
                    {
                        x = newBaseWorld.x + newOffset.x,
                        y = newBaseWorld.y + newOffset.y,
                        z = newBaseWorld.z + newOffset.z
                    };

                    if (i < starBaseWorldPositions.Count)
                        starBaseWorldPositions[i] = newBaseWorld;
                    else
                        starBaseWorldPositions.Add(newBaseWorld);

                    stars[i].ObjectOffsets = newOffset;
                    stars[i].WorldPosition = newStarWorld;

                    if (enableLogging)
                    {
                        Logger.Log(
                            $"[StarField] RECYCLE[{i}] new BaseWorld=({newBaseWorld.x:0.0}, {newBaseWorld.y:0.0}, {newBaseWorld.z:0.0}), " +
                            $"new Offsets=({newOffset.x:0.0}, {newOffset.y:0.0}, {newOffset.z:0.0}), " +
                            $"new StarWorld=({newStarWorld.x:0.0}, {newStarWorld.y:0.0}, {newStarWorld.z:0.0})"
                        );
                    }
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
