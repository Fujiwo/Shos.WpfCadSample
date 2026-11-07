using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 選択図形の属性の編集。連続的な編集は記録せずに表示し、確定で 1 回だけ記録する（仕様 5.4、設計書 5.3）。
/// </summary>
sealed class AttributeEditor(Drawing drawing)
{
    (int Index, Shape Original)? editing;

    public void Preview(ShapeId id, ShapeAttributes attributes)
    {
        var index = drawing.IndexOf(id);
        if (index < 0)
            return;
        editing ??= (index, drawing.Shapes[index]);
        drawing.PreviewReplace(index, editing.Value.Original with { Attributes = attributes });
    }

    /// <summary>
    /// 元の図形へ戻してから、属性を変えた図形を 1 回だけ記録する。属性が変わっていなければ記録しない。
    /// </summary>
    public void Commit()
    {
        if (editing is not { } current)
            return;
        var edited = drawing.Shapes[current.Index];
        editing = null;
        drawing.RestoreOriginal(current.Index, current.Original);
        if (edited.Attributes != current.Original.Attributes)
            drawing.Replace(current.Index, edited);
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
