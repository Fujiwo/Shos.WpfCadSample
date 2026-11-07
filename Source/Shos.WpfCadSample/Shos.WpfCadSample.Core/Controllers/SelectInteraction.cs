using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// Select ツールの入力：クリックでの選択と、ハンドル・図形全体のドラッグ（仕様 5.3、設計書 5.3、6.2）。
/// ドラッグ中は記録せずに表示だけを更新し、マウスを離したときに 1 回だけ記録する（設計書 DD-3）。
/// </summary>
sealed class SelectInteraction(Drawing drawing, Func<ShapeId?> selectedId, Action<ShapeId?> select) : IInteraction
{
    // 画面上の大きさ（仕様 A-9）
    const double handleTolerancePixels = 6;
    const double lineTolerancePixels = 5;
    const double dragThresholdPixels = 3;

    Drag? drag;

    /// <summary>
    /// ドラッグ中か（図形の上でボタンを押してから離すまで）。
    /// </summary>
    public bool IsActive => drag is not null;

    public void Handle(PointerInput input)
    {
        switch (input.Action) {
            case PointerAction.Down:
                Begin(input);
                break;
            case PointerAction.Move:
                Continue(input);
                break;
            case PointerAction.Up:
                Finish();
                break;
            case PointerAction.CaptureLost:
                Cancel();       // 操作が途切れたら、Esc と同じく取り消す（仕様 A-26）
                break;
        }
    }

    /// <summary>
    /// ドラッグを取り消し、図形を元に戻す。履歴は増えない。
    /// </summary>
    public void Cancel()
    {
        if (drag is { } current && current.HasChanged)
            drawing.RestoreOriginal(current.Index, current.Original);
        drag = null;
    }

    // 判定の順序：選択中の図形のハンドル → 図形の線（上から順）→ 何もなければ選択の解除（仕様 5.3、A-9）
    void Begin(PointerInput input)
    {
        Cancel();
        var index = selectedId() is { } id ? drawing.IndexOf(id) : -1;
        var handleIndex = index >= 0 ? ShapeHitTester.HitHandle(drawing.Shapes[index], input.Point, input.Pixels(handleTolerancePixels)) : -1;
        if (handleIndex < 0) {
            index = ShapeHitTester.HitShape(drawing.Shapes, input.Point, input.Pixels(lineTolerancePixels));
            select(index >= 0 ? drawing.Shapes[index].Id : null);   // 未選択の図形なら選択して、そのまま移動する（仕様 A-4）
        }
        drag = index >= 0 ? new Drag(index, drawing.Shapes[index], handleIndex, input.Point) : null;
    }

    // 約 3 px 動くまではクリックとして扱う。制約に違反する位置へは動かさず、直前の有効な状態を保つ（仕様 A-23）
    void Continue(PointerInput input)
    {
        if (drag is null || (!drag.HasMoved && input.Point.DistanceTo(drag.Start) < input.Pixels(dragThresholdPixels)))
            return;

        drag.HasMoved = true;
        var candidate = drag.Moved(input.Point - drag.Start);
        if (!candidate.IsValid())
            return;
        drawing.PreviewReplace(drag.Index, candidate);
        drag.Current = candidate;
    }

    // 元の図形へ戻してから、最終の図形を 1 回だけ記録する（R-7）。
    // 記録の通知（Committed）の時点でドラッグが終わっているよう、先にドラッグを終える（全体図の更新はドラッグ中にしないため）
    void Finish()
    {
        var finished = drag;
        drag = null;
        if (finished is { HasChanged: true }) {
            drawing.RestoreOriginal(finished.Index, finished.Original);
            drawing.Replace(finished.Index, finished.Current);
        }
    }

    sealed class Drag(int index, Shape original, int handleIndex, Point2 start)
    {
        public int Index { get; } = index;
        public Shape Original { get; } = original;
        public Point2 Start { get; } = start;
        public Shape Current { get; set; } = original;
        public bool HasMoved { get; set; }
        public bool HasChanged => !ReferenceEquals(Current, Original);

        // ハンドルのドラッグは、つかんだ位置とハンドルのずれを保って動かす。ハンドル以外は図形全体を動かす
        public Shape Moved(Vector2 delta)
            => handleIndex >= 0 ? Original.MoveHandle(handleIndex, Original.HandlePoints[handleIndex] + delta) : Original.Translate(delta);
    }
}
