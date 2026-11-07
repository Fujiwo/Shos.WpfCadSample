using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 状態表示の文言（英語。仕様 D-2、A-12）。
/// </summary>
static class StatusMessages
{
    public const string OutOfRange = "The point is outside the allowed coordinate range (±1,000,000).";
    public const string PointRejected = "The point cannot be added here because the shape would be invalid.";
    public const string InvalidShape = "The shape is invalid and was not added.";

    public static string Guidance(Tool tool) => tool switch {
        Tool.Select => "Click a shape to select it. Drag a shape or a handle to edit it.",
        Tool.Line => "Line: click the start point, then the end point. Press Esc to cancel.",
        Tool.Circle => "Circle: click the center, then a point on the circle. Press Esc to cancel.",
        Tool.Rectangle => "Rectangle: click a corner, then the opposite corner. Press Esc to cancel.",
        Tool.Polyline => "Polyline: click to add points, and double-click to finish. Press Esc to cancel.",
        Tool.Polygon => "Polygon: click to add vertices, and double-click to finish. Press Esc to cancel.",
        Tool.OpenCurve => "Open curve: click to add passing points, and double-click to finish. Press Esc to cancel.",
        Tool.ClosedCurve => "Closed curve: click to add passing points, and double-click to finish. Press Esc to cancel.",
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool.")
    };

    public static string TooFewPoints(ShapeKind kind)
        => $"A {Name(kind)} needs at least {ShapeKindInfo.MinPoints(kind)} points. The input was canceled.";

    static string Name(ShapeKind kind) => kind switch {
        ShapeKind.OpenCurve => "open curve",
        ShapeKind.ClosedCurve => "closed curve",
        _ => kind.ToString().ToLowerInvariant()
    };
}
