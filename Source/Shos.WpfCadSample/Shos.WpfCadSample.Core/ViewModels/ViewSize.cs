namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// 描画領域の大きさ（px）。<see cref="DrawingViewModel.SetViewSizeCommand"/> の引数。
/// </summary>
public readonly record struct ViewSize(double Width, double Height);
