using TheOmegaStrain.Game.Helpers;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Tests.WorldObjects;

[TestClass]
public class FishJumpAreaTests
{
    private const int MaxHeight = 100;

    // FindFishJumpAreas scales its minimum tile thresholds via SurfaceSetup.ScaleTileCount,
    // so the fixtures must be expressed in the same scaled tile space.
    private static int S(int originalTileCount) => SurfaceSetup.ScaleTileCount(originalTileCount);

    [TestMethod]
    public void FindFishJumpAreas_ReturnsOneAreaPerLargeWaterComponent()
    {
        var map = CreateLandMap(S(30));
        FillWater(map, startX: S(2), startZ: S(2), width: S(6), height: S(2));
        FillWater(map, startX: S(15), startZ: S(2), width: S(5), height: S(2));
        FillWater(map, startX: S(8), startZ: S(18), width: S(8), height: S(3));

        var areas = SurfaceGeneration.FindFishJumpAreas(map, MaxHeight, minWidthTiles: 6, minHeightTiles: 2, maxAreas: 100);

        Assert.AreEqual(2, areas.Count);
        Assert.IsTrue(areas.TrueForAll(area => area.WidthTiles >= S(6) && area.HeightTiles >= S(2)));
    }

    [TestMethod]
    public void FindFishJumpAreas_UsesOnlyOneFishPerConnectedWaterBody()
    {
        var map = CreateLandMap(S(30));
        FillWater(map, startX: S(2), startZ: S(2), width: S(12), height: S(4));

        var areas = SurfaceGeneration.FindFishJumpAreas(map, MaxHeight, minWidthTiles: 6, minHeightTiles: 2, maxAreas: 100);

        Assert.AreEqual(1, areas.Count);
        Assert.AreEqual(S(12) * S(4), areas[0].ComponentTileCount);
    }

    [TestMethod]
    public void FindFishJumpAreas_RespectsMaxAreas()
    {
        var map = CreateLandMap(S(30));
        for (int i = 0; i < 4; i++)
            FillWater(map, startX: S(2), startZ: S(2 + i * 7), width: S(6), height: S(2));

        var areas = SurfaceGeneration.FindFishJumpAreas(map, MaxHeight, minWidthTiles: 6, minHeightTiles: 2, maxAreas: 3);

        Assert.AreEqual(3, areas.Count);
    }

    [TestMethod]
    public void FindFishJumpAreas_WithPriority_ReturnsClosestWaterBodiesFirst()
    {
        var map = CreateLandMap(S(30));
        FillWater(map, startX: S(2), startZ: S(2), width: S(6), height: S(2));
        FillWater(map, startX: S(20), startZ: S(20), width: S(6), height: S(2));

        var areas = SurfaceGeneration.FindFishJumpAreas(
            map,
            MaxHeight,
            minWidthTiles: 6,
            minHeightTiles: 2,
            maxAreas: 1,
            priorityTileX: S(23),
            priorityTileZ: S(21));

        Assert.AreEqual(1, areas.Count);
        Assert.AreEqual(S(23), areas[0].CenterTileX);
        Assert.AreEqual(S(21), areas[0].CenterTileZ);
    }

    [TestMethod]
    public void FindFishJumpAreas_WithPriority_PlacesFishNearPriorityInsideLargeWaterBody()
    {
        var map = CreateLandMap(S(30));
        FillWater(map, startX: S(2), startZ: S(2), width: S(20), height: S(4));

        var areas = SurfaceGeneration.FindFishJumpAreas(
            map,
            MaxHeight,
            minWidthTiles: 6,
            minHeightTiles: 2,
            maxAreas: 100,
            priorityTileX: S(20),
            priorityTileZ: S(3));

        Assert.AreEqual(1, areas.Count);
        Assert.IsTrue(areas[0].CenterTileX >= S(18), "Fish should be anchored near the visible priority area, not at the first rectangle in the water body.");
        Assert.AreEqual(S(2), areas[0].StartTileX);
        Assert.AreEqual(S(2) + S(20) - 1, areas[0].EndTileX);
        Assert.AreEqual(S(20), areas[0].WidthTiles);
    }

    private static SurfaceData[,] CreateLandMap(int size)
    {
        var map = new SurfaceData[size, size];
        int mapId = 0;
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                mapId++;
                map[z, x] = new SurfaceData
                {
                    mapId = mapId,
                    mapDepth = 50,
                    isInfected = false
                };
            }
        }

        return map;
    }

    private static void FillWater(SurfaceData[,] map, int startX, int startZ, int width, int height)
    {
        for (int z = startZ; z < startZ + height; z++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                map[z, x].mapDepth = 0;
                map[z, x].isInfected = false;
                map[z, x].isCratered = false;
                map[z, x].hasLandbasedObject = false;
            }
        }
    }
}
