using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GameAiAndControls.Controls
{
    // Configurable settings for ship movement
    public class ShipControls : IObjectMovement
    {
        // === CONFIGURATION ===

        private const float MaxThrust = 10.0f;                   // Maximum thrust level
        private const float ThrustIncreaseRate = 0.5f;           // Rate of thrust increase when Space is held
        private const float GravityAcceleration = 0.3f;          // Acceleration from gravity per tick
        private const float MaxFallSpeed = 4f;                   // Terminal fall velocity
        private const float GravityMultiplier = 1.8f;            // Scales gravity strength dynamically

        private const int RotationStep = 5;                      // Angle change per key press
        private const float DEG2RAD = MathF.PI / 180f;           // Conversion constant
        private const int ShipCenterY = 0;                       // Visual Y-center for alignment

        private const float SpeedMultiplier = 2.2f;              // Controls horizontal movement speed
        private const float HeightMultiplier = 0.8f;             // Controls vertical movement gain
        private const float ThrustAccelerationRate = 12.0f;      // How quickly thrust ramps up when engaged
        private const float InertiaDrag = 0.92f;                 // Damping applied to movement inertia
        private const float MaxInertia = 9.0f;                   // Limits on stored inertia speed

        private const float VerticalThrustSmoothing = 0.6f;      // Reduces height gain from small bursts
        private const float VerticalLiftAcceleration = 0.06f;    // Gradual increase in vertical lift factor

        // === INTERNAL STATE ===

        private IKeyboardMouseEvents _globalHook;
        private float fallVelocity = 0f;
        private float inertiaX = 0f;
        private float inertiaZ = 0f;
        private float thrustEffect = 0f;
        private float verticalLiftFactor = 0f;

        // Input tracking
        public int FormerMouseX = 0;
        public int FormerMouseY = 0;
        public int rotationX = 120;
        public int rotationY = 0;
        public int rotationZ = 0;

        public int shipY = 0;
        public int zoom = 150;

        public I3dObject ParentObject { get; set; }
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }

        public float Thrust { get; set; } = 0;
        public bool ThrustOn { get; set; } = false;

        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public ShipControls()
        {
            if (_globalHook == null)
            {
                _globalHook = Hook.GlobalEvents();
                _globalHook.KeyDown += GlobalHookKeyDown;
                _globalHook.KeyUp += GlobalHookKeyUp;
                _globalHook.MouseMove += GlobalHookMouseMovement;
                _globalHook.MouseDown += GlobalHookMouseDown;
                _globalHook.MouseUp += GlobalHookMouseUp;
            }
        }

        private void GlobalHookKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) rotationZ -= RotationStep;
            if (e.KeyCode == Keys.Right) rotationZ += RotationStep;
            if (e.KeyCode == Keys.Up)
            {
                float tiltCompensation = MathF.Cos(rotationZ * DEG2RAD);
                float lateralCompensation = MathF.Sin(rotationZ * DEG2RAD);
                rotationX -= (int)(RotationStep * tiltCompensation);
                rotationY += (int)(RotationStep * lateralCompensation);
            }
            if (e.KeyCode == Keys.Down)
            {
                float tiltCompensation = MathF.Cos(rotationZ * DEG2RAD);
                float lateralCompensation = MathF.Sin(rotationZ * DEG2RAD);
                rotationX += (int)(RotationStep * tiltCompensation);
                rotationY -= (int)(RotationStep * lateralCompensation);
            }
            if (e.KeyCode == Keys.Space) ThrustOn = true;
        }

        private void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                ThrustOn = false;
                Thrust = 0;
                thrustEffect = 0f;
                verticalLiftFactor = 0f;
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

        private void GlobalHookMouseDown(object sender, MouseEventArgs e) { ThrustOn = true; }
        private void GlobalHookMouseUp(object sender, MouseEventArgs e) { ThrustOn = false; Thrust = 0; thrustEffect = 0f; verticalLiftFactor = 0f; }

        private void IncreaseThrustAndRelease()
        {
            if (Thrust < MaxThrust) Thrust += ThrustIncreaseRate;
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates, this.ParentObject.ParentSurface.GlobalMapPosition, this, (int)Thrust);
        }

        private float GetWrappedPosition(float position, float diff, float minValue, float maxValue)
        {
            if (diff != 0)
            {
                if (position + diff >= maxValue) return minValue;
                if (position + diff <= minValue) return maxValue;
                return position + diff;
            }
            return position;
        }

        public I3dObject MoveObject(I3dObject theObject)
        {
            ParentObject ??= theObject;

            if (Thrust > 0) HandleThrust();
            if (ThrustOn) IncreaseThrustAndRelease();
            if (Thrust == 0) ApplyGravity();
            if (ParentObject.Particles?.Particles.Count > 0) ParentObject.Particles.MoveParticles();

            if (theObject.Position != null)
            {
                theObject.Position.y = ParentObject.Position.y;
                theObject.Position.z = zoom;
            }
            if (theObject.Rotation != null)
            {
                theObject.Rotation.x = rotationX;
                theObject.Rotation.y = rotationY;
                theObject.Rotation.z = rotationZ;
            }
            return theObject;
        }

        public void HandleThrust()
        {
            // Build up thrust over time and smooth vertical gain
            thrustEffect = MathF.Min(thrustEffect + ThrustAccelerationRate, 1f);
            verticalLiftFactor = MathF.Min(verticalLiftFactor + VerticalLiftAcceleration, 1f);

            float forwardFactor = MathF.Sin(rotationX * DEG2RAD);
            float upwardFactor = MathF.Cos(rotationX * DEG2RAD);
            float directionFactor = MathF.Cos(rotationY * DEG2RAD);
            float speedFactor = MathF.Pow(MathF.Abs(forwardFactor), 1.2f);
            float verticalFactor = MathF.Max(0, 1 - MathF.Abs(forwardFactor));

            var zForce = Thrust * thrustEffect * SpeedMultiplier * speedFactor * MathF.Cos(rotationY * DEG2RAD);
            var xForce = Thrust * thrustEffect * SpeedMultiplier * speedFactor * MathF.Sin(rotationY * DEG2RAD);
            var yDiff = Thrust * verticalLiftFactor * HeightMultiplier * verticalFactor * VerticalThrustSmoothing;

            // Accumulate inertia
            inertiaX += xForce;
            inertiaZ += -zForce;

            inertiaX = Clamp(inertiaX * InertiaDrag, -MaxInertia, MaxInertia);
            inertiaZ = Clamp(inertiaZ * InertiaDrag, -MaxInertia, MaxInertia);

            float maxX = (ParentObject.ParentSurface.GlobalMapSize() * ParentObject.ParentSurface.TileSize()) -
                         (ParentObject.ParentSurface.ViewPortSize() * ParentObject.ParentSurface.TileSize());
            float maxZ = (ParentObject.ParentSurface.GlobalMapSize() * ParentObject.ParentSurface.TileSize()) -
                         (ParentObject.ParentSurface.ViewPortSize() * ParentObject.ParentSurface.TileSize());

            ParentObject.ParentSurface.GlobalMapPosition.x = GetWrappedPosition(ParentObject.ParentSurface.GlobalMapPosition.x, inertiaX, 75, maxX);
            ParentObject.ParentSurface.GlobalMapPosition.z = GetWrappedPosition(ParentObject.ParentSurface.GlobalMapPosition.z, inertiaZ, 0, maxZ);

            // Y-position follows ship until centered, then moves background instead
            float delta = ShipCenterY - ParentObject.Position.y;
            if (Math.Abs(delta) > 2f)
            {
                ParentObject.Position.y += delta * 0.1f + yDiff * 0.1f;
            }
            else
            {
                ParentObject.ParentSurface.GlobalMapPosition.y += yDiff * 1.5f;
            }
        }

        private float Clamp(float value, float min, float max)
        {
            return MathF.Min(MathF.Max(value, min), max);
        }

        public void ApplyGravity()
        {
            if (!ThrustOn)
            {
                // Sinusmodulert tyngdekraft for mer realistisk fall
                float gravityModifier = Clamp(MathF.Sin((rotationX % 180) * DEG2RAD), 0.3f, 1.0f);
                float adjustedGravity = GravityAcceleration * gravityModifier * GravityMultiplier;

                fallVelocity = Math.Min(fallVelocity + adjustedGravity, MaxFallSpeed);
                ParentObject.Position.y += fallVelocity;

                if (ParentObject?.ParentSurface?.GlobalMapPosition.y > -75)
                {
                    ParentObject.ParentSurface.GlobalMapPosition.y -= fallVelocity;
                    if (ParentObject.ParentSurface.GlobalMapPosition.y < -75)
                    {
                        ParentObject.ParentSurface.GlobalMapPosition.y = -75;
                    }
                }
            }
            else
            {
                float upwardFactor = MathF.Cos(rotationX * DEG2RAD);
                float thrustLift = Thrust * upwardFactor * 0.75f;
                fallVelocity = Math.Max(fallVelocity - thrustLift, 0f);
            }
        }
    }
}
