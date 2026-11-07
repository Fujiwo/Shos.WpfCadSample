using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// <see cref="Segment2"/>（<see cref="Outline.Flatten"/> の結果の線分。T-OUT-03 の距離の基礎）
/// </summary>
public class Segment2Tests
{
    [Theory]
    [InlineData(5.0, 2.0, 2.0)]     // 垂線の足が線分の途中
    [InlineData(-3.0, -4.0, 5.0)]   // 始点の外側
    [InlineData(13.0, 4.0, 5.0)]    // 終点の外側
    public void DistanceTo_IsDistanceToNearestPointOnSegment(double x, double y, double expected)
        => Assert.Equal(expected, new Segment2(new(0, 0), new(10, 0)).DistanceTo(new(x, y)), 1e-9);

    [Fact]
    public void DistanceTo_OfZeroLengthSegment_IsDistanceToThePoint()
        => Assert.Equal(5.0, new Segment2(new(1, 1), new(1, 1)).DistanceTo(new(4, 5)), 1e-9);
}
