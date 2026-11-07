using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// ドキュメントの JSON との変換（仕様 6.4、7 章、設計書 5.6）。
/// </summary>
public static class DocumentSerializer
{
    public const string Format = "Shos.WpfCadSample.Document";

    public static string Serialize(IEnumerable<Shape> shapes) => ShapesJson.Write(Format, shapes);

    /// <exception cref="DocumentFormatException">形式が不正、または現行より新しいバージョン。</exception>
    public static IReadOnlyList<Shape> Deserialize(string text) => ShapesJson.Read(Format, text);
}
