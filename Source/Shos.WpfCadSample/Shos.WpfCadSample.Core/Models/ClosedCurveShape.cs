using System.Collections.Immutable;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 閉じた自由線（通過点を結ぶ周期 3 次スプライン）。
/// </summary>
public sealed record ClosedCurveShape(ShapeId Id, ShapeAttributes Attributes, ImmutableArray<Point2> Points)
    : PointListShape(Id, Attributes, Points)
{
    public override ShapeKind Kind => ShapeKind.ClosedCurve;

    // スプラインを作れない点数（入力の途中や、入力時の 2 点目の判定の候補）では、折れ線で表す
    public override Outline CreateOutline()
        => Points.Length >= 3 ? new([SplineBezier.Closed(Points)]) : PolylineOutline();

    protected override bool IsClosed => true;
}
