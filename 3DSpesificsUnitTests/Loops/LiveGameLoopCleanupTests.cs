using _3dTesting.MainWindowClasses.Loops;
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
