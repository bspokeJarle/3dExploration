using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using CommonUtilities.GamePlayHelpers;
using Domain;
using GameAiAndControls.Controls;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class GroundControlsCraterTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState
        {
            Global2DMap = new SurfaceData[5, 5],
            AiObjects = new List<_3dObject>()
        };

        for (int z = 0; z < 5; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                GameState.SurfaceState.Global2DMap[z, x] = new SurfaceData
                {
                    mapDepth = 20,
                    isInfected = false
                };
            }
        }
    }

    [TestMethod]
    public void MoveObject_WhenBombCratersSurface_DoesNotCraterWaterOrCoastTiles()
    {
        SetTile(1, 1, 0);  // DeepWater
        SetTile(2, 1, 5);  // Coast
        SetTile(1, 2, 20); // Grassland
        SetTile(2, 2, 40); // Highlands
        SetTile(3, 2, 60); // Mountains

        GameState.SurfaceState.AiObjects.Add(CreateSurfaceBombAtTile(2, 2));

        var ground = new _3dObject
        {
            ObjectId = 100,
            ObjectName = "Surface",
            ImpactStatus = new ImpactStatus(),
            WorldPosition = new Vector3(),
            ObjectOffsets = new Vector3()
        };

        new GroundControls().MoveObject(ground, null, null);

        Assert.IsFalse(GameState.SurfaceState.Global2DMap![1, 1].isCratered, "Deep water should keep its normal water rendering.");
        Assert.AreEqual(0, GameState.SurfaceState.Global2DMap[1, 1].mapDepth);
        Assert.IsFalse(GameState.SurfaceState.Global2DMap[1, 2].isCratered, "Coast/water edge should not become black or grey.");
        Assert.AreEqual(5, GameState.SurfaceState.Global2DMap[1, 2].mapDepth);

        AssertDryCratered(1, 2, GamePlayHelpers.TerrainType.Grassland);
        AssertDryCratered(2, 2, GamePlayHelpers.TerrainType.Highlands);
        AssertDryCratered(3, 2, GamePlayHelpers.TerrainType.Mountains);
    }

    private static void SetTile(int x, int z, int depth)
    {
        GameState.SurfaceState.Global2DMap![z, x] = new SurfaceData
        {
            mapDepth = depth,
            isInfected = false
        };
    }

    private static _3dObject CreateSurfaceBombAtTile(int x, int z)
    {
        return new _3dObject
        {
            ObjectId = 9001,
            ObjectName = "BomberBomb",
            WorldPosition = new Vector3
            {
                x = x * SurfaceSetup.tileSize,
                y = 0f,
                z = z * SurfaceSetup.tileSize
            },
            ImpactStatus = new ImpactStatus
            {
                HasCrashed = true,
                ObjectName = "Surface"
            }
        };
    }

    private static void AssertDryCratered(int x, int z, GamePlayHelpers.TerrainType originalTerrain)
    {
        var tile = GameState.SurfaceState.Global2DMap![z, x];
        Assert.IsTrue(tile.isCratered, $"{originalTerrain} should be allowed to show crater damage.");
        Assert.IsTrue(tile.mapDepth >= Math.Ceiling(MapSetup.maxHeight * 0.15f), "Cratered dry terrain should not be pushed down into water/coast height.");
    }
}
