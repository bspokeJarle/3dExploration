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
        // Movement settings
        private const float MaxThrust = 5.0f; // Maximum thrust level
        private const float ThrustIncreaseRate = 0.3f; // Rate of thrust increase
        private const float GravityForce = 3.0f; // Gravity force applied when no thrust
        private const float SpeedMultiplier = 6.0f; // Multiplier for forward speed
        private const float HeightMultiplier = 2.0f; // Multiplier for height gain
        private const int RotationStep = 5; // Angle change per key press

        private IKeyboardMouseEvents _globalHook;
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
                float tiltCompensation = MathF.Cos(rotationZ * (MathF.PI / 180f));
                float lateralCompensation = MathF.Sin(rotationZ * (MathF.PI / 180f));
                rotationX -= (int)(RotationStep * tiltCompensation);
                rotationY += (int)(RotationStep * lateralCompensation);
            }
            if (e.KeyCode == Keys.Down)
            {
                float tiltCompensation = MathF.Cos(rotationZ * (MathF.PI / 180f));
                float lateralCompensation = MathF.Sin(rotationZ * (MathF.PI / 180f));
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
            }
        }

        public void GlobalHookMouseMovement(object sender, MouseEventArgs e)
        {
            var deltaX = e.X - FormerMouseX;
            var deltaY = e.Y - FormerMouseY;

            if (FormerMouseX > 0 && FormerMouseY > 0)
            {
                rotationX += (int)(deltaY / 3 * MathF.Cos(rotationZ * (MathF.PI / 180f))); // Compensation to prevent flipping
                rotationY += (int)(deltaY / 3 * MathF.Sin(rotationZ * (MathF.PI / 180f))); // Adjust Y rotation for smoother tilting
                rotationZ += deltaX / 3;
                rotationX %= 360;
                rotationY %= 360;
                rotationZ %= 360;
            }

            FormerMouseX = e.X;
            FormerMouseY = e.Y;
        }

        private void GlobalHookMouseDown(object sender, MouseEventArgs e) { ThrustOn = true; }
        private void GlobalHookMouseUp(object sender, MouseEventArgs e) { ThrustOn = false; Thrust = 0; }

        private void IncreaseThrustAndRelease()
        {
            if (Thrust < MaxThrust) Thrust += ThrustIncreaseRate;
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates, this.ParentObject.ParentSurface.GlobalMapPosition, this, (int)Thrust);
        }

        public void HandleThrust()
        {
            float forwardFactor = MathF.Sin(rotationX * (MathF.PI / 180f));
            float upwardFactor = MathF.Cos(rotationX * (MathF.PI / 180f));
            float directionFactor = MathF.Cos(rotationY * (MathF.PI / 180f));

            var zDiff = Thrust * (SpeedMultiplier / 2 ) * MathF.Cos(rotationY * (MathF.PI / 180f)) * forwardFactor * directionFactor;
            var xDiff = Thrust * (SpeedMultiplier / 2) * MathF.Sin(rotationY * (MathF.PI / 180f)) * forwardFactor * directionFactor;

            float maxX = (ParentObject.ParentSurface.GlobalMapSize() * ParentObject.ParentSurface.TileSize()) -
                        (ParentObject.ParentSurface.ViewPortSize() * ParentObject.ParentSurface.TileSize());

            float maxZ = (ParentObject.ParentSurface.GlobalMapSize() * ParentObject.ParentSurface.TileSize()) -
                         (ParentObject.ParentSurface.ViewPortSize() * ParentObject.ParentSurface.TileSize());

            ParentObject.ParentSurface.GlobalMapPosition.x = GetWrappedPosition(ParentObject.ParentSurface.GlobalMapPosition.x, xDiff, 75, maxX);
            ParentObject.ParentSurface.GlobalMapPosition.z = GetWrappedPosition(ParentObject.ParentSurface.GlobalMapPosition.z, -zDiff, 0, maxZ);

            ParentObject.ParentSurface.GlobalMapPosition.y += Thrust * HeightMultiplier * upwardFactor;
        }

        private float GetWrappedPosition(float position, float diff, float minValue, float maxValue)
        {
            if (diff != 0)
            {
                if (position + diff >= maxValue)
                {
                    return minValue; // Wrap around to minimum value
                }
                else if (position + diff <= minValue)
                {
                    return maxValue; // Wrap around to maximum value
                }
                else
                {
                    return position + diff; // Move normally
                }
            }
            return position; // No movement, return the same position
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

        public void ApplyGravity()
        {
            if (!ThrustOn)
            {
                if (ParentObject?.ParentSurface?.GlobalMapPosition.y >= -75)
                    ParentObject.ParentSurface.GlobalMapPosition.y -= GravityForce;
                else
                    ParentObject.Position.y += 3;
            }
        }
    }
}
