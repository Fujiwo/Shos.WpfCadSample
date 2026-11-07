using System.Text.Json;
using System.Text.Json.Serialization;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// 色を <c>#RRGGBB</c> の文字列で読み書きする。形式が違えば <see cref="JsonException"/>（仕様 7 章）。
/// </summary>
sealed class RgbColorJsonConverter : JsonConverter<RgbColor>
{
    public override RgbColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !RgbColor.TryParse(reader.GetString(), out var color))
            throw new JsonException("A color must be a string of the form #RRGGBB.");
        return color;
    }

    public override void Write(Utf8JsonWriter writer, RgbColor value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToHex());
}
