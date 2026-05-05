using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;
using GameAiAndControls.Controls;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class BomberBombControlsTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.SurfaceState.AiObjects = new List<_3dObject>();
    }

    [TestMethod]
    public void MoveObject_WhenBombCrashes_ExplosionKeepsSurfaceImpactMetadata()
    {
        var control = new BomberBombControls();
        var bomb = new _3dObject
        {
            ObjectId = 101,
            ObjectName = "BomberBomb",
            IsActive = true,
            WorldPosition = new Vector3 { x = 1000, y = 0, z = 2000 },
            ObjectOffsets = new Vector3 { x = 0, y = 0, z = 500 },
            Rotation = new Vector3(),
            CrashBoxes = new List<List<IVector3>> { new() { new Vector3(), new Vector3 { x = 1, y = 1, z = 1 } } },
            ImpactStatus = new ImpactStatus { HasCrashed = true },
            ObjectParts = new List<I3dObjectPart>
            {
                new _3dObjectPart
                {
                    PartName = "BombPart",
                    IsVisible = true,
                    Triangles = new List<ITriangleMeshWithColor>
                    {
                        new TriangleMeshWithColor
                        {
                            vert1 = new Vector3 { x = 0, y = 0, z = 0 },
                            vert2 = new Vector3 { x = 1, y = 0, z = 0 },
                            vert3 = new Vector3 { x = 0, y = 1, z = 0 },
                            Color = "FFFFFF"
                        }
                    }
                }
            }
        };

        GameState.SurfaceState.AiObjects.Add(bomb);

        // First move starts explosion path.
        control.MoveObject(bomb, null, null);

        Assert.IsTrue(bomb.ImpactStatus!.HasCrashed, "Bomb should remain marked as crashed.");
        Assert.AreEqual("Surface", bomb.ImpactStatus.ObjectName,
            "Bomb impact target should be persisted as Surface for crater detection.");
    }
}
