using System.Diagnostics;
using Shos.WpfCadSample.Models.Geometry;
using Xunit.Abstractions;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-SPL：<see cref="SplineBezier"/>（テスト計画書 4.2）。
/// 各ベジェ区間を (P0, C1, C2, P3) で表し、区間ごとの媒介変数 t ∈ [0, 1] で微分を比べる。
/// </summary>
public class SplineBezierTests(ITestOutputHelper output)
{
    const double tolerance = 1e-9;
    const double solvedTolerance = 1e-7;    // 連立方程式の解を含む値

    // 1 つのベジェ区間と、その両端（t = 0、t = 1）での 1 階・2 階微分
    readonly record struct Bezier(Point2 P0, Point2 C1, Point2 C2, Point2 P3)
    {
        public Vector2 FirstDerivativeAtStart => (C1 - P0) * 3;
        public Vector2 FirstDerivativeAtEnd => (P3 - C2) * 3;
        public Vector2 SecondDerivativeAtStart => ((C2 - C1) - (C1 - P0)) * 6;
        public Vector2 SecondDerivativeAtEnd => ((P3 - C2) - (C2 - C1)) * 6;
    }

    static Point2[] PassedPoints(OutlineFigure figure) => [figure.Start, .. figure.Segments.Select(segment => segment.End)];

    static Bezier[] Beziers(OutlineFigure figure)
    {
        var start = figure.Start;
        var beziers = new List<Bezier>();
        foreach (var segment in figure.Segments) {
            var bezier = Assert.IsType<CubicBezierSegment>(segment);
            beziers.Add(new(start, bezier.Control1, bezier.Control2, bezier.End));
            start = bezier.End;
        }
        return [.. beziers];
    }

    // 決定的な（種を固定した）通過点。隣り合う点は一致しない（X が点ごとに 20 以上増える）
    static Point2[] SamplePoints(int count, int seed = 1)
    {
        var random = new Random(seed);
        return [.. Enumerable.Range(0, count).Select(index => new Point2(index * 80 + random.NextDouble() * 60, random.NextDouble() * 700))];
    }

    public static TheoryData<int> OpenCounts => [.. Enumerable.Range(2, 11)];   // 2〜12
    public static TheoryData<int> ClosedCounts => [.. Enumerable.Range(3, 10)]; // 3〜12

    static void AssertEqual(Vector2 expected, Vector2 actual, double precision)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
    }

    static void AssertEqual(Point2 expected, Point2 actual, double precision)
        => AssertEqual(expected - new Point2(0, 0), actual - new Point2(0, 0), precision);

    // T-SPL-01
    [Theory]
    [MemberData(nameof(OpenCounts))]
    public void Open_PassesThroughEveryPointInOrder(int count)
    {
        var points = SamplePoints(count);

        var figure = SplineBezier.Open(points);

        Assert.False(figure.IsClosed);
        Assert.Equal(count - 1, figure.Segments.Count);
        Assert.Equal(points, PassedPoints(figure));
    }

    // T-SPL-02
    [Theory]
    [MemberData(nameof(OpenCounts))]
    public void Open_TangentIsContinuousAtInnerPoints(int count)
    {
        var beziers = Beziers(SplineBezier.Open(SamplePoints(count)));

        foreach (var (before, after) in beziers.Zip(beziers.Skip(1)))
            AssertEqual(before.FirstDerivativeAtEnd, after.FirstDerivativeAtStart, solvedTolerance);
    }

    // T-SPL-03
    [Theory]
    [MemberData(nameof(OpenCounts))]
    public void Open_SecondDerivativeIsContinuousAndZeroAtBothEnds(int count)
    {
        var beziers = Beziers(SplineBezier.Open(SamplePoints(count)));

        foreach (var (before, after) in beziers.Zip(beziers.Skip(1)))
            AssertEqual(before.SecondDerivativeAtEnd, after.SecondDerivativeAtStart, solvedTolerance);
        AssertEqual(new Vector2(0, 0), beziers[0].SecondDerivativeAtStart, solvedTolerance);
        AssertEqual(new Vector2(0, 0), beziers[^1].SecondDerivativeAtEnd, solvedTolerance);
    }

    // T-SPL-04
    [Theory]
    [MemberData(nameof(ClosedCounts))]
    public void Closed_PassesThroughEveryPointAndReturnsToStart(int count)
    {
        var points = SamplePoints(count);

        var figure = SplineBezier.Closed(points);

        Assert.True(figure.IsClosed);
        Assert.Equal(count, figure.Segments.Count);
        Assert.Equal([.. points, points[0]], PassedPoints(figure));
    }

    // T-SPL-04、T-SPL-05（継ぎ目を含む、すべての通過点で滑らか）
    [Theory]
    [MemberData(nameof(ClosedCounts))]
    public void Closed_TangentAndSecondDerivativeAreContinuousIncludingTheSeam(int count)
    {
        var beziers = Beziers(SplineBezier.Closed(SamplePoints(count)));

        foreach (var (before, after) in beziers.Zip([.. beziers.Skip(1), beziers[0]])) {
            AssertEqual(before.FirstDerivativeAtEnd, after.FirstDerivativeAtStart, solvedTolerance);
            AssertEqual(before.SecondDerivativeAtEnd, after.SecondDerivativeAtStart, solvedTolerance);
        }
    }

    // T-SPL-06
    [Fact]
    public void Open_WithTwoPoints_IsStraightLineWithControlPointsAtThirds()
    {
        // 2 点では接線が線分の向きで一定になり、制御点は線分を 3 等分する点になる
        Point2 start = new(10, 20), end = new(40, 80);

        var bezier = Beziers(SplineBezier.Open([start, end])).Single();

        AssertEqual(new Point2(20, 40), bezier.C1, tolerance);
        AssertEqual(new Point2(30, 60), bezier.C2, tolerance);
    }

    // T-SPL-07
    [Fact]
    public void Open_WithCollinearPoints_StaysOnTheLine()
    {
        static double LineY(double x) => 2 * x + 1;
        Point2[] points = [.. new[] { 0.0, 1.0, 3.0, 4.0, 10.0 }.Select(x => new Point2(x, LineY(x)))];

        var beziers = Beziers(SplineBezier.Open(points));

        // 制御点が直線上にあれば、曲線（制御点の凸包の内側）も直線上にある
        foreach (var control in beziers.SelectMany(bezier => new[] { bezier.C1, bezier.C2 }))
            Assert.Equal(LineY(control.X), control.Y, solvedTolerance);
    }

    // T-SPL-08
    [Fact]
    public void Open_TangentsMatchNaiveGaussianElimination()
    {
        Point2[] points = [new(0, 0), new(100, 250), new(300, 120), new(420, 400)];
        // 通過点 P(i) での接線 D(i) の連立方程式（自然スプライン）：
        // 両端は 2D(0) + D(1) = 3(P(1) − P(0)) など、内側は D(i−1) + 4D(i) + D(i+1) = 3(P(i+1) − P(i−1))
        double[,] matrix = {
            { 2, 1, 0, 0 },
            { 1, 4, 1, 0 },
            { 0, 1, 4, 1 },
            { 0, 0, 1, 2 }
        };
        Vector2[] rhs = [
            (points[1] - points[0]) * 3,
            (points[2] - points[0]) * 3,
            (points[3] - points[1]) * 3,
            (points[3] - points[2]) * 3
        ];

        AssertTangentsMatch(GaussianElimination.Solve(matrix, rhs), Beziers(SplineBezier.Open(points)));
    }

    // T-SPL-08（閉じた曲線も、巡回する連立方程式を素朴に解いた接線と比べる）
    [Fact]
    public void Closed_TangentsMatchNaiveGaussianElimination()
    {
        Point2[] points = [new(0, 0), new(100, 250), new(300, 120), new(420, 400)];
        // 周期スプライン：すべての点で D(i−1) + 4D(i) + D(i+1) = 3(P(i+1) − P(i−1))（添字は 4 で巡回する）
        double[,] matrix = {
            { 4, 1, 0, 1 },
            { 1, 4, 1, 0 },
            { 0, 1, 4, 1 },
            { 1, 0, 1, 4 }
        };
        Vector2[] rhs = [.. Enumerable.Range(0, 4).Select(index => (points[(index + 1) % 4] - points[(index + 3) % 4]) * 3)];

        AssertTangentsMatch(GaussianElimination.Solve(matrix, rhs), Beziers(SplineBezier.Closed(points)));
    }

    // 区間 i の始点の接線は D(i)、終点の接線は D(i+1)（閉じた曲線では、最後の区間の終点が D(0)）
    static void AssertTangentsMatch(Vector2[] expectedTangents, Bezier[] beziers)
    {
        foreach (var (bezier, index) in beziers.Select((bezier, index) => (bezier, index))) {
            AssertEqual(expectedTangents[index], bezier.FirstDerivativeAtStart, solvedTolerance);
            AssertEqual(expectedTangents[(index + 1) % expectedTangents.Length], bezier.FirstDerivativeAtEnd, solvedTolerance);
        }
    }

    // T-SPL-09
    [Fact]
    public void Open_IsInvariantUnderTranslationAndScaling() => AssertInvariant(SplineBezier.Open);

    // T-SPL-09
    [Fact]
    public void Closed_IsInvariantUnderTranslationAndScaling() => AssertInvariant(SplineBezier.Closed);

    static void AssertInvariant(Func<IReadOnlyList<Point2>, OutlineFigure> convert)
    {
        static Point2 Transform(Point2 point) => new(point.X * 2.5 + 100, point.Y * 2.5 - 50);
        var points = SamplePoints(7);

        var original = Beziers(convert(points));
        var transformed = Beziers(convert([.. points.Select(Transform)]));

        foreach (var (expected, actual) in original.Zip(transformed)) {
            AssertEqual(Transform(expected.C1), actual.C1, solvedTolerance);
            AssertEqual(Transform(expected.C2), actual.C2, solvedTolerance);
        }
    }

    // T-SPL-10
    [Fact]
    public void TenThousandPoints_ConvertToFiniteValuesInLinearTime()
    {
        var points = SamplePoints(10_000);
        var stopwatch = Stopwatch.StartNew();

        var open = SplineBezier.Open(points);
        var closed = SplineBezier.Closed(points);

        stopwatch.Stop();
        output.WriteLine($"10,000 points (open + closed): {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
        Assert.All(Beziers(open).Concat(Beziers(closed)), bezier => Assert.True(bezier.C1.IsFinite && bezier.C2.IsFinite));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10)); // 計算量の誤り（桁違いの遅さ）だけを検知する緩い上限
    }

    [Fact]
    public void Open_WithFewerThanTwoPoints_IsRejected()
        => Assert.Throws<ArgumentException>(() => SplineBezier.Open([new(0, 0)]));

    [Fact]
    public void Closed_WithFewerThanThreePoints_IsRejected()
        => Assert.Throws<ArgumentException>(() => SplineBezier.Closed([new(0, 0), new(1, 1)]));

    /// <summary>
    /// T-SPL-08 の参照値：ピボット選択付きのガウス消去で、係数行列の連立方程式を X・Y ごとに素朴に解く。
    /// </summary>
    static class GaussianElimination
    {
        public static Vector2[] Solve(double[,] matrix, Vector2[] rhs)
        {
            var xs = Solve(matrix, [.. rhs.Select(value => value.X)]);
            var ys = Solve(matrix, [.. rhs.Select(value => value.Y)]);
            return [.. xs.Zip(ys, (x, y) => new Vector2(x, y))];
        }

        static double[] Solve(double[,] matrix, double[] rhs)
        {
            var size = rhs.Length;
            var a = (double[,])matrix.Clone();
            var b = (double[])rhs.Clone();
            for (var pivot = 0; pivot < size; pivot++) {
                var best = Enumerable.Range(pivot, size - pivot).MaxBy(row => Math.Abs(a[row, pivot]));
                SwapRows(a, b, pivot, best);
                for (var row = pivot + 1; row < size; row++)
                    Eliminate(a, b, pivot, row);
            }
            var solution = new double[size];
            for (var row = size - 1; row >= 0; row--)
                solution[row] = (b[row] - Enumerable.Range(row + 1, size - row - 1).Sum(column => a[row, column] * solution[column])) / a[row, row];
            return solution;
        }

        static void SwapRows(double[,] a, double[] b, int first, int second)
        {
            for (var column = 0; column < b.Length; column++)
                (a[first, column], a[second, column]) = (a[second, column], a[first, column]);
            (b[first], b[second]) = (b[second], b[first]);
        }

        static void Eliminate(double[,] a, double[] b, int pivot, int row)
        {
            var factor = a[row, pivot] / a[pivot, pivot];
            for (var column = pivot; column < b.Length; column++)
                a[row, column] -= factor * a[pivot, column];
            b[row] -= factor * b[pivot];
        }
    }
}
