namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 3 次ベジェ曲線の区間。
/// </summary>
public sealed record CubicBezierSegment(Point2 Control1, Point2 Control2, Point2 End) : OutlineSegment(End)
{
    // 分割の段数の上限。許容誤差が座標の浮動小数点の精度を下回っても、分割が終わるようにする。
    // 1 段ごとに誤差は約 1/4 になり、16 段（1 区間あたり最大 65,536 本）で打ち切っても、画面上の誤差は 0.001 px 未満になる
    const int maxSubdivisionDepth = 16;

    /// <summary>
    /// 端点と、X・Y それぞれが極値をとる曲線上の点（1 階微分が 0 になる点）。
    /// </summary>
    internal override IEnumerable<Point2> ExtremePoints(Point2 start)
    {
        var parameters = ExtremeParameters(start.X, Control1.X, Control2.X, End.X)
                         .Concat(ExtremeParameters(start.Y, Control1.Y, Control2.Y, End.Y));
        return [.. parameters.Select(t => PointAt(start, t)), End];
    }

    // B'(t) / 3 = a·t² + b·t + c（d0, d1, d2 は隣り合う制御点の差）の、0 < t < 1 の根
    static IEnumerable<double> ExtremeParameters(double p0, double p1, double p2, double p3)
    {
        double d0 = p1 - p0, d1 = p2 - p1, d2 = p3 - p2;
        var (a, b, c) = (d0 - 2 * d1 + d2, 2 * (d1 - d0), d0);
        return QuadraticRoots(a, b, c).Where(t => t is > 0 and < 1);
    }

    // 桁落ちを避ける解の公式（q = −(b + sign(b)·√D) / 2、根は c / q と q / a）。a = 0（1 次式）も扱える
    static IEnumerable<double> QuadraticRoots(double a, double b, double c)
    {
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
            return [];

        var q = -(b + Math.CopySign(Math.Sqrt(discriminant), b)) / 2;
        return q == 0 ? [] : a == 0 ? [c / q] : [c / q, q / a];
    }

    // ベルンシュタイン多項式による曲線上の点
    Point2 PointAt(Point2 start, double t)
    {
        var s = 1 - t;
        double Coordinate(double p0, double p1, double p2, double p3)
            => s * s * s * p0 + 3 * s * s * t * p1 + 3 * s * t * t * p2 + t * t * t * p3;

        return new(Coordinate(start.X, Control1.X, Control2.X, End.X),
                   Coordinate(start.Y, Control1.Y, Control2.Y, End.Y));
    }

    /// <summary>
    /// 曲線を半分ずつに分け（de Casteljau）、制御点が弦から <paramref name="tolerance"/> 以内になった部分を弦で近似する。
    /// 曲線は制御点の凸包の内側にあるため、制御点と弦の距離が、曲線と弦の距離の上限になる。
    /// </summary>
    internal override IEnumerable<Segment2> Flatten(Point2 start, double tolerance)
    {
        var pending = new Stack<(Bezier Part, int Depth)>();
        pending.Push((new(start, Control1, Control2, End), 0));
        while (pending.TryPop(out var item)) {
            var (part, depth) = item;
            if (depth == maxSubdivisionDepth || part.IsFlat(tolerance)) {
                yield return part.Chord;
                continue;
            }
            var (first, second) = part.Split();
            pending.Push((second, depth + 1));  // 先に取り出す前半を、後から積む
            pending.Push((first, depth + 1));
        }
    }

    readonly record struct Bezier(Point2 P0, Point2 C1, Point2 C2, Point2 P3)
    {
        public Segment2 Chord => new(P0, P3);

        public bool IsFlat(double tolerance)
            => Chord.DistanceTo(C1) <= tolerance && Chord.DistanceTo(C2) <= tolerance;

        public (Bezier First, Bezier Second) Split()
        {
            var p01 = Midpoint(P0, C1);
            var p12 = Midpoint(C1, C2);
            var p23 = Midpoint(C2, P3);
            var p012 = Midpoint(p01, p12);
            var p123 = Midpoint(p12, p23);
            var middle = Midpoint(p012, p123);
            return (new(P0, p01, p012, middle), new(middle, p123, p23, P3));
        }

        static Point2 Midpoint(Point2 a, Point2 b) => a + (b - a) / 2;
    }
}
