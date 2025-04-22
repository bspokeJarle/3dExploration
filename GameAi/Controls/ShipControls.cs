using Domain;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    // Configurable settings for ship movement
    public class ShipControls : IObjectMovement
    {
        // === CONFIGURATION ===

        private const float MaxThrust = 10.0f;
        private const float ThrustIncreaseRate = 0.5f;
        private const float GravityAcceleration = 0.75f; // Increased further for stronger gravity
        private const float MaxFallSpeed = 6.9f;          // Increased for faster falling
        private const float GravityMultiplier = 1.8f;

        private const int RotationStep = 5;
        private const float DEG2RAD = MathF.PI / 180f;
        private const int ShipCenterY = 0;

        private const float SpeedMultiplier = 5.6f;          // Increased for faster movement
        private const float HeightMultiplier = 2.0f;         // Increased for more responsive height
        private const float ThrustAccelerationRate = 30.0f;  // Increased for quicker thrust buildup
        private const float InertiaDrag = 0.92f;
        private const float MaxInertia = 9.0f;

        private const float VerticalThrustSmoothing = 0.6f;
        private const float VerticalLiftAcceleration = 0.15f; // Increased for quicker vertical lift ramp-up

        // === INTERNAL STATE ===

        private IKeyboardMouseEvents _globalHook;
        private float fallVelocity = 0f;
        private float inertiaX = 0f;
        private float inertiaZ = 0f;
        private float thrustEffect = 0f;
        private float verticalLiftFactor = 0f;

        private DateTime lastUpdateTime = DateTime.Now;

        public int FormerMouseX = 0;
        public int FormerMouseY = 0;
        public int rotationX = 90;
        public int rotationY = 0;
        public int rotationZ = 0;
        public int tilt = 0;

        public int shipY = 0;
        public int zoom = 300;

        public I3dObject ParentObject { get; set; }
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }

        public float Thrust { get; set; } = 0;
        public bool ThrustOn { get; set; } = false;
        public IPhysics Physics { get; set; } = new Physics.Physics();

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
                tilt += RotationStep;               
            }
            if (e.KeyCode == Keys.Down)
            {
                tilt -= RotationStep;
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
            var now = DateTime.Now;
            float deltaTime = (float)(now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            ParentObject ??= theObject;

            if (ThrustOn)
            {
                IncreaseThrustAndRelease();
                HandleThrust(deltaTime);
            }

            if (Thrust == 0)
                ApplyGravity(deltaTime);

            if (ParentObject.Particles?.Particles.Count > 0)
                ParentObject.Particles.MoveParticles();

            if (theObject.ObjectOffsets != null)
            {
                theObject.ObjectOffsets.y = ParentObject.ObjectOffsets.y;
                theObject.ObjectOffsets.z = zoom;
            }

            if (theObject.Rotation != null)
            {
                theObject.Rotation.x = rotationX;
                theObject.Rotation.z = rotationZ;
            }

            ApplyLocalTiltToMesh(tilt, theObject);

            return theObject;
        }

        public void ApplyLocalTiltToMesh(int tilt, I3dObject inhabitant)
        {
            //To prevent flipping in certain angles, we need to rotate the flip internally here before the actual rotation later
            if (ParentObject == null) return;

            float radians = tilt * (MathF.PI / 180f);
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            foreach (I3dObjectPart part in inhabitant.ObjectParts)
            {
                for (int i = 0; i < part.Triangles.Count; i++)
                {
                    var tri = part.Triangles[i];
                    tri.vert1 = RotateAroundX((Vector3)tri.vert1, cos, sin);
                    tri.vert2 = RotateAroundX((Vector3)tri.vert2, cos, sin);
                    tri.vert3 = RotateAroundX((Vector3)tri.vert3, cos, sin);
                    part.Triangles[i] = tri; // apply modified triangle
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

            // Kalkuler faktorer basert p� tilt-vinkelen
            float upwardFactor = MathF.Cos(tilt * DEG2RAD);      // Hvor mye thrust som g�r opp
            float forwardFactor = MathF.Sin(tilt * DEG2RAD);     // Hvor mye thrust som g�r frem

            // Beregn thrust-retning basert p� rotationZ
            float rotationRad = rotationZ * DEG2RAD;
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

            ParentObject.ParentSurface.GlobalMapPosition.x = GetWrappedPosition(ParentObject.ParentSurface.GlobalMapPosition.x, inertiaX, 75, maxX);
            ParentObject.ParentSurface.GlobalMapPosition.z = GetWrappedPosition(ParentObject.ParentSurface.GlobalMapPosition.z, inertiaZ, 0, maxZ);

            float delta = ShipCenterY - ParentObject.ObjectOffsets.y;

            if (Math.Abs(delta) > 2f)
            {
                float liftAdjust = (upwardFactor > 0f) ? delta * 0.1f : 0f;
                ParentObject.ObjectOffsets.y += liftAdjust + yDiff * 0.1f;
            }
            else
            {
                ParentObject.ParentSurface.GlobalMapPosition.y += 2.5f;
            }
        }
  
        private float Clamp(float value, float min, float max)
        {
            return MathF.Min(MathF.Max(value, min), max);
        }

        public void ApplyGravity(float deltaTime)
        {
            if (!ThrustOn)
            {
                float gravityModifier = Clamp(MathF.Sin((rotationX % 180) * DEG2RAD), 0.3f, 1.0f);
                float adjustedGravity = GravityAcceleration * gravityModifier * GravityMultiplier * deltaTime;

                fallVelocity = Math.Min(fallVelocity + adjustedGravity, MaxFallSpeed);
                ParentObject.ObjectOffsets.y += fallVelocity;

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
                float thrustLift = Thrust * upwardFactor * 0.75f * deltaTime;
                fallVelocity = Math.Max(fallVelocity - thrustLift, 0f);
            }
        }
    }
}
