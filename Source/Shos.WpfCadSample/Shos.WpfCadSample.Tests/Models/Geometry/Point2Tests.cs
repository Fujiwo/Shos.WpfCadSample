using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-GEO-04、T-GEO-05：<see cref="Point2"/> と <see cref="Vector2"/>（テスト計画書 4.1）
/// </summary>
public class Point2Tests
{
    const double limit = 1_000_000;

    // T-GEO-04
    [Fact]
    public void DistanceTo_ReturnsEuclideanDistance()
        => Assert.Equal(5.0, new Point2(1, 2).DistanceTo(new Point2(4, 6)), 1e-9);

    // T-GEO-04
    [Theory]
    [InlineData(0.0, 0.0, true)]
    [InlineData(-1e300, 1e300, true)]   // 座標の範囲外でも有限（範囲の判定は IsWithinLimits の役目）
    [InlineData(double.NaN, 0.0, false)]
    [InlineData(0.0, double.NaN, false)]
    [InlineData(double.PositiveInfinity, 0.0, false)]
    [InlineData(0.0, double.NegativeInfinity, false)]
    public void IsFinite_IsFalseOnlyForNaNOrInfinity(double x, double y, bool expected)
        => Assert.Equal(expected, new Point2(x, y).IsFinite);

    // T-GEO-05
    [Theory]
    [InlineData(limit, limit)]
    [InlineData(-limit, -limit)]
    [InlineData(limit, -limit)]
    [InlineData(0.0, 0.0)]
    public void IsWithinLimits_IsTrueUpToExactlyTheLimit(double x, double y)
        => Assert.True(new Point2(x, y).IsWithinLimits());

    // T-GEO-05
    [Theory]
    [MemberData(nameof(PointsOutsideLimits))]
    public void IsWithinLimits_IsFalseOutsideTheLimit(double x, double y)
        => Assert.False(new Point2(x, y).IsWithinLimits());

    public static TheoryData<double, double> PointsOutsideLimits => new() {
        { Math.BitIncrement(limit), 0.0 },          // 外側の直近の値
        { Math.BitDecrement(-limit), 0.0 },
        { 0.0, Math.BitIncrement(limit) },
        { 0.0, Math.BitDecrement(-limit) },
        { Math.BitIncrement(limit), limit },        // X だけ範囲外
        { limit, Math.BitDecrement(-limit) },       // Y だけ範囲外
        { double.NaN, 0.0 },
        { 0.0, double.NaN },
        { double.PositiveInfinity, 0.0 },
        { 0.0, double.NegativeInfinity }
    };

    [Fact]
    public void SubtractingPoints_GivesVectorFromSecondToFirst()
        => Assert.Equal(new Vector2(3, 4), new Point2(4, 6) - new Point2(1, 2));

    [Fact]
    public void AddingVector_TranslatesPoint()
        => Assert.Equal(new Point2(4, 6), new Point2(1, 2) + new Vector2(3, 4));

    [Fact]
    public void Vector_SupportsScalingAndLength()
    {
        var vector = new Vector2(3, 4);

        Assert.Equal(new Vector2(6, 8), vector * 2);
        Assert.Equal(new Vector2(1.5, 2), vector / 2);
        Assert.Equal(new Vector2(4, 6), vector + new Vector2(1, 2));
        Assert.Equal(new Vector2(2, 2), vector - new Vector2(1, 2));
        Assert.Equal(5.0, vector.Length, 1e-9);
    }
}
