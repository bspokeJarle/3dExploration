using _3dTesting.MainWindowClasses.Loops;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Events;
using Domain;
using System.Reflection;
using System.Windows.Input;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Loops;

[TestClass]
public class LiveGameLoopCleanupTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.SurfaceState = new SurfaceState();
        GameState.GamePlayState = new GamePlayState();
        GameState.ShipState = new ShipState();
    }

    [TestMethod]
    public void CleanupExplodedObjects_RemovesOnlyObjectsQueuedByExplosionEvent()
    {
        var bus = new GameEventBus();
        var world = new TestWorld
        {
            EventBus = bus,
            WorldInhabitants = new List<I3dObject>
            {
                CreateExplodedObject(10),
                CreateExplodedObject(11)
            }
        };

        var loop = new LiveGameLoop();
        InvokePrivate(loop, "EnsureExplosionCleanupSubscription", bus);

        bus.Publish(new GameEvent
        {
            Type = GameEventType.ObjectExploded,
            Source = world.WorldInhabitants[0],
            ObjectName = world.WorldInhabitants[0].ObjectName
        });

        InvokePrivate(loop, "CleanupExplodedObjects", world);

        Assert.IsFalse(world.WorldInhabitants.Any(x => x.ObjectId == 10));
        Assert.IsTrue(world.WorldInhabitants.Any(x => x.ObjectId == 11));
    }

    [TestMethod]
    public void CleanupExplodedObjects_PowerUpDropPreservesSourceLocalRenderOffsets()
    {
        GameState.SurfaceState.GlobalMapPosition = new Vector3 { x = 500f, y = 80f, z = 900f };
        GameState.SurfaceState.AiObjects = new List<_3dObject>();
        var explodedSeeder = CreateExplodedObject(20);
        explodedSeeder.ObjectName = "Seeder";
        explodedSeeder.HasPowerUp = true;
        explodedSeeder.WorldPosition = new Vector3 { x = 1200f, y = 3f, z = 1800f };
        explodedSeeder.ObjectOffsets = new Vector3 { x = 35f, y = 260f, z = 125f };
        var sourceWorld = Copy(explodedSeeder.WorldPosition);
        var sourceOffsets = Copy(explodedSeeder.ObjectOffsets);
        GameState.SurfaceState.AiObjects.Add(explodedSeeder);
        var world = new TestWorld
        {
            WorldInhabitants = new List<I3dObject> { explodedSeeder }
        };

        var loop = new LiveGameLoop();

        InvokePrivate(loop, "CleanupExplodedObjects", world);

        var powerup = world.WorldInhabitants.OfType<_3dObject>().Single(x => x.ObjectName == "PowerUp");
        Assert.AreEqual(sourceWorld.x, powerup.WorldPosition!.x, 0.001f);
        Assert.AreEqual(sourceWorld.y, powerup.WorldPosition.y, 0.001f);
        Assert.AreEqual(sourceWorld.z, powerup.WorldPosition.z, 0.001f);
        Assert.AreEqual(sourceOffsets.x, powerup.ObjectOffsets!.x, 0.001f,
            "PowerUp drops must keep the source object's local X offset so they do not jump sideways.");
        Assert.AreEqual(sourceOffsets.z, powerup.ObjectOffsets.z, 0.001f,
            "PowerUp drops must keep the source object's local Z offset so they do not jump in depth.");
        Assert.AreEqual(sourceOffsets.y - GameState.SurfaceState.GlobalMapPosition.y * SurfacePositionSyncHelpers.DefaultEnemySurfaceSyncFactorY - 50f,
            powerup.ObjectOffsets.y,
            0.001f,
            "PowerUp Y is stored as raw surface-sync input; PowerUpControls reapplies surface sync on the next frame.");
        Assert.IsTrue(GameState.SurfaceState.AiObjects.Any(x => x.ObjectId == powerup.ObjectId));
    }

    private static Vector3 Copy(IVector3 source)
    {
        return new Vector3
        {
            x = source.x,
            y = source.y,
            z = source.z
        };
    }

    private static _3dObject CreateExplodedObject(int objectId)
    {
        return new _3dObject
        {
            ObjectId = objectId,
            ObjectName = "Decoration",
            ObjectOffsets = new Vector3(),
            Rotation = new Vector3(),
            WorldPosition = new Vector3(),
            CrashBoxes = new List<List<IVector3>>(),
            ObjectParts = new List<I3dObjectPart>(),
            ImpactStatus = new ImpactStatus { HasExploded = true }
        };
    }

    private static void InvokePrivate(LiveGameLoop loop, string methodName, params object?[] args)
    {
        var method = typeof(LiveGameLoop).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Expected private method '{methodName}' to exist.");
        method.Invoke(loop, args);
    }

    private sealed class TestWorld : I3dWorld
    {
        public List<I3dObject> WorldInhabitants { get; set; } = new();
        public ISceneHandler SceneHandler { get; set; } = new TestSceneHandler();
        public IGameEventBus? EventBus { get; set; }
        public bool IsPaused { get; set; }
    }

    private sealed class TestSceneHandler : ISceneHandler
    {
        public void SetupActiveScene(I3dWorld world) { }
        public void ResetActiveScene(I3dWorld world) { }
        public void NextScene(I3dWorld world) { }
        public IScene GetActiveScene() => null!;
        public void HandleKeyPress(KeyEventArgs k, I3dWorld world) { }
        public void UpdateFrame(I3dWorld world) { }
    }
}
