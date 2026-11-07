using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 論理座標へ変換した入力。画面上の大きさ（px）で決まる許容範囲を、論理座標へ換算できる。
/// </summary>
/// <param name="PixelSize">画面上の 1 px に当たる論理座標の長さ（1 ÷ Scale）。</param>
readonly record struct PointerInput(PointerAction Action, Point2 Point, int ClickCount, double PixelSize)
{
    /// <summary>
    /// 画面上の <paramref name="count"/> px を、論理座標の長さへ換算する（ズーム率によらない許容範囲のため）。
    /// </summary>
    public double Pixels(double count) => count * PixelSize;
}
