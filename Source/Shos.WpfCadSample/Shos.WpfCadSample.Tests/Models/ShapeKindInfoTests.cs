using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models;

/// <summary>
/// T-SHP-14、15、17：<see cref="ShapeKindInfo"/>（テスト計画書 4.5）
/// </summary>
public class ShapeKindInfoTests
{
    public static TheoryData<ShapeKind> AllKinds => [.. Enum.GetValues<ShapeKind>()];

    // T-SHP-14
    [Theory]
    [InlineData(ShapeKind.Line, 2, true)]
    [InlineData(ShapeKind.Circle, 2, true)]
    [InlineData(ShapeKind.Rectangle, 2, true)]
    [InlineData(ShapeKind.Polyline, 2, false)]
    [InlineData(ShapeKind.Polygon, 3, false)]
    [InlineData(ShapeKind.OpenCurve, 2, false)]
    [InlineData(ShapeKind.ClosedCurve, 3, false)]
    public void MinPointsAndFixedTwoPoint_FollowInputRules(ShapeKind kind, int minPoints, bool isFixedTwoPoint)
    {
        Assert.Equal(minPoints, ShapeKindInfo.MinPoints(kind));
        Assert.Equal(isFixedTwoPoint, ShapeKindInfo.IsFixedTwoPoint(kind));
    }

    // T-SHP-14
    [Theory]
    [MemberData(nameof(AllKinds))]
    public void CanAppend_FirstPoint_OnlyRequiresCoordinateWithinLimits(ShapeKind kind)
    {
        Assert.True(ShapeKindInfo.CanAppend(kind, [], new(Point2.CoordinateLimit, -Point2.CoordinateLimit)));
        Assert.False(ShapeKindInfo.CanAppend(kind, [], new(Math.BitIncrement(Point2.CoordinateLimit), 0)));
        Assert.False(ShapeKindInfo.CanAppend(kind, [], new(double.NaN, 0)));
    }

    // T-SHP-14
    [Theory]
    [MemberData(nameof(AllKinds))]
    public void CanAppend_RejectsSamePointAsPreviousAndOutOfLimitPoint(ShapeKind kind)
    {
        Point2[] points = [new(0, 0)];

        Assert.False(ShapeKindInfo.CanAppend(kind, points, new(0, 0)));
        Assert.False(ShapeKindInfo.CanAppend(kind, points, new(0, 2e6)));
        Assert.True(ShapeKindInfo.CanAppend(kind, points, new(3, 4)));
    }

    // T-SHP-14
    [Theory]
    [InlineData(0.0, 5.0)]  // 同じ X
    [InlineData(5.0, 0.0)]  // 同じ Y
    public void CanAppend_Rectangle_RejectsZeroWidthOrHeight(double x, double y)
        => Assert.False(ShapeKindInfo.CanAppend(ShapeKind.Rectangle, [new(0, 0)], new(x, y)));

    // T-SHP-14
    [Theory]
    [InlineData(ShapeKind.Polygon, false)]
    [InlineData(ShapeKind.ClosedCurve, false)]
    [InlineData(ShapeKind.Polyline, true)]
    [InlineData(ShapeKind.OpenCurve, true)]
    public void CanAppend_PointClosingOntoFirstPoint_IsRejectedOnlyForClosedKinds(ShapeKind kind, bool expected)
        => Assert.Equal(expected, ShapeKindInfo.CanAppend(kind, [new(0, 0), new(5, 5)], new(0, 0)));

    // 2 点で確定する種類は、3 点目を受け付けない
    [Theory]
    [InlineData(ShapeKind.Line)]
    [InlineData(ShapeKind.Circle)]
    [InlineData(ShapeKind.Rectangle)]
    public void CanAppend_FixedTwoPointKind_RejectsThirdPoint(ShapeKind kind)
        => Assert.False(ShapeKindInfo.CanAppend(kind, [new(0, 0), new(5, 5)], new(9, 1)));

    // T-SHP-15
    [Theory]
    [InlineData(ShapeKind.Line, typeof(LineShape))]
    [InlineData(ShapeKind.Circle, typeof(CircleShape))]
    [InlineData(ShapeKind.Rectangle, typeof(RectangleShape))]
    [InlineData(ShapeKind.Polyline, typeof(PolylineShape))]
    [InlineData(ShapeKind.Polygon, typeof(PolygonShape))]
    [InlineData(ShapeKind.OpenCurve, typeof(OpenCurveShape))]
    [InlineData(ShapeKind.ClosedCurve, typeof(ClosedCurveShape))]
    public void Create_MakesShapeOfKindWithPointsAndAttributes(ShapeKind kind, Type expectedType)
    {
        Point2[] points = ShapeKindInfo.IsFixedTwoPoint(kind) ? [new(1, 2), new(7, 9)] : [new(1, 2), new(7, 9), new(12, 3)];
        var attributes = new ShapeAttributes(new RgbColor(10, 20, 30), 2.5, LineStyle.Dot);

        var shape = ShapeKindInfo.Create(kind, points, attributes);

        Assert.IsType(expectedType, shape);
        Assert.Equal(kind, shape.Kind);
        Assert.Equal(attributes, shape.Attributes);
        Assert.True(shape.IsValid());
        Assert.Equal(points[0], shape.HandlePoints[0]);
        Assert.Contains(points[1], shape.HandlePoints);
    }

    [Fact]
    public void Create_GivesNewIdEachTime()
    {
        Point2[] points = [new(0, 0), new(1, 1)];

        Assert.NotEqual(ShapeKindInfo.Create(ShapeKind.Line, points, ShapeAttributes.Default).Id,
                        ShapeKindInfo.Create(ShapeKind.Line, points, ShapeAttributes.Default).Id);
    }

    [Theory]
    [InlineData(ShapeKind.Line)]
    [InlineData(ShapeKind.Circle)]
    [InlineData(ShapeKind.Rectangle)]
    public void Create_FixedTwoPointKindWithOtherPointCount_IsRejected(ShapeKind kind)
        => Assert.Throws<ArgumentException>(() => ShapeKindInfo.Create(kind, [new(0, 0), new(1, 1), new(2, 0)], ShapeAttributes.Default));

    // T-SHP-17：入力時（CanAppend）と読み込み時（IsValid / SatisfiesConstraints）の判定が一致する
    [Theory]
    [MemberData(nameof(AllKinds))]
    public void CanAppend_AgreesWithShapeValidation(ShapeKind kind)
    {
        // 直前の点・最初の点・同じ X や Y・範囲外を含む、決定的な候補点の列
        Point2[] candidates = [
            new(0, 0), new(0, 0), new(10, 0), new(10, 0), new(10, 10), new(0, 0), new(0, 10),
            new(5, 2e6), new(20, 15), new(20, 15), new(-5, 30), new(0, 0), new(15, -10)
        ];
        var accepted = new List<Point2>();

        foreach (var candidate in candidates) {
            if (ShapeKindInfo.IsFixedTwoPoint(kind) && accepted.Count == 2)
                break;
            if (ShapeKindInfo.CanAppend(kind, accepted, candidate)) {
                accepted.Add(candidate);
                if (accepted.Count >= ShapeKindInfo.MinPoints(kind))
                    Assert.True(ShapeKindInfo.Create(kind, accepted, ShapeAttributes.Default).IsValid());
            } else if (accepted.Count > 0) {
                Assert.False(ShapeKindInfo.Create(kind, [.. accepted, candidate], ShapeAttributes.Default).SatisfiesConstraints());
            }
        }
        Assert.True(accepted.Count >= ShapeKindInfo.MinPoints(kind));
    }

    // T-SHP-17
    [Theory]
    [InlineData(Tool.Line, ShapeKind.Line)]
    [InlineData(Tool.Circle, ShapeKind.Circle)]
    [InlineData(Tool.Rectangle, ShapeKind.Rectangle)]
    [InlineData(Tool.Polyline, ShapeKind.Polyline)]
    [InlineData(Tool.Polygon, ShapeKind.Polygon)]
    [InlineData(Tool.OpenCurve, ShapeKind.OpenCurve)]
    [InlineData(Tool.ClosedCurve, ShapeKind.ClosedCurve)]
    public void FromTool_MapsDrawingToolToKind(Tool tool, ShapeKind expected)
        => Assert.Equal(expected, ShapeKindInfo.FromTool(tool));

    // T-SHP-17
    [Fact]
    public void FromTool_Select_HasNoKind() => Assert.Null(ShapeKindInfo.FromTool(Tool.Select));
}
