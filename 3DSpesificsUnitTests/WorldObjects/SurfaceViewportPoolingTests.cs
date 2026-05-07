using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.WorldObjects;

[TestClass]
public class SurfaceViewportPoolingTests
{
    private int _originalMaxHeight;

    [TestInitialize]
    public void Setup()
    {
        _originalMaxHeight = MapSetup.maxHeight;
        MapSetup.maxHeight = 100;

        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState
        {
            Global2DMap = CreateMap(30),
            GlobalMapPosition = new Vector3 { x = 0f, y = 0f, z = 0f }
        };
    }

    [TestCleanup]
    public void Cleanup()
    {
        MapSetup.maxHeight = _originalMaxHeight;
    }

    [TestMethod]
    public void GetSurfaceViewPort_ReusesSurfaceTrianglesWithoutSharingReturnedPartState()
    {
        var surface = new Surface();
        int expectedTriangleCount = GetExpectedTriangleCount(surface.ViewPortSize());

        var firstViewport = surface.GetSurfaceViewPort();
        var firstPart = GetSurfacePart(firstViewport);
        var firstTriangle = firstPart.Triangles[0];

        firstPart.Triangles = new List<ITriangleMeshWithColor>();

        var secondViewport = surface.GetSurfaceViewPort();
        var secondPart = GetSurfacePart(secondViewport);

        Assert.AreEqual(expectedTriangleCount, secondPart.Triangles.Count, "Surface viewport should rebuild the full triangle list after a returned part is mutated by rendering.");
        Assert.AreSame(firstTriangle, secondPart.Triangles[0], "Surface viewport should reuse triangle instances to avoid per-frame mesh allocation.");
    }

    private static int GetExpectedTriangleCount(int viewPortSize)
    {
        int rowLimit = (int)(viewPortSize / 1.5) + 2;
        int tileCount = (rowLimit - 1) * (viewPortSize - 1);
        return tileCount * 2;
    }

    private static I3dObjectPart GetSurfacePart(I3dObject viewport)
    {
        var part = viewport.ObjectParts.SingleOrDefault(part => part.PartName == "Surface");
        Assert.IsNotNull(part, "Expected a Surface object part.");
        return part;
    }

    private static SurfaceData[,] CreateMap(int size)
    {
        var map = new SurfaceData[size, size];
        int mapId = 0;

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                map[z, x] = new SurfaceData
                {
                    mapDepth = 20,
                    mapId = ++mapId,
                    isInfected = false
                };
            }
        }

        return map;
    }
}
