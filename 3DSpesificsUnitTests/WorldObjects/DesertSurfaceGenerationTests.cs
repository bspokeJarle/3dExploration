using _3dRotations.Helpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using CommonUtilities.GamePlayHelpers;
using Domain;

namespace _3DSpesificsUnitTests.WorldObjects;

[TestClass]
public class DesertSurfaceGenerationTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState
        {
            SceneBiome = SceneBiomeTypes.Desert
        };
    }

    [TestMethod]
    public void ReturnPseudoRandomMap_ForDesert_CreatesFewLargeLakeComponents()
    {
        var map = SurfaceGeneration.ReturnPseudoRandomMap(MapSetup.globalMapSize, out int maxHeight, 30000, 15000);

        var allComponents = FindDeepWaterComponents(map, maxHeight);
        var components = allComponents
            .Where(c => c.Count >= 100 && c.Width >= 10 && c.Height >= 10)
            .ToList();

        Assert.IsTrue(components.Count >= 3 && components.Count <= 4,
            $"Desert should have 3-4 large lakes, not scattered small ponds. Found {components.Count} large components.");
        Assert.IsFalse(allComponents.Any(c => c.Count < 20),
            "Desert should not have isolated single-tile or tiny water patches.");
        Assert.IsTrue(components.All(c => c.Width >= 10 && c.Height >= 10),
            "Each desert lake should be at least about 10x10 tiles.");
        Assert.IsTrue(components.Any(c => IsNearPlatform(map, c)),
            "One large desert lake should be close enough to the starting platform to become an early oasis landmark.");
    }

    [TestMethod]
    public void FindDesertOasisPlantPlacements_ReturnsDryTilesNearWater()
    {
        var map = SurfaceGeneration.ReturnPseudoRandomMap(MapSetup.globalMapSize, out int maxHeight, 30000, 15000);

        var placements = SurfaceGeneration.FindDesertOasisPlantPlacements(map, MapSetup.globalMapSize, maxHeight, maxPlants: 40);

        Assert.IsTrue(placements.Count > 0);
        Assert.IsTrue(placements.All(p => IsDry(map[p.y, p.x], maxHeight)));
        Assert.IsTrue(placements.All(p => HasWaterNearby(map, maxHeight, p.x, p.y)));
    }

    private static List<WaterComponent> FindDeepWaterComponents(SurfaceData[,] map, int maxHeight)
    {
        int height = map.GetLength(0);
        int width = map.GetLength(1);
        var visited = new bool[height, width];
        var components = new List<WaterComponent>();

        for (int y = SurfaceSetup.viewPortSize; y < height - SurfaceSetup.viewPortSize; y++)
        {
            for (int x = SurfaceSetup.viewPortSize; x < width - SurfaceSetup.viewPortSize; x++)
            {
                if (visited[y, x] || !IsDeepWater(map[y, x], maxHeight))
                    continue;

                int count = 0;
                int minX = x;
                int maxX = x;
                int minY = y;
                int maxY = y;
                var queue = new Queue<(int x, int y)>();
                visited[y, x] = true;
                queue.Enqueue((x, y));

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    count++;
                    minX = Math.Min(minX, current.x);
                    maxX = Math.Max(maxX, current.x);
                    minY = Math.Min(minY, current.y);
                    maxY = Math.Max(maxY, current.y);

                    Enqueue(current.x - 1, current.y);
                    Enqueue(current.x + 1, current.y);
                    Enqueue(current.x, current.y - 1);
                    Enqueue(current.x, current.y + 1);
                }

                components.Add(new WaterComponent(count, minX, minY, maxX, maxY));

                void Enqueue(int nx, int ny)
                {
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        return;
                    if (visited[ny, nx] || !IsDeepWater(map[ny, nx], maxHeight))
                        return;

                    visited[ny, nx] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        return components;
    }

    private static bool IsNearPlatform(SurfaceData[,] map, WaterComponent component)
    {
        var platformCenter = LandingPlatformHelpers.GetLandingPlatformCenterTile(map);
        int lakeCenterX = (component.MinX + component.MaxX) / 2;
        int lakeCenterY = (component.MinY + component.MaxY) / 2;
        return Math.Abs(lakeCenterX - platformCenter.x) <= 80 &&
               Math.Abs(lakeCenterY - platformCenter.z) <= 80;
    }

    private readonly record struct WaterComponent(int Count, int MinX, int MinY, int MaxX, int MaxY)
    {
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
    }

    private static bool IsDry(SurfaceData tile, int maxHeight)
    {
        var terrain = CommonUtilities.GamePlayHelpers.GamePlayHelpers.GetTerrainType(tile.mapDepth, maxHeight);
        return terrain == CommonUtilities.GamePlayHelpers.GamePlayHelpers.TerrainType.Grassland ||
               terrain == CommonUtilities.GamePlayHelpers.GamePlayHelpers.TerrainType.Highlands;
    }

    private static bool IsDeepWater(SurfaceData tile, int maxHeight)
    {
        return CommonUtilities.GamePlayHelpers.GamePlayHelpers.GetTerrainType(tile.mapDepth, maxHeight) ==
               CommonUtilities.GamePlayHelpers.GamePlayHelpers.TerrainType.DeepWater;
    }

    private static bool HasWaterNearby(SurfaceData[,] map, int maxHeight, int x, int y)
    {
        for (int oy = -4; oy <= 4; oy++)
        {
            for (int ox = -4; ox <= 4; ox++)
            {
                int nx = x + ox;
                int ny = y + oy;
                if (nx < 0 || ny < 0 || nx >= map.GetLength(1) || ny >= map.GetLength(0))
                    continue;

                var terrain = CommonUtilities.GamePlayHelpers.GamePlayHelpers.GetTerrainType(map[ny, nx].mapDepth, maxHeight);
                if (terrain == CommonUtilities.GamePlayHelpers.GamePlayHelpers.TerrainType.DeepWater ||
                    terrain == CommonUtilities.GamePlayHelpers.GamePlayHelpers.TerrainType.Coast)
                    return true;
            }
        }

        return false;
    }
}
