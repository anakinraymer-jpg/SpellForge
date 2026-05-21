using System.Windows.Media;

namespace SpellForge;

public static class ColorHelper
{
    public static Color FromHex(string hex)
        => (Color)ColorConverter.ConvertFromString(hex);

    public static Color Blend(Color c1, Color c2, double t) => Color.FromArgb(
        (byte)(c1.A + (c2.A - c1.A) * t),
        (byte)(c1.R + (c2.R - c1.R) * t),
        (byte)(c1.G + (c2.G - c1.G) * t),
        (byte)(c1.B + (c2.B - c1.B) * t));

    public static string Blend(string hex1, string hex2, double t)
    {
        var c = Blend(FromHex(hex1), FromHex(hex2), t);
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    /// Fade: blend BG color with target at given alpha
    public static Color Fade(Color c, double alpha, Color? bg = null)
        => Blend(bg ?? Color.FromRgb(0x0a, 0x0a, 0x14), c, alpha);

    public static Color Fade(string hex, double alpha)
        => Fade(FromHex(hex), alpha);

    public static SolidColorBrush BrushFrom(Color c)              => new(c);
    public static SolidColorBrush BrushFrom(string hex)            => new(FromHex(hex));
    public static SolidColorBrush BrushFrom(string hex, double alpha) => new(Fade(hex, alpha));
}
