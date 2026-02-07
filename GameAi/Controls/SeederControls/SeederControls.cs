using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using CommonUtilities.GamePlayHelpers;
using Domain;
using GameAiAndControls.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static CommonUtilities.GamePlayHelpers.GamePlayHelpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls.SeederControls
{
    public class SeederControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        // Audio setup (gjøres lazy via ConfigureAudio)
        private IAudioPlayer? _audio;
        private SoundDefinition? _explosionSound;

        private float Yrotation = 0;
        private float Xrotation = 90;
        private float Zrotation = 0;

        private DateTime lastRelease = DateTime.Now;
        private readonly long releaseInterval = 10000000 * 10;

        private bool _syncInitialized = false;
        private float _syncY = 0;
        //Factor to stay in sync with surface movement
        private float _syncFactor = 2.5f;
        private bool enableLogging = false;
        private bool isExploding = false;
        private DateTime ExplosionDeltaTime;

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            //If configured already, skip
            if (_audio != null || _explosionSound != null)
                return;

            if (audioPlayer == null || soundRegistry == null)
                return;

            _audio = audioPlayer;
            _explosionSound = soundRegistry.Get("explosion_main");
        }

        // *** AI Start Code Below ***

        // -------------------------
        // AI State (per object)
        // -------------------------
        private sealed class AiState
        {
            public bool IsSearchingGlobally = true;
            public bool IsHuntingLocally = false;

            public bool HasMovementTarget = false;
            public bool TargetIsLocalBio = false;
            public Vector3 TargetWorld;

            public int LocalPickCursor = 0;

            public long NextLocalRetargetTicks = 0;
            public long NextGlobalDecisionTicks = 0;

            public long LastMoveStamp = 0;          // Stopwatch ticks
            public long TargetStartTicks = 0;       // DateTime ticks (for dynamic timeout)

            public Vector3 AuthWorldPos;            // authoritative position for this AI object
            public bool AuthPosInitialized = false;

            // optional throttled logging
            public long LastHeartbeatTicks = 0;
            public long LastWaitLogTicks = 0;
            public long LastDecisionLogTicks = 0;
        }

        private readonly Dictionary<int, AiState> _aiStates = new();

        private AiState GetAiState(int objectId)
        {
            if (!_aiStates.TryGetValue(objectId, out var s))
            {
                s = new AiState();
                _aiStates[objectId] = s;
            }
            return s;
        }

        // Call this when a Seeder dies/explodes OR when scene resets, to avoid stale states
        public void RemoveAiState(int objectId)
        {
            _aiStates.Remove(objectId);
        }

        private void SafeLog(string msg)
        {
            try
            {
                if (Logger.EnableFileLogging && enableLogging) Logger.Log(msg);
            }
            catch { /* never crash AI because of logging */ }
        }

        private static float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static string V2(Vector3 v) => $"({(int)v.x},{(int)v.z})";

        // -------------------------
        // New AI movement (per object state)
        // -------------------------
        public Vector3? MoveWorldPositionAccordingToAi(bool isOnScreen, I3dObject moveThisObject)
        {
            if (moveThisObject.WorldPosition.x == 0 && moveThisObject.WorldPosition.y == 0 && moveThisObject.WorldPosition.z == 0) return (Vector3)moveThisObject.WorldPosition;
            // Hard guards (only legit null exits)
            if (moveThisObject == null)
                return null;

            if (moveThisObject.WorldPosition == null)
            {
                SafeLog($"AI:SKIP WorldPosition=null onScreen={isOnScreen} ObjectId:{moveThisObject.ObjectId}");
                return null;
            }

            var surfaceState = GameState.SurfaceState;
            var ecoMap = surfaceState?.ScreenEcoMetas;

            int id = moveThisObject.ObjectId;
            var s = GetAiState(id);

            // If ecoMap is missing, DON'T flicker: just hold position
            if (ecoMap == null || surfaceState == null)
            {
                // Ensure auth pos is initialized so we can hold deterministically
                if (!s.AuthPosInitialized)
                {
                    s.AuthWorldPos = (Vector3)moveThisObject.WorldPosition;
                    s.AuthPosInitialized = true;
                }
                return s.AuthWorldPos;
            }

            // Authoritative position (per object)
            if (!s.AuthPosInitialized)
            {
                s.AuthWorldPos = (Vector3)moveThisObject.WorldPosition;
                s.AuthPosInitialized = true;
                SafeLog($"AI:POS_INIT onScreen={isOnScreen} pos={V2(s.AuthWorldPos)} ObjectId:{id}");
            }

            Vector3 current = s.AuthWorldPos;

            // dt (per object)
            long stamp = Stopwatch.GetTimestamp();
            if (s.LastMoveStamp == 0)
            {
                s.LastMoveStamp = stamp;
                return s.AuthWorldPos; // IMPORTANT: never null here
            }

            float dt = (stamp - s.LastMoveStamp) / (float)Stopwatch.Frequency;
            s.LastMoveStamp = stamp;

            if (dt > 0.1f) dt = 0.1f;
            if (dt < 1f / 240f) dt = 1f / 240f;

            // Speed model - speed to cross a screen
            const float secondsPerScreenCross = 5f;
            float screenWorldWidth = SurfaceSetup.viewPortSize * SurfaceSetup.tileSize;

            //And speed to cross a tile when hunting locally
            float globalSpeed = screenWorldWidth / secondsPerScreenCross;
            float localSpeed = SurfaceSetup.tileSize / 3f;

            float speed = s.TargetIsLocalBio ? localSpeed : globalSpeed;

            // Offscreen slowdown only for GLOBAL movement (not local hunting)
            if (!isOnScreen && !s.TargetIsLocalBio)
                speed *= 0.7f;

            float step = speed * dt;

            const float reachScreen = 120f;
            const float reachLocal = 8f;

            long nowTicks = DateTime.Now.Ticks;

            // Heartbeat (1/sec per object)
            if (nowTicks - s.LastHeartbeatTicks >= TimeSpan.TicksPerSecond)
            {
                s.LastHeartbeatTicks = nowTicks;

                string stateName = s.IsSearchingGlobally ? "GLOBAL" : (s.IsHuntingLocally ? "LOCAL" : "NONE");
                string targetStr = s.HasMovementTarget ? V2(s.TargetWorld) : "(none)";

                string gNext = s.NextGlobalDecisionTicks == 0 ? "n/a"
                    : $"{(s.NextGlobalDecisionTicks - nowTicks) / (double)TimeSpan.TicksPerSecond:0.00}s";
                string lNext = s.NextLocalRetargetTicks == 0 ? "n/a"
                    : $"{(s.NextLocalRetargetTicks - nowTicks) / (double)TimeSpan.TicksPerSecond:0.00}s";

                SafeLog(
                    $"AI:HB onScreen={isOnScreen} state={stateName} " +
                    $"hasTarget={s.HasMovementTarget} localTarget={s.TargetIsLocalBio} " +
                    $"dt={dt:0.0000} step={step:0.00} speed={speed:0.00}/s " +
                    $"pos={V2(current)} target={targetStr} " +
                    $"cooldowns globalNext={gNext} localNext={lNext} ObjectId:{id}"
                );
            }

            // ------------------------------------------------------------
            // If we have a target: move toward it
            // ------------------------------------------------------------
            if (s.HasMovementTarget)
            {
                float reach = s.TargetIsLocalBio ? reachLocal : reachScreen;

                // Dynamic abort timeout based on distance / speed
                float dist = DistanceXZ(current, s.TargetWorld);

                if (dist > reach)
                {
                    float minSpeed = Math.Max(1f, speed);
                    float etaSeconds = dist / minSpeed;

                    float margin = s.TargetIsLocalBio ? 2.0f : 5.0f;

                    float maxSeconds = etaSeconds + margin;
                    if (s.TargetIsLocalBio)
                        maxSeconds = Math.Clamp(maxSeconds, 4f, 12f);
                    else
                        maxSeconds = Math.Clamp(maxSeconds, 8f, 30f);

                    if (s.TargetStartTicks != 0)
                    {
                        long ageTicks = nowTicks - s.TargetStartTicks;
                        if (ageTicks > (long)(maxSeconds * TimeSpan.TicksPerSecond))
                        {
                            SafeLog(
                                $"AI:TARGET_ABORT_DYNAMIC age={(ageTicks / (double)TimeSpan.TicksPerSecond):0.00}s " +
                                $"max={maxSeconds:0.00}s eta={etaSeconds:0.00}s dist={dist:0.0} " +
                                $"onScreen={isOnScreen} localTarget={s.TargetIsLocalBio} pos={V2(current)} target={V2(s.TargetWorld)} ObjectId:{id}"
                            );

                            // Reset and immediately hold pos (never null)
                            s.HasMovementTarget = false;
                            s.TargetIsLocalBio = false;
                            s.IsSearchingGlobally = true;
                            s.IsHuntingLocally = false;
                            s.NextGlobalDecisionTicks = nowTicks; // immediate decision next call
                            return s.AuthWorldPos;
                        }
                    }
                }

                Vector3 next = SeederMovementHelpers.StepTowardTargetWorldXZ(current, s.TargetWorld, step);

                SafeLog(
                    $"AI:MOVE({(isOnScreen ? "onscreen" : "offscreen")}) " +
                    $"localTarget={s.TargetIsLocalBio} dt={dt:0.0000} step={step:0.00} reach={reach:0.0} " +
                    $"cur={V2(current)} next={V2(next)} target={V2(s.TargetWorld)} ObjectId:{id}"
                );

                // Persist authoritative position immediately (so no flicker)
                s.AuthWorldPos = next;

                if (SeederMovementHelpers.IsCloseEnoughXZ(next, s.TargetWorld, reach))
                {
                    s.AuthWorldPos = s.TargetWorld; // snap
                    next = s.AuthWorldPos;

                    if (s.TargetIsLocalBio)
                        SafeLog($"AI:REACHED_LOCAL_TARGET onScreen={isOnScreen} pos={V2(next)} ObjectId:{id}");
                    else
                        SafeLog($"AI:REACHED_SCREEN_CENTER onScreen={isOnScreen} pos={V2(next)} ObjectId:{id}");

                    s.HasMovementTarget = false;

                    if (!s.TargetIsLocalBio)
                    {
                        // GLOBAL -> LOCAL
                        s.IsSearchingGlobally = false;
                        s.IsHuntingLocally = true;
                        s.NextLocalRetargetTicks = nowTicks;
                        SafeLog($"AI:MODE_SWITCH GLOBAL->LOCAL onScreen={isOnScreen} ObjectId:{id}");
                    }
                    else
                    {
                        // Local cooldown after reaching a bio tile
                        s.NextLocalRetargetTicks = nowTicks + (long)(3f * TimeSpan.TicksPerSecond);
                        SafeLog($"AI:LOCAL_COOLDOWN set=3.00s onScreen={isOnScreen} ObjectId:{id}");

                        // Start seeding, convert tile and release particles
                        HandleStallSeeding(moveThisObject, id, s, dt, isOnScreen);
                    }

                    return s.AuthWorldPos;
                }

                return s.AuthWorldPos;
            }

            // ------------------------------------------------------------
            // GLOBAL SEARCH (never return null)
            // ------------------------------------------------------------
            if (s.IsSearchingGlobally)
            {
                if (nowTicks < s.NextGlobalDecisionTicks)
                {
                    if (nowTicks - s.LastWaitLogTicks > (TimeSpan.TicksPerSecond / 2))
                    {
                        s.LastWaitLogTicks = nowTicks;
                        SafeLog($"AI:GLOBAL_WAIT onScreen={isOnScreen} cooldown={(s.NextGlobalDecisionTicks - nowTicks) / (double)TimeSpan.TicksPerSecond:0.00}s pos={V2(current)} ObjectId:{id}");
                    }
                    return s.AuthWorldPos;
                }

                s.NextGlobalDecisionTicks = nowTicks + (TimeSpan.TicksPerSecond / 2);

                SeederMovementHelpers.GetScreenIndexFromWorldXZ(current, out int curSY, out int curSX);

                const int smellRadiusScreens = 5;
                const int roamTiles = 10;

                if (nowTicks - s.LastDecisionLogTicks > (TimeSpan.TicksPerSecond / 2))
                {
                    s.LastDecisionLogTicks = nowTicks;
                    SafeLog($"AI:GLOBAL_DECIDE onScreen={isOnScreen} from=[{curSY},{curSX}] smellRadius={smellRadiusScreens} roamTiles={roamTiles} ObjectId:{id}");
                }

                if (SeederMovementHelpers.TryFindBestScreenInRadius(ecoMap, curSY, curSX, smellRadiusScreens, out int bestSY, out int bestSX))
                {
                    int stepY = bestSY == curSY ? 0 : (bestSY > curSY ? 1 : -1);
                    int stepX = bestSX == curSX ? 0 : (bestSX > curSX ? 1 : -1);

                    int nextSY = curSY + stepY;
                    int nextSX = curSX + stepX;

                    // Clamp
                    if (nextSY < 0) nextSY = 0;
                    if (nextSX < 0) nextSX = 0;
                    if (nextSY >= ecoMap.GetLength(0)) nextSY = ecoMap.GetLength(0) - 1;
                    if (nextSX >= ecoMap.GetLength(1)) nextSX = ecoMap.GetLength(1) - 1;

                    s.TargetWorld = SeederMovementHelpers.GetScreenCenterWorldXZ(nextSY, nextSX, current.y);
                    s.HasMovementTarget = true;
                    s.TargetIsLocalBio = false;
                    s.TargetStartTicks = nowTicks;

                    SafeLog($"AI:GLOBAL_STEP onScreen={isOnScreen} from=[{curSY},{curSX}] best=[{bestSY},{bestSX}] next=[{nextSY},{nextSX}] smell={ecoMap[bestSY, bestSX].BioTileCount} target={V2(s.TargetWorld)} ObjectId:{id}");

                    // Hold position this tick; movement begins next call (no flicker)
                    return s.AuthWorldPos;
                }

                // No smell -> roam
                s.TargetWorld = SeederMovementHelpers.GetRandomRoamTargetWorldXZ(current, roamTiles);
                s.HasMovementTarget = true;
                s.TargetIsLocalBio = false;
                s.TargetStartTicks = nowTicks;

                SafeLog($"AI:GLOBAL_ROAM onScreen={isOnScreen} target={V2(s.TargetWorld)} ObjectId:{id}");
                return s.AuthWorldPos;
            }

            // ------------------------------------------------------------
            // LOCAL HUNT (never return null)
            // ------------------------------------------------------------
            if (s.IsHuntingLocally)
            {
                const float localRetargetSeconds = 3f;

                if (nowTicks < s.NextLocalRetargetTicks)
                {
                    if (nowTicks - s.LastWaitLogTicks > (TimeSpan.TicksPerSecond / 2))
                    {
                        s.LastWaitLogTicks = nowTicks;
                        SafeLog($"AI:LOCAL_WAIT onScreen={isOnScreen} cooldown={(s.NextLocalRetargetTicks - nowTicks) / (double)TimeSpan.TicksPerSecond:0.00}s pos={V2(current)} cursor={s.LocalPickCursor} ObjectId:{id}");
                    }
                    return s.AuthWorldPos;
                }

                s.NextLocalRetargetTicks = nowTicks + (long)(localRetargetSeconds * TimeSpan.TicksPerSecond);

                TerrainType GetTerrainTypeFromSurfaceData(SurfaceData sd) =>
                    GamePlayHelpers.GetTerrainType(sd.mapDepth, MapSetup.maxHeight);

                SafeLog($"AI:LOCAL_PICK onScreen={isOnScreen} cursor={s.LocalPickCursor} attempts=32 validateMap=true ObjectId:{id}");

                int cursor = s.LocalPickCursor;
                if (SeederMovementHelpers.TryPickLocalBioTargetWorldXZ(
                    surfaceState,
                    moveThisObject,
                    GetTerrainTypeFromSurfaceData,
                    out Vector3 localTarget,
                    ref cursor,
                    maxAttemptsPerCall: 32,
                    validateAgainstMap: true))
                {
                    s.LocalPickCursor = cursor;

                    s.TargetWorld = new Vector3 { x = localTarget.x, y = current.y, z = localTarget.z };
                    s.HasMovementTarget = true;
                    s.TargetIsLocalBio = true;
                    s.TargetStartTicks = nowTicks;

                    SafeLog($"AI:LOCAL_TARGET onScreen={isOnScreen} target={V2(s.TargetWorld)} cursor={s.LocalPickCursor} ObjectId:{id}");
                    return s.AuthWorldPos;
                }

                s.LocalPickCursor = cursor;

                // No local tiles left -> back to global
                s.IsHuntingLocally = false;
                s.IsSearchingGlobally = true;
                s.HasMovementTarget = false;
                s.TargetIsLocalBio = false;
                s.NextGlobalDecisionTicks = nowTicks;

                SafeLog($"AI:LOCAL_DEPLETED -> GLOBAL_SEARCH onScreen={isOnScreen} cursor={s.LocalPickCursor} ObjectId:{id}");
                return s.AuthWorldPos;
            }

            // Safety fallback: hold pos, reset state
            SafeLog($"AI:FALLBACK resetState onScreen={isOnScreen} pos={V2(current)} ObjectId:{id}");
            s.IsSearchingGlobally = true;
            s.IsHuntingLocally = false;
            s.HasMovementTarget = false;
            s.TargetIsLocalBio = false;
            s.NextGlobalDecisionTicks = nowTicks;
            return s.AuthWorldPos;
        }

        private void HandleStallSeeding(I3dObject moveThisObject, int id, AiState s, double dt, bool isOnScreen)
        {
            //Convert current tile to red\infected bio tile
            var tileX = (int)moveThisObject.WorldPosition.x / SurfaceSetup.tileSize;
            //tileZ is the depth position, not Y
            var tileZ = (int)moveThisObject.WorldPosition.z / SurfaceSetup.tileSize;
            var tile = GameState.SurfaceState.Global2DMap[tileZ, tileX];
            tile.isInfected = true;
            //Release particles at this location if on screen, off screen particles are not visible
            if (isOnScreen) ReleaseParticles(moveThisObject);
        }

        // *** AI Code Above ***

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            // Lazy audio-konfig – gjøres første gang MoveObject kalles
            ConfigureAudio(audioPlayer, soundRegistry);

            // Update world position according to AI
            theObject.WorldPosition = MoveWorldPositionAccordingToAi(theObject.IsOnScreen, theObject);

            
            //Set parent object
            ParentObject = theObject;
            if (theObject.Rotation != null) theObject.Rotation.y = Yrotation;
            if (theObject.Rotation != null) theObject.Rotation.x = Xrotation;
            if (theObject.Rotation != null) theObject.Rotation.z = Zrotation;

            //Handle impact status, trigger explosion if health is 0
            if (theObject.ImpactStatus.HasCrashed == true && isExploding == false)
            {
                HandleCrash(theObject);
            }

            if (isExploding)
            {
                //Update explosion
                Physics.UpdateExplosion(theObject, ExplosionDeltaTime);
                if (theObject.ImpactStatus.HasExploded == true)
                {
                    theObject.ObjectParts = new List<I3dObjectPart>();
                }
            }

            //If there are particles, move them
            if (ParentObject.Particles?.Particles.Count > 0)
            {
                ParentObject.Particles.MoveParticles();
            }
            //For now, just rotate the object at a fixed speed
            Zrotation += 2;
            //Xrotation += 1.5f;
            SyncMovement(theObject);
            return theObject;
        }

        public void HandleCrash(I3dObject theObject)
        {
            theObject.ImpactStatus.ObjectHealth = theObject.ImpactStatus.ObjectHealth - WeaponSetup.GetWeaponDamage("Lazer");
            if (theObject.ImpactStatus.ObjectHealth <= 0)
            {
                if (_audio != null && _explosionSound != null)
                {
                    //Play the explosion sound
                    _audio.Play(_explosionSound, AudioPlayMode.OneShot);
                }

                ExplosionDeltaTime = DateTime.Now;
                isExploding = true;
                // Handle object destruction or other logic here
                var explodedVersion = Physics.ExplodeObject(theObject, 200f);
                //Remove Crash boxes to avoid further collisions
                theObject.CrashBoxes = new List<List<IVector3>>();
                if (enableLogging) Logger.Log($"Seeder has exploded.");
            }
            if (enableLogging) Logger.Log($"Seeder has crashed, current health {theObject.ImpactStatus.ObjectHealth}. CrashedWith:{theObject.ImpactStatus.ObjectName} ObjectId:{theObject.ObjectId}");
        }

        public void SyncMovement(I3dObject theObject)
        {
            if (!_syncInitialized)
            {
                _syncInitialized = true;
                _syncY = theObject.ObjectOffsets.y;
            }

            theObject.ObjectOffsets = new Vector3()
            {
                x = theObject.ObjectOffsets.x,
                y = GameState.SurfaceState.GlobalMapPosition.y * _syncFactor + _syncY,
                z = theObject.ObjectOffsets.z
            };
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            if (StartCoordinates == null || GuideCoordinates == null) return;
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates, theObject.WorldPosition, this, 1, null);
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            //No implementation needed
        }
    }
}
