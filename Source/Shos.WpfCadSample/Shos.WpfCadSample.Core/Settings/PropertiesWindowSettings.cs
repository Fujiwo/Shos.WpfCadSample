namespace Shos.WpfCadSample.Settings;

/// <summary>
/// 属性ウィンドウの表示状態と配置（仕様 6.5）。
/// </summary>
public sealed record PropertiesWindowSettings(bool IsVisible, WindowPlacement Placement);
