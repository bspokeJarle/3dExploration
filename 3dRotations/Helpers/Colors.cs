using System;

namespace _3dTesting.Helpers
{
    public class Colors
    {
        //Add some brightness to the colors, temp solution
        const int brightness = 8;
        public static string getShadeOfColorFromNormal(float normal, string color)
        { 
            if (color==null) color = "#000000";
            //Remove the # from the color if it exists
            color = color.Replace("#", "");
            var localNormal = Math.Abs(normal);
            string rs = color[0..2];
            string gs = color[2..4];
            string bs = color[4..6];
            var r = Convert.ToInt16(Convert.ToInt16(rs, 16) * localNormal) + brightness;
            var g = Convert.ToInt16(Convert.ToInt16(gs, 16) * localNormal) + brightness;
            var b = Convert.ToInt16(Convert.ToInt16(bs, 16) * localNormal) + brightness;
            //Make sure the color is within the range of 0-255
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
            var rhex = r.ToString("X").PadLeft(2, '0');
            var ghex = g.ToString("X").PadLeft(2, '0');
            var bhex = b.ToString("X").PadLeft(2, '0');
            var hex = "#" + rhex + ghex + bhex;

            return hex;
        }
    }
}
