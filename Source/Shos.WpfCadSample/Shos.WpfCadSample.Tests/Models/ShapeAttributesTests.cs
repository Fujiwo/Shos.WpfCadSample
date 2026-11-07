using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Tests.Models;

/// <summary>
/// T-ATT：属性（テスト計画書 4.6）
/// </summary>
public class ShapeAttributesTests
{
    // T-ATT-01
    [Theory]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#1A2B3C", 0x1A, 0x2B, 0x3C)]
    [InlineData("#1a2b3c", 0x1A, 0x2B, 0x3C)]  // 小文字
    [InlineData("#aBcDeF", 0xAB, 0xCD, 0xEF)]  // 大文字・小文字の混在
    public void TryParse_ReadsHexColor(string text, int red, int green, int blue)
    {
        Assert.True(RgbColor.TryParse(text, out var color));
        Assert.Equal(new RgbColor((byte)red, (byte)green, (byte)blue), color);
    }

    // T-ATT-01
    [Theory]
    [InlineData(0, 0, 0, "#000000")]
    [InlineData(255, 128, 1, "#FF8001")]
    [InlineData(0xAB, 0xCD, 0xEF, "#ABCDEF")]
    public void ToHex_RoundTripsThroughTryParse(int red, int green, int blue, string expectedHex)
    {
        var color = new RgbColor((byte)red, (byte)green, (byte)blue);

        Assert.Equal(expectedHex, color.ToHex());
        Assert.True(RgbColor.TryParse(color.ToHex(), out var parsed));
        Assert.Equal(color, parsed);
    }

    // T-ATT-02
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("1A2B3C")]       // # なし
    [InlineData("#1A2B3")]       // 桁数が少ない
    [InlineData("#1A2B3C4")]     // 桁数が多い
    [InlineData("#1A2B3G")]      // 16 進でない文字
    [InlineData("# 1A2B3")]      // 空白
    [InlineData("#+1A2B3")]      // 符号
    [InlineData("#１A2B3C")]     // 全角数字
    public void TryParse_RejectsInvalidText(string? text)
        => Assert.False(RgbColor.TryParse(text, out _));

    // T-ATT-03
    [Theory]
    [InlineData(0.5, true)]
    [InlineData(50.0, true)]
    [InlineData(1.0, true)]
    [InlineData(0.49, false)]
    [InlineData(50.01, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void IsValid_ChecksThicknessRange(double thickness, bool expected)
        => Assert.Equal(expected, (ShapeAttributes.Default with { Thickness = thickness }).IsValid());

    // T-ATT-03
    [Fact]
    public void IsValid_RejectsUndefinedLineStyle()
        => Assert.False((ShapeAttributes.Default with { LineStyle = (LineStyle)99 }).IsValid());

    // T-ATT-04
    [Fact]
    public void Default_IsBlackOneSolid()
    {
        Assert.Equal("#000000", ShapeAttributes.Default.Color.ToHex());
        Assert.Equal(1.0, ShapeAttributes.Default.Thickness);
        Assert.Equal(LineStyle.Solid, ShapeAttributes.Default.LineStyle);
    }

    // T-ATT-05
    [Theory]
    [InlineData(LineStyle.Solid, new double[0])]
    [InlineData(LineStyle.Dash, new[] { 4.0, 2.0 })]
    [InlineData(LineStyle.Dot, new[] { 1.0, 2.0 })]
    [InlineData(LineStyle.DashDot, new[] { 4.0, 2.0, 1.0, 2.0 })]
    public void DashPattern_IsMultipleOfThickness(LineStyle style, double[] expected)
        => Assert.Equal(expected, style.DashPattern());
}
