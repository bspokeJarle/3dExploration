using Domain;
using Gma.System.MouseKeyHook;
using System.Linq;
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
        public int zoom = 300;
        public I3dObject ParentObject { get; set; }
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }

        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord!=null) StartCoordinates = StartCoord;
            if (GuideCoord!=null) GuideCoordinates = GuideCoord;
        }

        public ShipControls()
        {
            //Lets subscribe to global events
            if (_globalHook == null)
            {
                // Note: for the application hook, use the Hook.AppEvents() instead
                _globalHook = Hook.GlobalEvents();
                _globalHook.KeyPress += GlobalHookKeyPress;
                _globalHook.MouseMove += GlobalHookMouseMovement;
                _globalHook.MouseClick += GlobalHookMouseClick;
            }
        }

        private void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
        {
        }

        public void GlobalHookMouseMovement(object sender, MouseEventArgs e)
        {
            //Calculate the difference in mouse movement
            var deltaX = e.X - FormerMouseX;
            var deltaY = e.Y - FormerMouseY;

            if (FormerMouseX > 0 && FormerMouseY > 0)
            {
                rotationY += deltaX / 2;
                rotationX += deltaY / 2;
                if (rotationX > 360) rotationX = 0;
                if (rotationY > 360) rotationY = 0;
            }

            //When done, store the current mouse position as the former mouse position
            FormerMouseX = e.X;
            FormerMouseY = e.Y;
        }

        public void GlobalHookMouseClick(object sender, MouseEventArgs e)
        {
            ParentObject?.Particles?.ReleaseParticles(GuideCoordinates, StartCoordinates,this);
        }

        public I3dObject MoveObject(I3dObject theObject)
        {
            //If there is no parent object, set the parent object to the object that is being moved
            ParentObject ??= theObject;
            //If there are particles, move them
            if (ParentObject.Particles?.Particles.Count > 0)
            {
                ParentObject.Particles.MoveParticles();
            }
            if (theObject.Position != null)
            {
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
