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
        private readonly Random random = new Random();

        // Debug / perf
        private const int maxStarCount = 20;

        // Culling rett utenfor synlig område.
        private const float despawnRadius = 4000f;

        // Synlig område: ca -2000..+2000, sweet spot -1500..+1500.
        private const float visibleMinXY = -1500f;
        private const float visibleMaxXY = 1500f;

        // Global trygg Z-range for motoren din:
        private const float safeMinZ = -2000f;
        private const float safeMaxZ = 1200f;

        // For stjerner vil vi ha dem LANGT fremme, men innenfor safesone:
        private const float starMinZ = 600f;   // nær horisont
        private const float starMaxZ = 1200f;  // maks før ting "går bananas"

        // Vertikal bånd for stjerner når vi "scroller inn i feltet":
        // Litt mer opp enn ned.
        private const float centerBandMinY = -1100f;
        private const float centerBandMaxY = -300f;

        private readonly List<_3dObject> stars = new List<_3dObject>();

        private ISurface ParentSurface { get; }

        /// <summary>
        /// Sist kjente world-posisjon til surface (for å beregne retning).
        /// </summary>
        private IVector3 PriorWorldPosition { get; set; }

        private bool hasPriorWorldPosition = false;

        // Debug toggle
        private bool enableLogging = false;

        public StarFieldHandler(ISurface surface)
        {
            ParentSurface = surface;
            if (surface != null)
            {
                PriorWorldPosition = surface.GlobalMapPosition;
                hasPriorWorldPosition = true;
            }
        }

        public bool HasStars()
        {
            return stars.Count > 0;
        }

        public void SetLogging(bool enabled)
        {
            enableLogging = enabled;
        }

        /// <summary>
        /// n2: Manuelt tømme stjernefeltet (for eksempel ved krasj / reset).
        /// </summary>
        public void ClearStars()
        {
            if (enableLogging)
            {
                Logger.Log("[StarField] ClearStars() called. Removing " + stars.Count + " stars.");
            }

            stars.Clear();

            if (ParentSurface != null)
            {
                PriorWorldPosition = ParentSurface.GlobalMapPosition;
                hasPriorWorldPosition = true;
            }
            else
            {
                hasPriorWorldPosition = false;
            }
        }

        /// <summary>
        /// Kalles én gang per frame etter at surface/ship er oppdatert.
        /// </summary>
        public void GenerateStarfield()
        {
            if (ParentSurface == null)
                return;

            var currentWorldPos = ParentSurface.GlobalMapPosition;

            // Alltid cull stjerner som er for langt unna.
            CullFarStars();

            if (enableLogging)
            {
                Logger.Log(
                    "[StarField] Frame start: Surface=(" +
                    currentWorldPos.x.ToString("0.0") + ", " +
                    currentWorldPos.y.ToString("0.0") + ", " +
                    currentWorldPos.z.ToString("0.0") + "), Stars=" + stars.Count);
            }

            // n1: Når bakken er "tilbake i view", fjern alle stjerner.
            if (currentWorldPos.y <= 200f)
            {
                if (enableLogging)
                {
                    Logger.Log("[StarField] Surface Y=" + currentWorldPos.y.ToString("0.0") +
                               " <= 250 -> clearing all stars.");
                }

                ClearStars();
                return;
            }

            // Sørg for at vi har en prior posisjon å sammenligne med.
            if (!hasPriorWorldPosition)
            {
                PriorWorldPosition = currentWorldPos;
                hasPriorWorldPosition = true;
            }

            // Har vi allerede maks antall stjerner? Ikke spawn flere.
            if (stars.Count >= maxStarCount)
            {
                if (enableLogging)
                {
                    Logger.Log("[StarField] Stars already at max (" + maxStarCount + "). No spawn this frame.");
                    DebugLogStarPositions("Frame (no spawn)");
                }

                PriorWorldPosition = currentWorldPos;
                return;
            }

            // Spawn nye stjerner opp til maxStarCount.
            for (int i = stars.Count; i < maxStarCount; i++)
            {
                var offset = FindSpawnPosition(currentWorldPos);

                var star = Star.CreateStar(ParentSurface, offset);
                stars.Add(star);

                if (enableLogging)
                {
                    Logger.Log(
                        "[StarField] Spawned star #" + stars.Count +
                        " at local offset (" +
                        offset.x.ToString("0.0") + ", " +
                        offset.y.ToString("0.0") + ", " +
                        offset.z.ToString("0.0") + ")");
                }
            }

            PriorWorldPosition = currentWorldPos;

            if (enableLogging)
            {
                DebugLogStarPositions("Frame (after spawn)");
            }
        }

        /// <summary>
        /// Velger spawn-posisjon basert på bevegelsesretning.
        ///
        /// -Y = opp, +Y = ned
        /// -X = venstre, +X = høyre
        /// Større +Z = lengre frem/inn i perspektivet (horisont).
        ///
        /// Viktig:
        ///  - Ved vertikal og fremover-bevegelse spawner vi i et bånd rundt midten (centerBandMinY..centerBandMaxY),
        ///    med Z i [starMinZ, starMaxZ] slik at du "flyr inn i stjernene".
        ///  - Ved sideveis bevegelse bruker vi venstre/høyre kant på X, men fortsatt midt-bånd på Y og langt fremme på Z.
        /// </summary>
        private IVector3 FindSpawnPosition(IVector3 newWorldPosition)
        {
            // Default: random et sted langt fremme, rundt midten.
            NumericsVector3 spawn = new NumericsVector3(
                RandomRange(visibleMinXY, visibleMaxXY),
                RandomRange(centerBandMinY, centerBandMaxY),
                RandomRange(starMinZ, starMaxZ)
            );

            string mode = "Default";

            if (hasPriorWorldPosition)
            {
                NumericsVector3 prev = ToNumerics(PriorWorldPosition);
                NumericsVector3 curr = ToNumerics(newWorldPosition);
                NumericsVector3 delta = curr - prev;

                if (delta.LengthSquared() > 0.0001f)
                {
                    float absX = Math.Abs(delta.X);
                    float absY = Math.Abs(delta.Y);
                    float absZ = Math.Abs(delta.Z);

                    if (absY >= absX && absY >= absZ)
                    {
                        // DOMINERENDE VERTIKAL BEVEGELSE
                        // I stedet for topp/bunn-kant spawner vi i midt-båndet,
                        // så du "scroller inn i" stjernefeltet.
                        float x = RandomRange(visibleMinXY, visibleMaxXY);
                        float y = RandomRange(centerBandMinY, centerBandMaxY);
                        float z = RandomRange(starMinZ, starMaxZ);

                        spawn = new NumericsVector3(x, y, z);
                        mode = "VerticalCenterBand";
                    }
                    else if (absZ >= absX && absZ >= absY)
                    {
                        // DOMINERENDE FREM/BAK-BEVEGELSE
                        // Nye stjerner dypt fremme i perspektiv, også rundt midten.
                        float z = RandomRange(starMinZ, starMaxZ);
                        float x = RandomRange(visibleMinXY, visibleMaxXY);
                        float y = RandomRange(centerBandMinY, centerBandMaxY);

                        spawn = new NumericsVector3(x, y, z);
                        mode = "DepthCenterBand";
                    }
                    else
                    {
                        // DOMINERENDE SIDEVEIS
                        float signX = Math.Sign(delta.X);

                        // delta.X > 0 => verden mot høyre (skip visuelt mot venstre)
                        //               -> stjerner inn fra HØYRE kant.
                        // delta.X < 0 => verden mot venstre (skip visuelt mot høyre)
                        //               -> stjerner inn fra VENSTRE kant.
                        float x = (signX > 0)
                            ? visibleMaxXY   // høyre kant
                            : visibleMinXY;  // venstre kant

                        float y = RandomRange(centerBandMinY, centerBandMaxY);
                        float z = RandomRange(starMinZ, starMaxZ);

                        spawn = new NumericsVector3(x, y, z);
                        mode = "HorizontalEdge";
                    }

                    if (enableLogging)
                    {
                        Logger.Log(
                            "[StarField] FindSpawnPosition mode=" + mode +
                            ", delta=(" +
                            delta.X.ToString("0.0") + ", " +
                            delta.Y.ToString("0.0") + ", " +
                            delta.Z.ToString("0.0") + "), spawn=(" +
                            spawn.X.ToString("0.0") + ", " +
                            spawn.Y.ToString("0.0") + ", " +
                            spawn.Z.ToString("0.0") + ")");
                    }
                }
                else if (enableLogging)
                {
                    Logger.Log("[StarField] FindSpawnPosition: delta almost zero, using default random in center band (far depth).");
                }
            }
            else if (enableLogging)
            {
                Logger.Log("[StarField] FindSpawnPosition: no PriorWorldPosition, using default random in center band (far depth).");
            }

            // Sikkerhetsclamp: hold oss innenfor global Z-range [-2000, 1200]
            float clampedZ = ClampZ(spawn.Z);
            spawn.Z = clampedZ;

            return new EngineVector3
            {
                x = spawn.X,
                y = spawn.Y,
                z = spawn.Z
            };
        }

        public List<_3dObject> GetStars()
        {
            return stars;
        }

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
                            "[StarField] Culling star at offsets=(" +
                            p.x.ToString("0.0") + ", " +
                            p.y.ToString("0.0") + ", " +
                            p.z.ToString("0.0") + "), distSq=" +
                            distSq.ToString("0.0") + " (>" + maxDistSq.ToString("0.0") + ")");
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

        private float ClampZ(float z)
        {
            if (z < safeMinZ) return safeMinZ;
            if (z > safeMaxZ) return safeMaxZ;
            return z;
        }

        private void DebugLogStarPositions(string context)
        {
            if (!enableLogging)
                return;

            if (stars.Count == 0)
            {
                Logger.Log("[StarField] " + context + ": No stars.");
                return;
            }

            Logger.Log("[StarField] " + context + ": Stars=" + stars.Count);

            for (int i = 0; i < stars.Count; i++)
            {
                var p = stars[i].ObjectOffsets;
                Logger.Log(
                    "[StarField]   Star[" + i + "] offsets=(" +
                    p.x.ToString("0.0") + ", " +
                    p.y.ToString("0.0") + ", " +
                    p.z.ToString("0.0") + ")");
            }
        }
    }
}
