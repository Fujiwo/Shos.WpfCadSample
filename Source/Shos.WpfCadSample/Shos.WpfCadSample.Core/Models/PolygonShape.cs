using System.Collections.Immutable;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

public sealed record PolygonShape(ShapeId Id, ShapeAttributes Attributes, ImmutableArray<Point2> Points)
    : PointListShape(Id, Attributes, Points)
{
    public override ShapeKind Kind => ShapeKind.Polygon;

    public override Outline CreateOutline() => PolylineOutline();

    protected override bool IsClosed => true;
}
