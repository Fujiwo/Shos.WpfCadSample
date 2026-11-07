using System.Text.Json;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// 図形の列を持つ JSON（ドキュメントとクリップボードの独自形式に共通）の書き出しと、検証付きの読み込み（設計書 5.6）。
/// </summary>
static class ShapesJson
{
    /// <summary>
    /// 書き出す形式のバージョン。これより新しいバージョンは読み込まない。
    /// </summary>
    public const int CurrentVersion = 1;

    public static string Write(string format, IEnumerable<Shape> shapes)
        => JsonSerializer.Serialize(new DocumentFile(format, CurrentVersion, [.. shapes]), JsonRules.Options);

    /// <summary>
    /// 読み込んで検証する（仕様 7 章「不正とみなす条件」）。1 つでも不正なら全体を拒否する。
    /// 読み込んだ図形には、新しい <see cref="ShapeId"/> を付ける。
    /// </summary>
    /// <exception cref="DocumentFormatException">形式が不正、または現行より新しいバージョン。</exception>
    public static IReadOnlyList<Shape> Read(string format, string text)
    {
        var file = Parse(format, text);
        // null 非許容の注釈（RespectNullableAnnotations）は配列の要素を検査しないため、"shapes": [null] はここで拒否する
        var nullIndex = file.Shapes.ToList().FindIndex(shape => shape is null);
        if (nullIndex >= 0)
            throw Invalid($"Shape #{nullIndex + 1} is null.");
        var invalidIndex = file.Shapes.ToList().FindIndex(shape => !shape.IsValid());
        if (invalidIndex >= 0)
            throw Invalid($"Shape #{invalidIndex + 1} ({file.Shapes[invalidIndex].Kind}) violates the shape constraints.");

        return [.. file.Shapes.Select(shape => shape with { Id = ShapeId.New() })];
    }

    // 構文と型の誤りを、すべて DocumentFormatException（Invalid）にまとめる
    static DocumentFile Parse(string format, string text)
    {
        try {
            using var document = JsonDocument.Parse(text);
            ValidateHeader(format, document.RootElement);
            return document.RootElement.Deserialize<DocumentFile>(JsonRules.Options)
                   ?? throw Invalid("The content is empty.");
        } catch (JsonException exception) {
            throw Invalid(exception.Message, exception);
        } catch (NotSupportedException exception) {     // 判別子のない図形（抽象型）など
            throw Invalid(exception.Message, exception);
        }
    }

    // 形式の識別子とバージョンを、ほかの項目より先に判定する（新しいバージョンは、ほかが不正でも NewerVersion）
    static void ValidateHeader(string format, JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw Invalid("The content is not a JSON object.");
        if (!root.TryGetProperty("format", out var formatElement) || formatElement.ValueKind != JsonValueKind.String || formatElement.GetString() != format)
            throw Invalid($"The format identifier is not \"{format}\".");
        if (!root.TryGetProperty("version", out var versionElement) || versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version))
            throw Invalid("The version is not an integer.");
        if (version > CurrentVersion)
            throw new DocumentFormatException(DocumentFormatError.NewerVersion, $"The version {version} is newer than the supported version {CurrentVersion}.");
        if (version < 1)
            throw Invalid($"The version {version} is less than 1.");
    }

    static DocumentFormatException Invalid(string reason, Exception? innerException = null)
        => new(DocumentFormatError.Invalid, reason, innerException);
}
