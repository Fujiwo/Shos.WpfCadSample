using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// クリップボードの独自形式（JSON テキスト）との変換（仕様 6.6、設計書 5.6）。図形はちょうど 1 個。
/// 検証はドキュメントと同じで、形式の識別子だけが異なる。
/// </summary>
public static class ClipboardShapes
{
    /// <summary>
    /// 形式の識別子（JSON の <c>format</c>）。ドキュメントの形式と区別する。
    /// </summary>
    public const string Format = "Shos.WpfCadSample.Clipboard";

    public static string ToText(Shape shape) => ShapesJson.Write(Format, [shape]);

    /// <exception cref="DocumentFormatException">形式が不正（図形が 1 個でない場合を含む）、または現行より新しいバージョン。</exception>
    public static Shape FromText(string text)
    {
        var shapes = ShapesJson.Read(Format, text);
        if (shapes.Count != 1)
            throw new DocumentFormatException(DocumentFormatError.Invalid, $"The clipboard data must contain exactly one shape, but it contains {shapes.Count}.");
        return shapes[0];
    }
}
