using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models.Geometry;

/// <summary>
/// T-HIT：<see cref="ShapeHitTester"/>（テスト計画書 4.7）。
/// 許容範囲は論理座標で与える。既定の太さ 1 の線は、中心線から 0.5 + 許容範囲 以内で線上とする。
/// 曲線・円は折れ線で近似して測るため、境界の両側に近似誤差（許容範囲の 1/20）より大きい余裕を取る。
/// </summary>
public class ShapeHitTesterTests
{
    const double tolerance = 5;
    const double inside = 0.5 + tolerance - 0.3;   // 線上（太さ 1 の半分 + 許容範囲 の内側）
    const double outside = 0.5 + tolerance + 0.3;  // 範囲外

    static readonly ShapeAttributes attributes = ShapeAttributes.Default;

    static Shape Line(Point2 start, Point2 end, double thickness = 1)
        => new LineShape(ShapeId.New(), attributes with { Thickness = thickness }, start, end);

    static int Hit(Shape shape, Point2 point) => ShapeHitTester.HitShape([shape], point, tolerance);

    // T-HIT-01：各図形について、(線上の点, 線から外向きの単位ベクトル)
    public static TheoryData<Shape, Point2, Vector2> PointsOnEachKind => new() {
        { Line(new(0, 0), new(100, 0)), new(50, 0), new(0, 1) },
        { new CircleShape(ShapeId.New(), attributes, new(50, 50), new(50, 90)), new(50, 90), new(0, 1) },
        { new RectangleShape(ShapeId.New(), attributes, new(0, 0), new(100, 60)), new(50, 0), new(0, -1) },
        { new PolylineShape(ShapeId.New(), attributes, [new(0, 0), new(100, 0), new(100, 80)]), new(100, 40), new(1, 0) },
        { new PolygonShape(ShapeId.New(), attributes, [new(0, 0), new(100, 0), new(100, 80)]), new(50, 40), new Vector2(-80, 100) / Math.Sqrt(80 * 80 + 100 * 100) },  // 閉じる辺の外向きの法線
        { new OpenCurveShape(ShapeId.New(), attributes, [new(0, 0), new(50, 50), new(100, 0)]), new(50, 50), new(0, 1) },     // 対称の頂点で接線は水平
        { new ClosedCurveShape(ShapeId.New(), attributes, [new(0, 0), new(100, 0), new(100, 100), new(0, 100)]), new(100, 0), new Vector2(1, -1) / Math.Sqrt(2) }
    };

    // T-HIT-01
    [Theory]
    [MemberData(nameof(PointsOnEachKind))]
    public void HitShape_HitsWithinToleranceOfTheLine(Shape shape, Point2 onLine, Vector2 outward)
    {
        Assert.Equal(0, Hit(shape, onLine));
        Assert.Equal(0, Hit(shape, onLine + outward * inside));
    }

    // T-HIT-01
    [Theory]
    [MemberData(nameof(PointsOnEachKind))]
    public void HitShape_MissesBeyondTolerance(Shape shape, Point2 onLine, Vector2 outward)
        => Assert.Equal(-1, Hit(shape, onLine + outward * outside));

    // T-HIT-02
    [Fact]
    public void HitShape_InteriorIsNotHit()
    {
        Assert.Equal(-1, Hit(new RectangleShape(ShapeId.New(), attributes, new(0, 0), new(100, 60)), new(50, 30)));
        Assert.Equal(-1, Hit(new CircleShape(ShapeId.New(), attributes, new(50, 50), new(50, 90)), new(50, 50)));
        Assert.Equal(-1, Hit(new PolygonShape(ShapeId.New(), attributes, [new(0, 0), new(100, 0), new(50, 90)]), new(50, 30)));
        Assert.Equal(-1, Hit(new ClosedCurveShape(ShapeId.New(), attributes, [new(0, 0), new(100, 0), new(100, 100), new(0, 100)]), new(50, 50)));
    }

    // T-HIT-03
    [Fact]
    public void HitShape_ReturnsTopmostOfOverlappingShapes()
    {
        // 添字 0〜2 の線は (50, 50) を通り、添字 3 は通らない。重なりでは後に描いた（最前面の）図形を選ぶ
        Shape[] shapes = [Line(new(0, 0), new(100, 100)), Line(new(0, 100), new(100, 0)), Line(new(50, 0), new(50, 100)), Line(new(0, 0), new(10, 0))];

        Assert.Equal(2, ShapeHitTester.HitShape(shapes, new(50, 50), tolerance));
    }

    // T-HIT-04
    [Fact]
    public void HitShape_EmptySpaceOrNoShapes_ReturnsMinusOne()
    {
        Assert.Equal(-1, ShapeHitTester.HitShape([Line(new(0, 0), new(100, 0))], new(50, 50), tolerance));
        Assert.Equal(-1, ShapeHitTester.HitShape([], new(50, 50), tolerance));
    }

    // T-HIT-05：画面上 5 px の許容範囲を、拡大率に応じて論理座標へ換算する（px ÷ Scale）
    [Theory]
    [InlineData(0.25)]
    [InlineData(1.0)]
    [InlineData(4.0)]
    [InlineData(32.0)]
    public void HitShape_ScreenToleranceIsIndependentOfScale(double scale)
    {
        var line = Line(new(0, 0), new(100, 0));
        var logicalTolerance = 5 / scale;
        double ScreenOffsetToLogical(double pixels) => (0.5 * scale + pixels) / scale;  // 線の太さの半分（画面上）からの距離

        Assert.Equal(0, ShapeHitTester.HitShape([line], new(50, ScreenOffsetToLogical(4.9)), logicalTolerance));
        Assert.Equal(-1, ShapeHitTester.HitShape([line], new(50, ScreenOffsetToLogical(5.1)), logicalTolerance));
    }

    // T-HIT-06
    [Fact]
    public void HitHandle_ReturnsHandleWithinTolerance()
    {
        // ハンドルの添字は、角 (0, 0)、(100, 0)、(100, 60)、(0, 60) の順
        var rectangle = new RectangleShape(ShapeId.New(), attributes, new(0, 0), new(100, 60));

        Assert.Equal(2, ShapeHitTester.HitHandle(rectangle, new(103, 64), tolerance));     // 距離 5（境界上）
        Assert.Equal(1, ShapeHitTester.HitHandle(rectangle, new(98, 1), tolerance));
        Assert.Equal(-1, ShapeHitTester.HitHandle(rectangle, new(104, 64), tolerance));    // 距離 √32 ≈ 5.66
        Assert.Equal(-1, ShapeHitTester.HitHandle(rectangle, new(50, 30), tolerance));
    }

    // T-HIT-06
    [Fact]
    public void HitHandle_ReturnsNearestOfSeveralCloseHandles()
    {
        // ハンドルの間隔 3 は許容範囲 5 より狭く、許容範囲内に複数のハンドルがある。そのうち最も近いものを選ぶ
        var polyline = new PolylineShape(ShapeId.New(), attributes, [new(0, 0), new(3, 0), new(6, 0)]);

        Assert.Equal(1, ShapeHitTester.HitHandle(polyline, new(2, 1), tolerance));
        Assert.Equal(2, ShapeHitTester.HitHandle(polyline, new(5, -1), tolerance));
        Assert.Equal(0, ShapeHitTester.HitHandle(polyline, new(-1, 0), tolerance));
    }

    // T-HIT-07
    [Fact]
    public void HitShape_ThickLine_MeasuresToleranceFromTheVisibleEdge()
    {
        var thick = Line(new(0, 0), new(100, 0), thickness: 20);

        Assert.Equal(0, ShapeHitTester.HitShape([thick], new(50, 14.9), tolerance));    // 10 + 5 以内
        Assert.Equal(-1, ShapeHitTester.HitShape([thick], new(50, 15.1), tolerance));
    }
}
