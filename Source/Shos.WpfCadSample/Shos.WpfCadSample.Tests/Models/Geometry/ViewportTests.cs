using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-VPT：<see cref="Viewport"/>（テスト計画書 4.4）。
/// 基本は、ビューサイズ 1000 × 700、論理領域 0〜1000 × 0〜700（FitScale = 1）。
/// </summary>
public class ViewportTests
{
    const double tolerance = 1e-9;
    static readonly Point2 viewCenter = new(500, 350);

    static Viewport StandardViewport()
    {
        var viewport = new Viewport();
        viewport.SetViewSize(1000, 700);
        return viewport;
    }

    static void AssertEqual(Point2 expected, Point2 actual)
    {
        Assert.Equal(expected.X, actual.X, tolerance);
        Assert.Equal(expected.Y, actual.Y, tolerance);
    }

    // 表示範囲（画面の四隅に対応する論理座標の矩形）
    static Rect2 VisibleRange(Viewport viewport, double viewWidth = 1000, double viewHeight = 700)
        => Rect2.Bounding([viewport.Transform.ToLogical(new(0, 0)), viewport.Transform.ToLogical(new(viewWidth, viewHeight))]);

    // T-VPT-01
    [Fact]
    public void Initially_ShowsOverviewAtOneHundredPercent()
    {
        var viewport = StandardViewport();

        Assert.Equal(new Rect2(0, 0, 1000, 700), viewport.LogicalArea);
        Assert.Equal(viewport.LogicalArea, viewport.Overview);
        Assert.Equal(1.0, viewport.FitScale, tolerance);
        Assert.Equal(viewport.FitScale, viewport.Scale, tolerance);
        Assert.Equal(100.0, viewport.ZoomPercent, tolerance);
        AssertEqual(new Point2(500, 350), viewport.Center);
    }

    // T-VPT-01（Y 上向きの論理領域が、画面いっぱいに表示される）
    [Fact]
    public void Initially_MapsLogicalAreaCornersToViewCorners()
    {
        var transform = StandardViewport().Transform;

        AssertEqual(new Point2(0, 700), transform.ToScreen(new Point2(0, 0)));
        AssertEqual(new Point2(1000, 0), transform.ToScreen(new Point2(1000, 700)));
    }

    // T-VPT-02
    [Theory]
    [InlineData(2000.0, 700.0, 1.0)]   // 横長のビュー：高さで決まる
    [InlineData(500.0, 700.0, 0.5)]    // 縦長のビュー：幅で決まる
    [InlineData(300.0, 300.0, 0.3)]
    public void FitScale_FitsOverviewAndCentersIt(double viewWidth, double viewHeight, double expectedFitScale)
    {
        var viewport = new Viewport();

        viewport.SetViewSize(viewWidth, viewHeight);

        Assert.Equal(expectedFitScale, viewport.FitScale, tolerance);
        Assert.Equal(expectedFitScale, viewport.Scale, tolerance);
        AssertEqual(new Point2(viewWidth / 2, viewHeight / 2), viewport.Transform.ToScreen(viewport.Overview.Center));
        var visible = VisibleRange(viewport, viewWidth, viewHeight);
        Assert.True(visible.Inflate(tolerance).Contains(new Point2(0, 0)) && visible.Inflate(tolerance).Contains(new Point2(1000, 700)));
    }

    // T-VPT-03
    [Theory]
    [InlineData(300.0, 200.0)]
    [InlineData(950.0, 20.0)]
    [InlineData(500.0, 350.0)]
    public void ZoomAt_KeepsLogicalPointUnderCursorWhenZoomingIn(double cursorX, double cursorY)
    {
        var viewport = StandardViewport();
        var cursor = new Point2(cursorX, cursorY);
        var logicalUnderCursor = viewport.Transform.ToLogical(cursor);

        viewport.ZoomAt(cursor, 1);
        AssertEqual(cursor, viewport.Transform.ToScreen(logicalUnderCursor));

        viewport.ZoomAt(cursor, 3);
        AssertEqual(cursor, viewport.Transform.ToScreen(logicalUnderCursor));
    }

    // T-VPT-03
    [Fact]
    public void ZoomAt_KeepsLogicalPointUnderCursorWhenZoomingOut()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(viewCenter, 5);
        var cursor = new Point2(400, 300);
        var logicalUnderCursor = viewport.Transform.ToLogical(cursor);

        viewport.ZoomAt(cursor, -1);

        AssertEqual(cursor, viewport.Transform.ToScreen(logicalUnderCursor));
    }

    // T-VPT-04
    [Fact]
    public void ZoomAt_OneNotch_ScalesByOnePointTwo()
    {
        var viewport = StandardViewport();

        viewport.ZoomAt(viewCenter, 1);
        Assert.Equal(1.2, viewport.Scale, tolerance);

        viewport.ZoomAt(viewCenter, 2);
        viewport.ZoomAt(viewCenter, -1);
        Assert.Equal(1.2 * 1.2, viewport.Scale, tolerance);
    }

    // T-VPT-05
    [Fact]
    public void ZoomAt_DoesNotShrinkBelowOverview()
    {
        var viewport = StandardViewport();

        viewport.ZoomAt(new Point2(100, 100), -1);
        Assert.Equal(viewport.FitScale, viewport.Scale);

        viewport.ZoomAt(new Point2(100, 100), 2);
        viewport.ZoomAt(new Point2(900, 600), -5);
        Assert.Equal(viewport.FitScale, viewport.Scale);
        AssertEqual(viewport.Overview.Center, viewport.Center);
    }

    // T-VPT-06
    [Fact]
    public void ZoomAt_DoesNotExceedThirtyTwoTimesOverview()
    {
        var viewport = StandardViewport();

        viewport.ZoomAt(viewCenter, 100);
        Assert.Equal(32 * viewport.FitScale, viewport.Scale, tolerance);

        viewport.ZoomAt(viewCenter, 1);
        Assert.Equal(32 * viewport.FitScale, viewport.Scale, tolerance);
    }

    // T-VPT-07（画面の下向きの移動は、Y 上向きの論理座標では負の向き）
    [Fact]
    public void Scroll_WhileZoomed_ConvertsPixelsToLogicalUnits()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(viewCenter, 2);
        var before = viewport.Center;

        viewport.Scroll(50, 0);
        AssertEqual(new Point2(before.X + 50 / 1.44, before.Y), viewport.Center);

        viewport.Scroll(0, 50);
        AssertEqual(new Point2(before.X + 50 / 1.44, before.Y - 50 / 1.44), viewport.Center);
    }

    // T-VPT-08
    [Fact]
    public void Scroll_StopsAtOverviewEdge()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(viewCenter, 4);

        viewport.Scroll(100_000, 100_000);  // 右下の端まで

        var visible = VisibleRange(viewport);
        Assert.Equal(viewport.Overview.Right, visible.Right, tolerance);
        Assert.Equal(viewport.Overview.Bottom, visible.Bottom, tolerance);

        viewport.Scroll(-100_000, -100_000); // 左上の端まで

        visible = VisibleRange(viewport);
        Assert.Equal(viewport.Overview.Left, visible.Left, tolerance);
        Assert.Equal(viewport.Overview.Top, visible.Top, tolerance);
    }

    // T-VPT-08
    [Fact]
    public void Scroll_WhileShowingOverview_DoesNotMove()
    {
        var viewport = StandardViewport();

        viewport.Scroll(50, -50);

        AssertEqual(new Point2(500, 350), viewport.Center);
    }

    // T-VPT-09
    [Fact]
    public void UpdateOverview_WithShapesOutsideLogicalArea_UnitesThem()
    {
        var viewport = StandardViewport();

        viewport.UpdateOverview(new Rect2(900, -100, 1500, 300));

        Assert.Equal(new Rect2(0, -100, 1500, 700), viewport.Overview);
    }

    // T-VPT-09（図形がない、または論理領域の内側にあるときは、論理領域）
    [Fact]
    public void UpdateOverview_WithoutShapesOutside_IsLogicalArea()
    {
        var viewport = StandardViewport();
        viewport.UpdateOverview(new Rect2(900, -100, 1500, 300));

        viewport.UpdateOverview(null);
        Assert.Equal(viewport.LogicalArea, viewport.Overview);

        viewport.UpdateOverview(new Rect2(10, 10, 20, 20));
        Assert.Equal(viewport.LogicalArea, viewport.Overview);
    }

    // T-VPT-10
    [Fact]
    public void UpdateOverview_WhileShowingOverview_RefitsToNewOverview()
    {
        var viewport = StandardViewport();

        viewport.UpdateOverview(new Rect2(900, -100, 1500, 300));    // 全体図は 1500 × 800

        Assert.Equal(Math.Min(1000.0 / 1500, 700.0 / 800), viewport.FitScale, tolerance);
        Assert.Equal(viewport.FitScale, viewport.Scale, tolerance);
        Assert.Equal(100.0, viewport.ZoomPercent, tolerance);
        AssertEqual(new Point2(750, 300), viewport.Center);
    }

    // T-VPT-11
    [Fact]
    public void UpdateOverview_WhileZoomed_KeepsScaleAndCenterWhenOverviewGrows()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(new Point2(300, 200), 3);
        var (scale, center) = (viewport.Scale, viewport.Center);

        viewport.UpdateOverview(new Rect2(500, 0, 2000, 700));      // FitScale は 0.5 に下がる

        Assert.Equal(0.5, viewport.FitScale, tolerance);
        Assert.Equal(scale, viewport.Scale, tolerance);
        AssertEqual(center, viewport.Center);
    }

    // T-VPT-11
    [Fact]
    public void UpdateOverview_WhileZoomed_CapsScaleAtNewMaximumKeepingCenter()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(new Point2(300, 200), 100);                // 上限の 32 倍
        var center = viewport.Center;

        viewport.UpdateOverview(new Rect2(500, 0, 2000, 700));      // 新しい上限は 0.5 × 32 = 16

        Assert.Equal(16.0, viewport.Scale, tolerance);
        AssertEqual(center, viewport.Center);
    }

    // T-VPT-12
    [Fact]
    public void UpdateOverview_WhileZoomed_RaisesScaleToNewFitScaleWhenOverviewShrinks()
    {
        var viewport = StandardViewport();
        viewport.UpdateOverview(new Rect2(500, 0, 2000, 700));      // FitScale 0.5
        viewport.ZoomAt(viewCenter, 1);                             // Scale 0.6

        viewport.UpdateOverview(null);                              // FitScale 1 に戻る

        Assert.Equal(1.0, viewport.Scale, tolerance);
        Assert.Equal(100.0, viewport.ZoomPercent, tolerance);
        AssertEqual(viewport.Overview.Center, viewport.Center);
    }

    // T-VPT-12
    [Fact]
    public void UpdateOverview_WhileZoomed_PullsVisibleRangeBackInsideShrunkOverview()
    {
        var viewport = StandardViewport();
        viewport.UpdateOverview(new Rect2(500, 0, 2000, 700));      // 全体図 0〜2000
        viewport.ZoomAt(viewCenter, 8);                             // Scale 0.5 × 1.2^8 ≈ 2.15
        viewport.Scroll(100_000, 0);                                // 右端（X ≈ 2000）まで
        var scale = viewport.Scale;

        viewport.UpdateOverview(null);                              // 全体図 0〜1000

        Assert.Equal(scale, viewport.Scale, tolerance);
        Assert.Equal(1000.0, VisibleRange(viewport).Right, tolerance);
    }

    // T-VPT-13
    [Fact]
    public void SetViewSize_WhileShowingOverview_Refits()
    {
        var viewport = StandardViewport();

        viewport.SetViewSize(500, 350);

        Assert.Equal(0.5, viewport.Scale, tolerance);
        AssertEqual(new Point2(500, 350), viewport.Center);
        AssertEqual(new Point2(250, 175), viewport.Transform.ToScreen(viewport.Overview.Center));
    }

    // T-VPT-13（拡大中は、縮尺と中心を保つ）
    [Fact]
    public void SetViewSize_WhileZoomed_KeepsScaleAndCenter()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(viewCenter, 3);
        var (scale, center) = (viewport.Scale, viewport.Center);

        viewport.SetViewSize(1200, 800);

        Assert.Equal(scale, viewport.Scale, tolerance);
        AssertEqual(center, viewport.Center);
    }

    // 最小化などで大きさが 0 になったときは、直前の表示を保つ
    [Theory]
    [InlineData(0.0, 700.0)]
    [InlineData(1000.0, 0.0)]
    [InlineData(double.NaN, 700.0)]
    public void SetViewSize_WithoutPositiveFiniteSize_KeepsPreviousView(double width, double height)
    {
        var viewport = StandardViewport();

        viewport.SetViewSize(width, height);

        Assert.Equal(1.0, viewport.Scale, tolerance);
        Assert.Equal(1.0, viewport.FitScale, tolerance);
    }

    // T-VPT-14
    [Fact]
    public void ZoomPercent_IsScaleRelativeToCurrentFitScale()
    {
        var viewport = StandardViewport();

        viewport.ZoomAt(viewCenter, 1);
        Assert.Equal(120.0, viewport.ZoomPercent, tolerance);

        viewport.UpdateOverview(new Rect2(500, 0, 2000, 700));      // FitScale が半分になる

        Assert.Equal(240.0, viewport.ZoomPercent, tolerance);
    }

    // T-VPT-15
    [Fact]
    public void Reset_ReturnsToOverview()
    {
        var viewport = StandardViewport();
        viewport.ZoomAt(new Point2(100, 600), 4);
        viewport.Scroll(50, 50);

        viewport.Reset();

        Assert.Equal(viewport.FitScale, viewport.Scale);
        AssertEqual(viewport.Overview.Center, viewport.Center);
    }
}
