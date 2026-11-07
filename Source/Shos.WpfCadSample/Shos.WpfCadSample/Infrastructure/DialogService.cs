using System.Windows;
using Microsoft.Win32;
using Shos.WpfCadSample.Services;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Infrastructure;

/// <summary>
/// ファイルダイアログと保存確認・エラー表示（OS 標準のダイアログとメッセージボックス。仕様 4.6、A-16、設計書 5.5）。
/// </summary>
public sealed class DialogService : IDialogService
{
    const string fileFilter = $"CAD documents (*{MainViewModel.DocumentExtension})|*{MainViewModel.DocumentExtension}";

    public string? ShowOpenFileDialog()
    {
        var dialog = new OpenFileDialog { Filter = fileFilter, DefaultExt = MainViewModel.DocumentExtension, CheckFileExists = true };
        return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
    }

    public string? ShowSaveFileDialog(string suggestedName)
    {
        var dialog = new SaveFileDialog { Filter = fileFilter, DefaultExt = MainViewModel.DocumentExtension, AddExtension = true, FileName = suggestedName, OverwritePrompt = true };
        return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
    }

    public SaveChangesAnswer AskSaveChanges(string name)
        => Show($"Save changes to \"{name}\"?", MainViewModel.ApplicationName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch {
            MessageBoxResult.Yes => SaveChangesAnswer.Yes,
            MessageBoxResult.No => SaveChangesAnswer.No,
            _ => SaveChangesAnswer.Cancel
        };

    public void ShowError(string title, string message)
        => Show(message, $"{title} - {MainViewModel.ApplicationName}", MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowWarning(string message)
        => Show(message, MainViewModel.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Warning);

    static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage image)
        => Owner() is { } owner
           ? MessageBox.Show(owner, message, caption, buttons, image)
           : MessageBox.Show(message, caption, buttons, image);

    // 操作中のウィンドウ（属性ウィンドウからのショートカットを含む）を親にする。起動前はメインウィンドウもない
    static Window? Owner()
        => Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive) ?? Application.Current?.MainWindow;
}
