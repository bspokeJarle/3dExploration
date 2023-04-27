namespace _3dTesting.Helpers
{
    public class Colors
    {
        public static System.Windows.Media.Color getGrayColorFromNormal(float normal)
        {
            if (normal > 0 && normal <= 0.1) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#151515");
            if (normal > 0.1 && normal <= 0.2) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2a2a2a");
            if (normal > 0.2 && normal <= 0.3) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3f3f3f");
            if (normal > 0.3 && normal <= 0.4) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#545454");
            if (normal > 0.4 && normal <= 0.5) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#696969");
            if (normal > 0.5 && normal <= 0.6) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7e7e7e");
            if (normal > 0.6 && normal <= 0.7) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#939393");
            if (normal > 0.7 && normal <= 0.8) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#a8a8a8");
            if (normal > 0.8 && normal <= 0.9) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#bdbdbd");
            if (normal > 0.9 && normal <= 1.0) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#d3d3d3");
            if (normal > 1.0 && normal <= 1.1) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f3f3f3");
            if (normal <0 ) return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#000000");
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff");
        }
    }
}
