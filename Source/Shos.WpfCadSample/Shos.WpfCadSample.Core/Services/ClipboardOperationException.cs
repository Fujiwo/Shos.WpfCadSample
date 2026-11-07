namespace Shos.WpfCadSample.Services;

/// <summary>
/// クリップボードへの出力の生成や読み書きの失敗（Core が GDI+ や COM の例外型に依存しないため。設計書 8.2）。
/// </summary>
public sealed class ClipboardOperationException(string message, Exception? innerException = null) : Exception(message, innerException);
