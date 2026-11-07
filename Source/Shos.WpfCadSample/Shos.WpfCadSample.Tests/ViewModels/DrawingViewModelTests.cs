using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Tests.ViewModels;

/// <summary>
/// T-VM-15〜17、22：<see cref="DrawingViewModel"/>（テスト計画書 4.13）。
/// ビューサイズ 1000 × 700、論理領域 0〜1000 × 0〜700（FitScale = 1）。画面座標 (x, 700 − y) ↔ 論理座標 (x, y)。
/// </summary>
public class DrawingViewModelTests
{
    readonly Drawing drawing = new();
    readonly Viewport viewport = new();
    readonly EditorController editor;
    readonly DrawingViewModel view;

    public DrawingViewModelTests()
    {
        editor = new EditorController(drawing, viewport);
        view = new DrawingViewModel(drawing, editor, viewport);
        view.SetViewSizeCommand.Execute(new ViewSize(1000, 700));
    }

    static Point2 Screen(double x, double y) => new(x, 700 - y);

    void Pointer(PointerAction action, double x, double y, int clickCount = 1)
        => view.PointerCommand.Execute(new PointerEvent(action, Screen(x, y), clickCount));

    void Click(double x, double y)
    {
        Pointer(PointerAction.Down, x, y);
        Pointer(PointerAction.Up, x, y);
    }

    LineShape AddLine(double x1, double y1, double x2, double y2)
    {
        var line = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(x1, y1), new(x2, y2));
        drawing.Add(line);
        return line;
    }

    // T-VM-15
    [Fact]
    public void Handles_AreScreenPositionsOfSelectedShapeOrEmpty()
    {
        AddLine(100, 100, 300, 200);
        Assert.Empty(view.Handles);

        Click(200, 150);

        Assert.Equal([new HandleItem(100, 600), new HandleItem(300, 500)], view.Handles);
        Click(800, 600);
        Assert.Empty(view.Handles);
    }

    // T-VM-15（Transform の変化で更新される）
    [Fact]
    public void Handles_FollowZoom()
    {
        AddLine(100, 100, 300, 200);
        Click(200, 150);

        view.ZoomCommand.Execute(new ZoomRequest(Screen(100, 100), 1));    // 始点の位置で拡大

        Assert.Equal(new HandleItem(100, 600), view.Handles[0]);
        var end = view.Transform.ToScreen(new Point2(300, 200));
        Assert.Equal(new HandleItem(end.X, end.Y), view.Handles[1]);
    }

    // T-VM-15（ドラッグ中もハンドルが図形に追従する）
    [Fact]
    public void Handles_FollowShapeDuringDrag()
    {
        AddLine(100, 100, 300, 100);
        Pointer(PointerAction.Down, 200, 100);
        Pointer(PointerAction.Move, 220, 150);

        Assert.Equal([new HandleItem(120, 550), new HandleItem(320, 550)], view.Handles);
    }

    // T-VM-16
    [Fact]
    public void Overview_IsUpdatedAfterDragEndsButNotDuringDrag()
    {
        AddLine(100, 100, 300, 100);
        Pointer(PointerAction.Down, 200, 100);
        Pointer(PointerAction.Move, 1500, 100);         // 論理領域の右へはみ出す

        Assert.Equal(new Rect2(0, 0, 1000, 700), viewport.Overview);
        Pointer(PointerAction.Up, 1500, 100);

        Assert.Equal(new Rect2(0, 0, 1600, 700), viewport.Overview);
        Assert.Equal("Zoom: 100%", view.ZoomText);     // 全体図の表示中は、新しい全体図に合わせ直す
        Assert.Equal(viewport.Transform, view.Transform);
    }

    // T-VM-16（Committed：追加・Undo で全体図が変わる）
    [Fact]
    public void Overview_FollowsCommittedChanges()
    {
        drawing.Add(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(-500, 0), new(0, 0)));
        Assert.Equal(new Rect2(-500, 0, 1000, 700), viewport.Overview);

        editor.Undo();
        Assert.Equal(new Rect2(0, 0, 1000, 700), viewport.Overview);
    }

    // T-VM-16（ウィンドウサイズの変更）
    [Fact]
    public void SetViewSize_RefitsOverview()
    {
        view.SetViewSizeCommand.Execute(new ViewSize(500, 350));

        Assert.Equal(0.5, view.Transform.Scale, 1e-9);
        Assert.Equal("Zoom: 100%", view.ZoomText);
    }

    // T-VM-17
    [Fact]
    public void StatusText_ShowsToolGuidance()
    {
        view.SelectToolCommand.Execute(Tool.Polyline);

        Assert.StartsWith("Polyline", view.StatusText);
        Assert.Contains("double-click", view.StatusText);
    }

    // T-VM-17
    [Fact]
    public void CursorText_ShowsLogicalCoordinatesAndClearsWhenLeaving()
    {
        Pointer(PointerAction.Move, 123.44, 56.66);
        Assert.Equal("X: 123.4  Y: 56.7", view.CursorText);

        view.PointerLeaveCommand.Execute(null);
        Assert.Equal("", view.CursorText);
    }

    // T-VM-17
    [Fact]
    public void ZoomText_ShowsZoomPercent()
    {
        Assert.Equal("Zoom: 100%", view.ZoomText);

        view.ZoomCommand.Execute(new ZoomRequest(Screen(500, 350), 1));

        Assert.Equal("Zoom: 120%", view.ZoomText);
    }

    // T-VM-22
    [Fact]
    public void SelectTool_SelectsExactlyOneTool()
    {
        Assert.Equal(Tool.Select, view.CurrentTool);

        view.SelectToolCommand.Execute(Tool.Circle);
        Assert.Equal(Tool.Circle, view.CurrentTool);

        view.SelectToolCommand.Execute(Tool.Select);
        Assert.Equal(Tool.Select, view.CurrentTool);
    }

    // T-VM-22（ドラッグ中は切り替わらない）
    [Fact]
    public void SelectTool_DuringDrag_KeepsCurrentTool()
    {
        AddLine(100, 100, 300, 100);
        Pointer(PointerAction.Down, 200, 100);

        view.SelectToolCommand.Execute(Tool.Line);

        Assert.Equal(Tool.Select, view.CurrentTool);
    }

    [Fact]
    public void InputShapes_ShowPreviewWhileDrawing()
    {
        view.SelectToolCommand.Execute(Tool.Line);
        Click(10, 10);
        Pointer(PointerAction.Move, 50, 60);
        Assert.IsType<LineShape>(Assert.Single(view.InputShapes));

        view.CancelCommand.Execute(null);
        Assert.Empty(view.InputShapes);
    }

    [Fact]
    public void Scroll_MovesFiftyPixelsWhileZoomed()
    {
        view.ZoomCommand.Execute(new ZoomRequest(Screen(500, 350), 3));
        var before = viewport.Center;

        view.ScrollCommand.Execute(ScrollDirection.Right);
        view.ScrollCommand.Execute(ScrollDirection.Up);

        Assert.Equal(before.X + 50 / viewport.Scale, viewport.Center.X, 1e-9);
        Assert.Equal(before.Y + 50 / viewport.Scale, viewport.Center.Y, 1e-9);
        Assert.Equal(viewport.Transform, view.Transform);
    }

    // T-VM-29：描画ツールは十字、Select はハンドルの上とドラッグ中に移動の形、それ以外は矢印（仕様 4.3）
    [Fact]
    public void Cursor_FollowsToolHandleAndDrag()
    {
        Assert.Equal(PointerCursor.Arrow, view.Cursor);
        AddLine(100, 100, 300, 200);

        view.SelectToolCommand.Execute(Tool.Line);
        Assert.Equal(PointerCursor.Cross, view.Cursor);
        view.SelectToolCommand.Execute(Tool.Select);
        Assert.Equal(PointerCursor.Arrow, view.Cursor);

        Click(200, 150);
        Pointer(PointerAction.Move, 100, 100);
        Assert.Equal(PointerCursor.Move, view.Cursor);
        Pointer(PointerAction.Move, 200, 150);
        Assert.Equal(PointerCursor.Arrow, view.Cursor);

        Pointer(PointerAction.Down, 200, 150);
        Pointer(PointerAction.Move, 250, 150);
        Assert.Equal(PointerCursor.Move, view.Cursor);
        Pointer(PointerAction.Up, 250, 150);
        Assert.Equal(PointerCursor.Arrow, view.Cursor);
    }

    // I-8：描画領域の論理領域の枠（設計書 5.7 の層①）が使う
    [Fact]
    public void LogicalArea_IsFixedLogicalAreaOfViewport()
        => Assert.Equal(new Rect2(0, 0, 1000, 700), view.LogicalArea);

    [Fact]
    public void ResetForNewDocument_ShowsOverviewAndClearsSelection()
    {
        AddLine(100, 100, 300, 200);
        Click(200, 150);
        view.ZoomCommand.Execute(new ZoomRequest(Screen(500, 350), 4));
        drawing.Load([new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(2000, 0))]);

        view.ResetForNewDocument();

        Assert.Equal("Zoom: 100%", view.ZoomText);
        Assert.Equal(new Rect2(0, 0, 2000, 700), viewport.Overview);
        Assert.Null(editor.SelectedId);
        Assert.Empty(view.Handles);
    }
}
