using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-EXP：<see cref="ExportFrame"/>（テスト計画書 4.15）
/// </summary>
public class ExportFrameTests
{
    const int maxSide = 10_000;
    const int maxPixels = 4_000_000;

    // 太さ 1 の矩形で、出力範囲がちょうど 0〜width × 0〜height になる図形
    static Shape ShapeWithArea(double width, double height)
        => new RectangleShape(ShapeId.New(), ShapeAttributes.Default, new(0.5, 0.5), new(width - 0.5, height - 0.5));

    // T-EXP-01
    [Fact]
    public void Area_IsBoundsInflatedByHalfOfEachThickness()
    {
        Shape line = new LineShape(ShapeId.New(), ShapeAttributes.Default with { Thickness = 2 }, new(0, 0), new(100, 0));
        Shape circle = new CircleShape(ShapeId.New(), ShapeAttributes.Default with { Thickness = 6 }, new(200, 50), new(210, 50));

        var area = ExportFrame.For([line, circle]).Area;

        Assert.Equal(-1.0, area.Left, 1e-9);
        Assert.Equal(-1.0, area.Bottom, 1e-9);
        Assert.Equal(213.0, area.Right, 1e-9);
        Assert.Equal(63.0, area.Top, 1e-9);
    }

    // T-EXP-02
    [Theory]
    [InlineData(10_000.0, 1.0)]
    [InlineData(1.0, 10_000.0)]
    [InlineData(2_000.0, 2_000.0)]
    [InlineData(123.4, 56.7)]
    public void Scale_IsOneWhenWithinLimits(double width, double height)
    {
        var frame = ExportFrame.For([ShapeWithArea(width, height)]);

        Assert.Equal(1.0, frame.Scale);
        Assert.Equal((int)Math.Ceiling(width), frame.PixelWidth);
        Assert.Equal((int)Math.Ceiling(height), frame.PixelHeight);
    }

    // T-EXP-03、T-EXP-04
    [Theory]
    [InlineData(30_000.0, 1.0)]         // 横長：長辺が上限を超える
    [InlineData(2.0, 25_000.0)]         // 縦長
    [InlineData(3_000.0, 3_000.0)]      // 正方形に近い：画素数が上限を超える
    [InlineData(9_000.0, 1_000.0)]      // 画素数が上限を超える横長
    [InlineData(2_000.3, 2_000.3)]      // 切り上げると 2,001 × 2,001 で上限を超える
    [InlineData(10_000.2, 5.0)]         // 切り上げると 10,001 px
    [InlineData(2_000_000.0, 1_500_000.0)] // 座標の範囲いっぱい
    public void Scale_ShrinksKeepingAspectToSatisfyBothLimits(double width, double height)
    {
        var frame = ExportFrame.For([ShapeWithArea(width, height)]);

        Assert.True(frame.Scale < 1);
        Assert.InRange(frame.PixelWidth, 1, maxSide);
        Assert.InRange(frame.PixelHeight, 1, maxSide);
        Assert.True((long)frame.PixelWidth * frame.PixelHeight <= maxPixels);
        // 範囲を欠落させない（ピクセル寸法は、縮小した範囲を切り上げた値）
        Assert.Equal((int)Math.Ceiling(frame.Area.Width * frame.Scale), frame.PixelWidth);
        Assert.Equal((int)Math.Ceiling(frame.Area.Height * frame.Scale), frame.PixelHeight);
        // 必要以上には縮小しない（上限から決まる縮尺の 99% 以上）
        var upperBound = Math.Min(Math.Min(maxSide / width, maxSide / height), Math.Sqrt(maxPixels / (width * height)));
        Assert.InRange(frame.Scale, upperBound * 0.99, upperBound);
    }

    [Fact]
    public void For_WithoutShapes_IsRejected()
        => Assert.Throws<ArgumentException>(() => ExportFrame.For([]));
}
