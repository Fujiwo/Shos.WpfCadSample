using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Tests.ViewModels;

/// <summary>
/// T-VM-18〜21：<see cref="PropertiesViewModel"/> と <see cref="AttributeEditModel"/>（テスト計画書 4.13）
/// </summary>
public class PropertiesViewModelTests
{
    readonly Drawing drawing = new();
    readonly EditorController editor;
    readonly PropertiesViewModel properties;

    public PropertiesViewModelTests()
    {
        var viewport = new Viewport();
        viewport.SetViewSize(1000, 700);
        editor = new EditorController(drawing, viewport);
        properties = new PropertiesViewModel(editor);
    }

    static readonly ShapeAttributes red = new(new RgbColor(255, 0, 0), 2.5, LineStyle.Dash);

    LineShape AddLine(double y, ShapeAttributes attributes)
    {
        var line = new LineShape(ShapeId.New(), attributes, new(100, y), new(300, y));
        drawing.Add(line);
        return line;
    }

    // 画面座標 (x, 700 − y) で、線上をクリックして選択する
    void ClickAt(double x, double y)
    {
        editor.HandlePointer(new(PointerAction.Down, new(x, 700 - y)));
        editor.HandlePointer(new(PointerAction.Up, new(x, 700 - y)));
    }

    // T-VM-18
    [Fact]
    public void WithoutSelection_SelectedSectionIsEmptyAndDisabled()
    {
        Assert.False(properties.HasSelection);
        Assert.Equal("", properties.ShapeTypeName);
    }

    // T-VM-18
    [Fact]
    public void Selecting_ShowsKindAndAttributes()
    {
        AddLine(100, red);

        ClickAt(200, 100);

        Assert.True(properties.HasSelection);
        Assert.Equal("Line", properties.ShapeTypeName);
        Assert.Equal(red, properties.SelectedAttributes.Attributes);
        Assert.Equal("#FF0000", properties.SelectedAttributes.Hex);
        Assert.Equal(255, properties.SelectedAttributes.Red);
        Assert.Equal("2.5", properties.SelectedAttributes.ThicknessText);
        Assert.Equal(LineStyle.Dash, properties.SelectedAttributes.LineStyle);
    }

    // T-VM-18
    [Fact]
    public void ChangingSelectionAndUndoRedo_UpdateDisplay()
    {
        AddLine(100, red);
        AddLine(300, ShapeAttributes.Default);
        ClickAt(200, 300);
        Assert.Equal(ShapeAttributes.Default, properties.SelectedAttributes.Attributes);

        ClickAt(200, 100);
        Assert.Equal(red, properties.SelectedAttributes.Attributes);

        properties.SelectedAttributes.LineStyle = LineStyle.Dot;
        properties.CommitEditCommand.Execute(null);
        editor.Undo();
        Assert.Equal(LineStyle.Dash, properties.SelectedAttributes.LineStyle);
        editor.Redo();
        Assert.Equal(LineStyle.Dot, properties.SelectedAttributes.LineStyle);

        ClickAt(500, 500);
        Assert.False(properties.HasSelection);
    }

    // T-VM-18（編集はその場で図形に反映され、確定で 1 回の Undo になる）
    [Fact]
    public void EditingSelectedAttributes_PreviewsAndCommitsOnce()
    {
        AddLine(100, ShapeAttributes.Default);
        ClickAt(200, 100);

        properties.SelectedAttributes.Red = 10;
        properties.SelectedAttributes.Red = 20;
        Assert.Equal(new RgbColor(20, 0, 0), drawing.Shapes[0].Attributes.Color);
        properties.CommitEditCommand.Execute(null);

        editor.Undo();
        Assert.Equal(ShapeAttributes.Default, drawing.Shapes[0].Attributes);
    }

    // T-VM-19
    [Fact]
    public void EditingRgb_UpdatesHexAndSwatch()
    {
        var model = properties.DefaultAttributes;

        model.Red = 0x12;
        model.Green = 0xAB;
        model.Blue = 0xEF;

        Assert.Equal("#12ABEF", model.Hex);
        Assert.Equal(new RgbColor(0x12, 0xAB, 0xEF), model.Attributes.Color);
    }

    // T-VM-19
    [Fact]
    public void EditingHex_UpdatesRgb()
    {
        var model = properties.DefaultAttributes;
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        model.Hex = "#3c4d5e";

        Assert.Equal((0x3C, 0x4D, 0x5E), (model.Red, model.Green, model.Blue));
        Assert.Contains(nameof(AttributeEditModel.Red), changed);
        Assert.Contains(nameof(AttributeEditModel.Blue), changed);
    }

    // T-VM-19（不正な 16 進の入力の後でも、RGB の編集が 16 進へ連動する）
    [Fact]
    public void EditingRgbAfterInvalidHex_ReplacesHexWithValidValue()
    {
        var model = properties.DefaultAttributes;
        model.Hex = "#12";

        model.Green = 0x80;

        // 不正な 16 進は色に反映されていないため、既定の黒の G だけを変えた値になる
        Assert.Equal("#008000", model.Hex);
        Assert.True(model.IsHexValid);
    }

    // T-VM-20
    [Theory]
    [InlineData("#12345")]
    [InlineData("red")]
    [InlineData("#GG0000")]
    [InlineData("")]
    public void InvalidHex_IsNotAppliedAndRevertsOnCommit(string invalid)
    {
        var model = properties.DefaultAttributes;

        model.Hex = invalid;

        Assert.False(model.IsHexValid);
        Assert.Equal(ShapeAttributes.Default, model.Attributes);
        Assert.Equal(ShapeAttributes.Default, editor.DefaultAttributes);
        // 確定すると、表示を直前の有効値に戻す
        properties.CommitEditCommand.Execute(null);
        Assert.Equal("#000000", model.Hex);
        Assert.True(model.IsHexValid);
    }

    // T-VM-20
    [Theory]
    [InlineData("abc")]
    [InlineData("0.49")]
    [InlineData("50.01")]
    [InlineData("NaN")]
    [InlineData("")]
    public void InvalidThickness_IsNotAppliedAndRevertsOnCommit(string invalid)
    {
        var model = properties.DefaultAttributes;

        model.ThicknessText = invalid;

        Assert.False(model.IsThicknessValid);
        Assert.Equal(1.0, model.Attributes.Thickness);
        properties.CommitEditCommand.Execute(null);
        Assert.Equal("1.0", model.ThicknessText);
        Assert.True(model.IsThicknessValid);
    }

    [Theory]
    [InlineData("0.5", 0.5)]
    [InlineData("50", 50.0)]
    [InlineData(" 3.25 ", 3.25)]    // 前後の空白は許す
    public void ValidThickness_IsApplied(string text, double expected)
    {
        properties.DefaultAttributes.ThicknessText = text;

        Assert.Equal(expected, editor.DefaultAttributes.Thickness);
    }

    // T-VM-21
    [Fact]
    public void EditingDefaults_ChangesNewShapesOnlyWithoutHistory()
    {
        var existing = AddLine(100, ShapeAttributes.Default);
        drawing.MarkSaved();

        properties.DefaultAttributes.LineStyle = LineStyle.DashDot;
        properties.DefaultAttributes.Hex = "#00FF00";
        properties.CommitEditCommand.Execute(null);

        Assert.Equal(new ShapeAttributes(new RgbColor(0, 255, 0), 1.0, LineStyle.DashDot), editor.DefaultAttributes);
        Assert.Same(existing, drawing.Shapes[0]);
        Assert.False(drawing.IsModified);
        drawing.Undo();
        Assert.Empty(drawing.Shapes);           // 履歴は Add だけ
    }

    [Fact]
    public void LoadDefaults_ShowsAndAppliesSavedDefaults()
    {
        properties.LoadDefaults(red);

        Assert.Equal(red, editor.DefaultAttributes);
        Assert.Equal("#FF0000", properties.DefaultAttributes.Hex);
        Assert.Equal("2.5", properties.DefaultAttributes.ThicknessText);
    }

    [Fact]
    public void ShapeTypeName_UsesReadableNames()
    {
        drawing.Add(new OpenCurveShape(ShapeId.New(), ShapeAttributes.Default, [new(100, 100), new(200, 150), new(300, 100)]));

        ClickAt(300, 100);      // 曲線の終点（通過点）の上

        // 列挙の名前 OpenCurve ではなく、語を空白で区切った表示名
        Assert.Equal("Open Curve", properties.ShapeTypeName);
    }
}
