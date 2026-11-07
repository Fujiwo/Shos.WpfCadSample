using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// 保存する JSON の規則。ドキュメント・クリップボードの独自形式・設定に共通（設計書 3.3、5.6）。
/// </summary>
/// <remarks>
/// 項目名は camelCase。導出値（読み取り専用のプロパティ）は読み書きしない。読み込む項目はすべて必須とし、null は許さない。
/// 色は <c>#RRGGBB</c>、線種は名前の文字列（数値は受け付けない）。数値の文字列表記（"1" や "NaN"）は受け付けない（既定の Strict）。
/// </remarks>
static class JsonRules
{
    public static JsonSerializerOptions Options { get; } = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        AllowOutOfOrderMetadataProperties = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter<LineStyle>(allowIntegerValues: false), new RgbColorJsonConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ExcludeDerivedProperties, RequireAllProperties } }
    };

    // 導出値（HandlePoints、Bounds、Kind など、設定できずコンストラクターの引数でもないプロパティ）は、保存する項目ではない。
    // IgnoreReadOnlyProperties はコレクション型の読み取り専用プロパティを除かないため、ここで除く。
    // [JsonIgnore] の Shape.Id は、コンストラクターの引数に対応するため残す（読み書きはされない）
    static void ExcludeDerivedProperties(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties.Where(IsDerived).ToList())
            typeInfo.Properties.Remove(property);
    }

    static bool IsDerived(JsonPropertyInfo property) => property.Set is null && property.AssociatedParameter is null;

    // 読み込む項目（設定できるプロパティ）をすべて必須にする
    static void RequireAllProperties(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties.Where(property => property.Set is not null))
            property.IsRequired = true;
    }
}
