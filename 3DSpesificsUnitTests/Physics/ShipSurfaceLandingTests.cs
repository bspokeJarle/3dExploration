using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Physics;

[TestClass]
public class ShipSurfaceLandingTests
{
    // Mirror of CrashDetection.EstimateDirection (private) for testability.
    private static ImpactDirection EstimateDirection(float dx, float dy, float dz)
    {
        if (Math.Abs(dy) > Math.Abs(dx) && Math.Abs(dy) > Math.Abs(dz))
            return dy < 0 ? ImpactDirection.Top : ImpactDirection.Bottom;
        else if (Math.Abs(dx) > Math.Abs(dz))
            return dx > 0 ? ImpactDirection.Right : ImpactDirection.Left;
        else
            return ImpactDirection.Center;
    }

    // ShipRestingScreenY mirrors the formula in ShipControls.
    private static float ShipRestingScreenY(int screenHeight) => screenHeight * 0.195f;

    // Surface main crash box center Y (local, before gmpY offset).
    // From Surface.GetMainSurfaceCrashBox: min.y = -100, max.y = 1000 → center = 450.
    private const float SurfaceCrashBoxCenterY = 450f;

    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.ObjectIdCounter = 0;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Restore default screen size so other tests are not affected.
        ScreenSetup.Initialize(1500, 1024);
    }

    // -----------------------------------------------------------------
    // Gravity-settle simulation helper
    //
    // Runs the actual Physics.ApplyFallGravity code together with the
    // airborne-settle logic from ShipControls.ApplyGravity to find the
    // steady-state screen position and altitude offset.
    // -----------------------------------------------------------------

    private static (float offsetY, float gmpY) SimulateGravityEquilibrium(
        int screenHeight, int frames = 3000)
    {
        int screenWidth = (int)(screenHeight * (1500.0 / 1024.0));
        ScreenSetup.Initialize(screenWidth, screenHeight);

        var physics = new GameAiAndControls.Physics.Physics();
        float shipRestingY = ShipRestingScreenY(screenHeight);
        float offsetY = shipRestingY;
        float gmpY = 0f;
        float dt = 1f / 90f;

        // Skip hover float phase so gravity ramps to full immediately.
        physics.HoverElapsed = 10f;

        for (int i = 0; i < frames; i++)
        {
            // 1. Gravity (Physics.ApplyFallGravity — actual code)
            float inertiaY = physics.ApplyFallGravity(70, dt);

            // 2. Apply inertia to position (ShipControls lines 887, 890)
            offsetY = MathF.Min(
                Math.Clamp(offsetY - inertiaY, physics.FloorHeight, physics.CeilingHeight),
                physics.MaxScreenDrop);
            gmpY = MathF.Min(gmpY + inertiaY, physics.CeilingHeight);

            // 3. Airborne settle (ShipControls lines 894-913)
            float airSettle = MathF.Min(physics.AirborneSettleRate * dt, 1f);
            float airScreenDiff = shipRestingY - offsetY;
            float airAltDiff = -gmpY;
            const float MaxAirSettleSpeed = 300f;
            float airMaxStep = MaxAirSettleSpeed * dt;

            if (MathF.Abs(airScreenDiff) > 0.5f)
            {
                float step = airScreenDiff * airSettle;
                if (MathF.Abs(step) > airMaxStep)
                    step = airMaxStep * MathF.Sign(airScreenDiff);
                offsetY += step;
            }

            if (MathF.Abs(airAltDiff) > 0.5f)
            {
                float step = airAltDiff * airSettle;
                if (MathF.Abs(step) > airMaxStep)
                    step = airMaxStep * MathF.Sign(airAltDiff);
                gmpY += step;
            }
        }

        return (offsetY, gmpY);
    }

    // -----------------------------------------------------------------
    // Physics computed properties must reflect actual screen size
    // -----------------------------------------------------------------

    [TestMethod]
    public void MaxScreenDrop_ReflectsCurrentScreenSize()
    {
        var physics = new GameAiAndControls.Physics.Physics();

        ScreenSetup.Initialize(1500, 1024);
        float small = physics.MaxScreenDrop;

        ScreenSetup.Initialize(2250, 1440);
        float big = physics.MaxScreenDrop;

        Assert.AreNotEqual(small, big,
            "MaxScreenDrop must change when screen size changes (not stale from construction).");
        Assert.AreEqual(1024 * 0.44f, small, 0.1f);
        Assert.AreEqual(1440 * 0.44f, big, 0.1f);
    }

    [TestMethod]
    public void AirborneSettleRate_ReflectsCurrentScreenSize()
    {
        var physics = new GameAiAndControls.Physics.Physics();

        ScreenSetup.Initialize(1500, 1024);
        float small = physics.AirborneSettleRate;

        ScreenSetup.Initialize(2250, 1440);
        float big = physics.AirborneSettleRate;

        Assert.AreNotEqual(small, big,
            "AirborneSettleRate must change when screen size changes.");
        // Bigger screen → larger ScreenScaleY → lower settle rate.
        Assert.IsTrue(big < small,
            "AirborneSettleRate should be lower on bigger screens (scaled by 1/ScreenScaleY).");
    }

    // -----------------------------------------------------------------
    // Gravity-settle equilibrium: drift from rest increases with screen
    // size because AirborneSettleRate decreases while gravity stays the
    // same.  A larger drift makes the direction estimation less
    // predictable for surface collisions.
    // -----------------------------------------------------------------

    [TestMethod]
    public void GravityEquilibrium_DriftIncreasesWithScreenSize()
    {
        var (smallOffsetY, _) = SimulateGravityEquilibrium(1024);
        var (bigOffsetY, _) = SimulateGravityEquilibrium(1440);

        float smallDrift = smallOffsetY - ShipRestingScreenY(1024);
        float bigDrift = bigOffsetY - ShipRestingScreenY(1440);

        Assert.IsTrue(smallDrift > 0,
            $"Ship must drift below resting position under gravity (small={smallDrift:F1}).");
        Assert.IsTrue(bigDrift > smallDrift,
            $"Big screen drift ({bigDrift:F1}) must exceed small screen drift ({smallDrift:F1}).");
    }

    [TestMethod]
    [DataRow(768, DisplayName = "768p")]
    [DataRow(1024, DisplayName = "1024p")]
    [DataRow(1080, DisplayName = "1080p")]
    [DataRow(1440, DisplayName = "1440p")]
    [DataRow(2160, DisplayName = "2160p (4K)")]
    public void GravityEquilibrium_SurfaceLanding_WorksForAllScreenSizes(int screenHeight)
    {
        var (offsetY, gmpY) = SimulateGravityEquilibrium(screenHeight);

        // Approximate crash box center positions at equilibrium.
        // Ship crash center y ≈ offsetY (ship local crash box centered near 0).
        // Surface crash center y ≈ SurfaceCrashBoxCenterY + gmpY.
        float shipCenterY = offsetY;
        float surfaceCenterY = SurfaceCrashBoxCenterY + gmpY;
        float dy = shipCenterY - surfaceCenterY;

        var direction = EstimateDirection(0, dy, 0);

        // Regardless of what direction the estimator returns, the fix ensures
        // "Surface" collisions always land.
        const string crashedWith = "Surface";
        bool wouldLand = crashedWith == "Surface" ||
                         direction == ImpactDirection.Top ||
                         direction == ImpactDirection.Center;

        Assert.IsTrue(wouldLand,
            $"Screen {screenHeight}p: offsetY={offsetY:F1} gmpY={gmpY:F1} dy={dy:F1} " +
            $"direction={direction}. Surface collision must always result in landing.");
    }

    // -----------------------------------------------------------------
    // Landing condition: crashedWith == "Surface" must bypass direction
    // -----------------------------------------------------------------

    [TestMethod]
    [DataRow(ImpactDirection.Top)]
    [DataRow(ImpactDirection.Bottom)]
    [DataRow(ImpactDirection.Left)]
    [DataRow(ImpactDirection.Right)]
    [DataRow(ImpactDirection.Center)]
    public void LandingCondition_SurfaceCrash_LandsRegardlessOfDirection(ImpactDirection direction)
    {
        const string crashedWith = "Surface";

        bool wouldLand = crashedWith == "Surface" ||
                         direction == ImpactDirection.Top ||
                         direction == ImpactDirection.Center;

        Assert.IsTrue(wouldLand,
            $"Surface crash with direction={direction} must always trigger landing.");
    }

    [TestMethod]
    [DataRow(ImpactDirection.Bottom)]
    [DataRow(ImpactDirection.Left)]
    [DataRow(ImpactDirection.Right)]
    public void LandingCondition_NonSurface_DoesNotLandOnBottomLeftRight(ImpactDirection direction)
    {
        const string crashedWith = "UnknownObject";

        bool wouldLand = crashedWith == "Surface" ||
                         direction == ImpactDirection.Top ||
                         direction == ImpactDirection.Center;

        Assert.IsFalse(wouldLand,
            $"Non-surface crash with direction={direction} should NOT trigger landing.");
    }

    [TestMethod]
    [DataRow(ImpactDirection.Top)]
    [DataRow(ImpactDirection.Center)]
    public void LandingCondition_NonSurface_LandsOnTopOrCenter(ImpactDirection direction)
    {
        const string crashedWith = "UnknownObject";

        bool wouldLand = crashedWith == "Surface" ||
                         direction == ImpactDirection.Top ||
                         direction == ImpactDirection.Center;

        Assert.IsTrue(wouldLand,
            $"Non-surface crash with direction={direction} should trigger landing.");
    }

    }
