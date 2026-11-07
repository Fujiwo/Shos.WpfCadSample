using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-GEO-03：<see cref="Rect2"/>（テスト計画書 4.1）
/// </summary>
public class Rect2Tests
{
    [Fact]
    public void WidthHeightAndCenter_AreDerivedFromEdges()
    {
        var rect = new Rect2(Left: 10, Bottom: 20, Right: 110, Top: 70);

        Assert.Equal(100.0, rect.Width);
        Assert.Equal(50.0, rect.Height);
        Assert.Equal(new Point2(60, 45), rect.Center);
    }

    [Fact]
    public void Union_CoversBothRectangles()
    {
        var union = new Rect2(0, 0, 1000, 700).Union(new Rect2(-50, 100, 200, 900));

        Assert.Equal(new Rect2(-50, 0, 1000, 900), union);
    }

    [Fact]
    public void Union_WithContainedRectangle_IsUnchanged()
    {
        var outer = new Rect2(0, 0, 1000, 700);

        Assert.Equal(outer, outer.Union(new Rect2(10, 10, 20, 20)));
    }

    [Theory]
    [InlineData(50.0, 50.0)]   // 内側
    [InlineData(0.0, 0.0)]     // 角
    [InlineData(100.0, 30.0)]  // 辺の上
    public void Contains_IsTrueInsideAndOnTheEdges(double x, double y)
        => Assert.True(new Rect2(0, 0, 100, 100).Contains(new Point2(x, y)));

    // 各辺のすぐ外側（0.001 だけ外）。辺の上を含む判定が、外側まで広がっていないことを確かめる
    [Theory]
    [InlineData(-0.001, 50.0)]
    [InlineData(100.001, 50.0)]
    [InlineData(50.0, -0.001)]
    [InlineData(50.0, 100.001)]
    public void Contains_IsFalseOutside(double x, double y)
        => Assert.False(new Rect2(0, 0, 100, 100).Contains(new Point2(x, y)));

    [Fact]
    public void Bounding_IsSmallestRectangleContainingAllPoints()
    {
        Point2[] points = [new(3, -1), new(-2, 4), new(5, 2)];

        Assert.Equal(new Rect2(-2, -1, 5, 4), Rect2.Bounding(points));
    }

    [Fact]
    public void Inflate_MovesEveryEdgeOutward()
        => Assert.Equal(new Rect2(-2, 8, 102, 62), new Rect2(0, 10, 100, 60).Inflate(2));

    [Fact]
    public void Bounding_OfNoPoints_IsRejected()
        => Assert.Throws<ArgumentException>(() => Rect2.Bounding([]));
}
