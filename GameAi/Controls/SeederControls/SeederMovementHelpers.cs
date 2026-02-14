using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using static CommonUtilities.GamePlayHelpers.GamePlayHelpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Helpers
{
    internal static class SeederMovementHelpers
    {
        // ============================
        // AI MOVEMENT CONFIGURATION
        // ============================
        // Tile and screen geometry:
        // - TileSize: world units per tile (pulled from SurfaceSetup).
        // - TilesPerScreen: number of tiles visible per screen viewport.
        private static int TileSize => SurfaceSetup.tileSize;            // e.g., 75 world units
        private static int TilesPerScreen => SurfaceSetup.viewPortSize;  // e.g., 18 tiles

        // Global roam behavior:
        // - RoamTilesDefault: how many tiles the random roam jumps when no screen target is found.
        private const int RoamTilesDefault = 10;

        // Screen sniffing behavior:
        // - SmellScoreWeight: multiplier for BioTileCount dominance when scoring screens.
        // - DistancePenaltyWeight: penalty per Manhattan distance step to break ties toward closer screens.
        private const int SmellScoreWeight = 1000;
        private const int DistancePenaltyWeight = 10;

        // Synchronization policy:
        // - SyncToSurfaceForLocalPick: whether to use synchronized world position for local bio target selection (should stay true).
        private const bool SyncToSurfaceForLocalPick = true;

        // Logging:
        // - EnableLogging: toggles helper-level logging via global Logger.
        private const bool EnableLogging = false;

        // World bounds derived from map setup:
        private static float MaxWorld => (MapSetup.globalMapSize - 1) * (float)TileSize;

        private static readonly Random _rng = new Random();

        internal readonly struct MoveVector
        {
            public readonly Vector3 Direction; // normalized
            public readonly float Length;      // world units

            public MoveVector(Vector3 direction, float length)
            {
                Direction = direction;
                Length = length;
            }
        }

        internal static Vector3 SyncronizeSeederWithSurfacePosition(_3dObject seederObject)
        {
            // Both Seeder and Surface have ObjectOffsets that represent their local "center" point relative to their WorldPosition.
            // To sync the Seeder to the Surface, we calculate the delta between their ObjectOffsets and apply that delta to the Seeder's WorldPosition.
            // This ensures that the Seeder's "center" (as defined by its ObjectOffsets) will match the Surface's "center" in world space and the right tile will be infected
            var seederOffset = seederObject.ObjectOffsets;
            var surfaceOffset = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets;

            var deltaX = surfaceOffset?.x - seederOffset?.x ?? 0;
            var deltaZ = surfaceOffset?.z - seederOffset?.z ?? 0;

            var surfaceWorld = seederObject.WorldPosition ?? new Vector3();

            return new Vector3
            {
                x = surfaceWorld.x - deltaX,
                y = surfaceWorld.y,
                z = surfaceWorld.z - deltaZ
            };
        }

        // ------------------------------------------------------------
        // WORLD -> TILE INDEX -> SCREEN INDEX
        // ------------------------------------------------------------
        internal static void GetScreenIndexFromWorldXZ(Vector3 worldPos, out int screenY, out int screenX)
        {
            int tileX = (int)(worldPos.x / TileSize);
            int tileY = (int)(worldPos.z / TileSize);

            screenX = tileX / TilesPerScreen;
            screenY = tileY / TilesPerScreen;
        }

        internal static Vector3 GetScreenCenterWorldXZ(int screenY, int screenX, float keepY)
        {
            int centerTileX = (screenX * TilesPerScreen) + (TilesPerScreen / 2);
            int centerTileY = (screenY * TilesPerScreen) + (TilesPerScreen / 2);

            return new Vector3
            {
                x = centerTileX * TileSize,
                y = keepY,
                z = centerTileY * TileSize
            };
        }

        // ------------------------------------------------------------
        // GLOBAL SNIFF: pick best screen within radius (by BioTileCount)
        // ------------------------------------------------------------
        internal static bool TryFindBestScreenInRadius(
            ScreenEcoMeta[,] ecoMap,
            int currentY,
            int currentX,
            int radius,
            out int bestY,
            out int bestX)
        {
            bestY = currentY;
            bestX = currentX;

            int bestScore = int.MinValue;

            int maxY = ecoMap.GetLength(0);
            int maxX = ecoMap.GetLength(1);

            int startY = Math.Max(0, currentY - radius);
            int endY = Math.Min(maxY - 1, currentY + radius);

            int startX = Math.Max(0, currentX - radius);
            int endX = Math.Min(maxX - 1, currentX + radius);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int smell = ecoMap[y, x].BioTileCount;
                    if (smell <= 0) continue;

                    int dy = y - currentY;
                    int dx = x - currentX;
                    int dist = Math.Abs(dy) + Math.Abs(dx);

                    // smell dominates, distance breaks ties (tunable weights above)
                    int score = (smell * SmellScoreWeight) - (dist * DistancePenaltyWeight);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestY = y;
                        bestX = x;
                    }
                }
            }

            return bestScore != int.MinValue;
        }

        // ------------------------------------------------------------
        // ROAM: N tiles in random direction (clamped)
        // ------------------------------------------------------------
        internal static Vector3 GetRandomRoamTargetWorldXZ(Vector3 fromWorld, int roamTiles)
        {
            int dir = _rng.Next(0, 8);

            int dx = 0, dz = 0;
            switch (dir)
            {
                case 0: dx = 0; dz = -1; break;
                case 1: dx = 1; dz = -1; break;
                case 2: dx = 1; dz = 0; break;
                case 3: dx = 1; dz = 1; break;
                case 4: dx = 0; dz = 1; break;
                case 5: dx = -1; dz = 1; break;
                case 6: dx = -1; dz = 0; break;
                case 7: dx = -1; dz = -1; break;
            }

            // Allow callers to pass 0 to use default roam distance
            int tiles = roamTiles > 0 ? roamTiles : RoamTilesDefault;
            float step = tiles * TileSize;

            float x = fromWorld.x + (dx * step);
            float z = fromWorld.z + (dz * step);

            if (x < 0) x = 0;
            if (z < 0) z = 0;
            if (x > MaxWorld) x = MaxWorld;
            if (z > MaxWorld) z = MaxWorld;

            return new Vector3 { x = x, y = fromWorld.y, z = z };
        }

        // ------------------------------------------------------------
        // LOCAL HUNT: pick BioTile inside CURRENT screen.
        // BioTiles contain GLOBAL WORLD coords:
        //   tile.X = worldX, tile.Y = worldZ
        // For map validation: tileIndex = world / TileSize
        // ------------------------------------------------------------
        internal static bool TryPickLocalBioTargetWorldXZ(
            SurfaceState surfaceState,
            I3dObject obj,
            Func<SurfaceData, TerrainType> getTerrainType,
            out Vector3 targetWorld,
            ref int localPickCursor,
            int maxAttemptsPerCall = 32,
            bool validateAgainstMap = true)
        {
            targetWorld = default;

            var ecoMap = surfaceState.ScreenEcoMetas;
            var map = surfaceState.Global2DMap;

            // Use synchronized world position to align seeder center with surface center (configurable)
            Vector3 alignedWorld = SyncToSurfaceForLocalPick
                ? SyncronizeSeederWithSurfacePosition((_3dObject)obj)
                : (Vector3)obj.WorldPosition;

            GetScreenIndexFromWorldXZ(alignedWorld, out int screenY, out int screenX);

            if ((uint)screenY >= (uint)ecoMap.GetLength(0) || (uint)screenX >= (uint)ecoMap.GetLength(1))
                return false;

            var bioTiles = ecoMap[screenY, screenX].BioTiles;
            if (bioTiles == null || bioTiles.Count == 0)
                return false;

            int attempts = Math.Min(maxAttemptsPerCall, bioTiles.Count);

            int skippedOutOfRange = 0;
            int skippedNotBio = 0;

            for (int i = 0; i < attempts; i++)
            {
                if (localPickCursor >= bioTiles.Count)
                    localPickCursor = 0;

                var tile = bioTiles[localPickCursor++]; // world coords

                // World bounds check
                if (tile.X < 0 || tile.Y < 0 || tile.X > MaxWorld || tile.Y > MaxWorld)
                {
                    skippedOutOfRange++;
                    continue;
                }

                // Convert WORLD -> tile indices for map validation
                int tileX = tile.X / TileSize;
                int tileY = tile.Y / TileSize;

                if ((uint)tileX >= (uint)MapSetup.globalMapSize || (uint)tileY >= (uint)MapSetup.globalMapSize)
                {
                    skippedOutOfRange++;
                    continue;
                }

                if (validateAgainstMap && map != null)
                {
                    var terrain = getTerrainType(map[tileY, tileX]);
                    bool stillBio = terrain == TerrainType.Grassland || terrain == TerrainType.Highlands;
                    if (!stillBio)
                    {
                        skippedNotBio++;
                        continue;
                    }
                }

                // IMPORTANT: BioTiles already are world coords -> do NOT multiply by tileSize again
                targetWorld = new Vector3
                {
                    x = tile.X,
                    y = alignedWorld.y, // keep Y from synchronized seeder world position
                    z = tile.Y
                };

                if (Logger.EnableFileLogging && EnableLogging)
                    Logger.Log($"AI:LOCAL_PICK onScreen={obj.IsOnScreen} screen=[{screenY},{screenX}] world=({tile.X},{tile.Y})");

                return true;
            }

            if (Logger.EnableFileLogging && EnableLogging)
                Logger.Log($"AI:LOCAL_EMPTY onScreen={obj.IsOnScreen} screen=[{screenY},{screenX}] list={bioTiles.Count} tried={attempts} outOfRange={skippedOutOfRange} notBio={skippedNotBio}");

            return false;
        }

        // ------------------------------------------------------------
        // ECO COUNT: decrement screen bio count for the screen containing (tileY,tileX)
        // Inputs are TILE INDICES (not world coords).
        // ------------------------------------------------------------
        internal static int? DecrementBioCountForTile(SurfaceState surfaceState, int tileY, int tileX)
        {
            int screenY = tileY / TilesPerScreen;
            int screenX = tileX / TilesPerScreen;

            var eco = surfaceState.ScreenEcoMetas;
            if ((uint)screenY >= (uint)eco.GetLength(0) || (uint)screenX >= (uint)eco.GetLength(1))
                return null;

            var meta = eco[screenY, screenX];
            if (meta.BioTileCount > 0) meta.BioTileCount--;
            eco[screenY, screenX] = meta;
            return meta.BioTileCount;
        }

        internal static Vector3 StepTowardTargetWorldXZ(Vector3 current, Vector3 target, float step)
        {
            float dx = target.x - current.x;
            float dz = target.z - current.z;

            float lenSq = dx * dx + dz * dz;
            if (lenSq <= 0.00001f)
                return current;

            float len = (float)Math.Sqrt(lenSq);
            if (step > len) step = len;

            float nx = dx / len;
            float nz = dz / len;

            return new Vector3
            {
                x = current.x + nx * step,
                y = current.y,
                z = current.z + nz * step
            };
        }
    }
}
