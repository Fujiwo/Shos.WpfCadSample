using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 描画ツールの入力：クリックで点を加え、2 点目（線分・円・矩形）またはダブルクリックで確定する（仕様 5.2、設計書 5.3）。
/// </summary>
sealed class DrawInteraction(Drawing drawing, ShapeKind kind, Func<ShapeAttributes> defaultAttributes, Action<string> report) : IInteraction
{
    readonly List<Point2> points = [];   // クリックで確定した点（論理座標）。マウス位置は含めない

    /// <summary>
    /// 入力中の図形（マウス位置までを含むプレビュー）。入力中でなければ null。
    /// </summary>
    public Shape? InputShape { get; private set; }

    /// <summary>
    /// 入力中か（1 点目をクリックしてから、確定または取り消しまで）。
    /// </summary>
    public bool IsActive => points.Count > 0;

    // Up は使わない。CaptureLost は無視し、入力中の点を保つ（設計書 5.3）
    public void Handle(PointerInput input)
    {
        switch (input.Action) {
            // 新たな確定にしない：2 点図形は 2 回目のクリックで確定済み。入力中でなければ、直前のダブルクリックで確定済み（3 回目のクリック）
            case PointerAction.Down when input.ClickCount >= 2 && (ShapeKindInfo.IsFixedTwoPoint(kind) || !IsActive):
                break;
            case PointerAction.Down when input.ClickCount >= 2:
                Confirm();      // ダブルクリックの 2 回目は点を加えない（仕様 5.2）
                break;
            case PointerAction.Down:
                Append(input.Point);
                break;
            case PointerAction.Move when IsActive:
                InputShape = Preview(input.Point);
                break;
        }
    }

    public void Cancel()
    {
        points.Clear();
        InputShape = null;
    }

    // 加えると図形が不正になる点は加えず、理由（座標の範囲外か、それ以外か）を状態表示に出す
    void Append(Point2 point)
    {
        if (!ShapeKindInfo.CanAppend(kind, points, point)) {
            report(point.IsWithinLimits() ? StatusMessages.PointRejected : StatusMessages.OutOfRange);
            return;
        }
        points.Add(point);
        if (ShapeKindInfo.IsFixedTwoPoint(kind) && points.Count == 2)
            Confirm();
        else
            InputShape = Preview(point);
    }

    // 確定した点にマウス位置を加えた、表示だけの図形（記録しない）
    Shape Preview(Point2 cursor) => ShapeKindInfo.Create(kind, [.. points, cursor], defaultAttributes());

    // 最小点数に満たなければ、追加せずに取り消す（仕様 5.2）
    void Confirm()
    {
        if (points.Count < ShapeKindInfo.MinPoints(kind)) {
            Cancel();
            report(StatusMessages.TooFewPoints(kind));
            return;
        }
        var shape = ShapeKindInfo.Create(kind, points, defaultAttributes());
        Cancel();       // 追加の通知（Committed）の時点で、入力中でない状態にしておく
        if (shape.IsValid())
            drawing.Add(shape);
        else
            report(StatusMessages.InvalidShape);
    }
}
