using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.Serialization;
using Shos.WpfCadSample.Services;
using Shos.WpfCadSample.Settings;

namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// アプリケーション全体の状態とコマンド：ファイル操作の流れ（保存確認、エラー表示）、タイトル、クリップボード、
/// 属性ウィンドウの表示状態、ウィンドウの配置、設定（設計書 5.4、6.4、6.5）。
/// </summary>
/// <remarks>
/// 捕捉する例外は、<see cref="DocumentFormatException"/>、<see cref="IOException"/>、<see cref="UnauthorizedAccessException"/>、
/// <see cref="ClipboardOperationException"/> の 4 種に限り、ほかの例外は握りつぶさない（設計書 8.2）。
/// </remarks>
public sealed partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// アプリケーションの名前。タイトルバーとメッセージボックスの見出しに使う（仕様 4.1、4.6）。
    /// </summary>
    public const string ApplicationName = "WPF CAD Sample";

    /// <summary>
    /// 文書のファイルの拡張子（仕様 5.8）。保存時の補完と、ファイルダイアログのフィルターに使う。
    /// </summary>
    public const string DocumentExtension = ".json";

    const string untitledName = "Untitled";

    readonly Drawing document = new();
    readonly EditorController editor;
    readonly IDialogService dialogs;
    readonly IClipboardService clipboard;
    readonly DocumentStorage documentStorage;
    readonly SettingsStorage settingsStorage;
    readonly IRelayCommand[] stateDependentCommands;

    string? documentPath;
    bool clipboardHasShapes;
    string? lastPastedText;
    int pasteCount;

    public MainViewModel(IDialogService dialogs, IClipboardService clipboard, DocumentStorage documentStorage, SettingsStorage settingsStorage)
    {
        this.dialogs = dialogs;
        this.clipboard = clipboard;
        this.documentStorage = documentStorage;
        this.settingsStorage = settingsStorage;

        var viewport = new Viewport();
        editor = new EditorController(document, viewport);
        Drawing = new DrawingViewModel(document, editor, viewport);
        Properties = new PropertiesViewModel(document, editor);
        stateDependentCommands = [UndoCommand, RedoCommand, CopyCommand, PasteCommand, DeleteCommand, CopyMetafileCommand, CopyImageCommand];

        editor.StateChanged += (_, _) => RefreshCommands();
        editor.Committed += (_, _) => OnDocumentChanged();
    }

    public DrawingViewModel Drawing { get; }

    public PropertiesViewModel Properties { get; }

    /// <summary>
    /// <c>&lt;ファイル名または Untitled&gt;[*] - WPF CAD Sample</c>（仕様 4.1）。
    /// </summary>
    public string Title => $"{DocumentName}{(document.IsModified ? "*" : "")} - {ApplicationName}";

    [ObservableProperty]
    public partial bool IsPropertiesWindowVisible { get; set; } = AppSettings.Default.PropertiesWindow.IsVisible;

    /// <summary>
    /// メインウィンドウの配置。View が、起動時に読み、終了時に書く。
    /// </summary>
    public WindowPlacement MainPlacement { get; set; } = AppSettings.Default.MainWindow;

    /// <summary>
    /// 属性ウィンドウの配置。View が、起動時に読み、終了時に書く。
    /// </summary>
    public WindowPlacement PropertiesPlacement { get; set; } = AppSettings.Default.PropertiesWindow.Placement;

    /// <summary>
    /// Exit：メインウィンドウを閉じるよう求める。保存確認は、閉じるときの <see cref="TryClose"/> が行う（設計書 6.5）。
    /// </summary>
    public event EventHandler? CloseRequested;

    string DocumentName => documentPath is null ? untitledName : Path.GetFileName(documentPath);

    bool IsBusy => editor.IsInputInProgress || editor.IsDragging;

    /// <summary>
    /// 起動時に設定を読み込み、既定属性とウィンドウの配置に反映する。設定が不正だったときは警告を表示する（仕様 5.9、7 章）。
    /// </summary>
    public void LoadSettings()
    {
        SettingsLoadResult result;
        try {
            result = settingsStorage.Load();
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            result = new(AppSettings.Default, $"Settings could not be loaded. Default settings are used.\n{exception.Message}");
        }

        var settings = result.Settings;
        MainPlacement = settings.MainWindow;
        PropertiesPlacement = settings.PropertiesWindow.Placement;
        IsPropertiesWindowVisible = settings.PropertiesWindow.IsVisible;
        Properties.LoadDefaults(settings.DefaultAttributes);
        if (result.Warning is { } warning)
            dialogs.ShowWarning(warning);
    }

    /// <summary>
    /// メインウィンドウを閉じる直前（Exit、閉じるボタン、Alt+F4）：入力中の図形を取り消し、保存確認をして、閉じてよいかを返す。
    /// 閉じてよいときは設定を保存する（失敗は表示だけで、閉じるのを妨げない）。
    /// </summary>
    public bool TryClose()
    {
        Drawing.CancelInput();
        if (!ConfirmDiscardChanges())
            return false;
        SaveSettings();
        return true;
    }

    /// <summary>
    /// 設定を組み立てて保存する（終了時だけ。仕様 A-22）。設定の組み立てと保存は、ここ 1 か所で行う。
    /// </summary>
    public void SaveSettings()
    {
        var settings = new AppSettings(AppSettings.CurrentVersion, MainPlacement,
                                       new PropertiesWindowSettings(IsPropertiesWindowVisible, PropertiesPlacement),
                                       editor.DefaultAttributes);
        try {
            settingsStorage.Save(settings);
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            dialogs.ShowError("Settings", $"Could not save the settings.\n{exception.Message}");
        }
    }

    [RelayCommand]
    void New()
    {
        Drawing.CancelInput();
        if (!ConfirmDiscardChanges())
            return;
        ReplaceDocument([], path: null);
    }

    [RelayCommand]
    void Open()
    {
        Drawing.CancelInput();
        if (!ConfirmDiscardChanges() || dialogs.ShowOpenFileDialog() is not { } path)
            return;

        IReadOnlyList<Shape> shapes;
        try {
            shapes = documentStorage.Load(path);
        } catch (DocumentFormatException exception) {
            dialogs.ShowError("Open", exception.Error == DocumentFormatError.NewerVersion
                ? $"This file was created by a newer version and cannot be opened.\nFile: {path}"
                : $"The file is not a valid CAD document.\n{exception.Reason}\nFile: {path}");
            return;
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            dialogs.ShowError("Open", $"Could not open the file.\n{exception.Message}\nFile: {path}");
            return;
        }
        ReplaceDocument(shapes, path);
    }

    [RelayCommand]
    void Save() => SaveDocument();

    [RelayCommand]
    void SaveAs() => SaveDocumentAs();

    [RelayCommand]
    void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanUndo))]
    void Undo() => editor.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    void Redo() => editor.Redo();

    [RelayCommand(CanExecute = nameof(CanDelete))]
    void Delete() => editor.DeleteSelected();

    /// <summary>
    /// 選択図形を独自形式でコピーし、貼り付けの回数を 0 に戻す（仕様 5.7）。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCopy))]
    void Copy()
    {
        if (SelectedShape() is not { } shape)
            return;
        if (!TryClipboard("Copy", () => clipboard.SetShapesText(ClipboardShapes.ToText(shape))))
            return;
        pasteCount = 0;
        clipboardHasShapes = true;
        RefreshCommands();
    }

    /// <summary>
    /// 独自形式の図形を貼り付ける。前回と同じテキストを続けて貼ると、ずらし量が累積する（仕様 5.7、設計書 5.4）。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPaste))]
    void Paste()
    {
        string? text;
        try {
            text = clipboard.GetShapesText();
        } catch (ClipboardOperationException exception) {
            ShowClipboardError("Paste", exception);
            return;
        }
        if (text is null)
            return;

        Shape shape;
        try {
            shape = ClipboardShapes.FromText(text);
        } catch (DocumentFormatException exception) {
            dialogs.ShowError("Paste", exception.Error == DocumentFormatError.NewerVersion
                ? "The clipboard data was created by a newer version and cannot be pasted."
                : $"The clipboard data is not a valid shape.\n{exception.Reason}");
            return;
        }

        var repeat = text == lastPastedText ? pasteCount + 1 : 1;
        if (!editor.Paste(shape, repeat)) {
            dialogs.ShowError("Paste", "The shape was not pasted because, after the offset, it would be outside the allowed coordinate range or violate the shape constraints.");
            return;
        }
        (lastPastedText, pasteCount) = (text, repeat);
    }

    [RelayCommand(CanExecute = nameof(CanCopyPicture))]
    void CopyMetafile() => TryClipboard("Copy as Metafile", () => clipboard.CopyAsMetafile(PictureTargets()));

    [RelayCommand(CanExecute = nameof(CanCopyPicture))]
    void CopyImage() => TryClipboard("Copy as Image", () => clipboard.CopyAsImage(PictureTargets()));

    [RelayCommand]
    void ToggleProperties() => IsPropertiesWindowVisible = !IsPropertiesWindowVisible;

    /// <summary>
    /// Paste の有効状態を、クリップボードの内容に合わせる（WPF にはクリップボードの変更通知がないため。リスク R-5）。
    /// View が、ウィンドウの再アクティブ化と Edit メニューを開いたときに呼ぶ。
    /// </summary>
    [RelayCommand]
    void RefreshClipboardState()
    {
        clipboardHasShapes = clipboard.ContainsShapes;
        PasteCommand.NotifyCanExecuteChanged();
    }

    // 入力中・ドラッグ中のコマンドの制限（仕様 5.10）と、それぞれの前提（仕様 4.2）
    bool CanUndo() => !IsBusy && document.CanUndo;
    bool CanRedo() => !IsBusy && document.CanRedo;
    bool CanDelete() => !IsBusy && SelectedShape() is not null;
    bool CanCopy() => !editor.IsDragging && SelectedShape() is not null;
    bool CanPaste() => !IsBusy && clipboardHasShapes;
    bool CanCopyPicture() => !editor.IsDragging && document.Shapes.Count > 0;

    Shape? SelectedShape()
    {
        var index = editor.SelectedId is { } id ? document.IndexOf(id) : -1;
        return index >= 0 ? document.Shapes[index] : null;
    }

    // 選択があればその図形、なければドキュメント全体（入力中の図形は含まない。仕様 5.7、5.10）
    IReadOnlyList<Shape> PictureTargets() => SelectedShape() is { } shape ? [shape] : [.. document.Shapes];

    bool TryClipboard(string operation, Action action)
    {
        try {
            action();
            return true;
        } catch (ClipboardOperationException exception) {
            ShowClipboardError(operation, exception);
            return false;
        }
    }

    void ShowClipboardError(string operation, ClipboardOperationException exception)
        => dialogs.ShowError(operation, $"The clipboard operation failed.\n{exception.Message}");

    // 未保存の変更があれば保存確認をする。続けてよいか（Yes の後の保存失敗・Save As のキャンセルは中止。仕様 5.8、AC-34）
    bool ConfirmDiscardChanges()
    {
        if (!document.IsModified)
            return true;
        return dialogs.AskSaveChanges(DocumentName) switch {
            SaveChangesAnswer.Yes => SaveDocument(),
            SaveChangesAnswer.No => true,
            _ => false
        };
    }

    bool SaveDocument() => documentPath is { } path ? SaveDocumentTo(path) : SaveDocumentAs();

    bool SaveDocumentAs()
    {
        var suggestedName = documentPath is null ? untitledName + DocumentExtension : DocumentName;
        return dialogs.ShowSaveFileDialog(suggestedName) is { } path && SaveDocumentTo(WithDocumentExtension(path));
    }

    bool SaveDocumentTo(string path)
    {
        try {
            documentStorage.Save(path, document.Shapes);
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            dialogs.ShowError("Save", $"Could not save the file.\n{exception.Message}\nFile: {path}");
            return false;
        }
        documentPath = path;
        document.MarkSaved();
        OnPropertyChanged(nameof(Title));
        return true;
    }

    void ReplaceDocument(IReadOnlyList<Shape> shapes, string? path)
    {
        document.Load(shapes);
        documentPath = path;
        Drawing.ResetForNewDocument();
        OnDocumentChanged();
    }

    void OnDocumentChanged()
    {
        OnPropertyChanged(nameof(Title));
        RefreshCommands();
    }

    void RefreshCommands()
    {
        foreach (var command in stateDependentCommands)
            command.NotifyCanExecuteChanged();
    }

    // 拡張子が .json でなければ補う（仕様 5.8）
    static string WithDocumentExtension(string path)
        => Path.GetExtension(path).Equals(DocumentExtension, StringComparison.OrdinalIgnoreCase) ? path : path + DocumentExtension;
}
