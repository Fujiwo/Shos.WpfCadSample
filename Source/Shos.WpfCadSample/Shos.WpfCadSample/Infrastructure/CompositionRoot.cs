using System.IO;
using Shos.WpfCadSample.Serialization;
using Shos.WpfCadSample.Settings;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Infrastructure;

/// <summary>
/// 起動時の組み立て：サービスと <see cref="ViewModels.MainViewModel"/> を作り、設定を読み込む（設計書 5.7）。
/// <c>App.xaml</c> のリソースに置き、<c>MainWindow</c> はここから <c>DataContext</c> を得る（コードビハインドに起動時のコードを書かないため）。
/// </summary>
public sealed class CompositionRoot
{
    public CompositionRoot()
    {
        MainViewModel = new MainViewModel(new DialogService(), new ClipboardService(), new DocumentStorage(), new SettingsStorage(SettingsPath()));
        MainViewModel.LoadSettings();
    }

    public MainViewModel MainViewModel { get; }

    // %APPDATA%\Shos.WpfCadSample\settings.json（仕様 5.9）
    static string SettingsPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shos.WpfCadSample", "settings.json");
}
