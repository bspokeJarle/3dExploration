using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    /// <summary>
    /// Moves a single asteroid across the screen in a straight diagonal streak,
    /// then respawns it off-screen with a random delay.
    /// </summary>
    public class AsteroidControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject? ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        // Travel direction per frame
        private float _vx;
        private float _vy;
        // Rotation spin
        private float _rotY;
        private float _spinSpeed;

        // Countdown until next visible pass (in frames)
        private int _waitFrames;
        private bool _traveling;

        private readonly Random _rng;
        private readonly float _depth;

        private const float MinSpeed = 4.5f;
        private const float MaxSpeed = 9.0f;
        private const float MinWaitFrames = 180;
        private const float MaxWaitFrames = 500;

        // Optional forced direction (null = fully random)
        private bool? _forcedRight;
        private bool? _forcedDown;

        /// <summary>
        /// Lock the crossing direction for this asteroid so it always enters from a consistent edge.
        /// Call once after construction, before the first frame.
        /// </summary>
        public void ForceDirection(bool directionRight, bool directionDown)
        {
            _forcedRight = directionRight;
            _forcedDown  = directionDown;
        }

        /// <param name="startImmediately">When true the asteroid begins crossing the screen right away instead of waiting.</param>
        public AsteroidControls(Random rng, float depth, bool startImmediately = false)
        {
            _rng = rng;
            _depth = depth;
            _rotY = (float)(_rng.NextDouble() * 360.0);
            _spinSpeed = 0.4f + (float)_rng.NextDouble() * 1.2f;
            _waitFrames = startImmediately ? 0 : (int)(MinWaitFrames + _rng.NextDouble() * (MaxWaitFrames - MinWaitFrames));
            _traveling = false;
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            if (!_traveling)
            {
                _waitFrames--;
                if (_waitFrames <= 0)
                    Spawn(theObject);
                return theObject;
            }

            // Spin
            _rotY += _spinSpeed;
            if (_rotY > 360f) _rotY -= 360f;
            if (theObject.Rotation != null)
                theObject.Rotation.y = _rotY;

            // Move
            var off = theObject.ObjectOffsets;
            off.x += _vx;
            off.y += _vy;

            // Off-screen? wait again
            float hw = ScreenSetup.screenSizeX * 0.7f;
            float hh = ScreenSetup.screenSizeY * 0.7f;
            if (off.x < -hw || off.x > hw || off.y < -hh || off.y > hh)
            {
                _traveling = false;
                _waitFrames = (int)(MinWaitFrames + _rng.NextDouble() * (MaxWaitFrames - MinWaitFrames));
                theObject.IsActive = false;
            }

            return theObject;
        }

        private void Spawn(I3dObject theObject)
        {
            float hw = ScreenSetup.screenSizeX * 0.55f;
            float hh = ScreenSetup.screenSizeY * 0.55f;

            float ox, oy;

            if (_forcedRight.HasValue && _forcedDown.HasValue)
            {
                // Enter from the top edge, starting x-side determined by direction
                ox = _forcedRight.Value
                    ? -(hw * 0.4f + (float)_rng.NextDouble() * hw * 0.3f)   // enter from left side of top
                    : (hw * 0.4f + (float)_rng.NextDouble() * hw * 0.3f);   // enter from right side of top
                oy = -hh;
            }
            else
            {
                int edge = _rng.Next(4);
                switch (edge)
                {
                    case 0: ox = -hw; oy = (float)(_rng.NextDouble() * hh * 2 - hh); break;
                    case 1: ox =  hw; oy = (float)(_rng.NextDouble() * hh * 2 - hh); break;
                    case 2: ox = (float)(_rng.NextDouble() * hw * 2 - hw); oy = -hh; break;
                    default:ox = (float)(_rng.NextDouble() * hw * 2 - hw); oy =  hh; break;
                }
            }

            // Target: opposite side, respecting forced direction if set
            float targetX, targetY;
            if (_forcedRight.HasValue && _forcedDown.HasValue)
            {
                float xSign = _forcedRight.Value ? 1f : -1f;
                targetX = xSign * (hw * 0.3f + (float)_rng.NextDouble() * hw * 0.4f);
                targetY = hh * (0.5f + (float)_rng.NextDouble() * 0.5f);
            }
            else
            {
                targetX = (float)(_rng.NextDouble() * hw * 1.2 - hw * 0.6) * -MathF.Sign(ox + 0.001f);
                targetY = (float)(_rng.NextDouble() * hh * 1.2 - hh * 0.6) * -MathF.Sign(oy + 0.001f);
            }

            float dx = targetX - ox;
            float dy = targetY - oy;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            float speed = MinSpeed + (float)_rng.NextDouble() * (MaxSpeed - MinSpeed);
            _vx = dx / len * speed;
            _vy = dy / len * speed;

            theObject.ObjectOffsets.x = ox;
            theObject.ObjectOffsets.y = oy;
            theObject.ObjectOffsets.z = _depth;
            theObject.IsActive = true;
            _traveling = true;
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor s, ITriangleMeshWithColor g) { }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor s, ITriangleMeshWithColor g) { }
        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor s, ITriangleMeshWithColor g) { }
        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) { }
        public void ReleaseParticles(I3dObject theObject) { }

        public void Dispose()
        {
            _traveling = false;
        }
    }
}
