using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// 論理座標の矩形を、WPF の <see cref="Rect"/>（同じ論理座標）にする。論理領域の枠に使う。
/// </summary>
public sealed class Rect2ToRectConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        // WPF の Rect は、Y が最小の角（論理座標では下端）を起点にとる。Y の反転は、掛ける変換が行う
        => value is Rect2 rect ? new Rect(rect.Left, rect.Bottom, rect.Width, rect.Height) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
