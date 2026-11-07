namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 直線の区間。
/// </summary>
public sealed record LineSegment(Point2 End) : OutlineSegment(End)
{
    // 直線の外接矩形は両端で決まる。始点は直前の区間（または図形の始点）が含める
    internal override IEnumerable<Point2> ExtremePoints(Point2 start) => [End];

    // 直線は近似の誤差がないため、許容誤差によらず 1 本の線分になる
    internal override IEnumerable<Segment2> Flatten(Point2 start, double tolerance) => [new(start, End)];
}
