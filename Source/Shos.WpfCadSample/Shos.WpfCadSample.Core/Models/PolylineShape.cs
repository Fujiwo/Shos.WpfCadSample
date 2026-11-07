using System.Collections.Immutable;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 折れ線（点を順に直線で結ぶ。閉じない）。
/// </summary>
public sealed record PolylineShape(ShapeId Id, ShapeAttributes Attributes, ImmutableArray<Point2> Points)
    : PointListShape(Id, Attributes, Points)
{
    public override ShapeKind Kind => ShapeKind.Polyline;

    public override Outline CreateOutline() => PolylineOutline();

    protected override bool IsClosed => false;
}
