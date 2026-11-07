namespace Shos.WpfCadSample.Models;

/// <summary>
/// ツールの種類：選択（Select）と 7 つの描画ツール（仕様 4.2）。
/// <see cref="ShapeKindInfo.FromTool"/> が使うため、Controllers ではなく Models に置く（設計書 5.1）。
/// </summary>
public enum Tool
{
    Select,
    Line,
    Circle,
    Rectangle,
    Polyline,
    Polygon,
    OpenCurve,
    ClosedCurve
}
