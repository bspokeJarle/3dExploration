using TheOmegaStrain.Wpf.Input;

namespace TheOmegaStrain.Tests.Controls;

[TestClass]
public class XboxQuitHoldTrackerTests
{
    [TestMethod]
    public void Update_TriggersOnlyAfterTwoContinuousSeconds()
    {
        var tracker = new XboxQuitHoldTracker();
        var start = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

        Assert.IsFalse(tracker.Update(isPressed: true, start));
        Assert.IsFalse(tracker.Update(isPressed: true, start.AddMilliseconds(1999)));
        Assert.IsTrue(tracker.Update(isPressed: true, start.AddSeconds(2)));
        Assert.IsFalse(tracker.Update(isPressed: true, start.AddSeconds(3)));
    }

    [TestMethod]
    public void Update_ReleaseResetsRequiredHoldDuration()
    {
        var tracker = new XboxQuitHoldTracker();
        var start = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

        tracker.Update(isPressed: true, start);
        tracker.Update(isPressed: false, start.AddSeconds(1));

        Assert.IsFalse(tracker.Update(isPressed: true, start.AddSeconds(1.5)));
        Assert.IsFalse(tracker.Update(isPressed: true, start.AddSeconds(3.4)));
        Assert.IsTrue(tracker.Update(isPressed: true, start.AddSeconds(3.5)));
    }
}
