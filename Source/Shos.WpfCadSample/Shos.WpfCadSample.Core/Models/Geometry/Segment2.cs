namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 線分。<see cref="Outline.Flatten"/> が曲線を近似した折れ線の 1 本。
/// </summary>
public readonly record struct Segment2(Point2 Start, Point2 End)
{
    /// <summary>
    /// 点から線分上の最も近い点までの距離。
    /// </summary>
    public double DistanceTo(Point2 point)
    {
        var direction = End - Start;
        var lengthSquared = direction.X * direction.X + direction.Y * direction.Y;
        if (lengthSquared == 0)
            return point.DistanceTo(Start);    // 長さ 0 の線分は点として扱う

        // 点を直線へ正射影した位置の媒介変数 t（内積 ÷ 長さの 2 乗）。[0, 1] に収め、線分の外では近い端点を使う
        var fromStart = point - Start;
        var t = Math.Clamp((fromStart.X * direction.X + fromStart.Y * direction.Y) / lengthSquared, 0, 1);
        return point.DistanceTo(Start + direction * t);
    }
}
