using System.Text.Json;
using Shos.WpfCadSample.Serialization;

namespace Shos.WpfCadSample.Settings;

/// <summary>
/// 設定ファイルの読み書き（仕様 5.9、7 章、A-22、A-28、設計書 5.6）。
/// </summary>
/// <param name="path">設定ファイルのパス（アプリでは <c>%APPDATA%\Shos.WpfCadSample\settings.json</c>）。</param>
public sealed class SettingsStorage(string path)
{
    const string loadFailed = "Settings could not be loaded. Default settings are used.";

    readonly string backupPath = path + ".bak";

    /// <summary>
    /// ファイルがなければ既定値（警告なし）。不正・破損していれば、<c>settings.json.bak</c> へ退避して既定値と警告を返す。
    /// 不正・破損は例外にしない（設計書 8.2）。
    /// </summary>
    public SettingsLoadResult Load()
    {
        if (!File.Exists(path))
            return new(AppSettings.Default, null);

        return ReadValidSettings() is { } settings ? new(settings, null) : new(AppSettings.Default, BackUpDamagedFile());
    }

    /// <summary>
    /// フォルダーがなければ作成し、失敗しても元のファイルを壊さずに保存する。失敗は例外で伝える（呼び出し側が表示する）。
    /// </summary>
    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        SafeFile.WriteAllText(path, JsonSerializer.Serialize(settings, JsonRules.Options));
    }

    // 読めない・不正な設定は null（呼び出し側が退避して既定値にする）。I/O の例外は伝える
    AppSettings? ReadValidSettings()
    {
        try {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonRules.Options);
            return settings is not null && settings.IsValid() ? settings : null;
        } catch (JsonException) {
            return null;
        }
    }

    // 退避ファイルは置き換える。退避に失敗しても例外にせず、その旨を警告に含める
    string BackUpDamagedFile()
    {
        var backupName = Path.GetFileName(backupPath);
        try {
            File.Move(path, backupPath, overwrite: true);
            return $"{loadFailed} The damaged file was saved as {backupName}.";
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            return $"{loadFailed} The damaged file could not be saved as {backupName}: {exception.Message}";
        }
    }
}
