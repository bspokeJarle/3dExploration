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
    public void BitmapCropOffsets_CenterTheMapPositionInTheCrop()
    {
        Assert.AreEqual(MapSetup.bitmapSize * MapSetup.tileSize, MapSetup.bitmapMapCenterOffsetX);
        Assert.AreEqual((MapSetup.bitmapSize / 2) * MapSetup.tileSize, MapSetup.bitmapMapCenterOffsetY);
    }
}
