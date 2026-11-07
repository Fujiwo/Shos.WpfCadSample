using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.Serialization;
using Shos.WpfCadSample.Services;
using Shos.WpfCadSample.Settings;
using Shos.WpfCadSample.Tests.Support;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Tests.ViewModels;

/// <summary>
/// T-VM-01〜14、23〜28：<see cref="MainViewModel"/>（テスト計画書 4.13。偽の IDialogService、IClipboardService、一時フォルダー）。
/// ビューサイズ 1000 × 700（FitScale = 1）。画面座標 (x, 700 − y) ↔ 論理座標 (x, y)。
/// </summary>
public sealed class MainViewModelTests : IDisposable
{
    readonly TempFolder folder = new();
    readonly FakeDialogService dialogs = new();
    readonly FakeClipboardService clipboard = new();
    readonly MainViewModel main;

    public MainViewModelTests()
    {
        main = new MainViewModel(dialogs, clipboard, new DocumentStorage(), new SettingsStorage(SettingsPath));
        main.Drawing.SetViewSizeCommand.Execute(new ViewSize(1000, 700));
    }

    public void Dispose() => folder.Dispose();

    string SettingsPath => folder.Combine("settings", "settings.json");

    static Point2 Screen(double x, double y) => new(x, 700 - y);

    void Pointer(PointerAction action, double x, double y, int clickCount = 1)
        => main.Drawing.PointerCommand.Execute(new PointerEvent(action, Screen(x, y), clickCount));

    void Click(double x, double y)
    {
        Pointer(PointerAction.Down, x, y);
        Pointer(PointerAction.Up, x, y);
    }

    void DrawLine(double x1, double y1, double x2, double y2)
    {
        main.Drawing.SelectToolCommand.Execute(Tool.Line);
        Click(x1, y1);
        Click(x2, y2);
        main.Drawing.SelectToolCommand.Execute(Tool.Select);
    }

    void StartPolylineInput()
    {
        main.Drawing.SelectToolCommand.Execute(Tool.Polyline);
        Click(500, 500);
        Click(600, 500);
        Assert.NotEmpty(main.Drawing.InputShapes);
    }

    // 既存のファイル：論理領域の外（X = 2,000）まで伸びる線分 1 本。開いた後の全体図の変化も確かめられる
    string SaveExistingFile(string name)
    {
        var path = folder.Combine(name);
        new DocumentStorage().Save(path, [new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(2000, 0))]);
        return path;
    }

    // 3 ノッチ拡大し、全体図の表示（100%）から外す
    void ZoomIn() => main.Drawing.ZoomCommand.Execute(new ZoomRequest(Screen(500, 350), 3));

    static bool CanExecute(System.Windows.Input.ICommand command) => command.CanExecute(null);

    // T-VM-01
    [Fact]
    public void Title_ShowsNameAndModification()
    {
        Assert.Equal("Untitled - WPF CAD Sample", main.Title);

        DrawLine(100, 100, 200, 200);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);

        dialogs.SavePath = folder.Combine("drawing.json");
        main.SaveCommand.Execute(null);
        Assert.Equal("drawing.json - WPF CAD Sample", main.Title);
    }

    // T-VM-02
    [Fact]
    public void New_WithChanges_YesSavesThenClears()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Yes;
        dialogs.SavePath = folder.Combine("saved.json");

        main.NewCommand.Execute(null);

        Assert.Empty(main.Drawing.Shapes);
        Assert.Single(new DocumentStorage().Load(folder.Combine("saved.json")));
        Assert.Equal("Untitled - WPF CAD Sample", main.Title);
    }

    // T-VM-02
    [Fact]
    public void New_WithChanges_NoClearsWithoutSaving()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.No;

        main.NewCommand.Execute(null);

        Assert.Empty(main.Drawing.Shapes);
        Assert.DoesNotContain(nameof(IDialogService.ShowSaveFileDialog), dialogs.Calls);
    }

    // T-VM-02
    [Fact]
    public void New_WithChanges_CancelDoesNothing()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Cancel;

        main.NewCommand.Execute(null);

        Assert.Single(main.Drawing.Shapes);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
    }

    [Fact]
    public void New_WithoutChanges_DoesNotAsk()
    {
        main.NewCommand.Execute(null);

        Assert.DoesNotContain(nameof(IDialogService.AskSaveChanges), dialogs.Calls);
    }

    // T-VM-03
    [Fact]
    public void NewOpenAndClose_CancelInputBeforeAskingToSave()
    {
        DrawLine(100, 100, 200, 200);
        // 保存確認を表示した時点の、入力中の図形の数を記録する
        var inputShapesWhenAsked = new List<int>();
        dialogs.AskingSaveChanges = () => inputShapesWhenAsked.Add(main.Drawing.InputShapes.Count);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Cancel;

        StartPolylineInput();
        main.NewCommand.Execute(null);
        StartPolylineInput();
        main.OpenCommand.Execute(null);
        StartPolylineInput();
        main.TryClose();

        Assert.Equal([0, 0, 0], inputShapesWhenAsked);
    }

    // T-VM-04
    [Fact]
    public void Open_ReplacesDocumentAndShowsOverviewWithoutSelection()
    {
        DrawLine(100, 100, 200, 200);
        Click(150, 150);
        ZoomIn();
        dialogs.OpenPath = SaveExistingFile("opened.json");

        main.OpenCommand.Execute(null);

        var shape = Assert.Single(main.Drawing.Shapes);
        Assert.Equal(new Point2(2000, 0), shape.HandlePoints[1]);
        Assert.Equal("Zoom: 100%", main.Drawing.ZoomText);
        Assert.Empty(main.Drawing.Handles);
        Assert.Equal("opened.json - WPF CAD Sample", main.Title);
        Assert.False(CanExecute(main.UndoCommand));
    }

    // T-VM-05
    [Theory]
    [InlineData("""{ "format": "Other", "version": 1, "shapes": [] }""", "The file is not a valid CAD document.")]
    [InlineData("""{ "format": "Shos.WpfCadSample.Document", "version": 2, "shapes": [] }""", "This file was created by a newer version and cannot be opened.")]
    public void Open_InvalidOrNewerFile_ShowsErrorAndKeepsDocument(string content, string expectedMessage)
    {
        DrawLine(100, 100, 200, 200);
        var path = folder.Combine("bad.json");
        File.WriteAllText(path, content);
        dialogs.OpenPath = path;

        main.OpenCommand.Execute(null);

        var error = Assert.Single(dialogs.Errors);
        Assert.StartsWith(expectedMessage, error.Message);
        Assert.Contains("bad.json", error.Message);
        Assert.Single(main.Drawing.Shapes);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
    }

    // T-VM-05
    [Fact]
    public void Open_MissingFile_ShowsError()
    {
        dialogs.OpenPath = folder.Combine("missing.json");

        main.OpenCommand.Execute(null);

        Assert.Contains("missing.json", Assert.Single(dialogs.Errors).Message);
    }

    [Fact]
    public void Open_CanceledDialog_KeepsDocument()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.OpenPath = null;

        main.OpenCommand.Execute(null);

        Assert.Single(main.Drawing.Shapes);
        Assert.Empty(dialogs.Errors);
    }

    // T-VM-06
    [Fact]
    public void Save_WithoutPath_AsksForPathAndAddsJsonExtension()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SavePath = folder.Combine("drawing");

        main.SaveCommand.Execute(null);

        Assert.True(File.Exists(folder.Combine("drawing.json")));
        Assert.Equal("drawing.json - WPF CAD Sample", main.Title);

        DrawLine(300, 300, 400, 400);
        dialogs.Calls.Clear();
        main.SaveCommand.Execute(null);                 // 2 回目は同じパスへ上書き
        Assert.DoesNotContain(nameof(IDialogService.ShowSaveFileDialog), dialogs.Calls);
        Assert.Equal(2, new DocumentStorage().Load(folder.Combine("drawing.json")).Count);
    }

    // T-VM-06
    [Fact]
    public void SaveAs_KeepsJsonExtensionAndSuggestsCurrentName()
    {
        // 大文字の .JSON も拡張子として認め、.json を付け足さない。2 回目の提案名は、現在のファイル名
        dialogs.SavePath = folder.Combine("first.JSON");
        main.SaveAsCommand.Execute(null);
        Assert.True(File.Exists(folder.Combine("first.JSON")));

        dialogs.SavePath = folder.Combine("second.json");
        main.SaveAsCommand.Execute(null);

        Assert.Equal(["Untitled.json", "first.JSON"], dialogs.SuggestedNames);
        Assert.Equal("second.json - WPF CAD Sample", main.Title);
    }

    // T-VM-07
    [Fact]
    public void Save_FailureOnLockedFile_ShowsErrorAndKeepsModified()
    {
        var path = SaveExistingFile("locked.json");
        var original = File.ReadAllText(path);
        DrawLine(100, 100, 200, 200);
        dialogs.SavePath = path;

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            main.SaveCommand.Execute(null);

        Assert.Contains("locked.json", Assert.Single(dialogs.Errors).Message);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
        Assert.Equal(original, File.ReadAllText(path));
    }

    // T-VM-07
    [Fact]
    public void Save_FailureOnReadOnlyFile_ShowsErrorAndKeepsModified()
    {
        var path = SaveExistingFile("readonly.json");
        var original = File.ReadAllText(path);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        DrawLine(100, 100, 200, 200);
        dialogs.SavePath = path;

        main.SaveCommand.Execute(null);

        Assert.Single(dialogs.Errors);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
        Assert.Equal(original, File.ReadAllText(path));
    }

    // T-VM-08
    [Fact]
    public void TryClose_WithChangesAndCancel_ReturnsFalse()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Cancel;

        Assert.False(main.TryClose());
        Assert.False(File.Exists(SettingsPath));    // 閉じないため、設定も保存しない
    }

    // T-VM-09
    [Fact]
    public void Paste_InvalidClipboardData_ShowsErrorAndKeepsDocument()
    {
        clipboard.ShapesText = "{ not valid }";
        // Paste の有効状態は、RefreshClipboardState で調べたときのクリップボードに従う（View がアクティブ化などで呼ぶ）
        main.RefreshClipboardStateCommand.Execute(null);

        main.PasteCommand.Execute(null);

        Assert.Single(dialogs.Errors);
        Assert.Empty(main.Drawing.Shapes);
    }

    // T-VM-10
    [Fact]
    public void ClipboardFailures_AreShownAndDoNotThrow()
    {
        DrawLine(100, 100, 200, 200);
        Click(150, 150);
        clipboard.Fails = true;

        main.CopyCommand.Execute(null);
        main.CopyMetafileCommand.Execute(null);
        main.CopyImageCommand.Execute(null);

        Assert.Equal(3, dialogs.Errors.Count);
        Assert.Single(main.Drawing.Shapes);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
    }

    // T-VM-10（貼り付けで読み取りに失敗）
    [Fact]
    public void Paste_ClipboardReadFailure_IsShown()
    {
        // 有効状態の確認までは成功させ、貼り付けの読み取りで失敗させる
        clipboard.ShapesText = ClipboardShapes.ToText(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(1, 1)));
        main.RefreshClipboardStateCommand.Execute(null);
        clipboard.Fails = true;

        main.PasteCommand.Execute(null);

        Assert.Single(dialogs.Errors);
        Assert.Empty(main.Drawing.Shapes);
    }

    // T-VM-11
    [Fact]
    public void ClipboardCommands_AreEnabledAccordingToState()
    {
        Assert.False(CanExecute(main.CopyCommand));
        Assert.False(CanExecute(main.DeleteCommand));
        Assert.False(CanExecute(main.CopyMetafileCommand));
        Assert.False(CanExecute(main.CopyImageCommand));
        Assert.False(CanExecute(main.PasteCommand));

        DrawLine(100, 100, 200, 200);
        Assert.True(CanExecute(main.CopyMetafileCommand));
        Assert.True(CanExecute(main.CopyImageCommand));
        Assert.False(CanExecute(main.CopyCommand));

        Click(150, 150);
        Assert.True(CanExecute(main.CopyCommand));
        Assert.True(CanExecute(main.DeleteCommand));

        clipboard.ShapesText = "anything";
        Assert.False(CanExecute(main.PasteCommand));    // 更新するまでは、前回の状態のまま
        main.RefreshClipboardStateCommand.Execute(null);
        Assert.True(CanExecute(main.PasteCommand));
    }

    // T-VM-11（Copy の後は Paste できる。Copy as Metafile / Image は、選択があればその図形、なければ全体）
    [Fact]
    public void Copy_EnablesPasteAndMetafileTargetsSelectionOrAll()
    {
        DrawLine(100, 100, 200, 200);
        DrawLine(300, 300, 400, 400);

        main.CopyMetafileCommand.Execute(null);
        Assert.Equal(2, clipboard.Metafiles[^1].Count);

        Click(350, 350);
        main.CopyImageCommand.Execute(null);
        Assert.Equal(main.Drawing.Shapes[1].Id, Assert.Single(clipboard.Images[^1]).Id);

        main.CopyCommand.Execute(null);
        Assert.True(CanExecute(main.PasteCommand));
    }

    // T-VM-12
    [Fact]
    public void LoadSettings_WithWarning_ShowsWarningAndUsesDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ damaged");

        main.LoadSettings();

        Assert.Equal("Settings could not be loaded. Default settings are used. The damaged file was saved as settings.json.bak.",
                     Assert.Single(dialogs.Warnings));
        Assert.Equal(AppSettings.Default.MainWindow, main.MainPlacement);
        Assert.Equal(ShapeAttributes.Default, main.Properties.DefaultAttributes.Attributes);
    }

    // T-VM-12（設定ファイルを読めないとき：他のアプリが排他で開いている。I-4 からの申し送り）
    [Fact]
    public void LoadSettings_UnreadableFile_ShowsWarningAndUsesDefaults()
    {
        // 既定値と異なる設定を保存しておき、読めなかったときに既定値が使われることを区別できるようにする
        new SettingsStorage(SettingsPath).Save(AppSettings.Default with { MainWindow = new WindowPlacement(1, 2, 300, 200, false) });

        using (new FileStream(SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None))
            main.LoadSettings();

        Assert.StartsWith("Settings could not be loaded. Default settings are used.", Assert.Single(dialogs.Warnings));
        Assert.Equal(AppSettings.Default.MainWindow, main.MainPlacement);
    }

    // T-VM-12（保存した設定を読み込む）
    [Fact]
    public void LoadSettings_AppliesSavedSettings()
    {
        var attributes = new ShapeAttributes(new RgbColor(0, 0, 255), 3, LineStyle.Dot);
        var saved = new AppSettings(1, new WindowPlacement(10, 20, 900, 600, true),
                                    new PropertiesWindowSettings(false, new WindowPlacement(30, 40, 250, 400, false)), attributes);
        new SettingsStorage(SettingsPath).Save(saved);

        main.LoadSettings();

        Assert.Empty(dialogs.Warnings);
        Assert.Equal(saved.MainWindow, main.MainPlacement);
        Assert.Equal(saved.PropertiesWindow.Placement, main.PropertiesPlacement);
        Assert.False(main.IsPropertiesWindowVisible);
        DrawLine(100, 100, 200, 200);
        Assert.Equal(attributes, main.Drawing.Shapes[0].Attributes);
    }

    // T-VM-13、T-VM-24
    [Fact]
    public void TryClose_SavesSettings()
    {
        main.MainPlacement = new WindowPlacement(5, 6, 700, 500, false);
        main.PropertiesPlacement = new WindowPlacement(50, 60, 280, 420, false);
        main.TogglePropertiesCommand.Execute(null);
        main.Properties.DefaultAttributes.ThicknessText = "4";

        Assert.True(main.TryClose());

        var saved = new SettingsStorage(SettingsPath).Load().Settings;
        Assert.Equal(main.MainPlacement, saved.MainWindow);
        Assert.Equal(new PropertiesWindowSettings(false, main.PropertiesPlacement), saved.PropertiesWindow);
        Assert.Equal(4.0, saved.DefaultAttributes.Thickness);
    }

    // T-VM-13、T-VM-24（設定の保存の失敗は表示するが、閉じるのを妨げない）
    [Fact]
    public void TryClose_SettingsSaveFailure_IsShownButStillCloses()
    {
        Directory.CreateDirectory(SettingsPath);    // 同名のフォルダーがあり、保存できない

        Assert.True(main.TryClose());

        Assert.Single(dialogs.Errors);
    }

    // T-VM-14
    [Fact]
    public void ToggleProperties_SwitchesVisibility()
    {
        Assert.True(main.IsPropertiesWindowVisible);

        main.TogglePropertiesCommand.Execute(null);
        Assert.False(main.IsPropertiesWindowVisible);

        main.TogglePropertiesCommand.Execute(null);
        Assert.True(main.IsPropertiesWindowVisible);
    }

    // T-VM-23
    [Fact]
    public void Paste_RepeatCountAccumulatesAndResets()
    {
        var copied = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(100, 100), new(200, 100));
        var other = new LineShape(ShapeId.New(), ShapeAttributes.Default, new(500, 500), new(600, 500));
        clipboard.ShapesText = ClipboardShapes.ToText(copied);
        main.RefreshClipboardStateCommand.Execute(null);

        main.PasteCommand.Execute(null);
        main.PasteCommand.Execute(null);
        main.PasteCommand.Execute(null);
        clipboard.ShapesText = ClipboardShapes.ToText(other);
        main.PasteCommand.Execute(null);
        Click(510 + 50, 490);                           // 最後に貼った図形（Copy の対象）
        main.CopyCommand.Execute(null);
        main.PasteCommand.Execute(null);

        // 同じ内容は 10 ずつずれを重ねる（110、120、130）。内容が変わると 1 回目に戻る（500 + 10）。
        // Copy の後も 1 回目に戻る（コピーした 510 + 10）
        Assert.Equal([110.0, 120.0, 130.0, 510.0, 520.0], main.Drawing.Shapes.Select(shape => shape.HandlePoints[0].X));
    }

    // T-VM-24
    [Fact]
    public void Exit_OnlyRequestsClosing()
    {
        DrawLine(100, 100, 200, 200);
        var requested = 0;
        main.CloseRequested += (_, _) => requested++;

        main.ExitCommand.Execute(null);

        // 保存確認と設定の保存は、閉じるときの TryClose が行う（設計書 6.5）
        Assert.Equal(1, requested);
        Assert.Empty(dialogs.Calls);
        Assert.False(File.Exists(SettingsPath));
    }

    // T-VM-24
    [Theory]
    [InlineData(SaveChangesAnswer.No, true)]
    [InlineData(SaveChangesAnswer.Cancel, false)]
    public void TryClose_SavesSettingsOnlyWhenClosing(SaveChangesAnswer answer, bool expected)
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = answer;

        Assert.Equal(expected, main.TryClose());
        Assert.Equal(expected, File.Exists(SettingsPath));
    }

    // T-VM-24
    [Fact]
    public void TryClose_YesSavesDocumentThenCloses()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Yes;
        dialogs.SavePath = folder.Combine("closing.json");

        Assert.True(main.TryClose());
        Assert.True(File.Exists(folder.Combine("closing.json")));
    }

    // T-VM-25
    [Fact]
    public void CopyCommands_DuringDragAreDisabledAndDuringInputMetafileIsEnabled()
    {
        DrawLine(100, 100, 300, 100);
        Pointer(PointerAction.Down, 200, 100);
        Pointer(PointerAction.Move, 220, 150);
        Assert.False(CanExecute(main.CopyCommand));
        Assert.False(CanExecute(main.CopyMetafileCommand));
        Assert.False(CanExecute(main.CopyImageCommand));
        Assert.False(CanExecute(main.UndoCommand));
        Assert.False(CanExecute(main.DeleteCommand));
        Pointer(PointerAction.Up, 220, 150);

        StartPolylineInput();
        Assert.True(CanExecute(main.CopyMetafileCommand));
        Assert.True(CanExecute(main.CopyImageCommand));
        Assert.False(CanExecute(main.UndoCommand));
        main.CopyMetafileCommand.Execute(null);
        Assert.Single(clipboard.Metafiles[^1]);         // 入力中の図形は含まない
    }

    // T-VM-26
    [Fact]
    public void YesThenSaveAsCanceled_AbortsNewOpenAndClose()
    {
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Yes;
        dialogs.SavePath = null;
        dialogs.OpenPath = SaveExistingFile("other.json");

        // 保存確認で Yes を選び、続く Save As のダイアログをキャンセルする（SavePath = null）
        main.NewCommand.Execute(null);
        main.OpenCommand.Execute(null);
        var closed = main.TryClose();

        // どの操作も中止され、Open のファイル選択も表示されない
        Assert.False(closed);
        Assert.Single(main.Drawing.Shapes);
        Assert.Equal(new Point2(200, 200), main.Drawing.Shapes[0].HandlePoints[1]);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
        Assert.DoesNotContain(nameof(IDialogService.ShowOpenFileDialog), dialogs.Calls);
    }

    // T-VM-26
    [Fact]
    public void YesThenSaveFails_AbortsNewAndShowsError()
    {
        var path = SaveExistingFile("readonly.json");
        File.SetAttributes(path, FileAttributes.ReadOnly);
        DrawLine(100, 100, 200, 200);
        dialogs.SaveChangesAnswer = SaveChangesAnswer.Yes;
        dialogs.SavePath = path;

        main.NewCommand.Execute(null);

        Assert.Single(dialogs.Errors);
        Assert.Single(main.Drawing.Shapes);
        Assert.Equal("Untitled* - WPF CAD Sample", main.Title);
    }

    // T-VM-27
    [Fact]
    public void New_ClearsDocumentAndShowsOverviewWithoutSelection()
    {
        DrawLine(100, 100, 1500, 200);
        Click(800, 150);
        ZoomIn();
        dialogs.SaveChangesAnswer = SaveChangesAnswer.No;

        main.NewCommand.Execute(null);

        Assert.Empty(main.Drawing.Shapes);
        Assert.Equal("Zoom: 100%", main.Drawing.ZoomText);
        // 線分で広がっていた全体図が論理領域に戻り、ビュー 1000 × 700 にちょうど収まる
        Assert.Equal(1.0, main.Drawing.Transform.Scale, 1e-9);
        Assert.Empty(main.Drawing.Handles);
        Assert.Equal("Untitled - WPF CAD Sample", main.Title);
        Assert.False(CanExecute(main.UndoCommand));
    }

    // T-VM-28
    [Fact]
    public void Paste_BeyondCoordinateLimit_ShowsError()
    {
        // 貼り付けのずれ（X + 10）で、始点の X が 1,000,005 になり、上限 1,000,000 を超える
        clipboard.ShapesText = ClipboardShapes.ToText(new LineShape(ShapeId.New(), ShapeAttributes.Default, new(999_995, 0), new(999_990, 50)));
        main.RefreshClipboardStateCommand.Execute(null);

        main.PasteCommand.Execute(null);

        Assert.Single(dialogs.Errors);
        Assert.Empty(main.Drawing.Shapes);
    }

    [Fact]
    public void UndoRedoAndDelete_WorkThroughCommands()
    {
        DrawLine(100, 100, 200, 200);
        Click(150, 150);

        main.DeleteCommand.Execute(null);
        Assert.Empty(main.Drawing.Shapes);
        main.UndoCommand.Execute(null);
        Assert.Single(main.Drawing.Shapes);
        Assert.True(CanExecute(main.RedoCommand));
        main.RedoCommand.Execute(null);
        Assert.Empty(main.Drawing.Shapes);
    }
}
