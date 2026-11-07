using System.Collections.ObjectModel;
using Shos.Collections;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// ドキュメント：図形の一覧と、その Undo / Redo、未保存の状態を保つ（設計書 5.1）。
/// 変更は、このクラスの操作だけで行う（<see cref="IsModified"/> と <see cref="Committed"/> を迂回させない）。
/// </summary>
public sealed class Drawing
{
    const int maximumUndoTimes = 1_000; // 設計書 DA-1

    readonly UndoRedoObservableCollection<Shape> shapes = new(maximumUndoTimes);

    public Drawing() => Shapes = new(shapes.List);

    /// <summary>
    /// 図形の一覧（描画順。後ろほど上）。View は、この一覧にバインドする。
    /// </summary>
    public ReadOnlyObservableCollection<Shape> Shapes { get; }

    /// <summary>
    /// 最後の保存（または読み込み）の後に変更があったか。Undo / Redo も変更とみなす（仕様 A-11）。
    /// </summary>
    public bool IsModified { get; private set; }

    public bool CanUndo => shapes.CanUndo;
    public bool CanRedo => shapes.CanRedo;

    /// <summary>
    /// 記録される変更（追加・削除・置き換え・Undo・Redo）の後に発生する。購読するのは EditorController だけとする（設計書 5.3）。
    /// </summary>
    public event EventHandler? Committed;

    public void Add(Shape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        shapes.Add(shape);
        OnCommitted();
    }

    public void RemoveAt(int index)
    {
        shapes.RemoveAt(index);
        OnCommitted();
    }

    public void Replace(int index, Shape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        shapes[index] = shape;
        OnCommitted();
    }

    /// <summary>
    /// ドラッグ中などの表示のための置き換え。履歴に記録せず、<see cref="Committed"/> も発生しない（設計書 DD-3）。
    /// </summary>
    public void PreviewReplace(int index, Shape shape) => ReplaceWithoutRecording(index, shape);

    /// <summary>
    /// <see cref="PreviewReplace"/> の前の図形へ戻す（記録しない）。確定するときは、この後に <see cref="Replace"/> で 1 回だけ記録する。
    /// </summary>
    public void RestoreOriginal(int index, Shape original) => ReplaceWithoutRecording(index, original);

    public void Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("There is nothing to undo.");
        shapes.Undo();
        OnCommitted();
    }

    public void Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("There is nothing to redo.");
        shapes.Redo();
        OnCommitted();
    }

    /// <summary>
    /// 一覧を入れ替え、履歴を消去し、未変更とする（Open・New）。<see cref="Committed"/> は発生しない。
    /// </summary>
    public void Load(IEnumerable<Shape> loadedShapes)
    {
        using (new UndoRedoList<Shape, ObservableCollection<Shape>>.DisabledUndoScope(shapes)) {
            shapes.Clear();
            foreach (var shape in loadedShapes)
                shapes.Add(shape);
        }
        shapes.ClearUndo();
        IsModified = false;
    }

    public void MarkSaved() => IsModified = false;

    /// <summary>
    /// 識別子が同じ図形の添字。なければ −1。
    /// </summary>
    public int IndexOf(ShapeId id)
    {
        for (var index = 0; index < shapes.Count; index++) {
            if (shapes[index].Id == id)
                return index;
        }
        return -1;
    }

    void ReplaceWithoutRecording(int index, Shape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        using (new UndoRedoList<Shape, ObservableCollection<Shape>>.DisabledUndoScope(shapes))
            shapes[index] = shape;
    }

    void OnCommitted()
    {
        IsModified = true;
        Committed?.Invoke(this, EventArgs.Empty);
    }
}
