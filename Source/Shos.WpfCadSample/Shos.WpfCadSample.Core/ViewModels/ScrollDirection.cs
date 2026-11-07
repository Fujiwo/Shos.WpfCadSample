namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// Ctrl+矢印のスクロールの向き（表示範囲を動かす向き）。<see cref="DrawingViewModel.ScrollCommand"/> の引数。
/// </summary>
public enum ScrollDirection
{
    Left,
    Right,
    Up,
    Down
}
