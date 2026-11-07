using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// 描画領域の状態：図形、入力中の図形、ハンドル、ビュー変換、状態表示（設計書 5.4）。
/// 拡大率とスクロール位置の状態は <see cref="Viewport"/> として保持する（仕様 3.4、設計書 DD-15）。
/// </summary>
public sealed partial class DrawingViewModel : ObservableObject
{
    const double scrollPixels = 50;    // Ctrl+矢印の移動量（仕様 A-18）

    readonly Drawing drawing;
    readonly EditorController editor;
    readonly Viewport viewport;
    Point2? pointerPoint;     // 描画領域の上のマウスの位置（画面座標）。外にあれば null

    public DrawingViewModel(Drawing drawing, EditorController editor, Viewport viewport)
    {
        this.drawing = drawing;
        this.editor = editor;
        this.viewport = viewport;
        editor.StateChanged += (_, _) => RefreshState();
        editor.Committed += (_, _) => OnCommitted();
        Transform = viewport.Transform;
        RefreshState();
    }

    /// <summary>
    /// 描画する図形（追加順。後から追加した図形ほど上に描く。仕様 4.3）。
    /// </summary>
    public ReadOnlyObservableCollection<Shape> Shapes => drawing.Shapes;

    /// <summary>
    /// 論理領域（固定）。描画領域は、この外形を枠線で示す（仕様 4.3）。
    /// </summary>
    public Rect2 LogicalArea => viewport.LogicalArea;

    /// <summary>
    /// 入力中の図形（0 または 1 個）。
    /// </summary>
    public ObservableCollection<Shape> InputShapes { get; } = [];

    /// <summary>
    /// 選択図形のハンドル（画面座標）。選択がなければ空。
    /// </summary>
    public ObservableCollection<HandleItem> Handles { get; } = [];

    /// <summary>
    /// 論理座標から画面座標へのビュー変換。図形の層全体に掛ける（設計書 DD-4）。
    /// </summary>
    [ObservableProperty]
    public partial ViewTransform Transform { get; private set; }

    [ObservableProperty]
    public partial Tool CurrentTool { get; private set; }

    /// <summary>
    /// 描画領域のカーソルの種類：描画ツールは十字、Select はハンドルの上とドラッグ中に移動の形、それ以外は矢印（仕様 4.3）。
    /// </summary>
    [ObservableProperty]
    public partial PointerCursor Cursor { get; private set; }

    /// <summary>
    /// 状態表示の左：現在のツールと操作案内（仕様 4.4）。
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; private set; } = "";

    /// <summary>
    /// カーソル位置の論理座標（小数 1 桁）。描画領域の外では空（仕様 4.4）。
    /// </summary>
    [ObservableProperty]
    public partial string CursorText { get; private set; } = "";

    /// <summary>
    /// 状態表示の右：拡大率（全体図を 100% とする。仕様 4.4）。
    /// </summary>
    [ObservableProperty]
    public partial string ZoomText { get; private set; } = "";

    /// <summary>
    /// 入力中の図形を取り消す（New・Open・Exit の保存確認の前。仕様 5.10）。
    /// </summary>
    public void CancelInput() => editor.Cancel();

    /// <summary>
    /// 新しいドキュメント（New・Open）の表示：選択を解除し、拡大中でも全体図の表示に戻す（仕様 5.8）。
    /// </summary>
    public void ResetForNewDocument()
    {
        editor.Reset();
        viewport.UpdateOverview(ShapesBounds());
        viewport.Reset();
        RefreshView();
    }

    [RelayCommand]
    void Pointer(PointerEvent pointerEvent)
    {
        if (pointerEvent.Action is PointerAction.Move or PointerAction.Down)
            CursorText = FormatCursor(viewport.Transform.ToLogical(pointerEvent.ScreenPoint));
        // ハンドルの上かどうかでカーソルの形を決めるため、位置を覚えておく
        pointerPoint = pointerEvent.ScreenPoint;
        editor.HandlePointer(pointerEvent);
        RefreshCursor();
    }

    [RelayCommand]
    void PointerLeave()
    {
        CursorText = "";
        pointerPoint = null;
    }

    [RelayCommand]
    void Cancel() => editor.Cancel();

    [RelayCommand]
    void SelectTool(Tool tool) => editor.SetTool(tool);

    [RelayCommand]
    void SetViewSize(ViewSize size)
    {
        viewport.SetViewSize(size.Width, size.Height);
        RefreshView();
    }

    [RelayCommand]
    void Zoom(ZoomRequest request)
    {
        viewport.ZoomAt(request.ScreenPoint, request.Notches);
        RefreshView();
    }

    [RelayCommand]
    void Scroll(ScrollDirection direction)
    {
        var (dx, dy) = direction switch {
            ScrollDirection.Left => (-scrollPixels, 0.0),
            ScrollDirection.Right => (scrollPixels, 0.0),
            ScrollDirection.Up => (0.0, -scrollPixels),
            ScrollDirection.Down => (0.0, scrollPixels),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.")
        };
        viewport.Scroll(dx, dy);
        RefreshView();
    }

    // 全体図の再計算は、確定的な変更の後だけ。入力中・ドラッグ中はしない（仕様 5.6、A-20）
    void OnCommitted()
    {
        if (!editor.IsInputInProgress && !editor.IsDragging)
            viewport.UpdateOverview(ShapesBounds());
        RefreshView();
    }

    void RefreshState()
    {
        CurrentTool = editor.Tool;
        StatusText = editor.StatusMessage;
        RefreshCursor();
        SyncInputShapes();
        RefreshHandles();
    }

    // ハンドルは画面座標で置くため、ビュー変換が変わるたびに置き直す
    void RefreshView()
    {
        Transform = viewport.Transform;
        ZoomText = string.Create(CultureInfo.InvariantCulture, $"Zoom: {viewport.ZoomPercent:0}%");
        RefreshHandles();
    }

    void RefreshCursor()
        => Cursor = editor.Tool != Tool.Select ? PointerCursor.Cross
                  : editor.IsDragging || (pointerPoint is { } point && editor.IsOverHandle(point)) ? PointerCursor.Move
                  : PointerCursor.Arrow;

    // 同じ図形のままなら置き換えない（コレクションの変更による、不要な再描画を避ける）
    void SyncInputShapes()
    {
        if (InputShapes.FirstOrDefault() == editor.InputShape)
            return;
        InputShapes.Clear();
        if (editor.InputShape is { } inputShape)
            InputShapes.Add(inputShape);
    }

    void RefreshHandles()
    {
        Handles.Clear();
        var index = editor.SelectedId is { } id ? drawing.IndexOf(id) : -1;
        if (index < 0)
            return;
        foreach (var point in drawing.Shapes[index].HandlePoints.Select(viewport.Transform.ToScreen))
            Handles.Add(new(point.X, point.Y));
    }

    // 全体図に含める図形の外接矩形（線の太さは含めない。仕様 5.6）。図形がなければ null
    Rect2? ShapesBounds()
        => drawing.Shapes.Count == 0 ? null : drawing.Shapes.Select(shape => shape.Bounds).Aggregate((union, next) => union.Union(next));

    static string FormatCursor(Point2 point)
        => string.Create(CultureInfo.InvariantCulture, $"X: {point.X:0.0}  Y: {point.Y:0.0}");
}
