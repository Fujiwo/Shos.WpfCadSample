using System.Collections.Immutable;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 多角形（最後の点から最初の点へ直線で閉じる折れ線）。
/// </summary>
public sealed record PolygonShape(ShapeId Id, ShapeAttributes Attributes, ImmutableArray<Point2> Points)
    : PointListShape(Id, Attributes, Points)
{
    public override ShapeKind Kind => ShapeKind.Polygon;

    public override Outline CreateOutline() => PolylineOutline();

    protected override bool IsClosed => true;
}
