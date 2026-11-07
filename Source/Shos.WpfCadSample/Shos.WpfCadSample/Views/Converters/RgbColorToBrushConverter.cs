using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// 図形の色を、線のブラシにする。図形の色は属性で決まり、テーマには左右されない（仕様 4.0）。
/// </summary>
public sealed class RgbColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not RgbColor color)
            return null;
        var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
