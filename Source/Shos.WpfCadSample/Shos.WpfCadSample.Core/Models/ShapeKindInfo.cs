using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 図形の種類ごとの入力規則（設計書 5.1）。制約の知識は持たない（制約は <see cref="Shape.SatisfiesConstraints"/>）。
/// </summary>
public static class ShapeKindInfo
{
    /// <summary>
    /// 描画ツールに対応する図形の種類。Select は図形を描かないため null。<see cref="Tool"/> と種類の対応は、ここだけに置く。
    /// </summary>
    public static ShapeKind? FromTool(Tool tool) => tool switch {
        Tool.Select => null,
        Tool.Line => ShapeKind.Line,
        Tool.Circle => ShapeKind.Circle,
        Tool.Rectangle => ShapeKind.Rectangle,
        Tool.Polyline => ShapeKind.Polyline,
        Tool.Polygon => ShapeKind.Polygon,
        Tool.OpenCurve => ShapeKind.OpenCurve,
        Tool.ClosedCurve => ShapeKind.ClosedCurve,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool.")
    };

    /// <summary>
    /// 最小点数（仕様 5.2、A-5）。
    /// </summary>
    public static int MinPoints(ShapeKind kind) => kind switch {
        ShapeKind.Polygon or ShapeKind.ClosedCurve => 3,
        ShapeKind.Line or ShapeKind.Circle or ShapeKind.Rectangle or ShapeKind.Polyline or ShapeKind.OpenCurve => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown shape kind.")
    };

    /// <summary>
    /// 2 点目のクリックで確定する種類か（線分・円・矩形）。ほかはダブルクリックで確定する（仕様 5.2）。
    /// </summary>
    public static bool IsFixedTwoPoint(ShapeKind kind) => kind is ShapeKind.Line or ShapeKind.Circle or ShapeKind.Rectangle;

    /// <summary>
    /// 点列と属性から、新しい <see cref="ShapeId"/> の図形を作る。2 点で確定する種類は、点がちょうど 2 つ必要。検証はしない。
    /// </summary>
    public static Shape Create(ShapeKind kind, IReadOnlyList<Point2> points, ShapeAttributes attributes)
    {
        if (IsFixedTwoPoint(kind) && points.Count != 2)
            throw new ArgumentException($"A {kind} needs exactly two points.", nameof(points));

        var id = ShapeId.New();
        return kind switch {
            ShapeKind.Line => new LineShape(id, attributes, points[0], points[1]),
            ShapeKind.Circle => new CircleShape(id, attributes, points[0], points[1]),
            ShapeKind.Rectangle => new RectangleShape(id, attributes, points[0], points[1]),
            ShapeKind.Polyline => new PolylineShape(id, attributes, [.. points]),
            ShapeKind.Polygon => new PolygonShape(id, attributes, [.. points]),
            ShapeKind.OpenCurve => new OpenCurveShape(id, attributes, [.. points]),
            ShapeKind.ClosedCurve => new ClosedCurveShape(id, attributes, [.. points]),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown shape kind.")
        };
    }

    /// <summary>
    /// 入力中の点列 <paramref name="points"/> に <paramref name="point"/> を加えられるか（仕様 5.2）。
    /// 1 点目は座標の範囲だけを確かめ、2 点目以降は、加えた候補の図形の制約で判定する（読み込み時と同じ判定）。
    /// </summary>
    public static bool CanAppend(ShapeKind kind, IReadOnlyList<Point2> points, Point2 point)
    {
        if (points.Count == 0)
            return point.IsWithinLimits();
        if (IsFixedTwoPoint(kind) && points.Count >= 2)
            return false;

        return Create(kind, [.. points, point], ShapeAttributes.Default).SatisfiesConstraints();
    }
}
