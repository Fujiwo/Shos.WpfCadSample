using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 選択図形の属性の編集。連続的な編集は記録せずに表示し、確定で 1 回だけ記録する（仕様 5.4、設計書 5.3）。
/// </summary>
sealed class AttributeEditor(Drawing drawing)
{
    // 編集中の図形の添字と、編集を始める前の図形。編集中でなければ null
    (int Index, Shape Original)? editing;

    /// <summary>
    /// 図形の属性を、記録せずに変えて表示する。最初の呼び出しで、元の図形を覚える。図形がなければ何もしない。
    /// </summary>
    public void Preview(ShapeId id, ShapeAttributes attributes)
    {
        var index = drawing.IndexOf(id);
        if (index < 0)
            return;
        editing ??= (index, drawing.Shapes[index]);
        // 表示中の図形ではなく元の図形から作る（属性だけを変え、ほかは元のまま保つ）
        drawing.PreviewReplace(index, editing.Value.Original with { Attributes = attributes });
    }

    /// <summary>
    /// 属性を変えた図形を 1 回だけ記録する。属性が変わっていなければ、元へ戻すだけで記録しない。
    /// </summary>
    public void Commit()
    {
        if (editing is not { } current)
            return;
        var edited = drawing.Shapes[current.Index];
        editing = null;
        if (edited.Attributes == current.Original.Attributes)
            drawing.RestoreOriginal(current.Index, current.Original);
        else
            drawing.CommitPreview(current.Index, current.Original, edited);
    }

    /// <summary>
    /// 確定していない編集を取り消し、元の属性へ戻す。履歴は増えない。
    /// </summary>
    public void Cancel()
    {
        if (editing is { } current)
            drawing.RestoreOriginal(current.Index, current.Original);
        editing = null;
    }
}
