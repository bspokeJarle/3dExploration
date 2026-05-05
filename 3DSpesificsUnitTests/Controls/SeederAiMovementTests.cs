using System.Reflection;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls.SeederControls;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class SeederAiMovementTests
{
    private static readonly Type SeederAiType = typeof(SeederControls).Assembly.GetType(
        "GameAiAndControls.Controls.SeederControls.SeederAi",
        throwOnError: true)!;
    private static readonly Type AiStateType = SeederAiType.GetNestedType("AiState", BindingFlags.NonPublic)!;
    private static readonly MethodInfo MoveMethod = SeederAiType.GetMethod("HandleMoveTowardTarget", BindingFlags.Static | BindingFlags.NonPublic)!;

    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState
        {
            Global2DMap = new SurfaceData[4, 4],
            ScreenEcoMetas = new ScreenEcoMeta[1, 1],
            DirtyTiles = new List<IVector3>(),
            PendingLocalInfectionSpread = new List<(int tileX, int tileZ, long infectedTick)>()
        };
        GameState.SurfaceState.ScreenEcoMetas[0, 0] = new ScreenEcoMeta
        {
            BioTileCount = 0,
            BioTiles = new List<TileCoord>()
        };
    }

    [TestMethod]
    public void MoveTowardTarget_OnscreenDoesNotSnapWhenStepCounterExpiresBeforeTargetReached()
    {
        var state = CreateState();
        var current = new Vector3 { x = 0f, y = 0f, z = 0f };
        var target = new Vector3 { x = 100f, y = 0f, z = 0f };

        PrepareTarget(state, current, target, targetIsLocalBio: false, stepsRemaining: 1);

        var result = InvokeMove(isOnScreen: true, state, current, step: 1f, offscreenStepFactor: 1);

        Assert.AreEqual(1f, result.x, 0.001f, "Seeder should move one smooth step instead of snapping to the target.");
        Assert.AreEqual(0f, result.z, 0.001f);
        Assert.IsTrue((bool)GetField(state, "HasMovementTarget")!, "Seeder should keep the target until it actually reaches it.");
        Assert.AreEqual(1, (int)GetField(state, "StepsRemaining")!, "Stepper should stay alive for the next frame.");
    }

    [TestMethod]
    public void MoveTowardTarget_OffscreenUsesAcceleratedStepAndKeepsTargetUntilReached()
    {
        var state = CreateState();
        var current = new Vector3 { x = 0f, y = 0f, z = 0f };
        var target = new Vector3 { x = 100f, y = 0f, z = 0f };

        PrepareTarget(state, current, target, targetIsLocalBio: false, stepsRemaining: 10);

        var result = InvokeMove(isOnScreen: false, state, current, step: 10f, offscreenStepFactor: 3);

        Assert.AreEqual(10f, result.x, 0.001f, "Offscreen Seeder movement should use the larger offscreen step.");
        Assert.IsTrue((bool)GetField(state, "HasMovementTarget")!, "Offscreen travel should keep moving until the target is reached.");
        Assert.AreEqual(7, (int)GetField(state, "StepsRemaining")!, "Offscreen step countdown should use the offscreen factor.");
    }

    [TestMethod]
    public void MoveTowardTarget_WhenGlobalTargetReached_SwitchesToLocalHuntWithoutPause()
    {
        var state = CreateState();
        var current = new Vector3 { x = 0f, y = 0f, z = 0f };
        var target = new Vector3 { x = 1f, y = 0f, z = 0f };
        long now = DateTime.Now.Ticks;

        PrepareTarget(state, current, target, targetIsLocalBio: false, stepsRemaining: 1);

        _ = InvokeMove(isOnScreen: true, state, current, step: 5f, offscreenStepFactor: 1, nowTicks: now);

        Assert.IsFalse((bool)GetField(state, "IsSearchingGlobally")!);
        Assert.IsTrue((bool)GetField(state, "IsHuntingLocally")!);
        Assert.AreEqual(now, (long)GetField(state, "NextLocalRetargetTicks")!, "Global-to-local mode switch should let the Seeder pick local bio immediately.");
    }

    [TestMethod]
    public void MoveTowardTarget_WhenLocalBioTargetReached_PausesForSeeding()
    {
        int tile = 1;
        SetMapTile(tile, tile, mapDepth: 20, isInfected: false);
        AddBioTile(tile, tile);

        var state = CreateState();
        var current = TileWorld(tile, tile);
        long now = DateTime.Now.Ticks;

        PrepareTarget(state, current, current, targetIsLocalBio: true, stepsRemaining: 1);

        _ = InvokeMove(isOnScreen: true, state, current, step: 5f, offscreenStepFactor: 1, nowTicks: now);

        Assert.IsTrue(GameState.SurfaceState.Global2DMap![tile, tile].isInfected, "Seeder should infect the target tile when it reaches valid bio terrain.");
        Assert.IsTrue((long)GetField(state, "NextLocalRetargetTicks")! > now, "Seeder should pause only for a real seeding stall.");
        Assert.IsTrue((bool)GetField(state, "SeededAtCurrentStall")!);
    }

    [TestMethod]
    public void MoveTowardTarget_WhenLocalTargetIsNoLongerBio_RetargetsImmediately()
    {
        int tile = 1;
        SetMapTile(tile, tile, mapDepth: 0, isInfected: false);
        AddBioTile(tile, tile);

        var state = CreateState();
        var current = TileWorld(tile, tile);
        long now = DateTime.Now.Ticks;

        PrepareTarget(state, current, current, targetIsLocalBio: true, stepsRemaining: 1);

        _ = InvokeMove(isOnScreen: true, state, current, step: 5f, offscreenStepFactor: 1, nowTicks: now);

        Assert.IsFalse(GameState.SurfaceState.Global2DMap![tile, tile].isInfected, "Seeder should not infect water or invalid terrain.");
        Assert.AreEqual(now, (long)GetField(state, "NextLocalRetargetTicks")!, "Seeder should not spend its seeding pause on a stale non-bio target.");
        Assert.IsFalse((bool)GetField(state, "SeededAtCurrentStall")!);
    }

    private static object CreateState()
    {
        return Activator.CreateInstance(AiStateType)!;
    }

    private static void PrepareTarget(object state, Vector3 current, Vector3 target, bool targetIsLocalBio, int stepsRemaining)
    {
        SetField(state, "HasMovementTarget", true);
        SetField(state, "TargetIsLocalBio", targetIsLocalBio);
        SetField(state, "TargetWorld", target);
        SetField(state, "AuthWorldPos", current);
        SetField(state, "StepsRemaining", stepsRemaining);
    }

    private static Vector3 InvokeMove(bool isOnScreen, object state, Vector3 current, float step, int offscreenStepFactor, long? nowTicks = null)
    {
        var seeder = new _3dObject
        {
            ObjectId = 77,
            ObjectName = "Seeder",
            WorldPosition = current
        };

        return (Vector3)MoveMethod.Invoke(
            null,
            new object[] { isOnScreen, seeder, state, 77, nowTicks ?? DateTime.Now.Ticks, 1f / 60f, 1f, step, offscreenStepFactor, current })!;
    }

    private static void SetMapTile(int tileX, int tileZ, int mapDepth, bool isInfected)
    {
        GameState.SurfaceState.Global2DMap![tileZ, tileX] = new SurfaceData
        {
            mapDepth = mapDepth,
            isInfected = isInfected
        };
    }

    private static void AddBioTile(int tileX, int tileZ)
    {
        GameState.SurfaceState.ScreenEcoMetas[0, 0].BioTiles.Add(new TileCoord
        {
            X = tileX * SurfaceSetup.tileSize,
            Y = tileZ * SurfaceSetup.tileSize
        });
        GameState.SurfaceState.ScreenEcoMetas[0, 0].BioTileCount++;
    }

    private static Vector3 TileWorld(int tileX, int tileZ)
    {
        return new Vector3
        {
            x = tileX * SurfaceSetup.tileSize,
            y = 0f,
            z = tileZ * SurfaceSetup.tileSize
        };
    }

    private static void SetField(object target, string name, object value)
    {
        AiStateType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(target, value);
    }

    private static object? GetField(object target, string name)
    {
        return AiStateType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target);
    }
}
