namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 通過点列を、すべての通過点を通る 3 次スプライン曲線（3 次ベジェの区間列）に変換する（仕様 3.5、設計書 DD-7、5.2）。
/// </summary>
/// <remarks>
/// 各通過点の接線 m を三重対角の連立方程式で求め、区間 P(i) → P(i+1) の制御点を
/// C1 = P(i) + m(i)/3、C2 = P(i+1) − m(i+1)/3 とする。
/// 考え方の出典：Shos.BezierSamples の BezierWpf/Geometry.cs
/// （https://github.com/Fujiwo/Shos.BezierSamples 、MIT License）。コードは複製せず、同じ考え方で実装した。
/// </remarks>
public static class SplineBezier
{
    /// <summary>
    /// 開いた曲線（自然 3 次スプライン：両端の 2 階微分が 0）。通過点は 2 つ以上。2 点なら直線になる。
    /// </summary>
    public static OutlineFigure Open(IReadOnlyList<Point2> points)
    {
        if (points.Count < 2)
            throw new ArgumentException("An open curve needs at least two points.", nameof(points));

        return Figure(points, OpenTangents(points), isClosed: false);
    }

    /// <summary>
    /// 閉じた曲線（周期スプライン：最後の通過点から最初の通過点へ滑らかにつながる）。通過点は 3 つ以上。
    /// </summary>
    public static OutlineFigure Closed(IReadOnlyList<Point2> points)
    {
        if (points.Count < 3)
            throw new ArgumentException("A closed curve needs at least three points.", nameof(points));

        return Figure(points, ClosedTangents(points), isClosed: true);
    }

    // 2·m0 + m1 = 3·(P1 − P0)
    // m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1))
    // m(n−2) + 2·m(n−1) = 3·(P(n−1) − P(n−2))
    static Vector2[] OpenTangents(IReadOnlyList<Point2> points)
    {
        var last = points.Count - 1;
        var diagonal = Enumerable.Range(0, points.Count).Select(index => index == 0 || index == last ? 2.0 : 4.0).ToArray();
        var rhs = Enumerable.Range(0, points.Count)
                            .Select(index => (points[Math.Min(index + 1, last)] - points[Math.Max(index - 1, 0)]) * 3)
                            .ToArray();
        return Tridiagonal.Solve(diagonal, rhs);
    }

    // m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1))（添字は n を法とする）
    static Vector2[] ClosedTangents(IReadOnlyList<Point2> points)
    {
        var count = points.Count;
        var rhs = Enumerable.Range(0, count)
                            .Select(index => (points[(index + 1) % count] - points[(index + count - 1) % count]) * 3)
                            .ToArray();
        return Tridiagonal.SolveCyclic(count, diagonalValue: 4, rhs);
    }

    static OutlineFigure Figure(IReadOnlyList<Point2> points, Vector2[] tangents, bool isClosed)
    {
        var segmentCount = isClosed ? points.Count : points.Count - 1;
        var segments = Enumerable.Range(0, segmentCount).Select(index => {
            var next = (index + 1) % points.Count;
            return (OutlineSegment)new CubicBezierSegment(points[index] + tangents[index] / 3,
                                                          points[next] - tangents[next] / 3,
                                                          points[next]);
        }).ToList();
        return new(points[0], segments, isClosed);
    }

    /// <summary>
    /// 非対角の係数がすべて 1 の三重対角の連立方程式。右辺の X と Y を同時に解く。
    /// </summary>
    static class Tridiagonal
    {
        // トーマス法（前進消去と後退代入）。O(n)
        public static Vector2[] Solve(double[] diagonal, Vector2[] rhs)
        {
            var count = rhs.Length;
            var modifiedDiagonal = new double[count];
            var modifiedRhs = new Vector2[count];
            modifiedDiagonal[0] = diagonal[0];
            modifiedRhs[0] = rhs[0];
            for (var index = 1; index < count; index++) {
                var factor = 1 / modifiedDiagonal[index - 1];
                modifiedDiagonal[index] = diagonal[index] - factor;
                modifiedRhs[index] = rhs[index] - modifiedRhs[index - 1] * factor;
            }

            var solution = new Vector2[count];
            solution[count - 1] = modifiedRhs[count - 1] / modifiedDiagonal[count - 1];
            for (var index = count - 2; index >= 0; index--)
                solution[index] = (modifiedRhs[index] - solution[index + 1]) / modifiedDiagonal[index];
            return solution;
        }

        // 右上と左下の角にも 1 がある巡回三重対角の連立方程式を、Sherman-Morrison の公式で解く。
        // A = B + u·vᵀ（u = (γ, 0, …, 0, 1)、v = (1, 0, …, 0, 1/γ)）とし、三重対角の B について 2 回解く。
        public static Vector2[] SolveCyclic(int count, double diagonalValue, Vector2[] rhs)
        {
            var gamma = -diagonalValue;
            var diagonal = Enumerable.Repeat(diagonalValue, count).ToArray();
            diagonal[0] -= gamma;
            diagonal[count - 1] -= 1 / gamma;

            var y = Solve(diagonal, rhs);
            // u は実数の列のため、X 成分だけのベクトルとして同じ解法で解き、X 成分を取り出す
            var u = Enumerable.Range(0, count).Select(index => index == 0 ? gamma : index == count - 1 ? 1.0 : 0.0);
            var z = Solve(diagonal, [.. u.Select(value => new Vector2(value, 0))]).Select(value => value.X).ToArray();

            var factor = (y[0] + y[count - 1] / gamma) / (1 + z[0] + z[count - 1] / gamma);
            return [.. y.Select((value, index) => value - factor * z[index])];
        }
    }
}
