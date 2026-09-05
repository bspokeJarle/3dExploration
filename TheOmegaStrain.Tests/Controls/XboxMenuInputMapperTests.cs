using TheOmegaStrain.Wpf.Input;

namespace TheOmegaStrain.Tests.Controls;

[TestClass]
public class XboxMenuInputMapperTests
{
    private const ushort DPadUp = 0x0001;
    private const ushort DPadDown = 0x0002;
    private const ushort DPadLeft = 0x0004;
    private const ushort DPadRight = 0x0008;
    private const ushort Menu = 0x0010;
    private const ushort View = 0x0020;
    private const ushort LeftShoulder = 0x0100;
    private const ushort RightShoulder = 0x0200;
    private const ushort A = 0x1000;
    private const ushort B = 0x2000;
    private const ushort X = 0x4000;
    private const ushort Y = 0x8000;

    [TestMethod]
    public void ToGameInputKey_MapsConfirmAndCancelButtons()
    {
        Assert.AreEqual(GameInputKey.Return, XboxMenuInputMapper.ToGameInputKey(Snapshot(A)));
        Assert.AreEqual(GameInputKey.Return, XboxMenuInputMapper.ToGameInputKey(Snapshot(Menu)));
        Assert.AreEqual(GameInputKey.Escape, XboxMenuInputMapper.ToGameInputKey(Snapshot(B)));
        Assert.AreEqual(GameInputKey.Escape, XboxMenuInputMapper.ToGameInputKey(Snapshot(View)));
    }

    [TestMethod]
    public void ToGameInputKey_MapsDPadDirections()
    {
        Assert.AreEqual(GameInputKey.Up, XboxMenuInputMapper.ToGameInputKey(Snapshot(DPadUp)));
        Assert.AreEqual(GameInputKey.Down, XboxMenuInputMapper.ToGameInputKey(Snapshot(DPadDown)));
        Assert.AreEqual(GameInputKey.Left, XboxMenuInputMapper.ToGameInputKey(Snapshot(DPadLeft)));
        Assert.AreEqual(GameInputKey.Right, XboxMenuInputMapper.ToGameInputKey(Snapshot(DPadRight)));
    }

    [TestMethod]
    public void ToGameInputKey_MapsLeftStickDirectionsAboveNavigationThreshold()
    {
        Assert.AreEqual(GameInputKey.Up, XboxMenuInputMapper.ToGameInputKey(Snapshot(leftThumbstickY: 25000)));
        Assert.AreEqual(GameInputKey.Down, XboxMenuInputMapper.ToGameInputKey(Snapshot(leftThumbstickY: -25000)));
        Assert.AreEqual(GameInputKey.Left, XboxMenuInputMapper.ToGameInputKey(Snapshot(leftThumbstickX: -25000)));
        Assert.AreEqual(GameInputKey.Right, XboxMenuInputMapper.ToGameInputKey(Snapshot(leftThumbstickX: 25000)));
    }

    [TestMethod]
    public void ToGameInputKey_IgnoresSmallStickAndTriggerOnlyInput()
    {
        Assert.AreEqual(GameInputKey.None, XboxMenuInputMapper.ToGameInputKey(Snapshot(leftThumbstickX: 10000)));
        Assert.AreEqual(GameInputKey.None, XboxMenuInputMapper.ToGameInputKey(Snapshot(leftTrigger: 255, rightTrigger: 255)));
    }

    [TestMethod]
    public void ToGameInputKey_ChoosesDominantStickDirection()
    {
        Assert.AreEqual(GameInputKey.Right, XboxMenuInputMapper.ToGameInputKey(
            Snapshot(leftThumbstickX: 30000, leftThumbstickY: 20000)));
    }

    [TestMethod]
    public void ToShortcutGameInputKey_MapsIntroAndSettingsShortcuts()
    {
        Assert.AreEqual(GameInputKey.T, XboxMenuInputMapper.ToShortcutGameInputKey(Snapshot(Y)));
        Assert.AreEqual(GameInputKey.C, XboxMenuInputMapper.ToShortcutGameInputKey(Snapshot(X)));
        Assert.AreEqual(GameInputKey.S, XboxMenuInputMapper.ToShortcutGameInputKey(Snapshot(LeftShoulder)));
        Assert.AreEqual(GameInputKey.G, XboxMenuInputMapper.ToShortcutGameInputKey(Snapshot(RightShoulder)));
        Assert.AreEqual(GameInputKey.None, XboxMenuInputMapper.ToShortcutGameInputKey(Snapshot(A)));
    }

    [TestMethod]
    public void IsPauseTogglePressed_OnlyUsesMenuButton()
    {
        Assert.IsTrue(XboxMenuInputMapper.IsPauseTogglePressed(Snapshot(Menu)));
        Assert.IsFalse(XboxMenuInputMapper.IsPauseTogglePressed(Snapshot(A)));
    }

    [TestMethod]
    public void IsExitToMenuPressed_OnlyUsesViewButton()
    {
        Assert.IsTrue(XboxMenuInputMapper.IsExitToMenuPressed(Snapshot(View)));
        Assert.IsFalse(XboxMenuInputMapper.IsExitToMenuPressed(Snapshot(B)));
    }

    private static XboxControllerSnapshot Snapshot(
        ushort buttons = 0,
        byte leftTrigger = 0,
        byte rightTrigger = 0,
        short leftThumbstickX = 0,
        short leftThumbstickY = 0,
        short rightThumbstickX = 0,
        short rightThumbstickY = 0)
    {
        return new XboxControllerSnapshot(
            buttons,
            leftTrigger,
            rightTrigger,
            leftThumbstickX,
            leftThumbstickY,
            rightThumbstickX,
            rightThumbstickY);
    }
}
