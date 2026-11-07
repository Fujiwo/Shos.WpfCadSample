using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// ビュー変換を、論理座標（Y 上向き）から画面座標（Y 下向き、px）への <see cref="MatrixTransform"/> にする（設計書 DD-4）。
/// 画面座標 = (TranslateX + Scale·x, TranslateY − Scale·y)。
/// </summary>
public sealed class ViewTransformToMatrixConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ViewTransform transform)
            return null;
        var matrix = new MatrixTransform(transform.Scale, 0, 0, -transform.Scale, transform.TranslateX, transform.TranslateY);
        matrix.Freeze();
        return matrix;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
