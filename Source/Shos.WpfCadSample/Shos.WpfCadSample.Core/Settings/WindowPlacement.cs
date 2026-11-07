using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Settings;

/// <summary>
/// ウィンドウの位置・サイズ（画面座標。WPF の装置に依存しない単位）と最大化の状態（仕様 6.5）。
/// </summary>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height, bool IsMaximized)
{
    /// <summary>
    /// 位置・サイズが有限で、幅・高さが正か（仕様 7 章「設定で不正とみなす条件」）。
    /// </summary>
    public bool IsValid()
        => double.IsFinite(Left) && double.IsFinite(Top) && double.IsFinite(Width) && double.IsFinite(Height) && Width > 0 && Height > 0;

    /// <summary>
    /// ウィンドウ全体が、現在のモニターの範囲（仮想スクリーン）に含まれるか（仕様 5.9）。含まれなければ、呼び出し側が既定の位置へ戻す。
    /// </summary>
    /// <param name="virtualScreen">画面座標（Y 下向き）の矩形。<see cref="Rect2.Bottom"/> が上端（Y の最小）、<see cref="Rect2.Top"/> が下端（Y の最大）。</param>
    public bool IsWithin(Rect2 virtualScreen)
        => virtualScreen.Left <= Left && Left + Width <= virtualScreen.Right &&
           virtualScreen.Bottom <= Top && Top + Height <= virtualScreen.Top;
}
