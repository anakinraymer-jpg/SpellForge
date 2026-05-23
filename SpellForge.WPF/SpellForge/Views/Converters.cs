using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpellForge.Views;

/// <summary>True → Visible, False → Collapsed.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type _, object __, CultureInfo ___)
        => value is Visibility.Visible;
}

/// <summary>Non-empty string → Visible, empty/null → Collapsed.</summary>
public class EmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type _, object __, CultureInfo ___)
        => throw new NotSupportedException();
}

/// <summary>int (0-3) → pip string: ○○○ / ●○○ / ●●○ / ●●●.</summary>
public class IntToPipsConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___)
        => value is int n ? n switch
        {
            0 => "○○○",
            1 => "●○○",
            2 => "●●○",
            _ => "●●●",
        } : "○○○";

    public object ConvertBack(object value, Type _, object __, CultureInfo ___)
        => throw new NotSupportedException();
}

/// <summary>Truncate string for small column display (first word before space or paren).</summary>
public class TruncateTextConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___)
    {
        if (value is not string s || string.IsNullOrEmpty(s)) return "";
        // Strip element suffix inside brackets
        int bracket = s.IndexOf('[');
        if (bracket > 0) s = s[..bracket].Trim();
        if (string.IsNullOrEmpty(s)) return "";
        // Find first space after position min(4, length), max 12 chars total
        int startSearch = Math.Min(4, s.Length);
        int sp = s.IndexOf(' ', startSearch);
        return sp > 0 && sp < 12 ? s[..sp] : s.Length > 12 ? s[..12] : s;
    }

    public object ConvertBack(object value, Type _, object __, CultureInfo ___)
        => throw new NotSupportedException();
}
