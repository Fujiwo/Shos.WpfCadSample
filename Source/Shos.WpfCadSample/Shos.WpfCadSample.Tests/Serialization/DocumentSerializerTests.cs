using System.Text.Json;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.Serialization;

namespace Shos.WpfCadSample.Tests.Serialization;

/// <summary>
/// T-SER-01〜13、16、18：<see cref="DocumentSerializer"/>（テスト計画書 4.9）
/// </summary>
public class DocumentSerializerTests
{
    const string validAttributes = """{ "color": "#000000", "thickness": 1, "lineStyle": "Solid" }""";
    const string validLine = """{ "type": "line", "start": { "x": 0, "y": 0 }, "end": { "x": 10, "y": 5 }, "attributes": """ + validAttributes + " }";

    static string Document(string shapes, string format = "\"Shos.WpfCadSample.Document\"", string version = "1")
        => $$"""{ "format": {{format}}, "version": {{version}}, "shapes": [ {{shapes}} ] }""";

    static string Line(string start, string end, string attributes = validAttributes)
        => $$"""{ "type": "line", "start": {{start}}, "end": {{end}}, "attributes": {{attributes}} }""";

    static void AssertInvalid(string text)
    {
        var exception = Assert.Throws<DocumentFormatException>(() => DocumentSerializer.Deserialize(text));
        Assert.Equal(DocumentFormatError.Invalid, exception.Error);
        Assert.False(string.IsNullOrWhiteSpace(exception.Reason));
    }

    // 7 種の図形を 1 つずつ。属性は、色・線種と太さの境界（0.5、50）を図形ごとに変えている
    public static Shape[] AllKinds() => [
        new LineShape(ShapeId.New(), ShapeAttributes.Default, new(10, 20), new(110, 20)),
        new CircleShape(ShapeId.New(), new(new RgbColor(255, 0, 0), 2, LineStyle.Dash), new(50, 50), new(80, 90)),
        new RectangleShape(ShapeId.New(), new(new RgbColor(0, 128, 255), 0.5, LineStyle.Dot), new(-5, -5), new(5, 15)),
        new PolylineShape(ShapeId.New(), new(new RgbColor(1, 2, 3), 50, LineStyle.DashDot), [new(0, 0), new(10, 5), new(20, 0)]),
        new PolygonShape(ShapeId.New(), ShapeAttributes.Default, [new(0, 0), new(10, 0), new(5, 8)]),
        new OpenCurveShape(ShapeId.New(), ShapeAttributes.Default, [new(0, 0), new(10, 5), new(20, 0), new(30, 7)]),
        new ClosedCurveShape(ShapeId.New(), ShapeAttributes.Default, [new(0, 0), new(10, 0), new(10, 10), new(0, 10)])
    ];

    // 種類・点・属性・順序の一致（ShapeId を除く）
    internal static void AssertSameShapes(IReadOnlyList<Shape> expected, IReadOnlyList<Shape> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (before, after) in expected.Zip(actual)) {
            Assert.Equal(before.GetType(), after.GetType());
            Assert.True(before.HandlePoints.SequenceEqual(after.HandlePoints));
            Assert.Equal(before.Attributes, after.Attributes);
        }
    }

    // T-SER-01
    [Fact]
    public void RoundTrip_KeepsKindsPointsAttributesAndOrder()
    {
        var shapes = AllKinds();

        AssertSameShapes(shapes, DocumentSerializer.Deserialize(DocumentSerializer.Serialize(shapes)));
    }

    // T-SER-02
    [Fact]
    public void Serialize_WritesDocumentStructure()
    {
        using var json = JsonDocument.Parse(DocumentSerializer.Serialize(AllKinds()));
        var root = json.RootElement;

        Assert.Equal("Shos.WpfCadSample.Document", root.GetProperty("format").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        var shapes = root.GetProperty("shapes").EnumerateArray().ToList();
        Assert.Equal(["line", "circle", "rectangle", "polyline", "polygon", "openCurve", "closedCurve"],
                     shapes.Select(shape => shape.GetProperty("type").GetString()));
        var circle = shapes[1];
        Assert.Equal("#FF0000", circle.GetProperty("attributes").GetProperty("color").GetString());
        Assert.Equal("Dash", circle.GetProperty("attributes").GetProperty("lineStyle").GetString());
        Assert.Equal(2.0, circle.GetProperty("attributes").GetProperty("thickness").GetDouble());
        Assert.Equal(50.0, circle.GetProperty("center").GetProperty("x").GetDouble());
        Assert.Equal(90.0, circle.GetProperty("pointOnCircle").GetProperty("y").GetDouble());
        Assert.Equal(3, shapes[3].GetProperty("points").GetArrayLength());
    }

    // T-SER-02（定義点と属性だけを保存し、導出値は保存しない）
    [Fact]
    public void Serialize_WritesOnlyDefinitionOfShapes()
    {
        using var json = JsonDocument.Parse(DocumentSerializer.Serialize(AllKinds()));

        foreach (var shape in json.RootElement.GetProperty("shapes").EnumerateArray()) {
            string[] expected = shape.GetProperty("type").GetString() switch {
                "line" => ["type", "start", "end", "attributes"],
                "circle" => ["type", "center", "pointOnCircle", "attributes"],
                "rectangle" => ["type", "corner", "oppositeCorner", "attributes"],
                _ => ["type", "points", "attributes"]
            };
            Assert.Equal(expected.Order(), shape.EnumerateObject().Select(property => property.Name).Order());
        }
    }

    // T-SER-03
    [Fact]
    public void RoundTrip_OfEmptyDocument_IsEmpty()
        => Assert.Empty(DocumentSerializer.Deserialize(DocumentSerializer.Serialize([])));

    // T-SER-04
    [Fact]
    public void ShapeId_IsNotSavedAndIsNewOnEveryLoad()
    {
        var text = DocumentSerializer.Serialize(AllKinds());

        Assert.DoesNotContain("\"id\"", text, StringComparison.OrdinalIgnoreCase);
        var first = DocumentSerializer.Deserialize(text);
        var second = DocumentSerializer.Deserialize(text);
        Assert.All(first.Zip(second), pair => Assert.NotEqual(pair.First.Id, pair.Second.Id));
        Assert.All(first, shape => Assert.NotEqual(default, shape.Id));
    }

    // T-SER-05：10 進の表記で丸めると戻らない値も、許容誤差なしで元の double と一致する
    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0 / 3)]
    [InlineData(1e-7)]
    [InlineData(1e6 / 3)]
    [InlineData(-123456.789)]
    public void RoundTrip_KeepsDoubleValuesExactly(double value)
    {
        Shape line = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(value, -value), new(value * 2, 1 / 3.0));

        var loaded = Assert.IsType<LineShape>(Assert.Single(DocumentSerializer.Deserialize(DocumentSerializer.Serialize([line]))));

        Assert.Equal(new Point2(value, -value), loaded.Start);
        Assert.Equal(new Point2(value * 2, 1 / 3.0), loaded.End);
    }

    // T-SER-06
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    public void Deserialize_RejectsTextThatIsNotADocument(string text) => AssertInvalid(text);

    // T-SER-07
    public static TheoryData<string> DocumentsWithInvalidHeader => [
        Document(validLine, format: "\"Other.Format\""),
        Document(validLine, format: "\"Shos.WpfCadSample.Clipboard\""),
        """{ "version": 1, "shapes": [] }""",                                   // 形式の識別子の欠落
        """{ "format": "Shos.WpfCadSample.Document", "shapes": [] }""",         // version の欠落
        Document(validLine, version: "\"1\""),                                  // 整数でない
        Document(validLine, version: "1.5"),
        Document(validLine, version: "null"),
        """{ "format": "Shos.WpfCadSample.Document", "version": 1 }""",         // shapes の欠落
        """{ "format": "Shos.WpfCadSample.Document", "version": 1, "shapes": null }""",
        """{ "format": "Shos.WpfCadSample.Document", "version": 1, "shapes": {} }"""
    ];

    // T-SER-07
    [Theory]
    [MemberData(nameof(DocumentsWithInvalidHeader))]
    public void Deserialize_RejectsInvalidHeader(string text) => AssertInvalid(text);

    // T-SER-08
    public static TheoryData<string> DocumentsWithUnknownKindOrMissingItem => [
        Document("""{ "type": "triangle", "points": [], "attributes": """ + validAttributes + " }"),
        Document("""{ "start": { "x": 0, "y": 0 }, "end": { "x": 1, "y": 1 }, "attributes": """ + validAttributes + " }"),   // type の欠落
        Document("""{ "type": "line", "end": { "x": 1, "y": 1 }, "attributes": """ + validAttributes + " }"),               // start の欠落
        Document("""{ "type": "line", "start": { "x": 0, "y": 0 }, "attributes": """ + validAttributes + " }"),             // end の欠落
        Document("""{ "type": "line", "start": { "x": 0, "y": 0 }, "end": { "x": 1, "y": 1 } }"""),                        // attributes の欠落
        Document("""{ "type": "polyline", "attributes": """ + validAttributes + " }"),                                      // points の欠落
        Document("""{ "type": "polyline", "points": null, "attributes": """ + validAttributes + " }"),
        Document("""{ "type": "circle", "center": { "x": 0, "y": 0 }, "attributes": """ + validAttributes + " }"),
        Document(Line("""{ "x": 0 }""", """{ "x": 1, "y": 1 }""")),                                                         // y の欠落
        Document(Line("""{ "x": 0, "y": 0 }""", """{ "x": 1, "y": 1 }""", """{ "color": "#000000", "thickness": 1 }""")),  // lineStyle の欠落
        Document("null"),                                                                                                   // 図形が null（配列の要素は、null 非許容の注釈で検査されない）
        Document(validLine + ", null")
    ];

    // T-SER-08
    [Theory]
    [MemberData(nameof(DocumentsWithUnknownKindOrMissingItem))]
    public void Deserialize_RejectsUnknownKindOrMissingItem(string text) => AssertInvalid(text);

    // T-SER-09：順に、未知の線種、線種が数値、色名、色の桁数不足、色が数値、太さが下限未満、上限超え
    [Theory]
    [InlineData("""{ "color": "#000000", "thickness": 1, "lineStyle": "Wavy" }""")]
    [InlineData("""{ "color": "#000000", "thickness": 1, "lineStyle": 1 }""")]
    [InlineData("""{ "color": "red", "thickness": 1, "lineStyle": "Solid" }""")]
    [InlineData("""{ "color": "#12345", "thickness": 1, "lineStyle": "Solid" }""")]
    [InlineData("""{ "color": 0, "thickness": 1, "lineStyle": "Solid" }""")]
    [InlineData("""{ "color": "#000000", "thickness": 0.49, "lineStyle": "Solid" }""")]
    [InlineData("""{ "color": "#000000", "thickness": 50.01, "lineStyle": "Solid" }""")]
    public void Deserialize_RejectsInvalidAttributes(string attributes)
        => AssertInvalid(Document(Line("""{ "x": 0, "y": 0 }""", """{ "x": 1, "y": 1 }""", attributes)));

    // T-SER-10：1e999 は JSON の数値としては正しいが、double では無限大になる。"NaN" は文字列で、数値ではない
    [Theory]
    [InlineData("""{ "x": "0", "y": 0 }""")]
    [InlineData("""{ "x": 0, "y": null }""")]
    [InlineData("""{ "x": 1e999, "y": 0 }""")]
    [InlineData("""{ "x": 0, "y": -1e999 }""")]
    [InlineData("""{ "x": "NaN", "y": 0 }""")]
    [InlineData("""{ "x": true, "y": 0 }""")]
    public void Deserialize_RejectsNonNumericOrNonFiniteCoordinate(string point)
        => AssertInvalid(Document(Line(point, """{ "x": 1, "y": 1 }""")));

    // T-SER-11：順に、最小点数の不足（多角形 2 点、開いた曲線 1 点、折れ線 0 点）、線分の 2 点の一致、半径 0、
    // 幅 0、隣り合う点の一致、閉じた曲線の最後と最初の一致
    public static TheoryData<string> DocumentsViolatingShapeConstraints => [
        Document("""{ "type": "polygon", "points": [ { "x": 0, "y": 0 }, { "x": 1, "y": 1 } ], "attributes": """ + validAttributes + " }"),
        Document("""{ "type": "openCurve", "points": [ { "x": 0, "y": 0 } ], "attributes": """ + validAttributes + " }"),
        Document("""{ "type": "polyline", "points": [], "attributes": """ + validAttributes + " }"),
        Document(Line("""{ "x": 3, "y": 3 }""", """{ "x": 3, "y": 3 }""")),
        Document("""{ "type": "circle", "center": { "x": 3, "y": 3 }, "pointOnCircle": { "x": 3, "y": 3 }, "attributes": """ + validAttributes + " }"),
        Document("""{ "type": "rectangle", "corner": { "x": 0, "y": 0 }, "oppositeCorner": { "x": 0, "y": 5 }, "attributes": """ + validAttributes + " }"),
        Document("""{ "type": "polyline", "points": [ { "x": 0, "y": 0 }, { "x": 0, "y": 0 } ], "attributes": """ + validAttributes + " }"),
        Document("""{ "type": "closedCurve", "points": [ { "x": 0, "y": 0 }, { "x": 5, "y": 5 }, { "x": 0, "y": 0 } ], "attributes": """ + validAttributes + " }")
    ];

    // T-SER-11
    [Theory]
    [MemberData(nameof(DocumentsViolatingShapeConstraints))]
    public void Deserialize_RejectsShapeViolatingConstraints(string text) => AssertInvalid(text);

    // T-SER-12：不正な図形を有効な図形で挟み、途中までの図形を返さないことを確かめる
    [Fact]
    public void Deserialize_RejectsWholeDocumentIfAnyShapeIsInvalid()
        => AssertInvalid(Document(string.Join(", ", validLine, Line("""{ "x": 3, "y": 3 }""", """{ "x": 3, "y": 3 }"""), validLine)));

    // 有効な例（テストの組み立ての確認）
    [Fact]
    public void Deserialize_AcceptsHandWrittenDocument()
    {
        var shapes = DocumentSerializer.Deserialize(Document(validLine + ", " + validLine));

        Assert.Equal(2, shapes.Count);
        Assert.All(shapes, shape => Assert.True(shape.IsValid()));
    }

    // T-SER-13：図形が不正でも、先にバージョンを判定する。新しいバージョンで保存したファイルであることを伝えるため
    [Theory]
    [InlineData("2")]
    [InlineData("100")]
    public void Deserialize_NewerVersion_IsReportedBeforeOtherProblems(string version)
    {
        var exception = Assert.Throws<DocumentFormatException>(() => DocumentSerializer.Deserialize(Document("""{ "type": "unknown" }""", version: version)));

        Assert.Equal(DocumentFormatError.NewerVersion, exception.Error);
    }

    // T-SER-16
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Deserialize_RejectsVersionBelowOne(string version) => AssertInvalid(Document(validLine, version: version));

    // T-SER-18
    [Theory]
    [InlineData("""{ "x": 1000000.5, "y": 0 }""")]
    [InlineData("""{ "x": 0, "y": -1000001 }""")]
    public void Deserialize_RejectsCoordinateBeyondLimit(string point)
        => AssertInvalid(Document(Line(point, """{ "x": 1, "y": 1 }""")));

    // T-SER-18（境界の上は有効）
    [Fact]
    public void Deserialize_AcceptsCoordinateOnLimit()
        => Assert.Single(DocumentSerializer.Deserialize(Document(Line("""{ "x": 1000000, "y": -1000000 }""", """{ "x": 1, "y": 1 }"""))));
}
