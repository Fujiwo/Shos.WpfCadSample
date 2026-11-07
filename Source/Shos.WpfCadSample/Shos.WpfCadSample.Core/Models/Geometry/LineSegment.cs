namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 直線の区間。
/// </summary>
public sealed record LineSegment(Point2 End) : OutlineSegment(End)
{
    internal override IEnumerable<Point2> ExtremePoints(Point2 start) => [End];

    internal override IEnumerable<Segment2> Flatten(Point2 start, double tolerance) => [new(start, End)];
}
