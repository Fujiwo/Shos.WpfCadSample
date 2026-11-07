using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// 線種を <c>StrokeDashArray</c> にする。値は線の太さに対する倍率で、EMF・画像の出力と同じ（設計書 DA-5）。実線は空。
/// </summary>
public sealed class LineStyleToDashesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LineStyle style)
            return null;
        var dashes = new DoubleCollection(style.DashPattern());
        dashes.Freeze();
        return dashes;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
