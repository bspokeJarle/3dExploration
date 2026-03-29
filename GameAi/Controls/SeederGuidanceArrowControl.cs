using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
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
        private float Xrotation = 70f;
        private float Yrotation = 0f;
        private float Zrotation = 90f;

        private float TargetXrotation = 70f;
        private float TargetYrotation = 0f;
        private float TargetZrotation = 90f;

        private const float RotationDegreesPerSecond = 1800f;

        private DateTime _lastUpdate = DateTime.MinValue;

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            // Arrow is a fixed on-screen object with no world position.
            // It only rotates to point toward the closest seeder.
            theObject.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };

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

        /// <summary>
        /// Finds the world position of the closest living seeder to the ship.
        /// Returns null if no seeders are alive.
        /// </summary>
        private static Vector3? FindClosestSeederWorldPosition()
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null || aiObjects.Count == 0)
                return null;

            var shipWorld = GetShipWorldPosition();
            float bestDistSq = float.MaxValue;
            Vector3? bestPos = null;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                var obj = aiObjects[i];
                if (obj.ObjectName != "Seeder")
                    continue;
                if (obj.ImpactStatus?.HasExploded == true)
                    continue;

                var pos = obj.WorldPosition;
                if (pos == null)
                    continue;

                float dx = pos.x - shipWorld.x;
                float dz = pos.z - shipWorld.z;
                float distSq = dx * dx + dz * dz;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestPos = new Vector3 { x = pos.x, y = pos.y, z = pos.z };
                }
            }

            return bestPos;
        }

        private static Vector3 GetShipWorldPosition()
        {
            if (GameState.ShipState?.ShipWorldPosition is Vector3 swp)
                return swp;

            // Fallback before ship controls have run
            var map = GameState.SurfaceState.GlobalMapPosition;
            return new Vector3 { x = map.x, y = map.y, z = map.z };
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) { }
        public void ReleaseParticles(I3dObject theObject) { }
        public void Dispose() { }
    }
}
