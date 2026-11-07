using System.Text.Json;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Serialization;

namespace Shos.WpfCadSample.Tests.Serialization;

/// <summary>
/// T-SER-14〜18：<see cref="ClipboardShapes"/>（テスト計画書 4.9）
/// </summary>
public class ClipboardShapesTests
{
    const string validLine = """{ "type": "line", "start": { "x": 0, "y": 0 }, "end": { "x": 10, "y": 5 }, "attributes": { "color": "#000000", "thickness": 1, "lineStyle": "Solid" } }""";

    static string Clipboard(string shapes, string format = "\"Shos.WpfCadSample.Clipboard\"", string version = "1")
        => $$"""{ "format": {{format}}, "version": {{version}}, "shapes": [ {{shapes}} ] }""";

    static void AssertInvalid(string text)
    {
        var exception = Assert.Throws<DocumentFormatException>(() => ClipboardShapes.FromText(text));
        Assert.Equal(DocumentFormatError.Invalid, exception.Error);
    }

    public static TheoryData<Shape> AllKinds => [.. DocumentSerializerTests.AllKinds()];

    // T-SER-14
    [Theory]
    [MemberData(nameof(AllKinds))]
    public void RoundTrip_KeepsKindPointsAndAttributesWithNewId(Shape shape)
    {
        var restored = ClipboardShapes.FromText(ClipboardShapes.ToText(shape));

        DocumentSerializerTests.AssertSameShapes([shape], [restored]);
        Assert.NotEqual(shape.Id, restored.Id);
    }

    // T-SER-14（形式の識別子）
    [Fact]
    public void ToText_WritesClipboardFormatWithOneShape()
    {
        using var json = JsonDocument.Parse(ClipboardShapes.ToText(DocumentSerializerTests.AllKinds()[0]));

        Assert.Equal("Shos.WpfCadSample.Clipboard", json.RootElement.GetProperty("format").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("shapes").GetArrayLength());
    }

    // T-SER-15
    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    public void FromText_RejectsTextThatIsNotJson(string text) => AssertInvalid(text);

    // T-SER-15：ドキュメントの JSON は構造が似ていても、識別子が異なるため受け付けない
    [Fact]
    public void FromText_RejectsDocumentFormat()
        => AssertInvalid(Clipboard(validLine, format: "\"Shos.WpfCadSample.Document\""));

    // T-SER-15（T-SER-08〜11 と同じ検証）。順に：未知の種類、必須項目 end の欠落、色が #RRGGBB でない、
    // 太さが範囲外、座標が数値でない、2 点が一致
    [Theory]
    [InlineData("""{ "type": "triangle", "points": [], "attributes": { "color": "#000000", "thickness": 1, "lineStyle": "Solid" } }""")]
    [InlineData("""{ "type": "line", "start": { "x": 0, "y": 0 }, "attributes": { "color": "#000000", "thickness": 1, "lineStyle": "Solid" } }""")]
    [InlineData("""{ "type": "line", "start": { "x": 0, "y": 0 }, "end": { "x": 10, "y": 5 }, "attributes": { "color": "black", "thickness": 1, "lineStyle": "Solid" } }""")]
    [InlineData("""{ "type": "line", "start": { "x": 0, "y": 0 }, "end": { "x": 10, "y": 5 }, "attributes": { "color": "#000000", "thickness": 99, "lineStyle": "Solid" } }""")]
    [InlineData("""{ "type": "line", "start": { "x": "0", "y": 0 }, "end": { "x": 10, "y": 5 }, "attributes": { "color": "#000000", "thickness": 1, "lineStyle": "Solid" } }""")]
    [InlineData("""{ "type": "line", "start": { "x": 3, "y": 3 }, "end": { "x": 3, "y": 3 }, "attributes": { "color": "#000000", "thickness": 1, "lineStyle": "Solid" } }""")]
    public void FromText_RejectsInvalidShape(string shape) => AssertInvalid(Clipboard(shape));

    // T-SER-16
    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    public void FromText_RejectsVersionBelowOne(string version) => AssertInvalid(Clipboard(validLine, version: version));

    // 新しいバージョンは、不正（Invalid）と区別して報告する
    [Fact]
    public void FromText_NewerVersion_IsReported()
        => Assert.Equal(DocumentFormatError.NewerVersion,
                        Assert.Throws<DocumentFormatException>(() => ClipboardShapes.FromText(Clipboard(validLine, version: "2"))).Error);

    // T-SER-17
    [Theory]
    [InlineData("")]
    [InlineData(validLine + ", " + validLine)]
    public void FromText_RejectsOtherThanExactlyOneShape(string shapes) => AssertInvalid(Clipboard(shapes));

    // T-SER-18
    [Fact]
    public void FromText_RejectsCoordinateBeyondLimit()
        => AssertInvalid(Clipboard(validLine.Replace("\"x\": 10", "\"x\": 1000001")));
}
