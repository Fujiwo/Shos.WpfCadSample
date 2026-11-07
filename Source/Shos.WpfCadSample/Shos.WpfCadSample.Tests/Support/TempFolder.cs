namespace Shos.WpfCadSample.Tests.Support;

/// <summary>
/// テストごとに作る一時フォルダー。破棄すると、読み取り専用のファイルを含めて削除する（テスト計画書 2.2）。
/// </summary>
sealed class TempFolder : IDisposable
{
    // 並行して動くテストどうしで衝突しないよう、フォルダー名に GUID を使う
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Shos.WpfCadSample.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] names) => System.IO.Path.Combine([Path, .. names]);

    /// <summary>
    /// フォルダーの中のファイル名（一時ファイルが残っていないことの確認に使う）。
    /// </summary>
    public IEnumerable<string> FileNames() => Directory.EnumerateFiles(Path).Select(System.IO.Path.GetFileName)!;

    public void Dispose()
    {
        // 読み取り専用のファイルがあると、フォルダーごとの削除が失敗するため、先に属性を戻す
        foreach (var file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(Path, recursive: true);
    }
}
