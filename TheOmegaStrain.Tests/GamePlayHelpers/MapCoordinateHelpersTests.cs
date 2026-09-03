using TheOmegaStrain.Common.GamePlayHelpers;
using TheOmegaStrain.Common.CommonSetup;

namespace TheOmegaStrain.Tests.CommonHelpers;

[TestClass]
public class MapCoordinateHelpersTests
{
    [TestMethod]
    public void WorldToTileIndex_ReturnsContainingTile_NotNearestTileCenter()
    {
        int tile = MapCoordinateHelpers.WorldToTileIndex(2.75f * 75f, 75, 10);

        Assert.AreEqual(2, tile);
    }

    [TestMethod]
    public void WorldToTileIndex_WrapsNegativeAndOverflowCoordinates()
    {
        Assert.AreEqual(9, MapCoordinateHelpers.WorldToTileIndex(-1f, 75, 10));
        Assert.AreEqual(0, MapCoordinateHelpers.WorldToTileIndex(10f * 75f, 75, 10));
    }

    [TestMethod]
    public void GetWrappedRelativeIndex_ReturnsPositionInsideWrappedCrop()
    {
        Assert.AreEqual(2, MapCoordinateHelpers.GetWrappedRelativeIndex(1, -1, 10));
        Assert.AreEqual(1, MapCoordinateHelpers.GetWrappedRelativeIndex(0, 9, 10));
    }

    [TestMethod]
    public void BitmapCropOffsets_CenterTheShipInTheCrop()
    {
        // Surface.CenterViewportOnLandingPlatform sets GlobalMapPosition to
        // (shipTile * tileSize) - viewPortCenterOffsetX, using the same offset on both axes.
        // The minimap crop must therefore be centred on the ship, not on GlobalMapPosition.
        int tileSize = MapSetup.tileSize;

        int cropW = MapSetup.bitmapSize * 2;
        int cropH = MapSetup.bitmapSize;

        // Place the ship on a known tile and derive the viewport corner exactly as Surface does.
        int shipTileX = 500;
        int shipTileZ = 400;
        int mapX = (shipTileX * tileSize) - MapSetup.viewPortCenterOffsetX;
        int mapZ = (shipTileZ * tileSize) - MapSetup.viewPortCenterOffsetX;

        int cropX = (mapX - MapSetup.bitmapMapCenterOffsetX) / tileSize;
        int cropZ = (mapZ - MapSetup.bitmapMapCenterOffsetY) / tileSize;

        Assert.AreEqual(cropW / 2, shipTileX - cropX, "Ship must sit at the horizontal centre of the minimap crop.");
        Assert.AreEqual(cropH / 2, shipTileZ - cropZ, "Ship must sit at the vertical centre of the minimap crop.");
    }
}
