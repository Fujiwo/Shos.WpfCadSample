namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// 描画領域のカーソルの種類（仕様 4.3）。WPF の Cursor への変換は Views が行う。
/// </summary>
public enum PointerCursor
{
    Arrow,  // Select（ハンドルの外）
    Cross,  // 描画ツール
    Move    // Select のハンドルの上と、ドラッグ中
}
