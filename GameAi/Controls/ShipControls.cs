using Domain;
using Gma.System.MouseKeyHook;
using System.Diagnostics;
using System.Windows.Forms;

namespace GameAiAndControls.Controls
{
    public class ShipControls : IObjectMovement
    {
        private IKeyboardMouseEvents _globalHook;
        public int FormerMouseX = 0;
        public int FormerMouseY = 0;
        public int rotationX = 120;
        public int rotationY = 0;
        public int rotationZ = 0;
        public int shipY = 0;
        public int zoom = 300;
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
            if (e.KeyCode == Keys.Left) rotationZ -= 5;
            if (e.KeyCode == Keys.Right) rotationZ += 5;
            if (e.KeyCode == Keys.Up) rotationX -= 5;
            if (e.KeyCode == Keys.Down) rotationX += 5;
            if (e.KeyCode == Keys.Space) ThrustOn = true;
        }

        private void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) ThrustOn = false;
        }

        public void GlobalHookMouseMovement(object sender, MouseEventArgs e)
        {
            var deltaX = e.X - FormerMouseX;
            var deltaY = e.Y - FormerMouseY;

            if (FormerMouseX > 0 && FormerMouseY > 0)
            {
                rotationX += deltaY / 3;
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
            if (Thrust < 5) Thrust += 0.3f;
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates, this, (int)Thrust);
        }

        public void HandleThrust()
        {
            var deltaX = rotationX % 360;
            var deltaY = rotationY % 360;
            var deltaZ = rotationZ % 360;

            if (deltaX >= 90 && rotationX <= 180)
            {
                ParentObject.ParentSurface.GlobalMapPosition.z -= Thrust * 4;
                ParentObject.ParentSurface.GlobalMapPosition.y += 1f;
            }
            if (rotationX >= 0 && rotationX <= 90)
            {
                ParentObject.ParentSurface.GlobalMapPosition.z += Thrust * 4;
                ParentObject.ParentSurface.GlobalMapPosition.y += 4f;
            }
            Debug.WriteLine($"Delta X: {deltaX}, Delta Y: {deltaY}, Delta Z: {deltaZ}, Thrust: {Thrust}");
        }

        public void ApplyGravity()
        {
            if (ParentObject?.ParentSurface?.GlobalMapPosition.y >= -75)
                ParentObject.ParentSurface.GlobalMapPosition.y -= 3f;
            else
                ParentObject.Position.y += 3;

            Debug.WriteLine($"Mapy: {ParentObject?.ParentSurface?.GlobalMapPosition.y}");
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
    }
}
