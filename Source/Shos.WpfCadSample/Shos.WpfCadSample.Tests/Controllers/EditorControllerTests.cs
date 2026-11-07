using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Controllers;

/// <summary>
/// T-EDC：<see cref="EditorController"/>（テスト計画書 4.12）。
/// ビューサイズ 1000 × 700、論理領域 0〜1000 × 0〜700（FitScale = 1）。画面座標 (x, 700 − y) ↔ 論理座標 (x, y)。
/// 以下の補助は、論理座標で位置を受け取り、画面座標の <see cref="PointerEvent"/> にして渡す。
/// </summary>
public class EditorControllerTests
{
    readonly Drawing drawing = new();
    readonly Viewport viewport = new();
    readonly EditorController editor;
    int recordedChanges;    // Drawing.Committed の回数（記録される変更の回数。数えても状態を変えない）

    public EditorControllerTests()
    {
        viewport.SetViewSize(1000, 700);
        editor = new EditorController(drawing, viewport);
        drawing.Committed += (_, _) => recordedChanges++;
    }

    static Point2 Screen(double x, double y) => new(x, 700 - y);

    void Down(double x, double y, int clickCount = 1) => editor.HandlePointer(new(PointerAction.Down, Screen(x, y), clickCount));
    void Move(double x, double y) => editor.HandlePointer(new(PointerAction.Move, Screen(x, y)));
    void Up(double x, double y) => editor.HandlePointer(new(PointerAction.Up, Screen(x, y)));
    void CaptureLost() => editor.HandlePointer(new(PointerAction.CaptureLost, Screen(0, 0)));

    void Click(double x, double y, int clickCount = 1)
    {
        Down(x, y, clickCount);
        Up(x, y);
    }

    // ダブルクリック：1 回目のクリックで点を追加し、2 回目（ClickCount = 2）で確定する
    void DoubleClick(double x, double y)
    {
        Click(x, y);
        Click(x, y, clickCount: 2);
    }

    // 利用者と同じく、図形の線上（最後のハンドルは、どの種類でも線上にある）をクリックして選択する
    void Select(Shape shape)
    {
        var onLine = shape.HandlePoints[^1];
        Click(onLine.X, onLine.Y);
        Assert.Equal(shape.Id, editor.SelectedId);
    }

    // 途中の位置を経由して動かす（ドラッグ中の更新も通す）
    void Drag(double fromX, double fromY, double toX, double toY)
    {
        Down(fromX, fromY);
        Move((fromX + toX) / 2, (fromY + toY) / 2);
        Move(toX, toY);
        Up(toX, toY);
    }

    void Draw(Tool tool, params (double X, double Y)[] points)
    {
        editor.SetTool(tool);
        foreach (var (x, y) in points[..^1])
            Click(x, y);
        if (ShapeKindInfo.IsFixedTwoPoint(ShapeKindInfo.FromTool(tool)!.Value))
            Click(points[^1].X, points[^1].Y);
        else
            DoubleClick(points[^1].X, points[^1].Y);
    }

    LineShape AddLine(double x1, double y1, double x2, double y2)
    {
        var line = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(x1, y1), new(x2, y2));
        drawing.Add(line);
        return line;
    }

    // 取り消せる操作の数（Undo を繰り返して数え、Redo で元に戻す）。
    // 途中で図形が一時的になくなり選択が解除されるため、選択を確かめた後か、選択する前にだけ使う
    int UndoDepth()
    {
        var depth = 0;
        while (drawing.CanUndo) {
            drawing.Undo();
            depth++;
        }
        for (var count = 0; count < depth; count++)
            drawing.Redo();
        return depth;
    }

    // ---- 描画（DrawInteraction） ----

    // T-EDC-01、T-EDC-02、T-EDC-03
    [Theory]
    [InlineData(Tool.Line, typeof(LineShape))]
    [InlineData(Tool.Circle, typeof(CircleShape))]
    [InlineData(Tool.Rectangle, typeof(RectangleShape))]
    public void TwoClicks_AddFixedTwoPointShapeWithDefaultAttributes(Tool tool, Type expectedType)
    {
        editor.SetTool(tool);

        Click(10, 20);
        Assert.Empty(drawing.Shapes);
        Click(110, 70);

        var shape = Assert.Single(drawing.Shapes);
        Assert.IsType(expectedType, shape);
        Assert.Equal(new Point2(10, 20), shape.HandlePoints[0]);
        Assert.Equal(ShapeAttributes.Default, shape.Attributes);
    }

    // T-EDC-01〜03 の補足：2 点目をダブルクリックすると、2 点目のクリックで確定した後に、2 回目の Down（ClickCount = 2）が届く。
    // それを新しい入力の確定として扱わず、点数不足の状態表示を出さない（Reviews/20261107-source-code-review.md の指摘の回帰テスト）
    [Theory]
    [InlineData(Tool.Line)]
    [InlineData(Tool.Circle)]
    [InlineData(Tool.Rectangle)]
    public void DoubleClickingCompletedFixedShapeEndpoint_DoesNotReportTooFewPoints(Tool tool)
    {
        editor.SetTool(tool);
        var guidance = editor.StatusMessage;

        Click(10, 20);
        Click(110, 70);
        Click(110, 70, clickCount: 2);

        Assert.Single(drawing.Shapes);
        Assert.Equal(guidance, editor.StatusMessage);
    }

    // T-EDC-01〜03 の補足：ダブルクリックで確定した直後に素早くもう 1 回クリックすると、3 回目の Down（ClickCount = 3）が届く。
    // 入力中の点はないので、確定として扱わず、点数不足の状態表示を出さない（Reviews/20261107-source-code-review-2.md の指摘 4 の回帰テスト）
    [Theory]
    [InlineData(Tool.Polyline)]
    [InlineData(Tool.OpenCurve)]
    public void ClickingRightAfterDoubleClickConfirm_DoesNotReportTooFewPoints(Tool tool)
    {
        editor.SetTool(tool);
        var guidance = editor.StatusMessage;

        Click(10, 20);
        DoubleClick(110, 70);
        Click(110, 70, clickCount: 3);

        Assert.Single(drawing.Shapes);
        Assert.Equal(guidance, editor.StatusMessage);
    }

    // T-EDC-04、T-EDC-05
    [Theory]
    [InlineData(Tool.Polyline, typeof(PolylineShape))]
    [InlineData(Tool.Polygon, typeof(PolygonShape))]
    [InlineData(Tool.OpenCurve, typeof(OpenCurveShape))]
    [InlineData(Tool.ClosedCurve, typeof(ClosedCurveShape))]
    public void DoubleClick_ConfirmsPointListShapeWithoutExtraPoint(Tool tool, Type expectedType)
    {
        Draw(tool, (0, 0), (100, 0), (100, 80), (20, 60));

        var shape = Assert.Single(drawing.Shapes);
        Assert.IsType(expectedType, shape);
        Assert.Equal([new(0, 0), new(100, 0), new(100, 80), new(20, 60)], shape.HandlePoints);
    }

    // T-EDC-06
    [Theory]
    [InlineData(Tool.Polyline, 1)]
    [InlineData(Tool.OpenCurve, 1)]
    [InlineData(Tool.Polygon, 2)]
    [InlineData(Tool.ClosedCurve, 2)]
    public void ConfirmWithTooFewPoints_CancelsInputWithMessage(Tool tool, int pointCount)
    {
        editor.SetTool(tool);
        var guidance = editor.StatusMessage;

        // ダブルクリックの位置も 1 点に数え、合計 pointCount 点（最小点数より 1 少ない）で確定する
        foreach (var index in Enumerable.Range(0, pointCount - 1))
            Click(index * 50, 0);
        DoubleClick((pointCount - 1) * 50, 0);

        Assert.Empty(drawing.Shapes);
        Assert.False(editor.IsInputInProgress);
        Assert.Null(editor.InputShape);
        Assert.NotEqual(guidance, editor.StatusMessage);
        Assert.False(string.IsNullOrWhiteSpace(editor.StatusMessage));
    }

    // T-EDC-07
    [Fact]
    public void PointSameAsPrevious_IsNotAddedAndInputContinues()
    {
        editor.SetTool(Tool.Polyline);
        Click(10, 10);

        Click(10, 10);      // 直前の点と同じ位置

        Assert.True(editor.IsInputInProgress);
        DoubleClick(50, 20);
        Assert.Equal([new(10, 10), new(50, 20)], Assert.Single(drawing.Shapes).HandlePoints);
    }

    // T-EDC-07
    [Theory]
    [InlineData(0.0, 50.0)]     // 同じ X
    [InlineData(40.0, 0.0)]     // 同じ Y
    public void Rectangle_SecondPointMakingZeroWidthOrHeight_IsRejected(double x, double y)
    {
        editor.SetTool(Tool.Rectangle);
        Click(0, 0);
        var guidance = editor.StatusMessage;

        Click(x, y);

        Assert.Empty(drawing.Shapes);
        Assert.True(editor.IsInputInProgress);
        Assert.NotEqual(guidance, editor.StatusMessage);
        Click(40, 50);
        Assert.IsType<RectangleShape>(Assert.Single(drawing.Shapes));
    }

    // T-EDC-07
    [Theory]
    [InlineData(Tool.Polygon)]
    [InlineData(Tool.ClosedCurve)]
    public void ClosedKind_PointOntoFirstPoint_IsRejected(Tool tool)
    {
        editor.SetTool(tool);
        Click(0, 0);
        Click(50, 0);

        Click(0, 0);        // 最初の点と同じ位置：最後に置くと、閉じる辺の長さが 0 になる

        DoubleClick(25, 40);
        Assert.Equal([new(0, 0), new(50, 0), new(25, 40)], Assert.Single(drawing.Shapes).HandlePoints);
    }

    // T-EDC-08
    [Fact]
    public void MoveDuringInput_UpdatesPreviewUpToCursor()
    {
        editor.SetTool(Tool.Polyline);
        Click(0, 0);
        Click(50, 0);

        Move(60, 40);

        var preview = Assert.IsType<PolylineShape>(editor.InputShape);
        Assert.Equal([new(0, 0), new(50, 0), new(60, 40)], preview.HandlePoints);
        Assert.Empty(drawing.Shapes);
    }

    // T-EDC-08（2 点で確定する図形も、1 点目とカーソルで表示する）
    [Fact]
    public void MoveAfterFirstPoint_PreviewsLine()
    {
        editor.SetTool(Tool.Line);
        Click(0, 0);

        Move(30, 40);

        Assert.Equal([new(0, 0), new(30, 40)], Assert.IsType<LineShape>(editor.InputShape).HandlePoints);
    }

    [Fact]
    public void MoveBeforeFirstPoint_HasNoPreview()
    {
        editor.SetTool(Tool.Line);

        Move(30, 40);

        Assert.Null(editor.InputShape);
        Assert.False(editor.IsInputInProgress);
    }

    // T-EDC-09
    [Fact]
    public void CancelDuringInput_DiscardsInputAndKeepsTool()
    {
        editor.SetTool(Tool.Polygon);
        Click(0, 0);
        Click(50, 0);
        Move(20, 30);

        editor.Cancel();

        Assert.Null(editor.InputShape);
        Assert.False(editor.IsInputInProgress);
        Assert.Empty(drawing.Shapes);
        Assert.Equal(Tool.Polygon, editor.Tool);
    }

    // T-EDC-10
    [Fact]
    public void AfterConfirm_ToolStaysAndNewShapeIsNotSelected()
    {
        Draw(Tool.Line, (0, 0), (10, 10));

        Assert.Equal(Tool.Line, editor.Tool);
        Assert.Null(editor.SelectedId);
        Assert.Null(editor.InputShape);
        Click(20, 20);
        Click(30, 40);
        Assert.Equal(2, drawing.Shapes.Count);
    }

    // T-EDC-11
    [Fact]
    public void ChangedDefaultAttributes_ApplyToNewShapes()
    {
        var attributes = new ShapeAttributes(new RgbColor(255, 0, 0), 4, LineStyle.Dash);
        editor.DefaultAttributes = attributes;

        Draw(Tool.Circle, (50, 50), (80, 50));

        Assert.Equal(attributes, Assert.Single(drawing.Shapes).Attributes);
    }

    // ---- 選択と編集（SelectInteraction） ----

    // T-EDC-12
    [Fact]
    public void Click_SelectsTopmostShapeOnLineOrClearsSelection()
    {
        var bottom = AddLine(0, 0, 100, 100);
        var top = AddLine(0, 100, 100, 0);

        Click(20, 20);
        Assert.Equal(bottom.Id, editor.SelectedId);

        Click(50, 50);                      // 2 本が交わる位置
        Assert.Equal(top.Id, editor.SelectedId);

        Click(80, 30);                      // 何もない位置
        Assert.Null(editor.SelectedId);
    }

    // T-EDC-13
    [Fact]
    public void ClickInsideRectangle_DoesNotSelect()
    {
        drawing.Add(new RectangleShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(200, 100)));

        Click(100, 50);

        Assert.Null(editor.SelectedId);
    }

    // T-EDC-14
    public static TheoryData<Shape, int, Point2[]> HandleDrags => new() {
        // 図形、動かすハンドル、ハンドルを (+20, +10) 動かした後のハンドルの位置。
        // 円は、中心（添字 0）を動かすと全体が移動し、円周上の点（添字 1）を動かすと半径が変わる
        { new LineShape(ShapeId.New(), ShapeAttributes.Default, new(100, 100), new(300, 100)), 1, [new(100, 100), new(320, 110)] },
        { new CircleShape(ShapeId.New(), ShapeAttributes.Default, new(200, 200), new(260, 200)), 0, [new(220, 210), new(280, 210)] },
        { new CircleShape(ShapeId.New(), ShapeAttributes.Default, new(200, 200), new(260, 200)), 1, [new(200, 200), new(280, 210)] },
        { new RectangleShape(ShapeId.New(), ShapeAttributes.Default, new(100, 100), new(300, 200)), 2, [new(100, 100), new(320, 100), new(320, 210), new(100, 210)] },
        { new PolylineShape(ShapeId.New(), ShapeAttributes.Default, [new(100, 100), new(200, 150), new(300, 100)]), 1, [new(100, 100), new(220, 160), new(300, 100)] },
        { new PolygonShape(ShapeId.New(), ShapeAttributes.Default, [new(100, 100), new(200, 250), new(300, 100)]), 2, [new(100, 100), new(200, 250), new(320, 110)] },
        { new OpenCurveShape(ShapeId.New(), ShapeAttributes.Default, [new(100, 100), new(200, 150), new(300, 100)]), 0, [new(120, 110), new(200, 150), new(300, 100)] },
        { new ClosedCurveShape(ShapeId.New(), ShapeAttributes.Default, [new(100, 100), new(200, 250), new(300, 100)]), 1, [new(100, 100), new(220, 260), new(300, 100)] }
    };

    // T-EDC-14
    [Theory]
    [MemberData(nameof(HandleDrags))]
    public void HandleDrag_MovesHandleAsSpecified(Shape shape, int handleIndex, Point2[] expectedHandles)
    {
        drawing.Add(shape);
        Select(shape);
        var handle = shape.HandlePoints[handleIndex];

        Down(handle.X, handle.Y);
        Move(handle.X + 10, handle.Y + 5);
        Assert.NotSame(shape, drawing.Shapes[0]);       // ドラッグ中に更新される
        Move(handle.X + 20, handle.Y + 10);
        Up(handle.X + 20, handle.Y + 10);

        Assert.Equal(expectedHandles, drawing.Shapes[0].HandlePoints);
        Assert.Equal(shape.Id, drawing.Shapes[0].Id);
        Assert.Equal(shape.Id, editor.SelectedId);
    }

    // T-EDC-15
    [Fact]
    public void DragOnSelectedShapeLine_MovesWholeShape()
    {
        var line = AddLine(100, 100, 300, 100);
        Click(200, 100);

        Drag(200, 100, 230, 150);

        Assert.Equal([new(130, 150), new(330, 150)], drawing.Shapes[0].HandlePoints);
    }

    // T-EDC-16
    [Fact]
    public void DragOnUnselectedShape_SelectsAndMovesIt()
    {
        var line = AddLine(100, 100, 300, 100);

        Drag(150, 100, 150, 120);

        Assert.Equal(line.Id, editor.SelectedId);
        Assert.Equal([new(100, 120), new(300, 120)], drawing.Shapes[0].HandlePoints);
    }

    // T-EDC-17
    [Fact]
    public void Drag_IsOneUndoUnit()
    {
        var line = AddLine(100, 100, 300, 100);
        var before = UndoDepth();

        var changesBeforeDrag = recordedChanges;
        Down(200, 100);
        foreach (var step in Enumerable.Range(1, 10))
            Move(200 + step * 5, 100);
        Assert.Equal(changesBeforeDrag, recordedChanges);  // ドラッグ中の Move は履歴を増やさない
        Up(250, 100);

        Assert.Equal(before + 1, UndoDepth());
        editor.Undo();
        Assert.Equal(line.HandlePoints, drawing.Shapes[0].HandlePoints);
        editor.Redo();
        Assert.Equal([new(150, 100), new(350, 100)], drawing.Shapes[0].HandlePoints);
    }

    // T-EDC-17（Committed の時点でドラッグが終わっている。ViewModels が全体図を更新できるように。I-6 で見つけた不具合の再現）
    [Fact]
    public void Committed_AfterDrag_IsRaisedWhenDraggingHasEnded()
    {
        AddLine(100, 100, 300, 100);
        bool? draggingWhenCommitted = null;
        editor.Committed += (_, _) => draggingWhenCommitted = editor.IsDragging;

        Drag(200, 100, 200, 150);

        Assert.False(draggingWhenCommitted);
    }

    // T-EDC-18
    [Fact]
    public void MovementBelowThreshold_IsAClickAndRecordsNothing()
    {
        var line = AddLine(100, 100, 300, 100);
        var before = UndoDepth();

        Down(200, 100);
        Move(202, 101);                                 // 約 2.2 px
        Up(202, 101);

        Assert.Same(line, drawing.Shapes[0]);
        Assert.Equal(line.Id, editor.SelectedId);
        Assert.False(editor.IsDragging);
        Assert.Equal(before, UndoDepth());
    }

    // T-EDC-19
    [Fact]
    public void CancelDuringDrag_RestoresShapeWithoutHistory()
    {
        var line = AddLine(100, 100, 300, 100);
        var before = UndoDepth();
        Down(200, 100);
        Move(250, 150);

        editor.Cancel();

        Assert.Same(line, drawing.Shapes[0]);
        Assert.Equal(before, UndoDepth());
        Assert.False(editor.IsDragging);
        Up(250, 150);                                   // 取り消し後の Up は何もしない
        Assert.Same(line, drawing.Shapes[0]);
        Assert.Equal(before, UndoDepth());
    }

    // T-EDC-20
    [Fact]
    public void HandleDragOntoViolation_KeepsLastValidStateAndFollowsAgain()
    {
        var line = AddLine(100, 100, 300, 100);
        Select(line);

        Down(300, 100);
        Move(200, 100);
        Move(100, 100);                                 // 始点に重なる（制約違反）
        Assert.Equal([new(100, 100), new(200, 100)], drawing.Shapes[0].HandlePoints);
        Move(150, 120);                                 // 違反から戻すと、再び追従する
        Assert.Equal([new(100, 100), new(150, 120)], drawing.Shapes[0].HandlePoints);
        Up(150, 120);

        Assert.Equal([new(100, 100), new(150, 120)], drawing.Shapes[0].HandlePoints);
    }

    // T-EDC-20
    [Fact]
    public void CirclePointOntoCenterAndRectangleZeroWidth_AreRejected()
    {
        var circle = new CircleShape(ShapeId.New(), ShapeAttributes.Default, new(200, 200), new(260, 200));
        drawing.Add(circle);
        Select(circle);
        // 円周上の点を中心に重ねる（半径 0）。違反する位置は反映せず、Up でも元の図形のまま
        Down(260, 200);
        Move(200, 200);
        Up(200, 200);
        Assert.Same(circle, drawing.Shapes[0]);

        var rectangle = new RectangleShape(ShapeId.New(), ShapeAttributes.Default, new(500, 100), new(600, 200));
        drawing.Add(rectangle);
        Select(rectangle);
        Down(600, 200);
        Move(500, 250);                                 // 幅 0
        Up(500, 250);
        Assert.Same(rectangle, drawing.Shapes[1]);
    }

    // T-EDC-21
    [Fact]
    public void DeleteSelected_RemovesShapeAndClearsSelection()
    {
        var line = AddLine(100, 100, 300, 100);
        Select(line);

        editor.DeleteSelected();

        Assert.Empty(drawing.Shapes);
        Assert.Null(editor.SelectedId);
        editor.Undo();
        Assert.Same(line, Assert.Single(drawing.Shapes));
    }

    // T-EDC-22
    [Fact]
    public void UndoRedo_KeepsSelectionWhileShapeExists()
    {
        var first = AddLine(100, 100, 300, 100);
        Select(first);
        Drag(200, 100, 200, 150);

        editor.Undo();                                  // 移動の取り消し：図形は存在する
        Assert.Equal(first.Id, editor.SelectedId);

        editor.Undo();                                  // 追加の取り消し：図形がなくなる
        Assert.Null(editor.SelectedId);

        editor.Redo();
        Assert.Null(editor.SelectedId);                 // 解除した選択は復元しない
    }

    // T-EDC-32
    [Fact]
    public void HandleOfSelectedShape_TakesPriorityOverLineOfShapeAbove()
    {
        var below = AddLine(100, 100, 300, 100);
        var above = AddLine(300, 0, 300, 200);          // 下の図形の終点（ハンドル）を通る
        Click(200, 100);                                // 下の図形を、終点から離れた線上で選択する
        Assert.Equal(below.Id, editor.SelectedId);

        Drag(300, 100, 320, 130);

        Assert.Equal(below.Id, editor.SelectedId);
        Assert.Equal([new(100, 100), new(320, 130)], drawing.Shapes[0].HandlePoints);
        Assert.Same(above, drawing.Shapes[1]);
    }

    // T-EDC-33
    [Fact]
    public void CaptureLostDuringDrag_RestoresShapeWithoutHistory()
    {
        var line = AddLine(100, 100, 300, 100);
        var before = UndoDepth();
        Down(200, 100);
        Move(250, 150);

        CaptureLost();

        Assert.Same(line, drawing.Shapes[0]);
        Assert.Equal(before, UndoDepth());
        Assert.False(editor.IsDragging);
    }

    // T-EDC-33
    [Fact]
    public void CaptureLostDuringInput_IsIgnored()
    {
        editor.SetTool(Tool.Polyline);
        Click(0, 0);
        Click(50, 0);

        CaptureLost();

        Assert.True(editor.IsInputInProgress);
        DoubleClick(50, 50);
        Assert.Equal(3, Assert.Single(drawing.Shapes).HandlePoints.Count);
    }

    // T-EDC-34
    [Fact]
    public void Committed_IsRaisedAfterSelectionIsCorrected()
    {
        Assert.True(editor.Paste(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(50, 50)), 1));
        ShapeId? selectionWhenCommitted = editor.SelectedId;
        editor.Committed += (_, _) => selectionWhenCommitted = editor.SelectedId;

        editor.Undo();                                  // 選択中の図形の追加を取り消す

        Assert.Null(selectionWhenCommitted);
    }

    // T-EDC-35
    [Fact]
    public void FirstPointBeyondLimit_IsNotAddedWithMessage()
    {
        editor.SetTool(Tool.Line);
        var guidance = editor.StatusMessage;

        Click(2_000_000, 0);

        Assert.False(editor.IsInputInProgress);
        Assert.NotEqual(guidance, editor.StatusMessage);
    }

    // T-EDC-35
    [Fact]
    public void LaterPointBeyondLimit_IsNotAddedAndInputContinues()
    {
        editor.SetTool(Tool.Polyline);
        Click(0, 0);

        Click(2_000_000, 0);

        Assert.True(editor.IsInputInProgress);
        DoubleClick(10, 10);
        Assert.Equal([new(0, 0), new(10, 10)], Assert.Single(drawing.Shapes).HandlePoints);
    }

    // T-EDC-35
    [Fact]
    public void DragBeyondLimit_KeepsLastValidState()
    {
        var line = AddLine(100, 100, 300, 100);
        Select(line);

        Down(300, 100);
        Move(400, 100);
        Move(2_000_000, 100);                           // 端点が座標の範囲外
        Up(2_000_000, 100);
        Assert.Equal([new(100, 100), new(400, 100)], drawing.Shapes[0].HandlePoints);

        // 全体移動：50 動かした状態から範囲外へ動かすと、50 動かした状態に留まる
        Down(200, 100);
        Move(250, 100);
        Move(1_999_800, 100);                           // 全体移動で範囲外
        Up(1_999_800, 100);
        Assert.Equal([new(150, 100), new(450, 100)], drawing.Shapes[0].HandlePoints);
    }

    // T-EDC-36
    [Fact]
    public void PasteBeyondLimit_ReturnsFalseAndChangesNothing()
    {
        var existing = AddLine(0, 0, 10, 10);
        var before = UndoDepth();
        Select(existing);
        // 貼り付けのずれ（X + 10）で、始点の X が上限 1,000,000 を超える
        var nearLimit = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(999_995, 0), new(999_990, 50));

        Assert.False(editor.Paste(nearLimit, 1));

        Assert.Single(drawing.Shapes);
        Assert.Equal(existing.Id, editor.SelectedId);
        Assert.Equal(before, UndoDepth());
    }

    // T-EDC-36（ツールも変わらない）
    [Fact]
    public void PasteBeyondLimit_KeepsDrawingTool()
    {
        editor.SetTool(Tool.Circle);

        // 貼り付けのずれ（Y − 10）で、始点の Y が下限 −1,000,000 を超える
        Assert.False(editor.Paste(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, -999_995), new(5, 0)), 1));

        Assert.Equal(Tool.Circle, editor.Tool);
    }

    // T-EDC-37
    [Fact]
    public void Reset_CancelsInputAndClearsSelection()
    {
        var line = AddLine(100, 100, 300, 100);
        Select(line);
        editor.SetTool(Tool.Select);
        var changes = 0;
        editor.StateChanged += (_, _) => changes++;

        editor.Reset();
        Assert.Null(editor.SelectedId);

        editor.SetTool(Tool.Polyline);
        Click(0, 0);
        editor.Reset();
        Assert.False(editor.IsInputInProgress);
        Assert.Null(editor.InputShape);
        Assert.True(changes >= 2);
    }

    // T-EDC-38：ハンドルの許容範囲（画面上 6 px）内だけ真。拡大率に左右されない
    [Fact]
    public void IsOverHandle_IsTrueOnlyNearHandleOfSelectedShapeWithSelectTool()
    {
        var line = AddLine(100, 100, 300, 100);
        Assert.False(editor.IsOverHandle(Screen(100, 100)));     // 選択なし

        Select(line);
        Assert.True(editor.IsOverHandle(Screen(100, 100)));
        Assert.True(editor.IsOverHandle(Screen(105, 100)));
        Assert.False(editor.IsOverHandle(Screen(107, 100)));
        Assert.False(editor.IsOverHandle(Screen(200, 100)));     // 線の上（ハンドルではない）

        viewport.ZoomAt(Screen(100, 100), 4);
        var handle = viewport.Transform.ToScreen(new(300, 100));
        Assert.True(editor.IsOverHandle(new(handle.X + 5, handle.Y)));
        Assert.False(editor.IsOverHandle(new(handle.X + 7, handle.Y)));

        editor.SetTool(Tool.Line);
        Assert.False(editor.IsOverHandle(handle));               // 描画ツール（選択も解除される）
    }

    // ---- ツール・入力中の状態（仕様 5.10） ----

    // T-EDC-23
    [Fact]
    public void SwitchingToolDuringInput_CancelsInput()
    {
        editor.SetTool(Tool.Polyline);
        Click(0, 0);
        Click(50, 0);

        editor.SetTool(Tool.Circle);

        Assert.False(editor.IsInputInProgress);
        Assert.Null(editor.InputShape);
        Assert.Empty(drawing.Shapes);
    }

    // T-EDC-24
    [Fact]
    public void SwitchingToDrawingTool_ClearsSelectionWhichIsNotRestored()
    {
        var line = AddLine(100, 100, 300, 100);
        Select(line);

        editor.SetTool(Tool.Rectangle);
        Assert.Null(editor.SelectedId);

        editor.SetTool(Tool.Select);
        Assert.Null(editor.SelectedId);
    }

    // T-EDC-25
    [Fact]
    public void DuringInput_UndoRedoAndPasteDoNothing()
    {
        AddLine(0, 0, 10, 10);
        AddLine(20, 0, 30, 10);
        drawing.Undo();                                 // Redo できる状態
        editor.SetTool(Tool.Polyline);
        Click(100, 100);
        Assert.True(editor.IsInputInProgress);

        editor.Undo();
        editor.Redo();
        var pasted = editor.Paste(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(5, 5)), 1);

        Assert.False(pasted);
        Assert.Single(drawing.Shapes);
        Assert.True(drawing.CanRedo);
        Assert.True(editor.IsInputInProgress);
        Assert.Equal(Tool.Polyline, editor.Tool);
    }

    // T-EDC-26
    [Fact]
    public void DuringDrag_CommandsAndToolSwitchDoNothing()
    {
        AddLine(0, 0, 10, 10);
        var line = AddLine(100, 100, 300, 100);
        Down(200, 100);
        Move(250, 120);
        Assert.True(editor.IsDragging);
        var preview = drawing.Shapes[1];                // ドラッグ中の表示の図形

        editor.Undo();
        editor.Redo();
        editor.DeleteSelected();
        var pasted = editor.Paste(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(5, 5)), 1);
        editor.SetTool(Tool.Line);

        Assert.False(pasted);
        Assert.Equal(2, drawing.Shapes.Count);
        Assert.Same(preview, drawing.Shapes[1]);
        Assert.Equal(Tool.Select, editor.Tool);
        Assert.Equal(line.Id, editor.SelectedId);
        Up(250, 120);
        Assert.Equal([new(150, 120), new(350, 120)], drawing.Shapes[1].HandlePoints);
    }

    // ---- 属性・貼り付け ----

    // T-EDC-27
    [Fact]
    public void AttributePreviewThenCommit_IsOneUndoUnit()
    {
        var line = AddLine(100, 100, 300, 100);
        var before = UndoDepth();
        Select(line);
        var changesBeforePreview = recordedChanges;

        foreach (var thickness in new[] { 2.0, 3.0, 4.0 })
            editor.PreviewAttributes(ShapeAttributes.Default with { Thickness = thickness });
        Assert.Equal(4.0, drawing.Shapes[0].Attributes.Thickness);
        Assert.Equal(changesBeforePreview, recordedChanges);
        editor.CommitAttributes();

        Assert.Equal(before + 1, UndoDepth());
        Assert.Equal(4.0, drawing.Shapes[0].Attributes.Thickness);
        editor.Undo();
        Assert.Equal(ShapeAttributes.Default, drawing.Shapes[0].Attributes);
    }

    // T-EDC-28
    [Fact]
    public void AttributePreviewWithoutCommit_IsRevertedWhenSelectionChanges()
    {
        var first = AddLine(100, 100, 300, 100);
        var second = AddLine(100, 300, 300, 300);
        var before = UndoDepth();
        Select(first);
        editor.PreviewAttributes(ShapeAttributes.Default with { LineStyle = LineStyle.Dot });
        Assert.Equal(LineStyle.Dot, drawing.Shapes[0].Attributes.LineStyle);

        Select(second);

        Assert.Same(first, drawing.Shapes[0]);
        Assert.Equal(before, UndoDepth());
    }

    // T-EDC-28
    [Fact]
    public void AttributePreviewWithoutCommit_IsRevertedByCancel()
    {
        var line = AddLine(100, 100, 300, 100);
        var before = UndoDepth();
        Select(line);
        editor.PreviewAttributes(ShapeAttributes.Default with { Color = new RgbColor(0, 0, 255) });
        Assert.Equal(new RgbColor(0, 0, 255), drawing.Shapes[0].Attributes.Color);

        editor.Cancel();

        Assert.Same(line, drawing.Shapes[0]);
        Assert.Equal(before, UndoDepth());
        editor.CommitAttributes();                      // 取り消した後の確定は何もしない
        Assert.Equal(before, UndoDepth());
    }

    // T-EDC-29：論理座標は Y 上向きのため、右下へのずれは (X + 10·repeat, Y − 10·repeat)
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Paste_OffsetsByTenTimesRepeatToLowerRight(int repeat)
    {
        var copied = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(100, 100), new(200, 150));

        Assert.True(editor.Paste(copied, repeat));

        Assert.Equal([new(100 + 10 * repeat, 100 - 10 * repeat), new(200 + 10 * repeat, 150 - 10 * repeat)],
                     Assert.Single(drawing.Shapes).HandlePoints);
    }

    // T-EDC-30
    [Fact]
    public void Paste_SwitchesToSelectAndSelectsPastedShapeAsOneUndoUnit()
    {
        editor.SetTool(Tool.Polygon);

        editor.Paste(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(100, 100), new(200, 150)), 1);

        Assert.Equal(Tool.Select, editor.Tool);
        Assert.Equal(drawing.Shapes[0].Id, editor.SelectedId);
        Assert.Equal(1, UndoDepth());
        editor.Undo();
        Assert.Empty(drawing.Shapes);
    }

    // T-EDC-31
    [Fact]
    public void Paste_GivesNewShapeId()
    {
        var copied = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(100, 100), new(200, 150));

        editor.Paste(copied, 1);
        editor.Paste(copied, 2);

        Assert.All(drawing.Shapes, shape => Assert.NotEqual(copied.Id, shape.Id));
        Assert.NotEqual(drawing.Shapes[0].Id, drawing.Shapes[1].Id);
    }

    // ツールの切り替え・点の追加・プレビューの更新を、それぞれ View へ知らせる
    [Fact]
    public void StateChanged_IsRaisedByPointerInput()
    {
        var changes = 0;
        editor.StateChanged += (_, _) => changes++;
        editor.SetTool(Tool.Line);

        Click(10, 10);
        Move(20, 20);

        Assert.True(changes >= 3);
    }
}
