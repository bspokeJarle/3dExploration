using CommonUtilities._3DHelpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Windows;
using static CommonUtilities.WeaponHelpers.WeaponHelpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class Weapons : IWeapon
    {
        private static bool enableLogging = true;
        private static readonly int maxZ = 1200;
        private static readonly int minZ = -2500;
        private static readonly int maxX = 1200;
        private static readonly int minX = -1200;
        private static readonly int maxY = 1200;
        private static readonly int minY = -1200;

        private readonly List<I3dObject> _weaponObjects;
        private readonly _3dRotationCommon _rotate = new(); // Rotation fra CommonHelpers

        public IObjectMovement ParentShip { get; set; }
        public _3dObject ParentShipObject { get; set; }
        public IVector3 WorldPosition { get; set; } = new Vector3(0, 0, 0);
        public IVector3 ParentVelocityLocal { get; set; } = new Vector3(0, 0, 0);
        public List<IActiveWeapon> ActiveWeapons { get; set; } = new List<IActiveWeapon>();

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
                    SourceParticle = default, // Use default for non-nullable reference type
                    ObjectHealth = 100
                };
                instance.Rotation = ParentShipObject.Rotation;

                IVector3 dir = Normalize(trajectory);
                //Reverse Z axis to match game coord system
                dir = new Vector3 { x = dir.x, y = dir.y, z = -dir.z };

                //Startposition in local coords
                SetObjectOffsets(instance, startPosition);
                //Set world position for reference
                SetWorldPosition(instance, worldPosition);

                //Get ship rotation and apply to weapon geometry
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
            //MessageBox.Show($"Weapon HasCrashed with {objectName} ");
            if (enableLogging) Logger.Log($"Weapon HasCrashed:{hasCrashed} ImpactName:{objectName}");
            // Implement hit handling logic if needed
            // Implement thudding sound or effects here
        }

        public void MoveWeapon()
        {
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

                if (enableLogging) Logger.Log($"Weapon ImpactStatus:{w.WeaponObject.ImpactStatus.HasCrashed} ImpactName:{w.WeaponObject.ImpactStatus.ObjectName}");

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
