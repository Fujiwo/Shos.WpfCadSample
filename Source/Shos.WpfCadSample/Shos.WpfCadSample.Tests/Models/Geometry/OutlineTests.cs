using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-OUT：<see cref="Outline"/>（テスト計画書 4.3）
/// </summary>
public class OutlineTests
{
    const double tolerance = 1e-9;

    static Outline Single(OutlineFigure figure) => new([figure]);

    static OutlineFigure Polyline(bool isClosed, params Point2[] points)
        => new(points[0], [.. points.Skip(1).Select(point => new LineSegment(point))], isClosed);

    // 上に膨らむ曲線：t = 0.5 で Y が最大の 75 になり、端点より上へはみ出す
    static readonly OutlineFigure arch = new(new(0, 0), [new CubicBezierSegment(new(0, 100), new(100, 100), new(100, 0))], IsClosed: false);

    // 曲線上の点（テスト側の独立した計算。ベルンシュタイン多項式）
    static Point2 PointOnBezier(Point2 p0, CubicBezierSegment bezier, double t)
    {
        var s = 1 - t;
        double Coordinate(double v0, double v1, double v2, double v3) => s * s * s * v0 + 3 * s * s * t * v1 + 3 * s * t * t * v2 + t * t * t * v3;
        return new(Coordinate(p0.X, bezier.Control1.X, bezier.Control2.X, bezier.End.X),
                   Coordinate(p0.Y, bezier.Control1.Y, bezier.Control2.Y, bezier.End.Y));
    }

    static IEnumerable<Point2> DenseSamples(OutlineFigure figure, int perSegment = 2_000)
    {
        var start = figure.Start;
        foreach (var bezier in figure.Segments.Cast<CubicBezierSegment>()) {
            var p0 = start;
            foreach (var step in Enumerable.Range(0, perSegment + 1))
                yield return PointOnBezier(p0, bezier, (double)step / perSegment);
            start = bezier.End;
        }
    }

    // T-OUT-01
    [Fact]
    public void Bounds_OfLineSegment_SpansItsEndpoints()
        => Assert.Equal(new Rect2(-5, 2, 10, 8), Single(Polyline(false, new(10, 2), new(-5, 8))).Bounds());

    // T-OUT-01
    [Fact]
    public void Bounds_OfPolyline_SpansAllVertices()
        => Assert.Equal(new Rect2(0, -3, 7, 9), Single(Polyline(true, new(0, 0), new(7, 9), new(4, -3))).Bounds());

    // T-OUT-01
    [Fact]
    public void Bounds_OfCurve_IncludesBulgeBeyondEndpoints()
    {
        var bounds = Single(arch).Bounds();

        Assert.Equal(0.0, bounds.Left, tolerance);
        Assert.Equal(0.0, bounds.Bottom, tolerance);
        Assert.Equal(100.0, bounds.Right, tolerance);
        Assert.Equal(75.0, bounds.Top, tolerance);
    }

    // T-OUT-01
    [Fact]
    public void Bounds_OfCurve_MatchesExtremesOfDenseSamples()
    {
        var figure = SplineBezier.Closed([new(0, 0), new(300, -40), new(260, 210), new(-30, 180)]);

        var bounds = Single(figure).Bounds();
        var sampled = Rect2.Bounding(DenseSamples(figure));

        // 標本は曲線の内側にあるため、外接矩形は標本を含み、標本の極値との差は標本の間隔による誤差だけになる
        Assert.All(DenseSamples(figure), point => Assert.True(bounds.Inflate(tolerance).Contains(point)));
        Assert.Equal(sampled.Left, bounds.Left, 1e-3);
        Assert.Equal(sampled.Bottom, bounds.Bottom, 1e-3);
        Assert.Equal(sampled.Right, bounds.Right, 1e-3);
        Assert.Equal(sampled.Top, bounds.Top, 1e-3);
    }

    // T-OUT-01
    [Fact]
    public void Bounds_OfSeveralFigures_IsUnionOfTheirBounds()
    {
        var outline = new Outline([Polyline(false, new(0, 0), new(10, 10)), Polyline(false, new(20, -5), new(30, 0))]);

        Assert.Equal(new Rect2(0, -5, 30, 10), outline.Bounds());
    }

    // T-OUT-02
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.1)]
    [InlineData(0.001)]
    public void Flatten_StaysWithinToleranceOfTheCurve(double flatnessTolerance)
    {
        var figure = SplineBezier.Open([new(0, 0), new(120, 300), new(250, -80), new(400, 150), new(410, 160)]);

        var segments = Single(figure).Flatten(flatnessTolerance).ToList();

        var maxDeviation = DenseSamples(figure).Max(point => segments.Min(segment => segment.DistanceTo(point)));
        Assert.True(maxDeviation <= flatnessTolerance, $"max deviation {maxDeviation} > {flatnessTolerance}");
    }

    // T-OUT-02
    [Fact]
    public void Flatten_ProducesConnectedPolylineFromStartToEnd()
    {
        var segments = Single(arch).Flatten(0.01).ToList();

        Assert.Equal(arch.Start, segments[0].Start);
        Assert.Equal(arch.Segments[^1].End, segments[^1].End);
        Assert.All(segments.Zip(segments.Skip(1)), pair => Assert.Equal(pair.First.End, pair.Second.Start));
    }

    [Fact]
    public void Flatten_OfLines_KeepsThemAsIsAndClosesClosedFigure()
    {
        var segments = Single(Polyline(true, new(0, 0), new(10, 0), new(10, 10))).Flatten(0.01);

        Assert.Equal([new(new(0, 0), new(10, 0)), new(new(10, 0), new(10, 10)), new Segment2(new(10, 10), new(0, 0))], segments);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Flatten_WithNonPositiveOrNonFiniteTolerance_IsRejected(double flatnessTolerance)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Single(arch).Flatten(flatnessTolerance).ToList());

    // T-OUT-03
    [Theory]
    [InlineData(5.0, 3.0, 3.0)]     // 線分の途中への垂線
    [InlineData(-3.0, 4.0, 5.0)]    // 始点の外側：始点までの距離
    [InlineData(13.0, -4.0, 5.0)]   // 終点の外側：終点までの距離
    [InlineData(7.0, 0.0, 0.0)]     // 線上
    public void DistanceTo_LineSegment_IsShortestDistance(double x, double y, double expected)
        => Assert.Equal(expected, Single(Polyline(false, new(0, 0), new(10, 0))).DistanceTo(new(x, y), 0.01), tolerance);

    // T-OUT-03
    [Fact]
    public void DistanceTo_ClosedFigure_IncludesClosingEdge()
    {
        Point2[] square = [new(0, 0), new(10, 0), new(10, 10), new(0, 10)];
        var nearClosingEdge = new Point2(-1, 5);    // 閉じる辺 (0, 10) → (0, 0) の 1 だけ外側

        Assert.Equal(1.0, Single(Polyline(true, square)).DistanceTo(nearClosingEdge, 0.01), tolerance);
        Assert.Equal(Math.Sqrt(1 + 25), Single(Polyline(false, square)).DistanceTo(nearClosingEdge, 0.01), tolerance);
    }

    // T-OUT-03：曲線の頂点 (50, 75) の真上 5 の点。制御点までではなく、曲線までを測る
    [Fact]
    public void DistanceTo_Curve_IsMeasuredToTheCurve()
        => Assert.Equal(5.0, Single(arch).DistanceTo(new(50, 80), 1e-4), 1e-3);

    // T-OUT-04
    [Theory]
    [InlineData(0.0, 0.0, 1.0)]
    [InlineData(250.0, -40.0, 123.4)]
    [InlineData(-1000.0, 500.0, 0.01)]
    public void Circle_IsClosedFourBeziersWithinThreeHundredthsPercentOfRadius(double centerX, double centerY, double radius)
    {
        var center = new Point2(centerX, centerY);

        var circle = OutlineFigure.Circle(center, radius);

        Assert.True(circle.IsClosed);
        Assert.Equal(4, circle.Segments.Count);
        Assert.All(circle.Segments, segment => Assert.IsType<CubicBezierSegment>(segment));
        Assert.Equal(circle.Start, circle.Segments[^1].End);
        // 4 本の 3 次ベジェによる円の近似は、半径の誤差が理論上約 0.027% のため、0.03% を上限にする
        Assert.All(DenseSamples(circle, 500), point => Assert.Equal(radius, point.DistanceTo(center), radius * 0.0003));
    }
}
