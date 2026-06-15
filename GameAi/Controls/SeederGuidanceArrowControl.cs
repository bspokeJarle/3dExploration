using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using CommonUtilities.CommonSetup;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    /// <summary>
    /// Controls the seeder guidance arrow.
    /// The arrow is always attached to the ship and rotates to point
    /// toward the closest living seeder. Default orientation: +X (right).
    /// </summary>
    public class SeederGuidanceArrowControl : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        // The arrow's default forward is +X. Base rotation aligns it with the camera view.
        private float Xrotation = WorldViewSetup.SurfaceFacingObjectPitchDegrees;
        private float Yrotation = 0f;
        private float Zrotation = 90f;

        private float TargetXrotation = WorldViewSetup.SurfaceFacingObjectPitchDegrees;
        private float TargetYrotation = 0f;
        private float TargetZrotation = 90f;

        private const float RotationDegreesPerSecond = 1800f;

        private float? _preferredOffsetY;
        private DateTime _lastUpdate = DateTime.MinValue;

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            // Arrow is a fixed on-screen object with no world position.
            // It only rotates to point toward the closest seeder.
            theObject.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
            AnchorBelowGameOverlay(theObject);

            var now = DateTime.Now;
            if (_lastUpdate == DateTime.MinValue)
                _lastUpdate = now;

            double deltaSeconds = (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;

            // Find closest alive seeder and compute rotation to point at it
            var closestSeederWorld = FindClosestSeederWorldPosition();
            if (closestSeederWorld != null)
            {
                var shipWorld = GetShipWorldPosition();
                var heading = Common3dObjectHelpers.GetHeadingToTarget(shipWorld, closestSeederWorld);
                TargetXrotation = heading.X;
                TargetYrotation = heading.Y;
                TargetZrotation = heading.Z;
            }

            // Smoothly rotate toward target
            float maxDelta = RotationDegreesPerSecond * (float)deltaSeconds;
            Xrotation = Common3dObjectHelpers.MoveAngleTowards(Xrotation, TargetXrotation, maxDelta);
            Yrotation = Common3dObjectHelpers.MoveAngleTowards(Yrotation, TargetYrotation, maxDelta);
            Zrotation = Common3dObjectHelpers.MoveAngleTowards(Zrotation, TargetZrotation, maxDelta);

            if (theObject.Rotation != null)
            {
                theObject.Rotation.x = Xrotation;
                theObject.Rotation.y = Yrotation;
                theObject.Rotation.z = Zrotation;
            }

            return theObject;
        }

        private void AnchorBelowGameOverlay(I3dObject theObject)
        {
            var offsets = theObject.ObjectOffsets;
            if (offsets == null)
            {
                offsets = new Vector3 { x = 0, y = 0, z = 0 };
                theObject.ObjectOffsets = offsets;
            }

            _preferredOffsetY ??= offsets.y;

            float preferredScreenY = ScreenSetup.screenSizeY / 2f + _preferredOffsetY.Value;
            float anchoredScreenY = GameOverlaySetup.AnchorScreenYBelowHud(preferredScreenY);
            offsets.y = anchoredScreenY - ScreenSetup.screenSizeY / 2f;
        }

        /// <summary>
        /// Finds the world position of the closest objective enemy to the ship.
        /// Priority: living seeders first; when no seeders remain, any living
        /// scene-completion enemy (mothership, active drones, zeppelin bombers).
        /// Returns null if no targets are alive.
        /// </summary>
        private static Vector3? FindClosestSeederWorldPosition()
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null || aiObjects.Count == 0)
                return null;

            var shipWorld = GetShipWorldPosition();
            float bestSeederDistSq = float.MaxValue;
            Vector3? bestSeederPos = null;
            float bestDroneDistSq = float.MaxValue;
            Vector3? bestDronePos = null;
            float bestBomberDistSq = float.MaxValue;
            Vector3? bestBomberPos = null;
            float bestMotherShipDistSq = float.MaxValue;
            Vector3? bestMotherShipPos = null;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                var obj = aiObjects[i];
                if (!obj.IsActive) continue;
                if (obj.ImpactStatus?.HasExploded == true)
                    continue;

                bool isSeeder = obj.ObjectName == "Seeder";
                bool isDrone = obj.ObjectName == "KamikazeDrone";
                bool isBomber = obj.ObjectName == "ZeppelinBomber";
                bool isMotherShip = obj.ObjectName == "MotherShipSmall"
                    || obj.ObjectName == "MotherShipMedium"
                    || obj.ObjectName == "MotherShipLarge";
                if (!isSeeder && !isDrone && !isMotherShip && !isBomber)
                    continue;

                var pos = GetNavigationTargetWorldPosition(obj);
                if (pos == null)
                    continue;

                float dx = pos.x - shipWorld.x;
                float dz = pos.z - shipWorld.z;
                float distSq = dx * dx + dz * dz;

                if (isSeeder && distSq < bestSeederDistSq)
                {
                    bestSeederDistSq = distSq;
                    bestSeederPos = new Vector3 { x = pos.x, y = pos.y, z = pos.z };
                }
                else if (isMotherShip && distSq < bestMotherShipDistSq)
                {
                    bestMotherShipDistSq = distSq;
                    bestMotherShipPos = new Vector3 { x = pos.x, y = pos.y, z = pos.z };
                }
                else if (isBomber && distSq < bestBomberDistSq)
                {
                    bestBomberDistSq = distSq;
                    bestBomberPos = new Vector3 { x = pos.x, y = pos.y, z = pos.z };
                }
                else if (isDrone && distSq < bestDroneDistSq)
                {
                    bestDroneDistSq = distSq;
                    bestDronePos = new Vector3 { x = pos.x, y = pos.y, z = pos.z };
                }
            }

            // As long as seeders remain, always point to seeders.
            // After seeders are gone, point at any remaining objective enemy.
            return bestSeederPos ?? bestMotherShipPos ?? bestDronePos ?? bestBomberPos;
        }

        private static Vector3 GetShipWorldPosition()
        {
            if (GameState.ShipState?.ShipWorldPosition is Vector3 swp)
                return swp;

            // Fallback before ship controls have run
            var map = GameState.SurfaceState.GlobalMapPosition;
            return new Vector3 { x = map.x, y = map.y, z = map.z };
        }

        private static Vector3? GetNavigationTargetWorldPosition(I3dObject obj)
        {
            var pos = obj.WorldPosition;
            if (pos == null)
                return null;

            return new Vector3
            {
                x = pos.x + (ScreenSetup.screenSizeX / 2f) + (obj.ObjectOffsets?.x ?? 0f),
                y = pos.y,
                z = pos.z
            };
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) { }
        public void ReleaseParticles(I3dObject theObject) { }
        public void Dispose() { }
    }
}
