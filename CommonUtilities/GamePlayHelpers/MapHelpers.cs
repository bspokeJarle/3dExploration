using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommonUtilities.GamePlayHelpers
{
    public static class MapHelpers
    {
        public static void UpdateTilePixel(WriteableBitmap bitmap, int tileX, int tileZ, Color color)
        {
            // 1 pixel (BGRA)
            byte[] px = { color.B, color.G, color.R, 255 };

            // NB: Int32Rect(x,y,w,h) -> x=tileX, y=tileZ
            bitmap.WritePixels(new Int32Rect(tileX, tileZ, 1, 1), px, 4, 0);
        }
    }
}
