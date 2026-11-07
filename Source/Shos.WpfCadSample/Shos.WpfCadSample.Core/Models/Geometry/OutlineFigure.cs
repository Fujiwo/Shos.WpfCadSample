namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 始点から区間をつないだ 1 本の線。<see cref="IsClosed"/> なら、最後の区間の終点から始点へ閉じる。
/// </summary>
public sealed record OutlineFigure(Point2 Start, IReadOnlyList<OutlineSegment> Segments, bool IsClosed)
{
    // 90° の円弧を近似する 3 次ベジェの制御点の、半径に対する長さ 4/3·(√2 − 1)。半径の誤差は最大約 0.027%（設計書 DA-3）
    static readonly double circleControlRatio = 4.0 / 3.0 * (Math.Sqrt(2) - 1);

    /// <summary>
    /// 円を、4 本の 3 次ベジェ（各 90°）で表す。始点は中心の右（0°）で、反時計回り。
    /// </summary>
    public static OutlineFigure Circle(Point2 center, double radius)
    {
        Vector2[] axes = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];
        var segments = axes.Select((from, index) => {
            var to = axes[(index + 1) % axes.Length];
            return (OutlineSegment)new CubicBezierSegment(center + (from + to * circleControlRatio) * radius,
                                                          center + (to + from * circleControlRatio) * radius,
                                                          center + to * radius);
        }).ToList();
        return new(center + axes[0] * radius, segments, IsClosed: true);
    }

    internal IEnumerable<Point2> ExtremePoints()
    {
        var points = new List<Point2> { Start };
        foreach (var (segment, start) in SegmentsWithStart())
            points.AddRange(segment.ExtremePoints(start));
        return points;
    }

    internal IEnumerable<Segment2> Flatten(double tolerance)
    {
        foreach (var (segment, start) in SegmentsWithStart())
            foreach (var line in segment.Flatten(start, tolerance))
                yield return line;

        var end = Segments.Count == 0 ? Start : Segments[^1].End;
        if (IsClosed && end != Start)
            yield return new(end, Start);
    }

    IEnumerable<(OutlineSegment Segment, Point2 Start)> SegmentsWithStart()
        => Segments.Select((segment, index) => (segment, index == 0 ? Start : Segments[index - 1].End));
}
