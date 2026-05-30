using _3dRotations.Scene.Scene6;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using CommonUtilities.Events;
using Domain;
using GameAiAndControls.Controls;
using System.Linq;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class Scene6DesertAssetTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.WeatherVisualState = new WeatherVisualState();
        GameState.ObjectIdCounter = 0;
    }

    [TestMethod]
    public void SetupScene_UsesDesertWeatherCactusAndDesertTowers()
    {
        var scene = new Scene6();
        var world = new TestWorld();

        scene.SetupScene(world);

        Assert.AreEqual(SceneBiomeTypes.Desert, scene.SceneBiome);
        Assert.AreEqual(1, world.WorldInhabitants.Count(o => o.ObjectName == "SandEmitter"));
        Assert.AreEqual(0, world.WorldInhabitants.Count(o => o.ObjectName == "RainEmitter"));
        Assert.AreEqual(0, world.WorldInhabitants.Count(o => o.ObjectName == "SnowEmitter"));

        Assert.IsTrue(world.WorldInhabitants.Any(o => o.ObjectName == "Cactus"),
            "Desert scene should use cactus vegetation assets.");
        Assert.IsTrue(world.WorldInhabitants.Count(o => o.ObjectName == "Cactus") >= 5,
            "Desert scene should include guaranteed cactus landmarks near the starting route.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "Tree"),
            "Desert scene should not use hills/woods tree assets.");
        Assert.IsFalse(world.WorldInhabitants.Any(o => o.ObjectName == "House"),
            "Desert scene should not use temperate house assets.");

        var rockFormations = world.WorldInhabitants.Where(o => o.ObjectName == "DesertRockFormation").ToList();
        Assert.IsTrue(rockFormations.Count >= 3,
            "Desert scene should include rock formations as desert landmarks.");
        Assert.IsTrue(rockFormations.Any(IsNearStart),
            "Desert should include at least one rock formation near the starting route.");

        var towers = world.WorldInhabitants.Where(o => o.ObjectName == "Tower").ToList();
        Assert.IsTrue(towers.Count > 0, "Desert should keep tower landmarks.");
        Assert.IsTrue(towers.All(t => t.Movement is TowerControls), "Desert towers should keep the standard tower behavior.");
        Assert.IsTrue(towers.Any(t => t.ObjectParts.Any(p =>
            p.PartName == "TowerBase" &&
            p.Triangles.Count > 0 &&
            p.Triangles.All(tri => tri.Color == "8C6A3E"))),
            "Desert towers should use the sand/brown desert visual variant.");
        Assert.IsTrue(towers.Any(IsNearStart),
            "Desert should include at least one tower landmark near the starting route.");
    }

    private static bool IsNearStart(I3dObject obj)
    {
        if (obj.SurfaceBasedId == null)
            return false;

        var map = GameState.SurfaceState.Global2DMap!;
        int tileSize = SurfaceSetup.tileSize;
        int startX = ((int)(GameState.SurfaceState.GlobalMapPosition.x / tileSize)) % map.GetLength(1);
        int startY = ((int)(GameState.SurfaceState.GlobalMapPosition.z / tileSize)) % map.GetLength(0);
        if (startX < 0) startX += map.GetLength(1);
        if (startY < 0) startY += map.GetLength(0);

        for (int y = 0; y < map.GetLength(0); y++)
        {
            for (int x = 0; x < map.GetLength(1); x++)
            {
                if (map[y, x].mapId != obj.SurfaceBasedId.Value)
                    continue;

                return Math.Abs(x - startX) <= SurfaceSetup.viewPortSize &&
                       Math.Abs(y - startY) <= SurfaceSetup.viewPortSize;
            }
        }

        return false;
    }

    private sealed class TestWorld : I3dWorld
    {
        public List<I3dObject> WorldInhabitants { get; set; } = new();
        public ISceneHandler SceneHandler { get; set; } = null!;
        public IGameEventBus? EventBus { get; set; } = new GameEventBus();
        public bool IsPaused { get; set; }
    }
}
