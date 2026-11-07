using System.Collections.Immutable;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 開いた自由線（通過点を結ぶ自然 3 次スプライン）。
/// </summary>
public sealed record OpenCurveShape(ShapeId Id, ShapeAttributes Attributes, ImmutableArray<Point2> Points)
    : PointListShape(Id, Attributes, Points)
{
    // スプラインを作れない点数（入力の途中）では、折れ線で表す
    public override Outline CreateOutline()
        => Points.Length >= 2 ? new([SplineBezier.Open(Points)]) : PolylineOutline();

    public override ShapeKind Kind => ShapeKind.OpenCurve;

    protected override bool IsClosed => false;
}
