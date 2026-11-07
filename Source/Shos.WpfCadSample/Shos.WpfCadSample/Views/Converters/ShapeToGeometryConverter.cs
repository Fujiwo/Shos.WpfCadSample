using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// 図形の <see cref="Shape.CreateOutline"/>（直線と 3 次ベジェ）を、外接矩形の左下を原点とする相対座標の
/// <see cref="StreamGeometry"/> にする（設計書 5.7）。EMF・画像の出力と同じ <see cref="Outline"/> から描く（設計書 DD-6）。
/// </summary>
/// <remarks>
/// 図形の置き場所は、<c>Canvas.Left</c> / <c>Canvas.Top</c> に外接矩形の <c>Left</c> / <c>Bottom</c> をバインドして決める。
/// 座標は論理座標（Y 上向き）のままで、Y の反転は層の変換（<see cref="ViewTransformToMatrixConverter"/>）が行う。
/// </remarks>
public sealed class ShapeToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Shape shape ? CreateGeometry(shape.CreateOutline()) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    static StreamGeometry CreateGeometry(Outline outline)
    {
        var bounds = outline.Bounds();
        Point ToRelative(Point2 point) => new(point.X - bounds.Left, point.Y - bounds.Bottom);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open()) {
            foreach (var figure in outline.Figures) {
                // 図形は塗りつぶさない（仕様 A-17）
                context.BeginFigure(ToRelative(figure.Start), isFilled: false, isClosed: figure.IsClosed);
                foreach (var segment in figure.Segments) {
                    switch (segment) {
                        case CubicBezierSegment bezier:
                            context.BezierTo(ToRelative(bezier.Control1), ToRelative(bezier.Control2), ToRelative(bezier.End), isStroked: true, isSmoothJoin: false);
                            break;
                        default:
                            context.LineTo(ToRelative(segment.End), isStroked: true, isSmoothJoin: false);
                            break;
                    }
                }
            }
        }
        // 凍結して変更の追跡を省く（図形は不変のため、ジオメトリも変わらない）
        geometry.Freeze();
        return geometry;
    }
}
