namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// クリックした位置にある図形・ハンドルの判定（仕様 5.3、A-9、A-17）。
/// 許容範囲 tolerance は論理座標で受け取る（画面上の px ÷ Scale を呼び出し側が計算する）。
/// </summary>
public static class ShapeHitTester
{
    // 曲線を折れ線へ近似する許容誤差の、ヒットテストの許容範囲に対する比（約 5 px に対して 0.25 px。設計書 DA-2）
    const double flatteningRatio = 0.05;

    /// <summary>
    /// 中心線からの距離が「線の太さ ÷ 2 + <paramref name="tolerance"/>」以内の図形のうち、最も上（後から追加した）図形の添字。
    /// なければ −1。図形の内部は線上ではない（設計書 DA-7）。
    /// </summary>
    public static int HitShape(IReadOnlyList<Shape> shapes, Point2 point, double tolerance)
    {
        for (var index = shapes.Count - 1; index >= 0; index--) {
            if (IsOnLine(shapes[index], point, tolerance))
                return index;
        }
        return -1;
    }

    /// <summary>
    /// <paramref name="tolerance"/> 以内のハンドルのうち、最も近いものの添字。なければ −1。
    /// </summary>
    public static int HitHandle(Shape shape, Point2 point, double tolerance)
    {
        var nearest = shape.HandlePoints
                           .Select((handle, index) => (Index: index, Distance: handle.DistanceTo(point)))
                           .Where(candidate => candidate.Distance <= tolerance)
                           .OrderBy(candidate => candidate.Distance)
                           .FirstOrDefault((Index: -1, Distance: 0.0));  // 候補がなければ添字 −1
        return nearest.Index;
    }

    // 曲線の近似の誤差は許容範囲の 5% にとどまるため、判定の結果にほぼ影響しない
    static bool IsOnLine(Shape shape, Point2 point, double tolerance)
        => shape.CreateOutline().DistanceTo(point, tolerance * flatteningRatio) <= shape.Attributes.Thickness / 2 + tolerance;
}
