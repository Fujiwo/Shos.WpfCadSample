using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Settings;
using Shos.WpfCadSample.Tests.Support;

namespace Shos.WpfCadSample.Tests.Settings;

/// <summary>
/// T-SET-01〜06、08：<see cref="SettingsStorage"/>（テスト計画書 4.11。一時フォルダーを使う）
/// </summary>
public sealed class SettingsStorageTests : IDisposable
{
    const string damagedWarning = "Settings could not be loaded. Default settings are used. The damaged file was saved as settings.json.bak.";

    readonly TempFolder folder = new();
    readonly string path;
    readonly string backupPath;

    public SettingsStorageTests()
    {
        path = folder.Combine("settings.json");
        backupPath = folder.Combine("settings.json.bak");
    }

    public void Dispose() => folder.Dispose();

    // 既定値と異なる値（負の位置・小数・最大化・非表示・既定でない属性）。往復で値が失われないことを確かめる
    static readonly AppSettings custom = new(
        1,
        new WindowPlacement(-1500.5, 20, 1024, 768, IsMaximized: true),
        new PropertiesWindowSettings(IsVisible: false, new WindowPlacement(300, 40.25, 280, 500, IsMaximized: false)),
        new ShapeAttributes(new RgbColor(0x12, 0xAB, 0xEF), 3.5, LineStyle.DashDot));

    const string validJson = """
        {
          "version": 1,
          "mainWindow": { "left": 10, "top": 20, "width": 800, "height": 600, "isMaximized": false },
          "propertiesWindow": { "isVisible": true, "placement": { "left": 30, "top": 40, "width": 300, "height": 500, "isMaximized": false } },
          "defaultAttributes": { "color": "#FF0000", "thickness": 2, "lineStyle": "Dash" }
        }
        """;

    // 破損したファイルは退避して既定値で続ける：元のファイルはなくなり、退避ファイルに同じ内容が残る
    void AssertDamagedFileIsBackedUp(string content)
    {
        File.WriteAllText(path, content);

        var result = new SettingsStorage(path).Load();

        Assert.Equal(AppSettings.Default, result.Settings);
        Assert.Equal(damagedWarning, result.Warning);
        Assert.False(File.Exists(path));
        Assert.Equal(content, File.ReadAllText(backupPath));
    }

    // T-SET-01
    [Fact]
    public void Load_WithoutFile_ReturnsDefaultWithoutWarning()
    {
        var result = new SettingsStorage(path).Load();

        Assert.Equal(AppSettings.Default, result.Settings);
        Assert.Null(result.Warning);
    }

    // T-SET-02
    [Fact]
    public void SaveThenLoad_RoundTripsAndCreatesFolder()
    {
        var nestedPath = folder.Combine("Shos.WpfCadSample", "settings.json");
        var storage = new SettingsStorage(nestedPath);

        storage.Save(custom);
        var result = storage.Load();

        Assert.Equal(custom, result.Settings);
        Assert.Null(result.Warning);
        Assert.Equal(["settings.json"], Directory.EnumerateFiles(Path.GetDirectoryName(nestedPath)!).Select(Path.GetFileName));
    }

    // 手書きの有効な設定（テストの組み立ての確認）
    [Fact]
    public void Load_ReadsHandWrittenSettings()
    {
        File.WriteAllText(path, validJson);

        var result = new SettingsStorage(path).Load();

        Assert.Null(result.Warning);
        Assert.Equal(new ShapeAttributes(new RgbColor(255, 0, 0), 2, LineStyle.Dash), result.Settings.DefaultAttributes);
        Assert.Equal(new WindowPlacement(10, 20, 800, 600, false), result.Settings.MainWindow);
    }

    // T-SET-03
    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]    // JSON としては正しいが、設定にならない
    public void Load_DamagedFile_BacksUpAndReturnsDefault(string content) => AssertDamagedFileIsBackedUp(content);

    // T-SET-04
    [Fact]
    public void Load_DamagedFile_ReplacesExistingBackup()
    {
        File.WriteAllText(backupPath, "old backup");

        AssertDamagedFileIsBackedUp("{ damaged");
    }

    // T-SET-05
    [Fact]
    public void Load_WhenBackupFails_ReturnsDefaultWithWarningAndDoesNotThrow()
    {
        Directory.CreateDirectory(backupPath);      // 同名のフォルダーがあり、退避できない
        File.WriteAllText(path, "{ damaged");

        var result = new SettingsStorage(path).Load();

        Assert.Equal(AppSettings.Default, result.Settings);
        Assert.NotNull(result.Warning);
        Assert.NotEqual(damagedWarning, result.Warning);
        Assert.Contains("could not be saved", result.Warning);
    }

    // T-SET-06：有効な JSON の 1 か所だけを不正な値に置き換え、項目ごとの検証が働くことを確かめる
    [Theory]
    [InlineData("\"thickness\": 2", "\"thickness\": 0.1")]
    [InlineData("\"#FF0000\"", "\"red\"")]
    [InlineData("\"Dash\"", "\"Wavy\"")]
    [InlineData("\"width\": 800", "\"width\": 0")]
    [InlineData("\"height\": 500", "\"height\": -1")]
    [InlineData("\"left\": 10", "\"left\": 1e999")]
    [InlineData("\"top\": 20", "\"top\": \"NaN\"")]
    [InlineData("\"isVisible\": true, ", "")]                       // 必須項目の欠落
    public void Load_InvalidValue_IsTreatedAsDamaged(string valid, string invalid)
        => AssertDamagedFileIsBackedUp(validJson.Replace(valid, invalid));

    // T-SET-08
    [Theory]
    [InlineData("2")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("\"1\"")]
    public void Load_UnsupportedVersion_IsTreatedAsDamaged(string version)
        => AssertDamagedFileIsBackedUp(validJson.Replace("\"version\": 1", $"\"version\": {version}"));

    // 保存の失敗は例外で呼び出し側に伝え、元のファイルの内容は変わらない
    [Fact]
    public void Save_WhenTargetIsReadOnly_Throws()
    {
        var storage = new SettingsStorage(path);
        storage.Save(AppSettings.Default);
        File.SetAttributes(path, FileAttributes.ReadOnly);

        Assert.Throws<UnauthorizedAccessException>(() => storage.Save(custom));
        Assert.Equal(AppSettings.Default, storage.Load().Settings);
    }
}
