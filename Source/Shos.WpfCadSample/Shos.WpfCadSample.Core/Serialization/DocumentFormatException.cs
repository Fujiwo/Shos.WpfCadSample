namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// ドキュメントまたはクリップボードの独自形式を読み込めないことを表す（設計書 8.2）。
/// </summary>
public sealed class DocumentFormatException(DocumentFormatError error, string reason, Exception? innerException = null)
    : Exception(reason, innerException)
{
    public DocumentFormatError Error { get; } = error;

    /// <summary>
    /// 判明した範囲の理由（英語。仕様 7 章のエラーダイアログに表示する）。
    /// </summary>
    public string Reason { get; } = reason;
}
