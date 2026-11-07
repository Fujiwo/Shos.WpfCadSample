namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 2 次元の点。図形では論理座標（X は右向き、Y は上向き）に、ビュー変換の結果では画面座標に使う。
/// </summary>
public readonly record struct Point2(double X, double Y)
{
    /// <summary>
    /// 図形の座標の絶対値の上限（仕様 6.3、A-29）。この範囲では、図形から導出する値は有限になる。
    /// </summary>
    public const double CoordinateLimit = 1_000_000;

    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

    /// <summary>
    /// 図形の点として使える座標か（有限で、絶対値が <see cref="CoordinateLimit"/> 以下）。
    /// 座標の範囲の判定は、ここだけに置く（設計書 5.1）。
    /// </summary>
    public bool IsWithinLimits()
        => IsFinite && Math.Abs(X) <= CoordinateLimit && Math.Abs(Y) <= CoordinateLimit;

    public double DistanceTo(Point2 other) => (other - this).Length;

    // 点 − 点 = 変位、点 ± 変位 = 点。点どうしの和は意味を持たないため定義しない
    public static Vector2 operator -(Point2 end, Point2 start) => new(end.X - start.X, end.Y - start.Y);
    public static Point2 operator +(Point2 point, Vector2 offset) => new(point.X + offset.X, point.Y + offset.Y);
    public static Point2 operator -(Point2 point, Vector2 offset) => new(point.X - offset.X, point.Y - offset.Y);
}
