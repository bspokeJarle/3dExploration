using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using static CommonUtilities.WeaponHelpers.WeaponHelpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class ShipControls : IObjectMovement
    {
        private bool logging = false;
        private const bool enableMovementDiagnostics = false;
        private int movementLogCounter = 0;
        private const float MaxThrust = 10.0f;
        private const float ThrustIncreaseRate = 0.5f;
        private const float GravityAcceleration = 0.75f;
        private const float MaxFallSpeed = 6.9f;
        private const float GravityMultiplier = 1.8f;

        private const float RotationAcceleration = 1000f;
        private const float MaxRotationSpeed = 160f;
        private const float RotationDrag = 0.90f;
        private const float DEG2RAD = MathF.PI / 180f;
        private const int ShipCenterY = 0;

        private const float SpeedMultiplier = 9.6f;
        private const float HeightMultiplier = 2.0f;
        private const float ThrustAccelerationRate = 30.0f;
        private const float InertiaDrag = 0.92f;
        private const float MaxInertia = 9.0f;

        private const float VerticalThrustSmoothing = 0.6f;
        private const float VerticalLiftAcceleration = 0.15f;

        public float MaxHeight { get; set; } = 1000f;
        public float MinHeight { get; set; } = -100f;
        public float MaxScreenDrop { get; set; } = 450f;

        // Audio references are initialized lazily from ConfigureAudio.
        private IAudioPlayer? _audio;
        private SoundDefinition? _rocketSound;
        private SoundDefinition? _explosionSound;
        private SoundDefinition? _releaseDecoySound;
        private SoundDefinition? _changeWeaponSound;
        private SoundDefinition? _bulletSound;
        private IAudioInstance? _rocketInstance;
        private IAudioInstance? _bulletInstance;

        private float fallVelocity = 0f;
        private float inertiaX = 0f;
        private float inertiaZ = 0f;
        private float thrustEffect = 0f;
        private float verticalLiftFactor = 0f;
        private float _yawVelocity = 0f;
        private float _pitchVelocity = 0f;
        private float _yawAccumulator = 0f;
        private float _pitchAccumulator = 0f;
        private bool landed = false;

        private DateTime lastUpdateTime = DateTime.Now;

        public int FormerMouseX = 0;
        public int FormerMouseY = 0;
        public int rotationX = 70;
        public int rotationY = 0;
        public int rotationZ = 0;
        public int tilt = 0;

        public int shipY = 0;
        public int zoom = 400;

        public I3dObject ParentObject { get; set; }
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public ITriangleMeshWithColor? WeaponStartCoordinates { get; set; }
        public ITriangleMeshWithColor? WeaponGuideCoordinates { get; set; }

        public float Thrust { get; set; } = 0;
        public bool ThrustOn { get; set; } = false;
        public IPhysics Physics { get; set; } = new Physics.Physics();
        private bool hasInitialized = false;
        private bool isExploding = false;
        private bool _fireKeyHeld = false;
        private bool _leftHeld = false;
        private bool _rightHeld = false;
        private bool _upHeld = false;
        private bool _downHeld = false;
        private DateTime ExplosionDeltaTime = DateTime.Now;

        public ShipControls()
        {
            var hook = InputManager.SharedHook;

            hook.KeyDown += GlobalHookKeyDown;
            hook.KeyUp += GlobalHookKeyUp;
            hook.MouseMove += GlobalHookMouseMovement;
            hook.MouseDown += GlobalHookMouseDown;
            hook.MouseUp += GlobalHookMouseUp;
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            // Already configured, so nothing else is required.
            if (_audio != null && _rocketSound != null && _explosionSound != null && _releaseDecoySound != null && _changeWeaponSound != null && _bulletSound != null)
                return;

            if (audioPlayer == null || soundRegistry == null)
                return;

            _audio = audioPlayer;
            _rocketSound = soundRegistry.Get("rocket_main");
            _explosionSound = soundRegistry.Get("explosion_main");
            _releaseDecoySound = soundRegistry.Get("release_decoy");
            _changeWeaponSound = soundRegistry.Get("change_weapon");
            _bulletSound = soundRegistry.Get("bullet_main");
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        private void GlobalHookKeyDown(object sender, KeyEventArgs e)
        {
            if (GameState.ScreenOverlayState.Type == ScreenOverlayType.Intro) return;
            if (e.KeyCode == Keys.Left) _leftHeld = true;
            if (e.KeyCode == Keys.Right) _rightHeld = true;
            if (e.KeyCode == Keys.Up) _upHeld = true;
            if (e.KeyCode == Keys.Down) _downHeld = true;

            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                bool weaponChanged = GameState.GamePlayState.SelectedWeapon != WeaponType.Lazer ||
                    !string.Equals(GameState.GamePlayState.ActivePowerup, "LAZER", StringComparison.OrdinalIgnoreCase);

                GameState.GamePlayState.SelectedWeapon = WeaponType.Lazer;
                GameState.GamePlayState.ActivePowerup = "LAZER";

                if (weaponChanged && _audio != null && _changeWeaponSound != null)
                {
                    var audioPosition = ((_3dObject)ParentObject).GetAudioPosition();
                    _audio.Play(
                        _changeWeaponSound,
                        AudioPlayMode.OneShot,
                        new AudioPlayOptions
                        {
                            WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                        });
                }
            }

            if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                bool weaponChanged = !string.Equals(GameState.GamePlayState.ActivePowerup, "DECOY", StringComparison.OrdinalIgnoreCase);

                GameState.GamePlayState.ActivePowerup = "DECOY";

                if (weaponChanged && _audio != null && _changeWeaponSound != null)
                {
                    var audioPosition = ((_3dObject)ParentObject).GetAudioPosition();
                    _audio.Play(
                        _changeWeaponSound,
                        AudioPlayMode.OneShot,
                        new AudioPlayOptions
                        {
                            WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                        });
                }
            }

            if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
            {
                bool weaponChanged = GameState.GamePlayState.SelectedWeapon != WeaponType.Bullet ||
                    !string.Equals(GameState.GamePlayState.ActivePowerup, "BULLET", StringComparison.OrdinalIgnoreCase);

                GameState.GamePlayState.SelectedWeapon = WeaponType.Bullet;
                GameState.GamePlayState.ActivePowerup = "BULLET";

                if (weaponChanged && _audio != null && _changeWeaponSound != null)
                {
                    var audioPosition = ((_3dObject)ParentObject).GetAudioPosition();
                    _audio.Play(
                        _changeWeaponSound,
                        AudioPlayMode.OneShot,
                        new AudioPlayOptions
                        {
                            WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                        });
                }
            }

            if (e.KeyCode == Keys.Space)
            {
                if (ThrustOn == false)
                {
                    // If an older rocket instance is still finishing its tail, stop it before starting a new one.
                    if (_rocketInstance != null)
                    {
                        if (logging) Logger.Log("Audio: Force-stopping previous rocket instance before starting new.");
                        _rocketInstance.Stop(playEndSegment: false); // Hard cut the previous tail.
                        _rocketInstance = null;
                    }

                    if (_audio != null && _rocketSound != null)
                    {
                        var audioPosition = ((_3dObject)ParentObject).GetAudioPosition();
                        if (logging) Logger.Log("Audio: Starting new rocket segmented loop.");
                        _rocketInstance = _audio.Play(
                                       _rocketSound,
                            AudioPlayMode.SegmentedLoop,
                            new AudioPlayOptions
                            {
                                WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                            });
                    }
                }
                ThrustOn = true;
            }

            if (e.KeyCode == Keys.RShiftKey)
            {
                _fireKeyHeld = true;

                if (GameState.GamePlayState.SelectedWeapon == WeaponType.Bullet
                    && !string.Equals(GameState.GamePlayState.ActivePowerup, "DECOY", StringComparison.OrdinalIgnoreCase)
                    && _bulletInstance == null
                    && _audio != null && _bulletSound != null)
                {
                    var audioPosition = ((_3dObject)ParentObject).GetAudioPosition();
                    _bulletInstance = _audio.Play(
                        _bulletSound,
                        AudioPlayMode.SegmentedLoop,
                        new AudioPlayOptions
                        {
                            WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                        });
                }

                FireWeapon();
            }
            //Prevent further processing of this key event
            //#if DEBUG
            //    e.SuppressKeyPress = true;
            //#endif  
        }

        private void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.RShiftKey)
            {
                _fireKeyHeld = false;

                if (_bulletInstance != null)
                {
                    _bulletInstance.Stop(playEndSegment: true);
                    _bulletInstance = null;
                }
            }

            if (e.KeyCode == Keys.Left) _leftHeld = false;
            if (e.KeyCode == Keys.Right) _rightHeld = false;
            if (e.KeyCode == Keys.Up) _upHeld = false;
            if (e.KeyCode == Keys.Down) _downHeld = false;

            if (e.KeyCode == Keys.Space)
            {
                ThrustOn = false;
                Thrust = 0;
                thrustEffect = 0f;
                verticalLiftFactor = 0f;

                // Stop the rocket loop if it is still playing.
                if (_rocketInstance != null)
                {
                    _rocketInstance.Stop(playEndSegment: true);
                }
            }
        }

        public void GlobalHookMouseMovement(object sender, MouseEventArgs e)
        {
            var deltaX = e.X - FormerMouseX;
            var deltaY = e.Y - FormerMouseY;

            if (FormerMouseX > 0 && FormerMouseY > 0)
            {
                rotationX += (int)(deltaY / 3 * MathF.Cos(rotationZ * DEG2RAD));
                rotationY += (int)(deltaY / 3 * MathF.Sin(rotationZ * DEG2RAD));
                rotationZ += deltaX / 3;
                rotationX %= 360;
                rotationY %= 360;
                rotationZ %= 360;
            }

            FormerMouseX = e.X;
            FormerMouseY = e.Y;
        }

        private void GlobalHookMouseDown(object sender, MouseEventArgs e) => ThrustOn = true;

        private void GlobalHookMouseUp(object sender, MouseEventArgs e)
        {
            ThrustOn = false;
            Thrust = 0;
            thrustEffect = 0f;
            verticalLiftFactor = 0f;

            if (_rocketInstance != null)
            {
                _rocketInstance.Stop(playEndSegment: true);
                _rocketInstance = null;
            }
        }

        private void FireWeapon()
        {
            if (string.Equals(GameState.GamePlayState.ActivePowerup, "DECOY", StringComparison.OrdinalIgnoreCase))
            {
                if (_bulletInstance != null)
                {
                    _bulletInstance.Stop(playEndSegment: true);
                    _bulletInstance = null;
                }
                DeployDecoy();
                return;
            }

            if (!GameState.GamePlayState.TryFireSelectedWeapon())
                return;

            // Fire weapon from ship
            var rot = new Vector3
            {
                x = rotationX,
                y = rotationY,
                z = rotationZ
            };
            ParentObject.Rotation = rot;
            ParentObject.WeaponSystems?.FireWeapon(
                WeaponGuideCoordinates?.vert1,
                WeaponStartCoordinates?.vert1,
                GameState.SurfaceState.GlobalMapPosition,
                GameState.GamePlayState.SelectedWeapon,
                ParentObject,
                tilt);
        }

        private void DeployDecoy()
        {
            if (ParentObject?.ParentSurface == null)
            {
                return;
            }

            int activeDecoyCount = GameState.SurfaceState.AiObjects.Count(obj =>
                obj.ObjectName == "DroneDecoy" &&
                obj.ImpactStatus?.HasExploded != true &&
                obj.ObjectParts?.Count > 0);

            if (activeDecoyCount >= GameSetup.MaxActiveDecoys)
            {
                return;
            }

            var decoy = CreateDecoyBeaconObject(ParentObject.ParentSurface);
            if (decoy == null)
            {
                return;
            }

            var mapPosition = GameState.SurfaceState.GlobalMapPosition;
            var shipOffsets = ParentObject.ObjectOffsets ?? new Vector3();

            // The viewport center offset converts map position (top-left) to the
            // ship's actual world position (center of viewport) for correct minimap placement.
            // ObjectOffsets compensate so the decoy renders at the ship's screen position.
            // Screen formula: screenZ = (GlobalMapPos.z - WorldPos.z) + offsets.z
            //                 screenX = -(GlobalMapPos.x - WorldPos.x) + offsets.x
            int vc = (SurfaceSetup.viewPortSize * SurfaceSetup.tileSize) / 2;

            decoy.WorldPosition = new Vector3
            {
                x = mapPosition.x + vc + shipOffsets.x,
                y = mapPosition.y - 50f,
                z = mapPosition.z + vc
            };
            decoy.ObjectOffsets = new Vector3
            {
                x = -(vc + shipOffsets.x),
                y = shipOffsets.y - (mapPosition.y * SurfacePositionSyncHelpers.DefaultEnemySurfaceSyncFactorY),
                z = shipOffsets.z + vc
            };
            decoy.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            decoy.ObjectName = "DroneDecoy";
            decoy.ImpactStatus = new ImpactStatus();
            decoy.CrashBoxDebugMode = false;
            decoy.WeaponSystems = null;

            GameState.SurfaceState.AiObjects.Add(decoy);
            GameState.PendingWorldObjects.Add(decoy);

            if (_audio != null && _releaseDecoySound != null)
            {
                var audioPosition = ((_3dObject)ParentObject).GetAudioPosition();
                _audio.Play(
                    _releaseDecoySound,
                    AudioPlayMode.OneShot,
                    new AudioPlayOptions
                    {
                        WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                    });
            }
        }

        private static _3dObject? CreateDecoyBeaconObject(ISurface parentSurface)
        {
            const string decoyBeaconTypeName = "_3dRotations.World.Objects.DecoyBeacon";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var decoyType = assembly.GetType(decoyBeaconTypeName, throwOnError: false, ignoreCase: false);
                if (decoyType == null)
                {
                    continue;
                }

                var createMethod = decoyType.GetMethod("CreateDecoyBeacon", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (createMethod?.Invoke(null, new object[] { parentSurface }) is _3dObject decoy)
                {
                    return decoy;
                }
            }

            return null;
        }

        private void IncreaseThrustAndRelease()
        {
            if (Thrust < MaxThrust) Thrust += ThrustIncreaseRate;

            ParentObject?.Particles?.ReleaseParticles(
                GuideCoordinates,
                StartCoordinates,
                GameState.SurfaceState.GlobalMapPosition,
                this,
                (int)Thrust,
                false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float Clamp(float value, float min, float max) => MathF.Min(MathF.Max(value, min), max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ClampHeight(float value) => Clamp(value, MinHeight, MaxHeight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ClampScreenHeight(float value) => MathF.Min(value, MaxScreenDrop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetWrappedPosition(float position, float diff, float minValue, float maxValue)
        {
            float newPos = position + diff;
            if (newPos >= maxValue) return minValue;
            if (newPos <= minValue) return maxValue;
            return newPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateShipWorldPosition(I3dObject theObject)
        {
            float shipOffsetY = theObject.ObjectOffsets?.y ?? shipY;

            GameState.ShipState.ShipWorldPosition = SurfacePositionSyncHelpers.GetShipWorldPosition(shipOffsetY, zoom);
            GameState.ShipState.ShipCrashCenterWorldPosition = SurfacePositionSyncHelpers.GetObjectCrashCenterWorldPosition(theObject);
            GameState.ShipState.ShipImpactStatus = theObject.ImpactStatus;
            GameState.ShipState.ShipObjectOffsets = new Vector3
            {
                x = theObject.ObjectOffsets?.x ?? 0f,
                y = shipOffsetY,
                z = theObject.ObjectOffsets?.z ?? zoom
            };
            GameState.ShipState.ShipHasShadow = theObject.HasShadow;
        }

        // Merk: du har endret signatur her – sørg for at IObjectMovement matcher!
        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            //Update gamestate with thrust
            GameState.GamePlayState.Thrust = Thrust;

            // Lazily initialize audio the first time MoveObject runs.
            ConfigureAudio(audioPlayer, soundRegistry);

            ParentObject ??= theObject;

            if (!hasInitialized)
            {
                hasInitialized = true;
                Thrust = 0;
                ThrustOn = false;
                landed = true;
                ParentObject.ObjectOffsets.x = 0;
                ParentObject.ObjectOffsets.y = 200;
                ParentObject.ObjectOffsets.z = zoom;
            }

            var now = DateTime.Now;
            float deltaTime = (float)(now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            GameState.GamePlayState.Update(deltaTime);

            if (_leftHeld) _yawVelocity -= RotationAcceleration * deltaTime;
            if (_rightHeld) _yawVelocity += RotationAcceleration * deltaTime;
            if (_upHeld) _pitchVelocity += RotationAcceleration * deltaTime;
            if (_downHeld) _pitchVelocity -= RotationAcceleration * deltaTime;

            _yawVelocity = MathF.Max(-MaxRotationSpeed, MathF.Min(MaxRotationSpeed, _yawVelocity)) * RotationDrag;
            _pitchVelocity = MathF.Max(-MaxRotationSpeed, MathF.Min(MaxRotationSpeed, _pitchVelocity)) * RotationDrag;

            _yawAccumulator += _yawVelocity * deltaTime;
            _pitchAccumulator += _pitchVelocity * deltaTime;

            int yawStep = (int)_yawAccumulator;
            int pitchStep = (int)_pitchAccumulator;
            if (yawStep != 0) { rotationZ += yawStep; _yawAccumulator -= yawStep; }
            if (pitchStep != 0) { tilt += pitchStep; _pitchAccumulator -= pitchStep; }

            if (_fireKeyHeld && !isExploding)
                FireWeapon();

            if (ThrustOn)
            {
                landed = false;
                IncreaseThrustAndRelease();
                HandleThrust(deltaTime);
            }

            if (Thrust == 0)
                ApplyGravity(deltaTime);

            if (ParentObject.Particles?.Particles.Count > 0)
                ParentObject.Particles.MoveParticles();

            if (theObject.ObjectOffsets != null)
            {
                theObject.ObjectOffsets.y = ClampScreenHeight(ClampHeight(ParentObject.ObjectOffsets.y));
                theObject.ObjectOffsets.z = zoom;
            }

            if (_rocketInstance != null)
            {
                var audioPosition = ((_3dObject)theObject).GetAudioPosition();
                _rocketInstance.SetWorldPosition(new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z));
            }

            if (_bulletInstance != null)
            {
                var audioPosition = ((_3dObject)theObject).GetAudioPosition();
                _bulletInstance.SetWorldPosition(new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z));
            }

            GameState.GamePlayState.UpdateAltitude(
                GameState.SurfaceState.GlobalMapPosition.y,
                MinHeight,
                MaxHeight);

            // Only update explosion if it has already started
            if (isExploding)
            {
                Physics.UpdateExplosion(theObject, ExplosionDeltaTime);

                if (theObject.ImpactStatus?.HasExploded == true)
                {
                    theObject.ObjectParts = new List<I3dObjectPart>();
                }

                if (theObject.Particles?.Particles.Count > 0)
                    theObject.Particles.MoveParticles();
            }

            if (!isExploding)
                ApplyLocalTiltToMesh(tilt, theObject);

            if (theObject.Rotation != null)
            {
                theObject.Rotation.x = rotationX;
                theObject.Rotation.z = rotationZ;
            }

            if (theObject.ImpactStatus.HasCrashed == true && isExploding == false)
            {
                float landingSpeed = CurrentSpeed;
                string crashedWith = theObject.ImpactStatus.ObjectName;

                if (logging) Logger.Log($"[ShipCrash] HasCrashed=true, ObjectName='{crashedWith}', Health={theObject.ImpactStatus.ObjectHealth}, Direction={theObject.ImpactStatus.ImpactDirection}");

                // Apply damage from any enemy or weapon collision
                if (EnemySetup.IsEnemyTypeValid(crashedWith))
                {
                    theObject.ImpactStatus.ObjectHealth -= EnemySetup.KamikazeDroneCollisionDamage;
                    if (logging) Logger.Log($"[ShipCrash] Enemy hit! Damage={EnemySetup.KamikazeDroneCollisionDamage}, NewHealth={theObject.ImpactStatus.ObjectHealth}");
                }
                else if (WeaponSetup.IsWeaponTypeValid(crashedWith))
                {
                    int weaponDamage = WeaponSetup.GetWeaponDamage(crashedWith);
                    theObject.ImpactStatus.ObjectHealth -= weaponDamage;
                    if (logging) Logger.Log($"[ShipCrash] Weapon hit! Damage={weaponDamage}, NewHealth={theObject.ImpactStatus.ObjectHealth}");
                }
                else if (theObject.ImpactStatus.ImpactDirection == ImpactDirection.Top ||
                         theObject.ImpactStatus.ImpactDirection == ImpactDirection.Center)
                {
                    // Surface/terrain landing — damage only at high speed
                    landed = true;
                    if (landingSpeed > 5f)
                    {
                        theObject.ImpactStatus.ObjectHealth -= (int)(landingSpeed * 10);
                    }
                    if (logging) Logger.Log($"[ShipCrash] Landing. Speed={landingSpeed:F1}, NewHealth={theObject.ImpactStatus.ObjectHealth}");
                }
                else
                {
                    if (logging) Logger.Log($"[ShipCrash] NO DAMAGE APPLIED! crashedWith='{crashedWith}' did not match any category.");
                }

                if (theObject.ImpactStatus.ObjectHealth <= 0)
                {
                    // Stop the rocket loop before the explosion starts.
                    if (_rocketInstance != null)
                    {
                        _rocketInstance.Stop(playEndSegment: true);
                        _rocketInstance = null;
                    }

                    // Play the ship explosion if audio is configured.
                    if (_audio != null && _explosionSound != null)
                    {
                        var audioPosition = ((_3dObject)theObject).GetAudioPosition();
                        _audio.PlayOneShot(
                            _explosionSound,
                            new AudioPlayOptions
                            {
                                VolumeOverride = 1.0f,
                                WorldPosition = new System.Numerics.Vector3(audioPosition.x, audioPosition.y, audioPosition.z)
                            });
                    }

                    // Release some particles at the explosion, set fixed thrust level
                    Thrust = 10;
                    ParentObject?.Particles?.ReleaseParticles(
                        GuideCoordinates,
                        StartCoordinates,
                        GameState.SurfaceState.GlobalMapPosition,
                        this,
                        (int)Thrust,
                        true);

                    isExploding = true;
                    ExplosionDeltaTime = DateTime.Now;

                    var explodedVersion = Physics.ExplodeObject(theObject, 200f);
                    ParentObject = explodedVersion;
                }

                theObject.ImpactStatus.HasCrashed = false;
            }

            if (enableMovementDiagnostics)
            {
                movementLogCounter++;
                if (movementLogCounter % 60 == 0)
                {
                    var mapPos = GameState.SurfaceState.GlobalMapPosition;
                    Logger.Log($"[MoveDiag] map=({mapPos.x:0.##},{mapPos.y:0.##},{mapPos.z:0.##}) offsets=({theObject.ObjectOffsets.x:0.##},{theObject.ObjectOffsets.y:0.##},{theObject.ObjectOffsets.z:0.##}) thrustOn={ThrustOn} thrust={Thrust:0.##}");
                }
            }

            UpdateShipWorldPosition(theObject);

            // The weapon needs to move as well 
            if (theObject.WeaponSystems != null)
                theObject.WeaponSystems.MoveWeapon(audioPlayer, soundRegistry);

            return theObject;
        }

        public float CurrentSpeed
        {
            get
            {
                float horizontalSpeed = MathF.Sqrt(inertiaX * inertiaX + inertiaZ * inertiaZ);
            float verticalSpeed = landed ? 0f : MathF.Abs(fallVelocity); // Zero once the ship has landed.
                return horizontalSpeed + verticalSpeed;
            }
        }

        public void ApplyLocalTiltToMesh(int tilt, I3dObject inhabitant)
        {
            if (ParentObject == null) return;

            float radians = tilt * DEG2RAD;
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            foreach (var part in inhabitant.ObjectParts)
            {
                for (int i = 0; i < part.Triangles.Count; i++)
                {
                    var tri = part.Triangles[i];
                    tri.vert1 = RotateAroundX((Vector3)tri.vert1, cos, sin);
                    tri.vert2 = RotateAroundX((Vector3)tri.vert2, cos, sin);
                    tri.vert3 = RotateAroundX((Vector3)tri.vert3, cos, sin);
                    part.Triangles[i] = tri;
                }
            }

            for (int i = 0; i < inhabitant.CrashBoxes.Count; i++)
            {
                var crashbox = inhabitant.CrashBoxes[i];
                for (int j = 0; j < crashbox.Count; j++)
                {
                    crashbox[j] = RotateAroundX((Vector3)crashbox[j], cos, sin);
                }
            }
        }

        private Vector3 RotateAroundX(Vector3 point, float cos, float sin)
        {
            float y = point.y * cos - point.z * sin;
            float z = point.y * sin + point.z * cos;
            return new Vector3(point.x, y, z);
        }

        public void HandleThrust(float deltaTime)
        {
            thrustEffect = MathF.Min(thrustEffect + ThrustAccelerationRate * deltaTime, 1f);
            verticalLiftFactor = MathF.Min(verticalLiftFactor + VerticalLiftAcceleration * deltaTime, 1f);

            float tiltRad = tilt * DEG2RAD;
            float rotationRad = rotationZ * DEG2RAD;

            float upwardFactor = MathF.Cos(tiltRad);
            float forwardFactor = MathF.Sin(tiltRad);
            float dirX = MathF.Sin(rotationRad);
            float dirZ = MathF.Cos(rotationRad);

            float xForce = Thrust * thrustEffect * SpeedMultiplier * forwardFactor * dirX * deltaTime;
            float zForce = Thrust * thrustEffect * SpeedMultiplier * forwardFactor * dirZ * deltaTime;
            float yDiff = Thrust * verticalLiftFactor * HeightMultiplier * upwardFactor * VerticalThrustSmoothing * deltaTime;

            inertiaX += xForce;
            inertiaZ += -zForce;

            inertiaX = Clamp(inertiaX * InertiaDrag, -MaxInertia, MaxInertia);
            inertiaZ = Clamp(inertiaZ * InertiaDrag, -MaxInertia, MaxInertia);

            float maxX = (ParentObject.ParentSurface.GlobalMapSize() * ParentObject.ParentSurface.TileSize()) -
                         (ParentObject.ParentSurface.ViewPortSize() * ParentObject.ParentSurface.TileSize());
            float maxZ = (ParentObject.ParentSurface.GlobalMapSize() * ParentObject.ParentSurface.TileSize()) -
                         (ParentObject.ParentSurface.ViewPortSize() * ParentObject.ParentSurface.TileSize());

            GameState.SurfaceState.GlobalMapPosition.x = GetWrappedPosition(GameState.SurfaceState.GlobalMapPosition.x, inertiaX, 75, maxX);
            GameState.SurfaceState.GlobalMapPosition.z = GetWrappedPosition(GameState.SurfaceState.GlobalMapPosition.z, inertiaZ, 0, maxZ);

            float delta = ShipCenterY - ParentObject.ObjectOffsets.y;

            if (delta > 2f || delta < -2f)
            {
                float liftAdjust = (upwardFactor > 0f) ? delta * 0.1f : 0f;
                ParentObject.ObjectOffsets.y = ClampScreenHeight(ClampHeight(ParentObject.ObjectOffsets.y + liftAdjust + yDiff * 0.1f));
            }
            else
            {
                GameState.SurfaceState.GlobalMapPosition.y = ClampHeight(GameState.SurfaceState.GlobalMapPosition.y + 2.5f);
            }
        }

        public void ApplyGravity(float deltaTime)
        {
            //If ship has landed and thrust is off, we need to stop the ship
            if (landed && !ThrustOn) return;
            if (!ThrustOn)
            {
                float rotationXMod180Rad = (rotationX % 180) * DEG2RAD;
                float gravityModifier = Clamp(MathF.Sin(rotationXMod180Rad), 0.3f, 1.0f);
                float adjustedGravity = GravityAcceleration * gravityModifier * GravityMultiplier * deltaTime;

                fallVelocity = Math.Min(fallVelocity + adjustedGravity, MaxFallSpeed);
                ParentObject.ObjectOffsets.y = ClampScreenHeight(ClampHeight(ParentObject.ObjectOffsets.y + fallVelocity));

                if (GameState.SurfaceState.GlobalMapPosition.y > MinHeight)
                {
                    GameState.SurfaceState.GlobalMapPosition.y = ClampHeight(GameState.SurfaceState.GlobalMapPosition.y - fallVelocity);
                }
            }
            else
            {
                float upwardFactor = MathF.Cos(rotationX * DEG2RAD);
                float thrustLift = Thrust * upwardFactor * 0.75f * deltaTime;
                fallVelocity = Math.Max(fallVelocity - thrustLift, 0f);
            }
        }

        public void Dispose()
        {
            if (_rocketInstance != null)
            {
                //Try to force-stop the rocket sound immediately
                _rocketInstance.Stop(playEndSegment: false);
                _rocketInstance = null;
            }

            var hook = InputManager.SharedHook;

            hook.KeyDown -= GlobalHookKeyDown;
            hook.KeyUp -= GlobalHookKeyUp;
            hook.MouseMove -= GlobalHookMouseMovement;
            hook.MouseDown -= GlobalHookMouseDown;
            hook.MouseUp -= GlobalHookMouseUp;

            //When disposing, reset the global map position to default
            GameState.SurfaceState.GlobalMapPosition = new Vector3 { x = SurfaceSetup.DefaultMapPosition.x, y = SurfaceSetup.DefaultMapPosition.y, z = SurfaceSetup.DefaultMapPosition.z };
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) WeaponStartCoordinates = StartCoord;
            if (GuideCoord != null) WeaponGuideCoordinates = GuideCoord;
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            throw new NotImplementedException();
        }
    }
}
