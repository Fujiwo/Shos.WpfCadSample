using System.Collections.Specialized;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Tests.Models;

/// <summary>
/// T-DRW：<see cref="Drawing"/>（テスト計画書 4.8）
/// </summary>
public class DrawingTests
{
    static LineShape NewLine(double x) => new(ShapeId.New(), ShapeAttributes.Default, new(x, 0), new(x, 10));

    static Drawing DrawingWith(params Shape[] shapes)
    {
        var drawing = new Drawing();
        foreach (var shape in shapes)
            drawing.Add(shape);
        return drawing;
    }

    static int CountCommitted(Drawing drawing, Action action)
    {
        var count = 0;
        drawing.Committed += (_, _) => count++;
        action();
        return count;
    }

    static int CountCollectionChanged(Drawing drawing, Action action)
    {
        var count = 0;
        ((INotifyCollectionChanged)drawing.Shapes).CollectionChanged += (_, _) => count++;
        action();
        return count;
    }

    // T-DRW-01
    [Fact]
    public void Add_CanBeUndoneAndRedone()
    {
        var shape = NewLine(1);
        var drawing = DrawingWith(shape);
        Assert.Equal([shape], drawing.Shapes);

        drawing.Undo();
        Assert.Empty(drawing.Shapes);

        drawing.Redo();
        Assert.Equal([shape], drawing.Shapes);
    }

    // T-DRW-02
    [Fact]
    public void RemoveAt_Undo_RestoresShapeAtOriginalPosition()
    {
        Shape[] shapes = [NewLine(1), NewLine(2), NewLine(3)];
        var drawing = DrawingWith(shapes);

        drawing.RemoveAt(1);
        Assert.Equal([shapes[0], shapes[2]], drawing.Shapes);

        drawing.Undo();
        Assert.Equal(shapes, drawing.Shapes);
    }

    // T-DRW-03
    [Fact]
    public void Replace_CanBeUndoneAndRedone()
    {
        var original = NewLine(1);
        var drawing = DrawingWith(original);
        var replaced = original.Translate(new Vector2(5, 5));

        drawing.Replace(0, replaced);
        Assert.Equal([replaced], drawing.Shapes);

        drawing.Undo();
        Assert.Equal([original], drawing.Shapes);

        drawing.Redo();
        Assert.Equal([replaced], drawing.Shapes);
    }

    // T-DRW-04
    [Fact]
    public void PreviewReplace_IsNotRecordedButNotifies()
    {
        var original = NewLine(1);
        var drawing = DrawingWith(original);

        var notifications = CountCollectionChanged(drawing, () => {
            foreach (var step in Enumerable.Range(1, 3))
                drawing.PreviewReplace(0, original.Translate(new Vector2(step, 0)));
        });

        Assert.Equal(3, notifications);
        Assert.Equal(original.Translate(new Vector2(3, 0)), drawing.Shapes[0]);
        drawing.Undo();                     // 履歴は Add だけ
        Assert.Empty(drawing.Shapes);
        Assert.False(drawing.CanUndo);
    }

    // T-DRW-05（R-7：ドラッグ 1 回が Undo 1 回になる）
    [Fact]
    public void PreviewsThenCommitPreview_IsUndoneByOneUndo()
    {
        var original = NewLine(1);
        var drawing = DrawingWith(original);
        var final = original.Translate(new Vector2(30, 0));

        // ドラッグと同じ手順：表示だけを更新し、確定で、元 → 最終の置き換えを 1 回だけ記録する
        foreach (var step in Enumerable.Range(1, 3))
            drawing.PreviewReplace(0, original.Translate(new Vector2(step * 10, 0)));
        drawing.CommitPreview(0, original, final);

        drawing.Undo();
        Assert.Equal([original], drawing.Shapes);

        drawing.Redo();
        Assert.Equal([final], drawing.Shapes);

        drawing.Undo();
        drawing.Undo();                     // 履歴は Add と Replace の 2 つだけ
        Assert.Empty(drawing.Shapes);
        Assert.False(drawing.CanUndo);
    }

    // T-DRW-06（ドラッグの取り消し）
    [Fact]
    public void PreviewThenRestore_KeepsHistoryIncludingRedo()
    {
        LineShape first = NewLine(1), second = NewLine(2);
        var drawing = DrawingWith(first, second);
        drawing.Undo();                     // Redo できる状態

        drawing.PreviewReplace(0, first.Translate(new Vector2(9, 9)));
        drawing.RestoreOriginal(0, first);

        Assert.Equal([first], drawing.Shapes);
        Assert.True(drawing.CanRedo);
        drawing.Redo();
        Assert.Equal([first, second], drawing.Shapes);
        drawing.Undo();
        drawing.Undo();
        Assert.False(drawing.CanUndo);
    }

    // T-DRW-07
    [Fact]
    public void Committed_IsRaisedByRecordedChangesAndUndoRedo()
    {
        var shape = NewLine(1);
        var drawing = new Drawing();

        Assert.Equal(1, CountCommitted(drawing, () => drawing.Add(shape)));
        Assert.Equal(1, CountCommitted(drawing, () => drawing.Replace(0, NewLine(2))));
        Assert.Equal(1, CountCommitted(drawing, drawing.Undo));
        Assert.Equal(1, CountCommitted(drawing, drawing.Redo));
        Assert.Equal(1, CountCommitted(drawing, () => drawing.RemoveAt(0)));
    }

    // T-DRW-07：確定は、元へ戻す置き換えを含めても 1 回だけ通知する
    [Fact]
    public void Committed_IsRaisedOnceByCommitPreview()
    {
        var shape = NewLine(1);
        var drawing = DrawingWith(shape);
        drawing.PreviewReplace(0, NewLine(2));

        Assert.Equal(1, CountCommitted(drawing, () => drawing.CommitPreview(0, shape, NewLine(3))));
    }

    // T-DRW-07
    [Fact]
    public void Committed_IsNotRaisedByPreviewOrRestore()
    {
        var shape = NewLine(1);
        var drawing = DrawingWith(shape);

        Assert.Equal(0, CountCommitted(drawing, () => {
            drawing.PreviewReplace(0, NewLine(2));
            drawing.RestoreOriginal(0, shape);
        }));
    }

    // T-DRW-08
    [Fact]
    public void Load_ReplacesShapesAndClearsHistoryAndModification()
    {
        var drawing = DrawingWith(NewLine(1), NewLine(2));
        drawing.Undo();
        Shape[] loaded = [NewLine(7), NewLine(8), NewLine(9)];

        // Open・New による入れ替えは編集ではないため、Committed を発生させない
        var committed = CountCommitted(drawing, () => drawing.Load(loaded));

        Assert.Equal(loaded, drawing.Shapes);
        Assert.False(drawing.CanUndo);
        Assert.False(drawing.CanRedo);
        Assert.False(drawing.IsModified);
        Assert.Equal(0, committed);
    }

    // T-DRW-09（仮定 A-11：Undo で保存済みの状態に戻っても、変更ありのまま）
    [Fact]
    public void IsModified_FollowsChangesAndSaving()
    {
        var drawing = new Drawing();
        Assert.False(drawing.IsModified);

        drawing.Add(NewLine(1));
        Assert.True(drawing.IsModified);

        drawing.MarkSaved();
        Assert.False(drawing.IsModified);

        // 保存した後に Undo すると、保存した内容とは異なる状態になる
        drawing.Add(NewLine(2));
        drawing.MarkSaved();
        drawing.Undo();
        Assert.True(drawing.IsModified);
    }

    // T-DRW-10
    [Fact]
    public void IndexOf_FindsShapeByIdAfterReplaceAndNotAfterRemoval()
    {
        LineShape first = NewLine(1), second = NewLine(2);
        var drawing = DrawingWith(first, second);

        drawing.Replace(1, second.Translate(new Vector2(3, 3)));
        Assert.Equal(1, drawing.IndexOf(second.Id));

        drawing.RemoveAt(1);
        Assert.Equal(-1, drawing.IndexOf(second.Id));
        Assert.Equal(0, drawing.IndexOf(first.Id));
    }

    // T-DRW-11
    [Fact]
    public void NewChangeAfterUndo_DiscardsRedoHistory()
    {
        var drawing = DrawingWith(NewLine(1), NewLine(2));
        drawing.Undo();
        Assert.True(drawing.CanRedo);

        drawing.Add(NewLine(3));

        Assert.False(drawing.CanRedo);
    }

    // T-DRW-12（仮定 DA-1：履歴の上限は 1,000 操作）
    [Fact]
    public void History_KeepsLatestThousandChanges()
    {
        var drawing = new Drawing();
        foreach (var index in Enumerable.Range(0, 1_001))
            drawing.Add(NewLine(index));

        var undoCount = 0;
        while (drawing.CanUndo) {
            drawing.Undo();
            undoCount++;
        }

        Assert.Equal(1_000, undoCount);
        Assert.Single(drawing.Shapes);      // 最も古い操作（最初の Add）は取り消せない
    }

    [Fact]
    public void UndoOrRedo_WithoutHistory_IsRejected()
    {
        var drawing = new Drawing();

        Assert.Throws<InvalidOperationException>(drawing.Undo);
        Assert.Throws<InvalidOperationException>(drawing.Redo);
    }
}
