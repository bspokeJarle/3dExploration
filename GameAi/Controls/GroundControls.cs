using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAiAndControls.Controls
{
    public class GroundControls : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITriangleMeshWithColor? GuideCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public I3dObject ParentObject { get; set; }

        public float zPosition { get; set; } = 0;
        public IPhysics Physics { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public I3dObject MoveObject(I3dObject theObject)
        {
            ParentObject = theObject;
            if (theObject != null && theObject.ParentSurface != null)
            {
                //Replace the surfaces from the new viewport - other objects might have moved surface position
                var newViewPort = theObject!.ParentSurface!.GetSurfaceViewPort();                
                theObject.ObjectParts = newViewPort.ObjectParts; 
                theObject.CrashBoxes = newViewPort.CrashBoxes;
            }            
            return theObject!;
        }

        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }
    }
}
