using _3dRotations.World.Objects;
using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Rendering;

[TestClass]
public class RenderSimpleOptimizationTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3()
        };
    }

    [TestMethod]
    public void SurfaceTriangleLookup_UsesCachedLandBasedTriangle()
    {
        var surface = new Surface
        {
            RotatedSurfaceTriangles = new List<ITriangleMeshWithColor>()
        };
        surface.RotatedSurfaceTriangleByLandId[42] = new TriangleMeshWithColor
        {
            landBasedPosition = 42,
            vert1 = new Vector3 { x = 25f, y = 15f, z = 5f },
            vert2 = new Vector3 { x = 35f, y = 15f, z = 5f },
            vert3 = new Vector3 { x = 25f, y = 25f, z = 5f }
        };

        var obj = CreateRenderableObject();
        obj.ParentSurface = surface;
        obj.SurfaceBasedId = 42;

        bool positioned = ObjectPlacementHelpers.TryGetRenderPosition(obj, 100, 100, out var x, out var y, out var z);

        Assert.IsTrue(positioned);
        Assert.AreEqual(100, x);
        Assert.AreEqual(100, y);
        Assert.AreEqual(0, z);
    }

    [TestMethod]
    public void ConvertTo2dFromObjects_ReusesProvidedResultList()
    {
        var converter = new _3dTo2d();
        var reusable = new List<_2dTriangleMesh>
        {
            new() { PartName = "Stale" }
        };

        var result = converter.ConvertTo2dFromObjects(
            new List<_3dObject> { CreateRenderableObject() },
            1,
            reusable);

        Assert.AreSame(reusable, result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Main", result[0].PartName);

        var emptyResult = converter.ConvertTo2dFromObjects(new List<_3dObject>(), 2, reusable);

        Assert.AreSame(reusable, emptyResult);
        Assert.AreEqual(0, emptyResult.Count);
    }

    private static _3dObject CreateRenderableObject()
    {
        return new _3dObject
        {
            ObjectId = 1,
            ObjectName = "Renderable",
            ObjectOffsets = new Vector3(),
            Rotation = new Vector3(),
            WorldPosition = new Vector3(),
            CrashBoxes = new List<List<IVector3>>(),
            ObjectParts = new List<I3dObjectPart>
            {
                new _3dObjectPart
                {
                    PartName = "Main",
                    IsVisible = true,
                    Triangles = new List<ITriangleMeshWithColor>
                    {
                        new TriangleMeshWithColor
                        {
                            Color = "ffffff",
                            noHidden = true,
                            normal1 = new Vector3 { x = 0f, y = 0f, z = 1f },
                            vert1 = new Vector3 { x = -10f, y = -10f, z = 0f },
                            vert2 = new Vector3 { x = 10f, y = -10f, z = 0f },
                            vert3 = new Vector3 { x = 0f, y = 10f, z = 0f }
                        }
                    }
                }
            }
        };
    }
}
