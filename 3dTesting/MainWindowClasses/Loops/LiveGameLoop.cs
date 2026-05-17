using _3dRotations.World.Objects;
using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using CommonUtilities.Persistence;
using Domain;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.SeederControls;
using GameAudioInstances;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses.Loops
{
    public class LiveGameLoop : IGameLoop<_2dTriangleMesh>
    {
        private const int perfLogInterval = 10;

        private long FrameCounter = 0;
        private readonly Stopwatch frameTimer = new();
        private long performanceFrameCount = 0;
        private double averageFrameMs = 0;
        private double averageHeadroomMs = 0;
        private int AiUpdateCounter = 0;
        private const int AiUpdateInterval = 5; // Update offscreen AI every 5 frames
        private readonly _3dTo2d From3dTo2d = new();
        private readonly _3dRotationCommon Rotate3d = new();
        private readonly ParticleManager particleManager = new();
        private readonly WeaponsManager weaponsManager = new();
        private readonly ObjectShadowManager objectShadowManager = new();
        private StarFieldHandler StarFieldHandler { get; set; }

        private readonly IAudioPlayer audioPlayer = new NAudioAudioPlayer(AudioSetup.AudioBasePath);
        private readonly ISoundRegistry soundRegistry = new JsonSoundRegistry(AudioSetup.SoundRegistryPath);
        private SoundDefinition MusicDef { get; set; } = null;
        private bool MusicIsPlaying { get; set; } = false;
        private string CurrentSceneMusicId { get; set; } = string.Empty;

        public string DebugMessage { get; set; }
        private bool enableLocalLogging = false;
        private const bool enableProgressionLogging = false;
        public bool FadeOutWorld { get; set; } = false;
        public bool FadeInWorld { get; set; } = false;
        public bool SceneResetReady { get; set; } = false;
        private bool _deathSequenceStarted = false;
        private bool _victorySequenceStarted = false;
        private long _victoryStartTicks = 0;
        private const float VictoryDisplaySeconds = 3.0f;
        private static readonly HashSet<Type> movementDisposeNotImplementedTypes = new();
        private static readonly object movementDisposeNotImplementedTypesLock = new();

        private readonly object _lock = new object();
        public I3dObject ShipCopy { get; set; }
        public I3dObject SurfaceCopy { get; set; }

        public List<_2dTriangleMesh> UpdateWorld(I3dWorld world, ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            frameTimer.Restart();
            FrameCounter++;
            List<_3dObject> deepCopiedWorld;
            List<_3dObject> activeWorld;
            lock (_lock)
            {
                if (GameState.PendingWorldObjects.Count > 0)
                {
                    world.WorldInhabitants.AddRange(GameState.PendingWorldObjects);
                    GameState.PendingWorldObjects.Clear();
                }

                var inhabitants = world.WorldInhabitants;
                activeWorld = new List<_3dObject>(inhabitants.Count);

                foreach (var inhabitant in inhabitants)
                {
                    if (inhabitant.ObjectParts.Count == 0) continue;
                    if (!inhabitant.IsActive) continue;

                    if (inhabitant is _3dObject concreteInhabitant && concreteInhabitant.CheckInhabitantVisibility())
                    {
                        activeWorld.Add(concreteInhabitant);
                    }
                }

                deepCopiedWorld = Common3dObjectHelpers.DeepCopy3dObjects(activeWorld);
            }
            if (StarFieldHandler == null)
            {
                var parentSurface = world.WorldInhabitants?.FirstOrDefault(obj => obj.ObjectName == "Surface")?.ParentSurface;
                if (parentSurface != null)
                {
                    StarFieldHandler = new StarFieldHandler(parentSurface);
                }
            }
            if (StarFieldHandler != null)
            {
                StarFieldHandler.GenerateStarfield();
                if (StarFieldHandler.HasStars()) deepCopiedWorld.AddRange(StarFieldHandler.GetStars());
                float weatherOpacity = 1f - StarFieldHandler.PoolOpacity;
                SnowfallControls.GlobalSnowOpacity = weatherOpacity;
                RainfallControls.GlobalRainOpacity = weatherOpacity;
            }

            var particleObjectList = new List<_3dObject>();
            var weaponObjectList = new List<_3dObject>();
            var shadowObjectList = new List<_3dObject>();
            var renderedList = new List<_3dObject>(deepCopiedWorld.Count);
            DebugMessage = string.Empty;

            AiUpdateCounter++;
            bool doAiMark = AiUpdateCounter >= AiUpdateInterval;
            if (doAiMark) AiUpdateCounter = 0;

            Dictionary<int, _3dObject> aiById = null;
            if (doAiMark)
            {
                aiById = InitializeAiOnScreenTracking();
            }

            foreach (var inhabitant in deepCopiedWorld)
            {
                if (inhabitant.ObjectName != "Star" && !inhabitant.CheckInhabitantVisibility()) continue;
                inhabitant.IsOnScreen = true;
                if (doAiMark)
                {
                    SetAiIsOnScreen(aiById, inhabitant.ObjectId);
                }

                inhabitant.Movement?.MoveObject(inhabitant, audioPlayer, soundRegistry);
                if (inhabitant.CrashBoxesFollowRotation) inhabitant.CrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation);
                if (inhabitant.ObjectName == "Ship")
                {
                    ShipCopy = inhabitant;
                }
                if (inhabitant.ObjectName == "Surface")
                {
                    SurfaceCopy = inhabitant;
                }

                foreach (var part in inhabitant.ObjectParts)
                {
                    part.Triangles = RotateMesh(part.Triangles, (Vector3)inhabitant.Rotation);

                    if (inhabitant.ObjectName == "Surface")
                    {
                        inhabitant.ParentSurface.RotatedSurfaceTriangles = part.Triangles;

                        var landBasedIds = inhabitant.ParentSurface.LandBasedIds;
                        landBasedIds.Clear();

                        var triangleByLandId = inhabitant.ParentSurface.RotatedSurfaceTriangleByLandId;
                        triangleByLandId.Clear();

                        foreach (var triangle in part.Triangles)
                        {
                            var landBasedPosition = triangle.landBasedPosition;
                            landBasedIds.Add(landBasedPosition);

                            if (landBasedPosition.HasValue)
                                triangleByLandId[landBasedPosition.Value] = triangle;
                        }
                    }

                    SetMovementGuides(inhabitant, part, part.Triangles);
                }

                if (inhabitant.ObjectName == "Surface")
                    DebugMessage += $" Surface: Y Pos: {inhabitant.ObjectOffsets.y}";

                if (inhabitant.ObjectName == "Ship")
                    DebugMessage += $" Ship: Y Pos: {inhabitant.ObjectOffsets.y + 300} Z Rotation: {inhabitant.Rotation.z}";

                particleManager.HandleParticles(inhabitant, particleObjectList);
                weaponsManager.HandleWeapons(inhabitant, weaponObjectList);
                objectShadowManager.HandleObjectShadow(inhabitant, shadowObjectList);
                renderedList.Add(inhabitant);

            }

            if (shadowObjectList.Count > 0)
            {
                renderedList.InsertRange(0, shadowObjectList);
            }
            if (particleObjectList.Count > 0)
            {
                renderedList.AddRange(particleObjectList);
                DebugMessage += $" Number of Particles on screen {particleObjectList.Count}";
            }
            if (weaponObjectList.Count > 0)
            {
                renderedList.AddRange(weaponObjectList);
                DebugMessage += $" Number of Weapons on screen {weaponObjectList.Count}";
            }

            var activeScene = world.SceneHandler.GetActiveScene();

            var ship = activeWorld.FirstOrDefault(x => x.ObjectName == "Ship");
            if (ship != null && ship.ImpactStatus.HasExploded && !_deathSequenceStarted)
            {
                _deathSequenceStarted = true;
                _victorySequenceStarted = false;
                FadeOutWorld = true;
            }

            if (!_deathSequenceStarted && GameState.GamePlayState.IsInfectionCritical)
            {
                _deathSequenceStarted = true;
                _victorySequenceStarted = false;
                FadeOutWorld = true;
            }

            if ((_deathSequenceStarted || _victorySequenceStarted) && SceneResetReady)
            {
                CleanupWorldObjects(world.WorldInhabitants.OfType<_3dObject>().ToList());
                world.WorldInhabitants.Clear();
                GameState.SurfaceState.AiObjects.Clear();
                GameState.SurfaceState.DirtyTiles.Clear();
                GameState.SurfaceState.PendingLocalInfectionSpread.Clear();
                GameState.ShipState.BestCandidateStates.Clear();
                StarFieldHandler.ClearStars();
                StarFieldHandler = null;
                SnowfallControls.GlobalSnowOpacity = 1f;
                RainfallControls.GlobalRainOpacity = 1f;

                if (_victorySequenceStarted && !_deathSequenceStarted)
                    world.SceneHandler.NextScene(world);
                else
                    world.SceneHandler.ResetActiveScene(world);

                _deathSequenceStarted = false;
                _victorySequenceStarted = false;
                _victoryStartTicks = 0;
                SceneResetReady = false;
                FadeInWorld = true;
                TrackFrameTiming((int)FrameCounter);
                return [];
            }

            if (doAiMark)
            {
                AiUpdateCounter = 0;
                foreach (var aiObject in GameState.SurfaceState.AiObjects)
                {
                    if (!aiObject.IsActive) continue;
                    if (aiObject.IsOnScreen == false)
                    {
                        aiObject.Movement.MoveObject(aiObject, audioPlayer, soundRegistry);
                        aiObject.IsOnScreen = false;
                    }
                }
            }

            // Process cascading local infection spread (seeder-infected tiles spread to neighbors after a delay)
            SeederControls.ProcessLocalInfectionSpread(GameState.SurfaceState);

            projectedCoordinates = From3dTo2d.ConvertTo2dFromObjects(renderedList, FrameCounter, projectedCoordinates);
            CrashDetection.HandleCrashboxes(renderedList, world.IsPaused);
            CleanupExplodedObjects(world);

            // Scene director: centralizes drone activation, mothership activation,
            // victory/defeat conditions, and checkpoint logic per scene.
            var director = activeScene?.Director;
            if (!_deathSequenceStarted && !_victorySequenceStarted && director != null)
            {
                director.Update();
            }

            UpdateHudState(world);

            // Victory: the director signals when all scene objectives are met
            if (!_deathSequenceStarted && !_victorySequenceStarted && director != null && director.IsVictory)
            {
                _victorySequenceStarted = true;
                _victoryStartTicks = Stopwatch.GetTimestamp();

                var o = GameState.ScreenOverlayState;
                o.ResetToDefaults();
                o.Type = ScreenOverlayType.Game;
                o.Anchor = ScreenOverlayAnchor.Center;
                o.Header = "PLANET SECURED";
                o.Title = "ALL THREATS ELIMINATED";
                o.Body = "Proceeding to next sector...";
                o.Footer = "";
                o.DimStrength = 0.50f;
                o.PanelWidthRatio = 0.60f;
                o.PanelHeightRatio = 0.22f;
                o.ShowOverlay = true;
                o.AutoHide = false;
                o.ShowDebugOverlay = false;
            }

            // Victory timer: show overlay briefly then trigger scene transition
            if (_victorySequenceStarted && !_deathSequenceStarted && !FadeOutWorld)
            {
                float elapsed = (Stopwatch.GetTimestamp() - _victoryStartTicks) / (float)Stopwatch.Frequency;
                if (elapsed >= VictoryDisplaySeconds)
                {
                    FadeOutWorld = true;
                }
            }

            if (activeScene != null)
            {
                HandleMusic(renderedList, activeScene.SceneMusic);
            }

            TrackFrameTiming((int)FrameCounter);
            return projectedCoordinates;
        }

        private void CleanupExplodedObjects(I3dWorld world)
        {
            lock (_lock)
            {
                List<_3dObject>? explodedObjects = null;
                HashSet<int>? explodedIds = null;

                var inhabitants = world.WorldInhabitants;
                for (int i = 0; i < inhabitants.Count; i++)
                {
                    if (inhabitants[i] is not _3dObject obj || obj.ObjectName == "Ship" || obj.ImpactStatus?.HasExploded != true)
                    {
                        continue;
                    }

                    explodedObjects ??= new List<_3dObject>();
                    explodedIds ??= new HashSet<int>(inhabitants.Count);
                    explodedObjects.Add(obj);
                    explodedIds.Add(obj.ObjectId);
                }

                if (explodedObjects == null || explodedIds == null)
                {
                    return;
                }

                // Spawn PowerUp objects at the location of exploded objects that have the flag
                // and award score for enemy kills / trigger checkpoints
                var gps = GameState.GamePlayState;
                bool checkpointTriggered = false;
                bool powerUpAlreadyExists = false;
                for (int i = 0; i < inhabitants.Count; i++)
                {
                    if (inhabitants[i].ObjectName == "PowerUp")
                    {
                        powerUpAlreadyExists = true;
                        break;
                    }
                }

                var aiObjects = GameState.SurfaceState.AiObjects;

                foreach (var obj in explodedObjects)
                {
                    if (EnemySetup.IsEnemyTypeValid(obj.ObjectName))
                    {
                        gps.RecordKill(obj.ObjectName);

                        if (Logger.ShouldLog(enableProgressionLogging))
                        {
                            var pos = obj.WorldPosition;
                            var offs = obj.ObjectOffsets;
                            Logger.Log(
                                $"EnemyKilled: name={obj.ObjectName}; id={obj.ObjectId}; world=({pos?.x ?? 0f};{pos?.y ?? 0f};{pos?.z ?? 0f}); offsets=({offs?.x ?? 0f};{offs?.y ?? 0f};{offs?.z ?? 0f}); status={GetEnemyStatusSnapshot(aiObjects, explodedIds)}",
                                "Progression");
                        }

                        if (GameSetup.IsCheckpointEnemy(obj.ObjectName, obj.HasPowerUp))
                            checkpointTriggered = true;
                    }

                    if (obj.HasPowerUp && obj.WorldPosition != null)
                    {
                        // Only one PowerUp at a time — skip if one already exists.
                        if (powerUpAlreadyExists) continue;

                        var powerup = PowerUp.CreatePowerup(obj.ParentSurface);
                        powerup.WorldPosition = new Vector3
                        {
                            x = obj.WorldPosition.x,
                            y = 0,
                            z = obj.WorldPosition.z
                        };
                        // Un-sync the parent's ObjectOffsets.y to recover the raw initial value.
                        // SyncMovement formula: synced_y = globalMapY * 2.5 + rawY
                        // PowerUpControls.SyncMovement will re-apply its own sync from this raw value.
                        var globalMapY = GameState.SurfaceState?.GlobalMapPosition?.y ?? 0;
                        var parentRawY = (obj.ObjectOffsets?.y ?? 0) - globalMapY * 2.5f;
                        powerup.ObjectOffsets = new Vector3
                        {
                            x = 0,
                            y = parentRawY - 50,
                            z = 400
                        };
                        powerup.Movement = new PowerUpControls();
                        inhabitants.Add(powerup);
                        GameState.SurfaceState.AiObjects.Add(powerup);
                        powerUpAlreadyExists = true;
                    }
                }

                for (int i = inhabitants.Count - 1; i >= 0; i--)
                {
                    if (explodedIds.Contains(inhabitants[i].ObjectId))
                    {
                        inhabitants.RemoveAt(i);
                    }
                }

                for (int i = aiObjects.Count - 1; i >= 0; i--)
                {
                    if (explodedIds.Contains(aiObjects[i].ObjectId))
                    {
                        aiObjects.RemoveAt(i);
                    }
                }

                if (checkpointTriggered)
                {
                    // Recount remaining enemies after removal so the checkpoint
                    // reflects the actual state, not the stale previous-frame values.
                    int seedersLeft = 0, dronesLeft = 0, motherShipsLeft = 0;
                    for (int i = 0; i < aiObjects.Count; i++)
                    {
                        var o = aiObjects[i];
                        if (o.ImpactStatus?.HasExploded == true) continue;
                        if (o.ObjectName == "Seeder") seedersLeft++;
                        else if (o.ObjectName == "KamikazeDrone" && o.IsActive) dronesLeft++;
                        else if ((o.ObjectName == "MotherShipSmall" || o.ObjectName == "MotherShipMedium" || o.ObjectName == "MotherShipLarge") && o.IsActive) motherShipsLeft++;
                    }
                    gps.SeedersRemaining = seedersLeft;
                    gps.DronesRemaining = dronesLeft;
                    gps.MotherShipsRemaining = motherShipsLeft;

                    gps.SaveCheckpoint();
                    try { GameStatePersistence.SaveGameState(); } catch { }
                    try { HighscoreService.SubmitFromGamePlay(gps); } catch { }
                }

                var bestCandidateStates = GameState.ShipState.BestCandidateStates;
                for (int i = bestCandidateStates.Count - 1; i >= 0; i--)
                {
                    var enemyObject = bestCandidateStates[i]?.BestEnemyCandidate?.EnemyObject;
                    if (enemyObject != null && explodedIds.Contains(enemyObject.ObjectId))
                    {
                        bestCandidateStates.RemoveAt(i);
                    }
                }

                CleanupWorldObjects(explodedObjects);
            }
        }

        private static void CleanupWorldObjects(List<_3dObject> objects)
        {
            foreach (var obj in objects)
            {
                TryDisposeMovement(obj);

                if (obj.Particles != null)
                {
                    obj.Particles.Particles.Clear();
                    obj.Particles = null;
                }

                if (obj.WeaponSystems != null)
                {
                    obj.WeaponSystems.ActiveWeapons.Clear();
                    obj.WeaponSystems = null;
                }

                obj.CrashBoxes?.Clear();
                obj.ObjectParts?.Clear();
                obj.CalculatedCrashOffset = null;
                obj.WorldPosition = null;
                obj.ObjectOffsets = null;
                obj.ParentSurface = null;
                obj.Movement = null;
                obj.ImpactStatus = null;
            }
        }

        private static string GetEnemyStatusSnapshot(List<_3dObject> aiObjects, HashSet<int> pendingRemovalIds)
        {
            int liveSeeders = 0;
            int liveDrones = 0;
            int liveMotherShips = 0;
            int liveOtherEnemies = 0;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                var aiObject = aiObjects[i];
                if (pendingRemovalIds.Contains(aiObject.ObjectId))
                    continue;
                if (aiObject.ImpactStatus?.HasExploded == true)
                    continue;
                if (!EnemySetup.IsEnemyTypeValid(aiObject.ObjectName))
                    continue;

                if (aiObject.ObjectName == "Seeder")
                    liveSeeders++;
                else if (aiObject.ObjectName == "KamikazeDrone" && aiObject.IsActive)
                    liveDrones++;
                else if ((aiObject.ObjectName == "MotherShipSmall" || aiObject.ObjectName == "MotherShipMedium" || aiObject.ObjectName == "MotherShipLarge") && aiObject.IsActive)
                    liveMotherShips++;
                else
                    liveOtherEnemies++;
            }

            var gps = GameState.GamePlayState;
            return $"liveSeeders={liveSeeders}; liveDrones={liveDrones}; liveMotherShips={liveMotherShips}; liveOtherEnemies={liveOtherEnemies}; gpsSeeders={gps.SeedersRemaining}; gpsDrones={gps.DronesRemaining}; gpsMotherShips={gps.MotherShipsRemaining}; initialSeeders={gps.InitialSeeders}; initialDrones={gps.InitialDrones}";
        }

        private static void TryDisposeMovement(_3dObject obj)
        {
            var movement = obj?.Movement;
            if (movement == null)
            {
                return;
            }

            var movementType = movement.GetType();
            lock (movementDisposeNotImplementedTypesLock)
            {
                if (movementDisposeNotImplementedTypes.Contains(movementType))
                {
                    return;
                }
            }

            try
            {
                movement.Dispose();
            }
            catch (NotImplementedException)
            {
                lock (movementDisposeNotImplementedTypesLock)
                {
                    movementDisposeNotImplementedTypes.Add(movementType);
                }
            }
        }

        /// <summary>
        /// Syncs per-frame game state into GamePlayState for the HUD:
        /// ship health from ImpactStatus, and surviving enemy counts.
        /// </summary>
        private static void UpdateHudState(I3dWorld world)
        {
            var gps = GameState.GamePlayState;

            // Sync ship health from the actual object into GamePlayState
            var inhabitants = world.WorldInhabitants;
            for (int i = 0; i < inhabitants.Count; i++)
            {
                if (inhabitants[i].ObjectName == "Ship")
                {
                    int hp = inhabitants[i].ImpactStatus?.ObjectHealth ?? 0;
                    gps.Health = hp;
                    break;
                }
            }

            // Count surviving enemies
            var aiObjects = GameState.SurfaceState.AiObjects;
            int drones = 0;
            int seeders = 0;
            int motherShips = 0;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                var obj = aiObjects[i];
                if (obj.ImpactStatus?.HasExploded == true)
                    continue;

                if (obj.ObjectName == "KamikazeDrone" && obj.IsActive)
                    drones++;
                else if (obj.ObjectName == "Seeder")
                    seeders++;
                else if ((obj.ObjectName == "MotherShipSmall" || obj.ObjectName == "MotherShipMedium" || obj.ObjectName == "MotherShipLarge") && obj.IsActive)
                    motherShips++;
            }

            // Capture initial totals once (first frame where enemies exist)
            if (gps.InitialDrones == 0 && drones > 0)
                gps.InitialDrones = drones;
            if (gps.InitialSeeders == 0 && seeders > 0)
                gps.InitialSeeders = seeders;
            if (gps.InitialMotherShips == 0 && motherShips > 0)
                gps.InitialMotherShips = motherShips;

            gps.DronesRemaining = drones;
            gps.SeedersRemaining = seeders;
            gps.MotherShipsRemaining = motherShips;

            // MotherShip health bar tracking
            bool foundMotherShip = false;
            for (int i = 0; i < aiObjects.Count; i++)
            {
                var obj = aiObjects[i];
                if ((obj.ObjectName != "MotherShipSmall" && obj.ObjectName != "MotherShipMedium" && obj.ObjectName != "MotherShipLarge") || !obj.IsActive)
                        continue;
                    if (obj.ImpactStatus?.HasExploded == true)
                        continue;

                    foundMotherShip = true;
                    int maxHealth = obj.ObjectName switch
                    {
                        "MotherShipLarge" => EnemySetup.MotherShipLargeHealth,
                        "MotherShipMedium" => EnemySetup.MotherShipMediumHealth,
                        _ => EnemySetup.MotherShipSmallHealth
                    };
                    int currentHealth = obj.ImpactStatus?.ObjectHealth ?? maxHealth;
                float healthPct = (float)currentHealth / maxHealth;

                gps.ShowMotherShipHealthBar = true;

                gps.MotherShipHealthPercent = healthPct;

                var localWorldPos = obj.GetLocalWorldPosition();
                if (localWorldPos != null)
                {
                    float sx = ScreenSetup.screenSizeX / 2f - localWorldPos.x + (obj.ObjectOffsets?.x ?? 0);
                    float sy = ScreenSetup.screenSizeY / 2f - localWorldPos.y + (obj.ObjectOffsets?.y ?? 0);
                    gps.MotherShipScreenX = sx;
                    gps.MotherShipScreenY = sy;
                    gps.MotherShipIsOnScreen = sx > -100 && sx < ScreenSetup.screenSizeX + 100
                                             && sy > -100 && sy < ScreenSetup.screenSizeY + 100;
                }
                else
                {
                    gps.MotherShipIsOnScreen = false;
                }
                break;
            }

            if (!foundMotherShip)
            {
                gps.ShowMotherShipHealthBar = false;
                gps.MotherShipIsOnScreen = false;
            }
        }

        private Dictionary<int, _3dObject> InitializeAiOnScreenTracking()
        {
            var aiObjects = GameState.SurfaceState.AiObjects;
            if (aiObjects == null || aiObjects.Count == 0)
                return null;

            var aiById = new Dictionary<int, _3dObject>(aiObjects.Count);

            foreach (var ai in aiObjects)
            {
                ai.IsOnScreen = false;
                aiById[ai.ObjectId] = ai;
            }

            return aiById;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAiIsOnScreen(
            Dictionary<int, _3dObject> aiById,
            int objectId
        )
        {
            if (aiById != null && aiById.TryGetValue(objectId, out var aiObj))
            {
                aiObj.IsOnScreen = true;
            }
        }

        public void HandleMusic(List<_3dObject> renderedObjects, string sceneMusic)
        {
            if (string.IsNullOrWhiteSpace(sceneMusic))
            {
                return;
            }

            bool sceneMusicChanged = !string.Equals(CurrentSceneMusicId, sceneMusic, StringComparison.Ordinal);

            if (sceneMusicChanged)
            {
                audioPlayer.StopMusic();
                MusicDef = soundRegistry.Get(sceneMusic);
                CurrentSceneMusicId = sceneMusic;
                MusicIsPlaying = false;
            }

            if (!MusicIsPlaying && MusicDef != null)
            {
                audioPlayer.PlayMusic(MusicDef, 0.2f);
                MusicIsPlaying = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<List<IVector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation)
        {
            var rotatedCrashboxes = new List<List<IVector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<IVector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint((Vector3)point, rotation));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IVector3 RotatePoint(Vector3 point, Vector3 rotation)
        {
            var rotatedPoint = Rotate3d.RotatePoint(rotation.z, point, 'Z');
            rotatedPoint = Rotate3d.RotatePoint(rotation.y, rotatedPoint, 'Y');
            rotatedPoint = Rotate3d.RotatePoint(rotation.x, rotatedPoint, 'X');
            return rotatedPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = Rotate3d.RotateMesh(mesh, rotation.z, 'Z');
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.y, 'Y');
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.x, 'X');
            return rotatedMesh;
        }

        private void SetMovementGuides(_3dObject inhabitant, I3dObjectPart part, List<ITriangleMeshWithColor> rotatedMesh)
        {
            switch (part.PartName)
            {
                case "SeederParticlesStartGuide":
                    inhabitant.Movement.SetParticleGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "SeederParticlesGuide":
                    inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "JetMotor":
                    inhabitant.Movement.SetParticleGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "WeaponDirectionGuide":
                    inhabitant.Movement.SetWeaponGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "WeaponStartGuide":
                    inhabitant.Movement.SetWeaponGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "LaserDirectionGuide":
                    inhabitant.Movement.SetWeaponGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "LaserStartGuide":
                    inhabitant.Movement.SetWeaponGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "JetMotorDirectionGuide":
                    if (Logger.ShouldLog(enableLocalLogging)) Logger.Log($"MainLoop Set Guide after rotation: {rotatedMesh.First().vert1.x + ", " + rotatedMesh.First().vert1.y + ", " + rotatedMesh.First().vert1.z} Inhabitant:{inhabitant.ObjectName} ");
                    inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "RearEngine":
                    inhabitant.Movement.SetRearEngineGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "RearEngineDirectionGuide":
                    inhabitant.Movement.SetRearEngineGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "LeftWingEngineStart":
                    inhabitant.Movement.SetParticleGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "LeftWingEngineGuide":
                    inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "RightWingEngineStart":
                    inhabitant.Movement.SetRearEngineGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "RightWingEngineGuide":
                    inhabitant.Movement.SetRearEngineGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "BomberBombDropStart":
                    inhabitant.Movement.SetWeaponGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "BomberBombDropEnd":
                    inhabitant.Movement.SetWeaponGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
            }
        }

        private void TrackFrameTiming(int frameIndex)
        {
            if (!frameTimer.IsRunning)
                return;

            frameTimer.Stop();
            if (!enableLocalLogging)
                return;
            if (!Logger.ShouldLog(enableLocalLogging))
                return;

            var budgetMs = 1000.0 / CommonUtilities.CommonSetup.ScreenSetup.targetFps;
            var elapsedMs = frameTimer.Elapsed.TotalMilliseconds;
            var headroomMs = budgetMs - elapsedMs;
            var headroomPct = (headroomMs / budgetMs) * 100.0;

            performanceFrameCount++;
            averageFrameMs += (elapsedMs - averageFrameMs) / performanceFrameCount;
            averageHeadroomMs += (headroomMs - averageHeadroomMs) / performanceFrameCount;

            DebugMessage += $" PerfHeadroom: {headroomPct:0.#}%";

            if (performanceFrameCount % perfLogInterval == 0)
            {
                var avgHeadroomPct = (averageHeadroomMs / budgetMs) * 100.0;
                Logger.Log($"[LivePerf] frame={frameIndex} frameMs={elapsedMs:0.###} headroomMs={headroomMs:0.###} headroomPct={headroomPct:0.#} avgFrameMs={averageFrameMs:0.###} avgHeadroomMs={averageHeadroomMs:0.###} avgHeadroomPct={avgHeadroomPct:0.#}");
            }
        }

    }
}
