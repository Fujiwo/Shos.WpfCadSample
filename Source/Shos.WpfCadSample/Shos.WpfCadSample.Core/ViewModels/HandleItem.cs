namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// 選択ハンドルの位置（画面座標。ハンドルは画面上で固定の大きさのため、図形とは別の層に置く。設計書 DD-4）。
/// </summary>
public sealed record HandleItem(double X, double Y);
