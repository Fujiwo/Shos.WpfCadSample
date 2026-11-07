namespace Shos.WpfCadSample.Models;

/// <summary>
/// 線種の拡張メンバー。線種と破線のパターンの対応は、ここだけに置く。
/// </summary>
public static class LineStyleExtensions
{
    // 描画のたびに配列を作らないよう、パターンは共有する（戻り値は読み取り専用の型で公開する）
    static readonly double[] solid = [];
    static readonly double[] dash = [4, 2];
    static readonly double[] dot = [1, 2];
    static readonly double[] dashDot = [4, 2, 1, 2];

    /// <summary>
    /// 破線の線と間隔の長さ（線の太さに対する倍率。設計書 DA-5）。実線は空。
    /// 画面（WPF の StrokeDashArray）と出力（GDI+ の DashPattern）で、同じ倍率を使う。
    /// </summary>
    public static IReadOnlyList<double> DashPattern(this LineStyle style) => style switch {
        LineStyle.Solid => solid,
        LineStyle.Dash => dash,
        LineStyle.Dot => dot,
        LineStyle.DashDot => dashDot,
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown line style.")
    };
}
