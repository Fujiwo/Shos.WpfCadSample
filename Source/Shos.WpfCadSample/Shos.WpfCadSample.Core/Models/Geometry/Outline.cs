namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 直線と 3 次ベジェだけで図形を表す、共通の中間表現（設計書 DD-6、5.2）。
/// 画面の描画、ヒットテスト、EMF・画像の出力が、すべてこれを入力にする。
/// </summary>
public sealed record Outline(IReadOnlyList<OutlineFigure> Figures)
{
    /// <summary>
    /// 外接矩形（線の太さは含めない）。曲線は、端点ではなく曲線上の点の最小・最大で求める。
    /// </summary>
    public Rect2 Bounds() => Rect2.Bounding(Figures.SelectMany(figure => figure.ExtremePoints()));

    /// <summary>
    /// 折れ線で近似する。近似した線分のどの点も、元の線から <paramref name="tolerance"/> 以内にある。
    /// 閉じた図形は、閉じる辺も含む。
    /// </summary>
    /// <param name="tolerance">近似の許容誤差（論理座標。正の有限値）。画面上 0.25 px 相当を想定する（設計書 DA-2）。</param>
    public IEnumerable<Segment2> Flatten(double tolerance)
    {
        ThrowIfNotPositiveFinite(tolerance);
        return Figures.SelectMany(figure => figure.Flatten(tolerance));
    }

    /// <summary>
    /// 点から線までの最短距離。曲線は <see cref="Flatten"/> の折れ線で測る。
    /// </summary>
    public double DistanceTo(Point2 point, double tolerance)
        => Flatten(tolerance).Min(segment => segment.DistanceTo(point));

    /// <summary>
    /// すべての点（始点、区間の終点、ベジェの制御点）が有限か。
    /// </summary>
    internal bool IsFinite()
        => Figures.All(figure => figure.Start.IsFinite && figure.Segments.All(segment => segment.IsFinite));

    static void ThrowIfNotPositiveFinite(double tolerance)
    {
        if (!double.IsFinite(tolerance))
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be finite.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tolerance);
    }
}
