namespace Shos.WpfCadSample.Settings;

/// <summary>
/// 設定の読み込みの結果。設定が不正だったときは、既定値と、利用者に表示する文言（英語）を持つ。
/// </summary>
public sealed record SettingsLoadResult(AppSettings Settings, string? Warning);
