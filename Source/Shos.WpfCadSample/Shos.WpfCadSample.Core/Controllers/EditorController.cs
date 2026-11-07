using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 現在のツールに応じて入力を解釈し、図形の編集を実行する（設計書 5.3）。
/// 状態の変化は <see cref="StateChanged"/>、ドキュメントの確定的な変更は <see cref="Committed"/> で通知する。
/// </summary>
public sealed class EditorController
{
    const double pasteOffset = 10;   // 貼り付けのずらし量 (X + 10, Y − 10)（仕様 A-18）

    readonly Drawing drawing;
    readonly Viewport viewport;
    readonly SelectInteraction selectInteraction;
    readonly AttributeEditor attributeEditor;
    DrawInteraction? drawInteraction;
    ShapeAttributes defaultAttributes = ShapeAttributes.Default;

    public EditorController(Drawing drawing, Viewport viewport)
    {
        this.drawing = drawing;
        this.viewport = viewport;
        selectInteraction = new(drawing, () => SelectedId, Select);
        attributeEditor = new(drawing);
        drawing.Committed += OnDrawingCommitted;   // Drawing.Committed を購読するのは、このクラスだけ（設計書 5.3）
        StatusMessage = StatusMessages.Guidance(Tool);
    }

    public Tool Tool { get; private set; } = Tool.Select;

    public ShapeId? SelectedId { get; private set; }

    /// <summary>
    /// 入力中の図形（プレビュー）。入力中でなければ null。
    /// </summary>
    public Shape? InputShape => drawInteraction?.InputShape;

    /// <summary>
    /// 状態表示の文言（英語）：ツールの操作案内、または直前の操作を受け付けなかった理由。
    /// </summary>
    public string StatusMessage { get; private set; }

    public bool IsInputInProgress => drawInteraction?.IsActive == true;

    public bool IsDragging => selectInteraction.IsActive;

    /// <summary>
    /// 新しい図形に適用する属性（仕様 5.4）。
    /// </summary>
    public ShapeAttributes DefaultAttributes {
        get => defaultAttributes;
        set {
            if (!value.IsValid())
                throw new ArgumentException("The default attributes are invalid.", nameof(value));
            defaultAttributes = value;
        }
    }

    public event EventHandler? StateChanged;

    /// <summary>
    /// ドキュメントの確定的な変更の後、選択の補正（仕様 5.5）を終えてから発生する。ViewModels は、こちらだけを購読する。
    /// </summary>
    public event EventHandler? Committed;

    // 入力中・ドラッグ中は、Undo / Redo / Paste などを実行しない（仕様 5.10）
    bool IsBusy => IsInputInProgress || IsDragging;

    IInteraction CurrentInteraction => drawInteraction ?? (IInteraction)selectInteraction;

    /// <summary>
    /// ツールを切り替える。入力中の図形は取り消し、描画ツールへの切り替えでは選択を解除する（仕様 5.10）。ドラッグ中は切り替えない。
    /// </summary>
    public void SetTool(Tool tool)
    {
        if (IsDragging)
            return;
        drawInteraction?.Cancel();
        Tool = tool;
        drawInteraction = ShapeKindInfo.FromTool(tool) is { } kind
                          ? new DrawInteraction(drawing, kind, () => DefaultAttributes, Report)
                          : null;
        if (drawInteraction is not null)
            Select(null);
        StatusMessage = StatusMessages.Guidance(tool);
        OnStateChanged();
    }

    /// <summary>
    /// 画面座標の入力を論理座標へ変換し、現在のツールの入力として解釈する。
    /// </summary>
    public void HandlePointer(PointerEvent pointerEvent)
    {
        var input = new PointerInput(pointerEvent.Action, viewport.Transform.ToLogical(pointerEvent.ScreenPoint),
                                     pointerEvent.ClickCount, 1 / viewport.Scale);
        if (input.Action == PointerAction.Down)
            StatusMessage = StatusMessages.Guidance(Tool);   // 受け付けなかった理由は、次のクリックまで表示する
        CurrentInteraction.Handle(input);
        OnStateChanged();
    }

    /// <summary>
    /// Esc：入力中の図形、ドラッグ、確定していない属性の編集を取り消す。
    /// </summary>
    public void Cancel()
    {
        CancelOperations();
        OnStateChanged();
    }

    public void DeleteSelected()
    {
        var index = SelectedIndex();
        if (IsBusy || index < 0)
            return;
        Select(null);
        drawing.RemoveAt(index);
        OnStateChanged();
    }

    public void Undo()
    {
        if (IsBusy || !drawing.CanUndo)
            return;
        attributeEditor.Cancel();
        drawing.Undo();
        OnStateChanged();
    }

    public void Redo()
    {
        if (IsBusy || !drawing.CanRedo)
            return;
        attributeEditor.Cancel();
        drawing.Redo();
        OnStateChanged();
    }

    /// <summary>
    /// 図形に新しい <see cref="ShapeId"/> を付け、(X + 10·repeat, Y − 10·repeat) だけずらして追加し、Select ツールで選択する（仕様 5.7、A-19）。
    /// ずらした結果が制約または座標の範囲に違反するとき、および入力中・ドラッグ中は、何も変えずに false を返す。
    /// </summary>
    /// <param name="repeat">同じ内容を続けて貼った回数（1、2、…）。決めるのは MainViewModel（設計書 5.4）。</param>
    public bool Paste(Shape shape, int repeat)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentOutOfRangeException.ThrowIfLessThan(repeat, 1);
        if (IsBusy)
            return false;

        var pasted = shape.Translate(new Vector2(pasteOffset * repeat, -pasteOffset * repeat)) with { Id = ShapeId.New() };
        if (!pasted.IsValid())
            return false;

        SetTool(Tool.Select);
        Select(pasted.Id);      // 追加の Committed の時点で、選択が貼り付けた図形になっているようにする
        drawing.Add(pasted);
        OnStateChanged();
        return true;
    }

    /// <summary>
    /// 入力中の図形とドラッグを取り消し、選択を解除する（Open・New でドキュメントを置き換えたとき）。
    /// </summary>
    public void Reset()
    {
        CancelOperations();
        Select(null);
        OnStateChanged();
    }

    /// <summary>
    /// 選択図形の属性を、記録せずに変えて表示する（スライダーのドラッグなどの連続的な編集）。
    /// </summary>
    public void PreviewAttributes(ShapeAttributes attributes)
    {
        if (!attributes.IsValid())
            throw new ArgumentException("The attributes are invalid.", nameof(attributes));
        if (SelectedId is { } id && !IsBusy)
            attributeEditor.Preview(id, attributes);
    }

    /// <summary>
    /// 属性の編集を確定し、1 回の Undo 単位として記録する。
    /// </summary>
    public void CommitAttributes() => attributeEditor.Commit();

    void CancelOperations()
    {
        drawInteraction?.Cancel();
        selectInteraction.Cancel();
        attributeEditor.Cancel();
    }

    // 選択が変わるときは、確定していない属性の編集を取り消す（仕様 5.4）
    void Select(ShapeId? id)
    {
        if (id == SelectedId)
            return;
        attributeEditor.Cancel();
        SelectedId = id;
    }

    int SelectedIndex() => SelectedId is { } id ? drawing.IndexOf(id) : -1;

    void Report(string message) => StatusMessage = message;

    // 選択中の図形がなくなっていれば選択を解除してから、自身の Committed を発生させる（仕様 5.5）
    void OnDrawingCommitted(object? sender, EventArgs e)
    {
        if (SelectedIndex() < 0)
            SelectedId = null;
        Committed?.Invoke(this, EventArgs.Empty);
    }

    void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
