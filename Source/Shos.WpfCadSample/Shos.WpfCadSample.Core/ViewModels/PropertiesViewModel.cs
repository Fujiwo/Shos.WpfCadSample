using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// 属性ウィンドウの状態：選択図形の属性（選択がなければ空欄・無効）と、新規図形の既定属性（仕様 4.5、5.4、設計書 5.4）。
/// </summary>
public sealed partial class PropertiesViewModel : ObservableObject
{
    readonly Drawing drawing;
    readonly EditorController editor;
    ShapeId? shownId;

    public PropertiesViewModel(Drawing drawing, EditorController editor)
    {
        this.drawing = drawing;
        this.editor = editor;
        SelectedAttributes = new(editor.PreviewAttributes);
        DefaultAttributes = new(attributes => editor.DefaultAttributes = attributes);
        DefaultAttributes.Load(editor.DefaultAttributes);
        editor.StateChanged += (_, _) => Refresh();
        editor.Committed += (_, _) => Refresh();
    }

    [ObservableProperty]
    public partial bool HasSelection { get; private set; }

    /// <summary>
    /// 選択図形の種類の名前。選択がなければ空。
    /// </summary>
    [ObservableProperty]
    public partial string ShapeTypeName { get; private set; } = "";

    public AttributeEditModel SelectedAttributes { get; }

    /// <summary>
    /// 新規図形の既定属性。編集は <see cref="EditorController.DefaultAttributes"/> にすぐ反映し、Undo の対象にしない。
    /// </summary>
    public AttributeEditModel DefaultAttributes { get; }

    /// <summary>
    /// 設定から読み込んだ既定属性を、表示と <see cref="EditorController"/> に反映する。
    /// </summary>
    public void LoadDefaults(ShapeAttributes attributes)
    {
        editor.DefaultAttributes = attributes;
        DefaultAttributes.Load(attributes);
    }

    /// <summary>
    /// 連続的な編集の完了（スライダーを離す、Enter、フォーカスの移動、リストの選択）：不正な入力を戻し、選択図形の属性を 1 回の Undo 単位として記録する。
    /// </summary>
    [RelayCommand]
    void CommitEdit()
    {
        SelectedAttributes.RevertInvalidInput();
        DefaultAttributes.RevertInvalidInput();
        editor.CommitAttributes();
    }

    // 選択の変更と、Undo / Redo による属性の変化に、表示を合わせる
    void Refresh()
    {
        var index = editor.SelectedId is { } id ? drawing.IndexOf(id) : -1;
        var shape = index >= 0 ? drawing.Shapes[index] : null;
        HasSelection = shape is not null;
        ShapeTypeName = shape is null ? "" : DisplayName(shape.Kind);
        if (shape is not null && (shape.Id != shownId || shape.Attributes != SelectedAttributes.Attributes))
            SelectedAttributes.Load(shape.Attributes);
        shownId = shape?.Id;
    }

    static string DisplayName(ShapeKind kind) => kind switch {
        ShapeKind.OpenCurve => "Open Curve",
        ShapeKind.ClosedCurve => "Closed Curve",
        _ => kind.ToString()
    };
}
