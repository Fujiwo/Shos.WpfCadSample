using System.Collections.Immutable;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models;

/// <summary>
/// T-SHP-01〜13、16、18：図形（テスト計画書 4.5）
/// </summary>
public class ShapeTests
{
    const double limit = Point2.CoordinateLimit;
    static readonly ShapeAttributes attributes = ShapeAttributes.Default;

    static LineShape Line(Point2 start, Point2 end) => new(ShapeId.New(), attributes, start, end);
    static CircleShape Circle(Point2 center, Point2 pointOnCircle) => new(ShapeId.New(), attributes, center, pointOnCircle);
    static RectangleShape Rectangle(Point2 corner, Point2 opposite) => new(ShapeId.New(), attributes, corner, opposite);
    static PolylineShape Polyline(params Point2[] points) => new(ShapeId.New(), attributes, [.. points]);
    static PolygonShape Polygon(params Point2[] points) => new(ShapeId.New(), attributes, [.. points]);
    static OpenCurveShape OpenCurve(params Point2[] points) => new(ShapeId.New(), attributes, [.. points]);
    static ClosedCurveShape ClosedCurve(params Point2[] points) => new(ShapeId.New(), attributes, [.. points]);

    // 7 種の有効な図形
    public static TheoryData<Shape> ValidShapes => [
        Line(new(0, 0), new(10, 5)),
        Circle(new(5, 5), new(8, 9)),
        Rectangle(new(0, 0), new(10, 5)),
        Polyline(new(0, 0), new(10, 0), new(10, 10)),
        Polygon(new(0, 0), new(10, 0), new(10, 10)),
        OpenCurve(new(0, 0), new(10, 5), new(20, 0)),
        ClosedCurve(new(0, 0), new(10, 0), new(5, 8))
    ];

    // 点 p0 を (x, y) に置き換えた 7 種の図形。座標の範囲の検証に使う
    public static IEnumerable<Shape> ShapesWithPoint(Point2 point) => [
        Line(point, new(1, 1)),
        Circle(point, new(1, 1)),
        Rectangle(point, new(1, 1)),
        Polyline(point, new(1, 1)),
        Polygon(point, new(1, 1), new(2, 0)),
        OpenCurve(point, new(1, 1)),
        ClosedCurve(point, new(1, 1), new(2, 0))
    ];

    // T-SHP-01
    [Theory]
    [MemberData(nameof(ValidShapes))]
    public void IsValid_IsTrueForValidShapes(Shape shape) => Assert.True(shape.IsValid());

    // T-SHP-01
    public static TheoryData<Shape> ShapesViolatingConstraints => [
        Line(new(3, 4), new(3, 4)),                                     // 2 点が一致
        Circle(new(3, 4), new(3, 4)),                                   // 半径 0
        Rectangle(new(0, 0), new(0, 5)),                                // 幅 0
        Rectangle(new(0, 0), new(5, 0)),                                // 高さ 0
        Rectangle(new(2, 2), new(2, 2)),                                // 幅・高さとも 0
        Polyline(new(0, 0), new(1, 1), new(1, 1), new(2, 0)),           // 隣り合う点が一致
        Polygon(new(0, 0), new(1, 1), new(2, 0), new(0, 0)),            // 最後と最初が一致
        Polygon(new(0, 0), new(1, 1)),                                  // 点数 3 未満
        Polygon(new(0, 0), new(0, 0), new(1, 1)),                       // 隣り合う点が一致
        OpenCurve(new(0, 0), new(5, 5), new(5, 5)),                     // 隣り合う点が一致
        OpenCurve(new Point2(0, 0)),                                           // 点数不足
        ClosedCurve(new(0, 0), new(5, 5), new(9, 0), new(0, 0)),        // 最後と最初が一致
        ClosedCurve(new(0, 0), new(5, 5)),                              // 点数不足
        ClosedCurve(new(0, 0), new(5, 5), new(5, 5))                    // 隣り合う点が一致
    ];

    // T-SHP-01
    [Theory]
    [MemberData(nameof(ShapesViolatingConstraints))]
    public void IsValid_IsFalseWhenConstraintIsViolated(Shape shape) => Assert.False(shape.IsValid());

    // T-SHP-02
    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 0.0)]
    [InlineData(1_000_000.5, 0.0)]
    [InlineData(0.0, -1_000_000.5)]
    public void IsValid_IsFalseForNonFiniteOrOutOfLimitCoordinate(double x, double y)
        => Assert.All(ShapesWithPoint(new(x, y)), shape => Assert.False(shape.IsValid(), shape.Kind.ToString()));

    // T-SHP-02、T-SHP-18（境界の上は有効）
    [Theory]
    [InlineData(limit, limit)]
    [InlineData(-limit, -limit)]
    [InlineData(limit, -limit)]
    public void IsValid_IsTrueForCoordinateOnTheLimit(double x, double y)
        => Assert.All(ShapesWithPoint(new(x, y)), shape => Assert.True(shape.IsValid(), shape.Kind.ToString()));

    // T-SHP-18
    [Theory]
    [InlineData(limit, limit)]
    [InlineData(-limit, limit)]
    [InlineData(999_999.0, -999_999.0)]
    public void DerivedValues_AreFiniteForShapesOnOrInsideTheLimit(double x, double y)
    {
        foreach (var shape in ShapesWithPoint(new(x, y)).Append(Rectangle(new(-limit, -limit), new(limit, limit)))) {
            var outline = shape.CreateOutline();
            var bounds = shape.Bounds;
            Assert.True(double.IsFinite(bounds.Left) && double.IsFinite(bounds.Bottom) && double.IsFinite(bounds.Right) && double.IsFinite(bounds.Top));
            Assert.All(OutlinePoints(outline), point => Assert.True(point.IsFinite, shape.Kind.ToString()));
        }
    }

    // 中間表現のすべての点（始点、区間の終点、ベジェの制御点）
    static IEnumerable<Point2> OutlinePoints(Outline outline)
        => outline.Figures.SelectMany(figure => figure.Segments.SelectMany(segment => segment switch {
            CubicBezierSegment bezier => new[] { bezier.Control1, bezier.Control2, bezier.End },
            _ => [segment.End]
        }).Prepend(figure.Start));

    // T-SHP-03
    [Theory]
    [InlineData(0.5, true)]
    [InlineData(50.0, true)]
    [InlineData(0.49, false)]
    [InlineData(50.01, false)]
    public void IsValid_ChecksAttributes(double thickness, bool expected)
    {
        var shape = Line(new(0, 0), new(1, 1)) with { Attributes = attributes with { Thickness = thickness } };

        Assert.Equal(expected, shape.IsValid());
    }

    // T-SHP-04
    [Fact]
    public void HandlePoints_AreDefinitionPointsOrRectangleCorners()
    {
        Assert.Equal([new(0, 0), new(10, 5)], Line(new(0, 0), new(10, 5)).HandlePoints);
        Assert.Equal([new(5, 5), new(8, 9)], Circle(new(5, 5), new(8, 9)).HandlePoints);
        Assert.Equal([new(0, 0), new(10, 0), new(10, 5), new(0, 5)], Rectangle(new(0, 0), new(10, 5)).HandlePoints);
        Point2[] points = [new(0, 0), new(10, 0), new(10, 10), new(0, 20)];
        Assert.Equal(points, Polyline(points).HandlePoints);
        Assert.Equal(points, Polygon(points).HandlePoints);
        Assert.Equal(points, OpenCurve(points).HandlePoints);
        Assert.Equal(points, ClosedCurve(points).HandlePoints);
    }

    // T-SHP-05
    [Fact]
    public void MoveHandle_Line_MovesOnlyThatEndpoint()
    {
        var line = Line(new(0, 0), new(10, 5));

        Assert.Equal([new(-3, 2), new(10, 5)], line.MoveHandle(0, new(-3, 2)).HandlePoints);
        Assert.Equal([new(0, 0), new(7, 7)], line.MoveHandle(1, new(7, 7)).HandlePoints);
    }

    // T-SHP-06
    [Fact]
    public void MoveHandle_CircleCenter_TranslatesWholeCircleKeepingRadius()
    {
        var circle = Circle(new(5, 5), new(8, 9));

        var moved = Assert.IsType<CircleShape>(circle.MoveHandle(0, new(15, 25)));

        Assert.Equal(new Point2(15, 25), moved.Center);
        Assert.Equal(new Point2(18, 29), moved.PointOnCircle);
        Assert.Equal(circle.Radius, moved.Radius, 1e-9);
    }

    // T-SHP-06
    [Fact]
    public void MoveHandle_CirclePointOnCircle_ChangesRadius()
    {
        var moved = Assert.IsType<CircleShape>(Circle(new(5, 5), new(8, 9)).MoveHandle(1, new(5, 15)));

        Assert.Equal(new Point2(5, 5), moved.Center);
        Assert.Equal(10.0, moved.Radius, 1e-9);
    }

    // T-SHP-07：角 i を動かすと、対角 (i + 2) % 4 は固定され、添字 i の角が動かした位置になる（辺は軸に平行のまま）
    [Theory]
    [InlineData(0, 3.0, 2.0)]
    [InlineData(1, 12.0, -4.0)]
    [InlineData(2, 20.0, 9.0)]
    [InlineData(3, -6.0, 8.0)]
    [InlineData(0, 15.0, 9.0)]      // 対角を越える
    [InlineData(1, -5.0, 9.0)]      // 対角を越える
    public void MoveHandle_RectangleCorner_MovesCornerKeepingOppositeFixed(int index, double x, double y)
    {
        var rectangle = Rectangle(new(0, 0), new(10, 5));
        var opposite = rectangle.HandlePoints[(index + 2) % 4];

        var moved = rectangle.MoveHandle(index, new(x, y));

        Assert.Equal(new Point2(x, y), moved.HandlePoints[index]);
        Assert.Equal(opposite, moved.HandlePoints[(index + 2) % 4]);
        AssertAxisAligned(moved.HandlePoints);
    }

    static void AssertAxisAligned(IReadOnlyList<Point2> corners)
    {
        Assert.Equal(corners[0].Y, corners[1].Y);
        Assert.Equal(corners[1].X, corners[2].X);
        Assert.Equal(corners[2].Y, corners[3].Y);
        Assert.Equal(corners[3].X, corners[0].X);
    }

    // T-SHP-08
    public static TheoryData<Shape> PointListShapes => [
        Polyline(new(0, 0), new(10, 0), new(10, 10)),
        Polygon(new(0, 0), new(10, 0), new(10, 10)),
        OpenCurve(new(0, 0), new(10, 0), new(10, 10)),
        ClosedCurve(new(0, 0), new(10, 0), new(10, 10))
    ];

    // T-SHP-08
    [Theory]
    [MemberData(nameof(PointListShapes))]
    public void MoveHandle_PointList_MovesOnlyThatPointAndKeepsKind(Shape shape)
    {
        var moved = shape.MoveHandle(1, new(20, -5));

        Assert.Equal([new(0, 0), new(20, -5), new(10, 10)], moved.HandlePoints);
        Assert.Equal(shape.GetType(), moved.GetType());
        Assert.Equal(shape.Id, moved.Id);
    }

    [Theory]
    [MemberData(nameof(ValidShapes))]
    public void MoveHandle_WithIndexOutOfRange_IsRejected(Shape shape)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => shape.MoveHandle(-1, new(0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => shape.MoveHandle(shape.HandlePoints.Count, new(0, 0)));
    }

    // T-SHP-09
    [Fact]
    public void MoveHandle_ResultViolatingConstraint_IsInvalid()
    {
        Assert.False(Line(new(0, 0), new(10, 5)).MoveHandle(0, new(10, 5)).IsValid());
        Assert.False(Circle(new(5, 5), new(8, 9)).MoveHandle(1, new(5, 5)).IsValid());
        Assert.False(Rectangle(new(0, 0), new(10, 5)).MoveHandle(0, new(10, 3)).IsValid());
        Assert.False(Polyline(new(0, 0), new(10, 0), new(10, 10)).MoveHandle(1, new(10, 10)).IsValid());
        Assert.False(Polygon(new(0, 0), new(10, 0), new(10, 10)).MoveHandle(2, new(0, 0)).IsValid());
        Assert.False(Line(new(0, 0), new(10, 5)).MoveHandle(1, new(limit * 2, 0)).IsValid());
    }

    // T-SHP-10
    [Theory]
    [MemberData(nameof(ValidShapes))]
    public void Translate_MovesEveryPointKeepingAttributesAndId(Shape shape)
    {
        var delta = new Vector2(12.5, -7);

        var moved = shape.Translate(delta);

        Assert.Equal(shape.HandlePoints.Select(point => point + delta), moved.HandlePoints);
        Assert.Equal(shape.Id, moved.Id);
        Assert.Equal(shape.Attributes, moved.Attributes);
        Assert.Equal(shape.GetType(), moved.GetType());
        Assert.Equal(shape.HandlePoints, moved.Translate(new Vector2(-12.5, 7)).HandlePoints);
    }

    // T-SHP-11
    [Theory]
    [MemberData(nameof(ValidShapes))]
    public void ReplacingAttributes_KeepsIdAndPoints(Shape shape)
    {
        var newAttributes = new ShapeAttributes(new RgbColor(255, 0, 0), 3.5, LineStyle.DashDot);

        var changed = shape with { Attributes = newAttributes };

        Assert.Equal(newAttributes, changed.Attributes);
        Assert.Equal(shape.Id, changed.Id);
        Assert.Equal(shape.HandlePoints, changed.HandlePoints);
    }

    // T-SHP-12
    [Fact]
    public void Bounds_AreExpectedForEachKind()
    {
        Assert.Equal(new Rect2(-2, 1, 10, 5), Line(new(10, 1), new(-2, 5)).Bounds);
        AssertNear(new Rect2(0, 5, 10, 15), Circle(new(5, 10), new(5, 15)).Bounds, 1e-9);
        Assert.Equal(new Rect2(0, -5, 10, 0), Rectangle(new(10, -5), new(0, 0)).Bounds);
        Assert.Equal(new Rect2(0, 0, 10, 10), Polyline(new(0, 0), new(10, 0), new(10, 10)).Bounds);
        Assert.Equal(new Rect2(0, 0, 10, 10), Polygon(new(0, 0), new(10, 0), new(10, 10)).Bounds);
        AssertNear(new Rect2(0, 0, 10, 0), OpenCurve(new(0, 0), new(4, 0), new(10, 0)).Bounds, 1e-9);
    }

    // T-SHP-12（閉じた曲線は通過点の外へ膨らみ、外接矩形は通過点の矩形を含む）
    [Fact]
    public void Bounds_OfClosedCurve_ContainsPassingPointsAndBulge()
    {
        var bounds = ClosedCurve(new(0, 0), new(10, 0), new(10, 10), new(0, 10)).Bounds;

        Assert.True(bounds.Left < 0 && bounds.Bottom < 0 && bounds.Right > 10 && bounds.Top > 10);
    }

    static void AssertNear(Rect2 expected, Rect2 actual, double precision)
    {
        Assert.Equal(expected.Left, actual.Left, precision);
        Assert.Equal(expected.Bottom, actual.Bottom, precision);
        Assert.Equal(expected.Right, actual.Right, precision);
        Assert.Equal(expected.Top, actual.Top, precision);
    }

    // T-SHP-13
    [Fact]
    public void CreateOutline_HasExpectedFigureAndSegments()
    {
        AssertFigure(Line(new(0, 0), new(1, 1)), isClosed: false, segmentCount: 1, typeof(LineSegment));
        AssertFigure(Rectangle(new(0, 0), new(10, 5)), isClosed: true, segmentCount: 3, typeof(LineSegment));
        AssertFigure(Circle(new(0, 0), new(1, 0)), isClosed: true, segmentCount: 4, typeof(CubicBezierSegment));
        AssertFigure(Polyline(new(0, 0), new(1, 1), new(2, 0)), isClosed: false, segmentCount: 2, typeof(LineSegment));
        AssertFigure(Polygon(new(0, 0), new(1, 1), new(2, 0)), isClosed: true, segmentCount: 2, typeof(LineSegment));
        AssertFigure(OpenCurve(new(0, 0), new(1, 1), new(2, 0)), isClosed: false, segmentCount: 2, typeof(CubicBezierSegment));
        AssertFigure(ClosedCurve(new(0, 0), new(1, 1), new(2, 0)), isClosed: true, segmentCount: 3, typeof(CubicBezierSegment));
    }

    // 閉じた折れ線（矩形・多角形）の最後の辺は、OutlineFigure.IsClosed で閉じる
    static void AssertFigure(Shape shape, bool isClosed, int segmentCount, Type segmentType)
    {
        var figure = Assert.Single(shape.CreateOutline().Figures);
        Assert.Equal(isClosed, figure.IsClosed);
        Assert.Equal(segmentCount, figure.Segments.Count);
        Assert.All(figure.Segments, segment => Assert.IsType(segmentType, segment));
    }

    // 入力中の図形（最小点数に満たない曲線）も表示できる：スプラインを作れない点数では折れ線で表す
    [Fact]
    public void CreateOutline_OfCurveBelowSplineMinimum_IsPolyline()
    {
        AssertFigure(OpenCurve(new Point2(0, 0)), isClosed: false, segmentCount: 0, typeof(LineSegment));
        AssertFigure(ClosedCurve(new(0, 0), new(5, 5)), isClosed: true, segmentCount: 1, typeof(LineSegment));
    }

    // T-SHP-16
    [Fact]
    public void SatisfiesConstraints_JudgesConstraintsIndependentlyOfMinimumPoints()
    {
        Assert.True(Polygon(new(0, 0), new(1, 1)).SatisfiesConstraints());
        Assert.False(Polygon(new(0, 0), new(1, 1)).IsValid());
        Assert.False(Polygon(new(0, 0), new(0, 0)).SatisfiesConstraints());
        Assert.True(ClosedCurve(new(0, 0), new(1, 1)).SatisfiesConstraints());
        Assert.False(ClosedCurve(new(1, 1), new(1, 1)).SatisfiesConstraints());
    }

    // T-SHP-16（点の一致は完全一致）
    [Fact]
    public void SatisfiesConstraints_TreatsOnlyExactlyEqualPointsAsCoincident()
    {
        var almost = new Point2(1 + 1e-12, 1);

        Assert.False(Polyline(new(1, 1), new(1, 1)).SatisfiesConstraints());
        Assert.True(Polyline(new(1, 1), almost).SatisfiesConstraints());
        Assert.False(Line(new(1, 1), new(1, 1)).SatisfiesConstraints());
        Assert.True(Line(new(1, 1), almost).SatisfiesConstraints());
        Assert.True(Rectangle(new(1, 1), new(1 + 1e-12, 1 + 1e-12)).SatisfiesConstraints());
    }

    [Theory]
    [MemberData(nameof(ValidShapes))]
    public void Kind_MatchesShapeType(Shape shape)
        => Assert.Equal(shape.GetType().Name, shape.Kind + "Shape");

    [Fact]
    public void ShapeId_New_IsUniqueAndValueEquatable()
    {
        var id = ShapeId.New();

        Assert.NotEqual(id, ShapeId.New());
        Assert.Equal(id, new ShapeId(id.Value));
    }
}
