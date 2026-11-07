using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Settings;

/// <summary>
/// アプリケーションの設定（仕様 5.9、6.5）。
/// </summary>
public sealed record AppSettings(int Version, WindowPlacement MainWindow, PropertiesWindowSettings PropertiesWindow, ShapeAttributes DefaultAttributes)
{
    /// <summary>
    /// 書き出す設定のバージョン。これより新しい設定は不正とみなし、既定値を使う。
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// 既定値（初回起動、または設定が不正なとき）。ウィンドウの配置は、1366 × 768 の画面にも収まる大きさにする。
    /// </summary>
    public static AppSettings Default { get; } = new(
        CurrentVersion,
        new WindowPlacement(Left: 80, Top: 60, Width: 1000, Height: 700, IsMaximized: false),
        new PropertiesWindowSettings(IsVisible: true, new WindowPlacement(Left: 760, Top: 120, Width: 300, Height: 520, IsMaximized: false)),
        ShapeAttributes.Default);

    /// <summary>
    /// 仕様 7 章「設定で不正とみなす条件」に該当しないか（バージョンが 1 以上で現行以下、既定属性と各ウィンドウの配置が有効）。
    /// </summary>
    public bool IsValid()
        => Version is >= 1 and <= CurrentVersion &&
           DefaultAttributes.IsValid() && MainWindow.IsValid() && PropertiesWindow.Placement.IsValid();
}
