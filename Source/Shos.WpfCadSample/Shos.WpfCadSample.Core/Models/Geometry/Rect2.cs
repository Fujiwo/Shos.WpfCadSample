namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 座標軸に平行な矩形（論理座標。Y は上向きのため、Bottom ≤ Top）。辺の上は内側として扱う。
/// </summary>
public readonly record struct Rect2(double Left, double Bottom, double Right, double Top)
{
    public double Width => Right - Left;
    public double Height => Top - Bottom;
    public Point2 Center => new((Left + Right) / 2, (Bottom + Top) / 2);

    public Rect2 Union(Rect2 other)
        => new(Math.Min(Left, other.Left), Math.Min(Bottom, other.Bottom),
               Math.Max(Right, other.Right), Math.Max(Top, other.Top));

    /// <summary>
    /// 各辺を <paramref name="amount"/> だけ外側へ広げた矩形。
    /// </summary>
    public Rect2 Inflate(double amount) => new(Left - amount, Bottom - amount, Right + amount, Top + amount);

    public bool Contains(Point2 point)
        => Left <= point.X && point.X <= Right && Bottom <= point.Y && point.Y <= Top;

    /// <summary>
    /// すべての点を含む最小の矩形（外接矩形）。点は 1 つ以上必要。
    /// </summary>
    public static Rect2 Bounding(IEnumerable<Point2> points)
    {
        var list = points.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one point is required.", nameof(points));

        return new(list.Min(point => point.X), list.Min(point => point.Y),
                   list.Max(point => point.X), list.Max(point => point.Y));
    }
}
