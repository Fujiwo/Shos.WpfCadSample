using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-GEO-01、T-GEO-02：<see cref="ViewTransform"/>（テスト計画書 4.1）
/// </summary>
public class ViewTransformTests
{
    const double tolerance = 1e-9;

    [Fact]
    public void ToScreen_ScalesTranslatesAndFlipsY()
    {
        var transform = new ViewTransform(Scale: 2, TranslateX: 10, TranslateY: 500);

        Assert.Equal(new Point2(10 + 2 * 3, 500 - 2 * 4), transform.ToScreen(new Point2(3, 4)));
    }

    // T-GEO-01
    [Theory]
    [InlineData(1.0, 0.0, 700.0)]
    [InlineData(0.25, -120.5, 33.0)]
    [InlineData(32.0, 4000.0, -1500.0)]
    [InlineData(0.0013, 12.0, 0.5)]
    public void ToLogical_InvertsToScreen(double scale, double translateX, double translateY)
    {
        var transform = new ViewTransform(scale, translateX, translateY);
        Point2[] logicalPoints = [new(0, 0), new(123.456, -78.9), new(-1_000_000, 1_000_000)];

        foreach (var logical in logicalPoints) {
            var roundTripped = transform.ToLogical(transform.ToScreen(logical));
            Assert.Equal(logical.X, roundTripped.X, Math.Abs(logical.X) * tolerance + tolerance);
            Assert.Equal(logical.Y, roundTripped.Y, Math.Abs(logical.Y) * tolerance + tolerance);
        }
    }

    // T-GEO-02
    [Fact]
    public void LargerLogicalY_IsHigherOnScreen()
    {
        var transform = new ViewTransform(Scale: 1.5, TranslateX: 0, TranslateY: 700);

        var lower = transform.ToScreen(new Point2(0, 100));
        var upper = transform.ToScreen(new Point2(0, 200));

        Assert.True(upper.Y < lower.Y);
    }
}
