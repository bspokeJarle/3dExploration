using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Net.Security;
using static CommonUtilities.WeaponHelpers.WeaponHelpers;
using static Domain._3dSpecificsImplementations;
using CommonUtilities.CommonSetup;

namespace GameAiAndControls.Controls
{
    public class Weapons : IWeapon
    {
        private static bool enableLogging = false;
        private static readonly int maxZ = 1200;
        private static readonly int minZ = -2500;
        private static readonly int maxX = 1200;
        private static readonly int minX = -1200;
        private static readonly int maxY = 1200;
        private static readonly int minY = -1200;

        // Audio setup (gjøres lazy via ConfigureAudio)
        private IAudioPlayer? _audio;
        private SoundDefinition? _thudSound;
        private SoundDefinition? _lazerSound;
        private IAudioInstance? _thudInstance;
        private IAudioInstance? _lazerInstance;

        private readonly List<I3dObject> _weaponObjects;
        private readonly _3dRotationCommon _rotate = new(); // Rotation fra CommonHelpers

        public IObjectMovement ParentShip { get; set; }
        public _3dObject ParentShipObject { get; set; }
        public IVector3 WorldPosition { get; set; } = new Vector3(0, 0, 0);
        public IVector3 ParentVelocityLocal { get; set; } = new Vector3(0, 0, 0);
        public List<IActiveWeapon> ActiveWeapons { get; set; } = new List<IActiveWeapon>();

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            // Already configured? do nothing
            if (_audio != null || _thudSound != null)
                return;

            if (audioPlayer == null || soundRegistry == null)
                return;

            _audio = audioPlayer;
            _thudSound = soundRegistry.Get("lazer_thud");
            _lazerSound = soundRegistry.Get("lazer_main");
        }

        public Weapons(List<I3dObject> weapons, IObjectMovement parent, _3dObject ship)
        {
            _weaponObjects = weapons ?? new List<I3dObject>();
            ParentShip = parent;
            ParentShipObject = ship;
        }

        public IWeapon FireWeapon(
            IVector3 trajectory,
            IVector3 startPosition,
            IVector3 worldPosition,
            WeaponType weaponType,
            I3dObject parentShip,
            int tilt
        )
        {
            if (_audio != null && _lazerSound != null)
            {
                _lazerInstance = _audio.Play(_lazerSound, AudioPlayMode.OneShot);
            }

            ParentShipObject = (_3dObject)parentShip;

            if (weaponType == WeaponType.Lazer)
            {
                I3dObject template = _weaponObjects.Count > 0
                    ? _weaponObjects[0]
                    : new _3dObject { ObjectName = "Lazer" };

                I3dObject instance = Common3dObjectHelpers.DeepCopySingleObject(template);
                instance.ImpactStatus = new ImpactStatus
                {
                    HasExploded = false,
                    HasCrashed = false,
                    ObjectName = "",
                    ImpactDirection = null,
                    SourceParticle = default,
                    ObjectHealth = 100
                };

                // Default: inherit ship rotation first
                trajectory.z = -trajectory.z; // Invert Z to match WORLD coord system   
                var enemyTarget = FindPossibleBestEnemyTarget(ParentShipObject, trajectory);

                bool guided = false;
                if (enemyTarget != null)
                {
                    trajectory = (Vector3)enemyTarget;
                    guided = true;
                }

                // Only update weapon rotation if we actually guided toward an enemy
                if (guided)
                {
                    var startWorld = ((Vector3)startPosition).ToWorldPoint(ParentShipObject);
                    var trajWorld = ((Vector3)trajectory).ToWorldPoint(ParentShipObject);

                    var dirWorld = Normalize(trajWorld - startWorld);

                    var lookRot = RotationFromDirectionWorld((Vector3)dirWorld);

                    var shipRot = (Vector3)(ParentShipObject.Rotation as Vector3 ?? new Vector3(0, 0, 0));
                    lookRot.z = shipRot.z;

                    instance.Rotation = lookRot;

                    //if (enableLogging)
                    //{
                        Logger.Log(
                            $"[WEAPON ROT] guided=1 startW=({startWorld.x:F0},{startWorld.y:F0},{startWorld.z:F0}) " +
                            $"trajW=({trajWorld.x:F0},{trajWorld.y:F0},{trajWorld.z:F0}) " +
                            $"dirW=({dirWorld.x:F3},{dirWorld.y:F3},{dirWorld.z:F3}) " +
                            $"rot=({lookRot.x:F1},{lookRot.y:F1},{lookRot.z:F1})"
                        );
                    //}
                }
                else
                {
                    // Keep original behavior: inherit ship rotation only
                    instance.Rotation = ParentShipObject.Rotation;
                }

                IVector3 dir = Normalize(trajectory);

                //Startposition in local coords
                SetObjectOffsets(instance, startPosition);
                //Set world position for reference
                SetWorldPosition(instance, worldPosition);

                //Get ship rotation and apply to weapon geometry (now uses updated instance.Rotation)
                InitializeWeaponGeometry(instance, instance.Rotation as Vector3, tilt);

                ActiveWeapon weapon = new ActiveWeapon
                {
                    WeaponType = weaponType,
                    WeaponObject = instance,
                    FiredTime = DateTime.UtcNow,
                    Velocity = 3500f,
                    Acceleration = 0f,
                    Trajectory = dir,
                    MaxRange = 6000f,
                    LifetimeSeconds = 3f,
                    DistanceTraveled = 0f,
                    LastUpdateUtc = default(DateTime)
                };

                ActiveWeapons.Add(weapon);

                if (enableLogging) Logger.Log(
                    $"[WeaponSystem] Fired {weaponType} from {startPosition} " +
                    $"with dir={dir} globalmapposition={worldPosition} at {DateTime.UtcNow}"
                );
            }

            return this;
        }

        public static IVector3? FindPossibleBestEnemyTarget(_3dObject parentShip, IVector3 trajectoryLocal)
        {
            try
            {
                var enemies = CommonUtilities.CommonGlobalState.GameState.ShipState.BestCandidateStates;
                if (enemies == null || enemies.Count == 0)
                {
                    Logger.Log("[AIM] no enemies");
                    return null;
                }

                // --- INPUTS ---
                var origTrajLocal = (Vector3)trajectoryLocal;

                var shipCenterWorld = (Vector3)Common3dObjectHelpers.GetCenterOfBox(parentShip.GetAllCrashPointsWorld());

                // Cannon tip / guide in WORLD (translation-only)
                var cannonGuideWorld = origTrajLocal.ToWorldPoint(parentShip);

                // Aim direction in WORLD (ship center -> cannon tip)
                var aimDirWorld = cannonGuideWorld - shipCenterWorld;

                const float minDotToAccept = 0.80f;
                const float maxShipDist = 775f;

                // Optional: how strong the aim assist is (1.0 = full snap, 0.2 = mild)
                const float assistStrength = 1.0f;

                // Logging control
                const bool logCandidates = true;

                Logger.Log(
                    $"[AIM] start enemies={enemies.Count} minDot={minDotToAccept:F2} maxDist={maxShipDist:F0} assist={assistStrength:F2} | " +
                    $"shipC=({shipCenterWorld.x:F0},{shipCenterWorld.y:F0},{shipCenterWorld.z:F0}) " +
                    $"cannonG=({cannonGuideWorld.x:F0},{cannonGuideWorld.y:F0},{cannonGuideWorld.z:F0}) " +
                    $"trajL=({origTrajLocal.x:F0},{origTrajLocal.y:F0},{origTrajLocal.z:F0})"
                );

                bool found = false;
                float bestDot = float.MinValue;
                float bestShipDist = float.MaxValue;

                Vector3 bestEnemyCenterWorld = default;
                string bestName = "";

                // --- FIND BEST ENEMY ---
                foreach (var st in enemies)
                {
                    var cand = st?.BestEnemyCandidate;
                    if (cand?.EnemyCenterPosition == null || cand.EnemyObject == null)
                        continue;

                    var enemyCenterWorld = (Vector3)cand.EnemyCenterPosition;

                    // Use shipCenter basis for dot (matches your "direction" concept cleanly)
                    var shipToEnemyWorld = enemyCenterWorld - shipCenterWorld;

                    float dot = Common3dObjectHelpers.DotNormalized(aimDirWorld, shipToEnemyWorld);

                    float shipDist = (float)Common3dObjectHelpers.GetDistance(cannonGuideWorld, enemyCenterWorld);
                    cand.DistanceToShip = shipDist;

                    bool okDot = dot >= minDotToAccept;
                    bool okDist = shipDist <= maxShipDist;

                    if (logCandidates)
                    {
                        Logger.Log(
                            $"[AIM] cand {cand.EnemyObject.ObjectName,-10} dot={dot,7:F4} dist={shipDist,7:F1} ok={(okDot && okDist ? 1 : 0)}"
                        );
                    }

                    if (!okDot || !okDist)
                        continue;

                    // Best = highest dot, tie-break by nearest
                    if (!found || dot > bestDot || (MathF.Abs(dot - bestDot) < 0.0001f && shipDist < bestShipDist))
                    {
                        found = true;
                        bestDot = dot;
                        bestShipDist = shipDist;
                        bestEnemyCenterWorld = enemyCenterWorld;
                        bestName = cand.EnemyObject.ObjectName;
                    }
                }

                if (!found)
                {
                    Logger.Log("[AIM] result no valid candidates");
                    return null;
                }

                // --- PRODUCE OUTPUT TRAJECTORY POINT (LOCAL) ---
                // We want to adjust the *trajectory point* (cannon tip point), not return a direction vector.
                // Move cannonGuideWorld toward enemyCenterWorld, then apply same delta to local point.
                var deltaWorld = bestEnemyCenterWorld - cannonGuideWorld;

                // optional strength (blend)
                var deltaApplied = new Vector3(
                    deltaWorld.x * assistStrength,
                    deltaWorld.y * assistStrength,
                    deltaWorld.z * assistStrength
                );

                var outTrajLocal = origTrajLocal + deltaApplied;

                // Sanity: compare direction change (orig vs new) as directions from shipCenter
                var origDirWorld = cannonGuideWorld - shipCenterWorld;
                var newCannonGuideWorld = outTrajLocal.ToWorldPoint(parentShip); // for logging only
                var newDirWorld = newCannonGuideWorld - shipCenterWorld;
                float dotOrigNew = Common3dObjectHelpers.DotNormalized(origDirWorld, newDirWorld);

                Logger.Log(
                    $"[AIM] result best={bestName} dot={bestDot:F4} dist={bestShipDist:F1} | " +
                    $"enemyC=({bestEnemyCenterWorld.x:F0},{bestEnemyCenterWorld.y:F0},{bestEnemyCenterWorld.z:F0}) " +
                    $"deltaW=({deltaWorld.x:F0},{deltaWorld.y:F0},{deltaWorld.z:F0}) " +
                    $"outTrajL=({outTrajLocal.x:F0},{outTrajLocal.y:F0},{outTrajLocal.z:F0}) " +
                    $"dot(origDir,newDir)={dotOrigNew:F4}"
                );

                return outTrajLocal;
            }
            catch (Exception ex)
            {
                Logger.Log($"[AIM] ERROR {ex}");
                return null;
            }
        }

        private static Vector3 RotationFromDirectionWorld(Vector3 dirWorld)
        {
            // dirWorld should be normalized-ish
            float x = dirWorld.x;
            float y = dirWorld.y;
            float z = dirWorld.z;

            float yawRad = (float)Math.Atan2(x, z);
            float pitchRad = (float)Math.Atan2(-y, Math.Sqrt(x * x + z * z));

            float yawDeg = yawRad * 180f / (float)Math.PI;
            float pitchDeg = pitchRad * 180f / (float)Math.PI;

            // Rotation = (X=pitch, Y=yaw, Z=roll)
            return new Vector3(pitchDeg, yawDeg, 0f);
        }


        //Rotates the weapon geometry according to ship rotation + tilt
        private void InitializeWeaponGeometry(I3dObject weaponObj, Vector3 rotation, int tilt)
        {
            if (weaponObj is not _3dObject weapon)
                return;

            if (weapon.ObjectParts == null)
                return;

            if (enableLogging) Logger.Log(
                $"Weapon rotation initialization started. BaseDeg:{rotation.x},{rotation.y},{rotation.z} Tilt:{tilt}"
            );
            if (enableLogging) Logger.Log("Before rotation:" +
                weapon.ObjectParts[0].Triangles[0].vert1.x + ", " +
                weapon.ObjectParts[0].Triangles[0].vert1.y + ", " +
                weapon.ObjectParts[0].Triangles[0].vert1.z);

            foreach (var part in weapon.ObjectParts)
            {
                if (part?.Triangles == null || part.Triangles.Count == 0)
                    continue;

                // 1) Tilt around X axis first
                var withTilt = _rotate.RotateXMesh(part.Triangles, tilt);

                // 2) Then the main rotation in the same order as in GameWorldManager.RotateMesh (Z → Y → X)
                var rotated = _rotate.RotateXMesh(
                    _rotate.RotateYMesh(
                        _rotate.RotateZMesh(withTilt, rotation.z),
                        rotation.y
                    ),
                    rotation.x
                );
                part.Triangles = rotated;
            }
            var crashBoxesWithTilt = RotateCrashBoxes(weapon.CrashBoxes, tilt, rotation);
            weapon.CrashBoxes = crashBoxesWithTilt;

            if (enableLogging) Logger.Log("After rotation:" +
                weapon.ObjectParts[0].Triangles[0].vert1.x + ", " +
                weapon.ObjectParts[0].Triangles[0].vert1.y + ", " +
                weapon.ObjectParts[0].Triangles[0].vert1.z);
        }

        public List<List<IVector3>>? RotateCrashBoxes(List<List<IVector3>> crashBoxes, int tilt, IVector3 rotation)
        {
            if (crashBoxes == null)
                return null;

            var result = new List<List<IVector3>>(crashBoxes.Count);

            foreach (var box in crashBoxes)
            {
                var rotatedBox = new List<IVector3>(box.Count);

                foreach (var vertex in box)
                {
                    // Start with original vertex
                    var v = new Vector3(vertex.x, vertex.y, vertex.z);

                    // 1) Tilt around X
                    v = (Vector3)_rotate.RotatePoint(tilt, v, 'X');

                    // 2) Main rotation Z → Y → X
                    v = (Vector3)_rotate.RotatePoint(rotation.z, v, 'Z');
                    v = (Vector3)_rotate.RotatePoint(rotation.y, v, 'Y');
                    v = (Vector3)_rotate.RotatePoint(rotation.x, v, 'X');

                    rotatedBox.Add(v);
                }

                result.Add(rotatedBox);
            }

            // Now all boxes have tilt + main rotation
            return result;
        }

        public IEnumerable<I3dObject> Get3DObjects()
        {
            if (ActiveWeapons.Count == 0) yield break;
            foreach (ActiveWeapon w in ActiveWeapons)
            {
                if (Expired(w))
                    continue;

                yield return w.WeaponObject;
            }
        }

        public void HandleHit(bool hasCrashed, string objectName)
        {
            if (enableLogging) Logger.Log($"Weapon HasCrashed:{hasCrashed} ImpactName:{objectName}");
            if (_audio != null && _thudSound != null)
            {
                //Stop Lazer, it owerpowers the thud
                _lazerInstance?.Stop(playEndSegment: false);
                // Implement thudding sound or effects here
                _thudInstance = _audio.Play(_thudSound, AudioPlayMode.OneShot);
            }
        }

        public void MoveWeapon(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            if (audioPlayer != null && soundRegistry != null) ConfigureAudio(audioPlayer, soundRegistry);

            DateTime now = DateTime.UtcNow;

            for (int i = 0; i < ActiveWeapons.Count; i++)
            {
                ActiveWeapon w = ActiveWeapons[i] as ActiveWeapon;

                //If weapon has crashed, handle hit effects
                if (w.WeaponObject.ImpactStatus.HasCrashed) HandleHit(w.WeaponObject.ImpactStatus.HasCrashed, w.WeaponObject.ImpactStatus.ObjectName);

                if (w == null || Expired(w) || OutOfBounds(w.WeaponObject.ObjectOffsets) || w.WeaponObject.ImpactStatus.HasCrashed)
                    continue;

                double dt = w.LastUpdateUtc == default(DateTime)
                    ? 0.0
                    : (now - w.LastUpdateUtc).TotalSeconds;

                w.LastUpdateUtc = now;

                if (dt > 0.0)
                {
                    w.Velocity += w.Acceleration * (float)dt;
                    if (w.Velocity < 0f)
                        w.Velocity = 0f;

                    IVector3 deltaProj = Scale(w.Trajectory, w.Velocity * (float)dt);
                    IVector3 deltaParent = Scale(ParentVelocityLocal, (float)dt);
                    IVector3 local = GetObjectOffsets(w.WeaponObject);
                    IVector3 newLocal = Add(Add(local, deltaProj), deltaParent);

                    SetObjectOffsets(w.WeaponObject, newLocal);
                    w.DistanceTraveled += Magnitude(deltaProj);

                    if (enableLogging) Logger.Log(
                        $"[WeaponSystem] {w.WeaponType} moved Δ=({deltaProj.x:F2},{deltaProj.y:F2},{deltaProj.z:F2}) " +
                        $"| LocalPos=({newLocal.x:F2},{newLocal.y:F2},{newLocal.z:F2}) | Range={w.DistanceTraveled:F2}"
                    );

                    ActiveWeapons[i] = w;
                }
            }

            for (int i = ActiveWeapons.Count - 1; i >= 0; i--)
            {
                ActiveWeapon w = ActiveWeapons[i] as ActiveWeapon;
                if (w != null && Expired(w) || OutOfBounds(w.WeaponObject.ObjectOffsets) || w.WeaponObject.ImpactStatus.HasCrashed)
                {
                    if (enableLogging) Logger.Log(
                        $"[WeaponSystem] {w.WeaponType} expired\\out\\crashed of bounds after {w.DistanceTraveled:F2} units. Current Z={w.WeaponObject.ObjectOffsets.z:F2}"
                    );
                    ActiveWeapons.RemoveAt(i);
                }
            }
        }

        public List<List<IVector3>> GetCrashBoxes()
        {
            var boxes = new List<List<IVector3>>();

            foreach (var w in ActiveWeapons)
            {
                if (w is not ActiveWeapon aw || Expired(aw))
                    continue;

                if (aw.WeaponObject?.CrashBoxes != null)
                    boxes.AddRange(aw.WeaponObject.CrashBoxes);
            }

            if (enableLogging) Logger.Log($"[WeaponSystem] CrashBoxes collected: {boxes.Count}");
            return boxes;
        }

        private static void SetWorldPosition(I3dObject obj, IVector3 pos)
        {
            var effectivePos = pos.x + obj.ObjectOffsets?.x ?? 0;
            effectivePos = pos.y + obj.ObjectOffsets?.y ?? 0;
            effectivePos = pos.z + obj.ObjectOffsets?.z ?? 0;
            ((_3dObject)obj).WorldPosition = pos;
        }

        private static void SetObjectOffsets(I3dObject obj, IVector3 pos)
        {
            ((_3dObject)obj).ObjectOffsets = pos;
        }

        private static IVector3 GetObjectOffsets(I3dObject obj)
        {
            var o = (_3dObject)obj;
            return o.ObjectOffsets ?? new Vector3(0, 0, 0);
        }

        private static bool Expired(ActiveWeapon w)
        {
            double lifetime = (DateTime.UtcNow - w.FiredTime).TotalSeconds;
            return lifetime > w.LifetimeSeconds || w.DistanceTraveled >= w.MaxRange;
        }

        private static bool OutOfBounds(IVector3 pos)
        {
            return pos.z > maxZ || pos.z < minZ || pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY;
        }

        private static IVector3 Add(IVector3 a, IVector3 b) =>
            new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);

        private static IVector3 Scale(IVector3 a, float s) =>
            new Vector3(a.x * s, a.y * s, a.z * s);

        private static float Magnitude(IVector3 a) =>
            (float)Math.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);

        private static IVector3 Normalize(IVector3 a)
        {
            float m = Magnitude(a);
            if (m <= 1e-6f)
                return new Vector3(0, 0, 0);

            float inv = 1f / m;
            return new Vector3(a.x * inv, a.y * inv, a.z * inv);
        }
    }
}
