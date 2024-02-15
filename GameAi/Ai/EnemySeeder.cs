using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAiAndControls.Ai
{
    public class EnemySeeder : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITriangleMeshWithColor? GuideCoordinates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public I3dObject MoveObject(I3dObject theObject)
        {
            throw new NotImplementedException();
        }

        public void SetStartGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            throw new NotImplementedException();
        }
    }
}
