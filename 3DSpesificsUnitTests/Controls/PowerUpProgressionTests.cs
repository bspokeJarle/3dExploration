using _3dRotations.Helpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;

namespace _3DSpesificsUnitTests.Controls;

/// <summary>
/// Locks in two coupled gameplay rules so future scene tuning cannot regress them:
///   1. Powerups are awarded by kill-time thresholds spread evenly across the wave.
///   2. Every powerup pickup advances the unlock tier (1 -> Decoy, 2 -> Lazer,
///      3 -> future weapon). Tutorial pickups count too, so a player who completes
///      training enters Scene1 with Decoy already unlocked.
/// </summary>
[TestClass]
public class PowerUpProgressionTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
    }

    [TestMethod]
    public void GetPowerUpCountForSeeders_FollowsBracketRule()
    {
        Assert.AreEqual(0, SeederPlacementHelpers.GetPowerUpCountForSeeders(0));
        Assert.AreEqual(1, SeederPlacementHelpers.GetPowerUpCountForSeeders(1));
        Assert.AreEqual(1, SeederPlacementHelpers.GetPowerUpCountForSeeders(15));
        Assert.AreEqual(2, SeederPlacementHelpers.GetPowerUpCountForSeeders(16));
        Assert.AreEqual(2, SeederPlacementHelpers.GetPowerUpCountForSeeders(22));
        Assert.AreEqual(3, SeederPlacementHelpers.GetPowerUpCountForSeeders(23));
        Assert.AreEqual(3, SeederPlacementHelpers.GetPowerUpCountForSeeders(40));
    }

    [TestMethod]
    public void ComputeThresholds_SpreadsDropsEvenlyAcrossWave()
    {
        var single = PowerUpDropPolicy.CalculateThresholds(15, 1);
        CollectionAssert.AreEqual(new[] { 8 }, single, "15 seeders with 1 drop should fire around the middle.");

        var two = PowerUpDropPolicy.CalculateThresholds(18, 2);
        CollectionAssert.AreEqual(new[] { 6, 12 }, two, "18 seeders with 2 drops should be evenly spaced.");

        var three = PowerUpDropPolicy.CalculateThresholds(23, 3);
        CollectionAssert.AreEqual(new[] { 6, 12, 18 }, three, "23 seeders with 3 drops should be evenly spaced.");
    }

    [TestMethod]
    public void ComputeThresholds_AvoidsDuplicatesWhenWaveIsTiny()
    {
        var thresholds = PowerUpDropPolicy.CalculateThresholds(3, 3);
        CollectionAssert.AllItemsAreUnique(thresholds);
        Assert.AreEqual(3, thresholds.Count);
    }

    [TestMethod]
    public void TryConsumeDrop_FiresOnlyOnConfiguredThresholds()
    {
        PowerUpDropPolicy.ConfigureForWave(totalSeeders: 6, powerUpCount: 2);

        var thresholds = PowerUpDropPolicy.CurrentThresholds;
        Assert.AreEqual(2, thresholds.Count);

        var dropAt = new HashSet<int>(thresholds);
        for (int kill = 1; kill <= 6; kill++)
        {
            bool dropped = PowerUpDropPolicy.TryConsumeDrop();
            Assert.AreEqual(dropAt.Contains(kill), dropped,
                $"Kill #{kill} should{(dropAt.Contains(kill) ? string.Empty : " not")} produce a powerup drop.");
        }

        // No more drops after the policy is exhausted.
        Assert.IsFalse(PowerUpDropPolicy.TryConsumeDrop(), "Further kills must not produce extra drops.");
    }

    [TestMethod]
    public void ConfigureForWave_ResetsCountersBetweenScenes()
    {
        PowerUpDropPolicy.ConfigureForWave(totalSeeders: 6, powerUpCount: 2);
        for (int i = 0; i < 6; i++) PowerUpDropPolicy.TryConsumeDrop();

        PowerUpDropPolicy.ConfigureForWave(totalSeeders: 15, powerUpCount: 1);
        Assert.AreEqual(0, PowerUpDropPolicy.SeederKillsObserved,
            "Configure must reset the observed kill counter for the new wave.");
        Assert.AreEqual(1, PowerUpDropPolicy.CurrentThresholds.Count);
    }

    [TestMethod]
    public void UnlockTiers_OnePowerupDecoyTwoPowerupsLazer()
    {
        // Spec: 1 powerup -> Decoy, 2 powerups -> Lazer, 3 powerups -> future weapon.
        // Every pickup counts (including the tutorial pickup), so this state is what the
        // ship sees regardless of where the pickups came from.
        var gps = GameState.GamePlayState;

        Assert.IsFalse(gps.IsDecoyUnlocked);
        Assert.IsFalse(gps.IsLazerUnlocked);

        // First powerup: Decoy unlocks, Lazer must still be locked.
        gps.PowerUpsCollected = 1;
        Assert.IsTrue(gps.IsDecoyUnlocked, "1 powerup must unlock Decoy.");
        Assert.IsFalse(gps.IsLazerUnlocked,
            "Lazer must not unlock from a single powerup pickup; it requires the second one.");

        // Second powerup: Lazer unlocks.
        gps.PowerUpsCollected = 2;
        Assert.IsTrue(gps.IsDecoyUnlocked, "Decoy must remain unlocked at tier 2.");
        Assert.IsTrue(gps.IsLazerUnlocked, "2 powerups must unlock Lazer.");

        // Third powerup is reserved for a future weapon; existing tiers must remain unlocked
        // and no current flag must regress.
        gps.PowerUpsCollected = 3;
        Assert.IsTrue(gps.IsDecoyUnlocked, "Decoy must remain unlocked at tier 3.");
        Assert.IsTrue(gps.IsLazerUnlocked, "Lazer must remain unlocked at tier 3.");
    }
}
