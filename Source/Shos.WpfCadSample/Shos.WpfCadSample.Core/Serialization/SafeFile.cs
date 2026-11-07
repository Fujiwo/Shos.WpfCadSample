namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// ファイルの一部だけが書かれた状態を残さない保存（仕様 7 章）。ドキュメントと設定の保存に共通（設計書 3.3、5.6）。
/// </summary>
static class SafeFile
{
    /// <summary>
    /// 同じフォルダーの一時ファイルへ書いてから、元のファイルと置き換える（新規は移動する）。
    /// 失敗しても元のファイルは変わらず、一時ファイルは残らない。I/O の例外は、そのまま伝える。
    /// </summary>
    public static void WriteAllText(string path, string text)
    {
        var fullPath = Path.GetFullPath(path);
        // 同じフォルダーに置くのは、置き換え（File.Replace）が同じボリューム内でしかできないため。名前は衝突しないよう GUID を含める
        var temporaryPath = Path.Combine(Path.GetDirectoryName(fullPath)!, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try {
            File.WriteAllText(temporaryPath, text);
            if (File.Exists(fullPath))
                File.Replace(temporaryPath, fullPath, destinationBackupFileName: null);
            else
                File.Move(temporaryPath, fullPath);
        } finally {
            DeleteIfExists(temporaryPath);  // 成功時は移動済みで存在しない。失敗時の一時ファイルを残さない
        }
    }

    // 後始末の失敗で、保存の失敗の例外を隠さない
    static void DeleteIfExists(string path)
    {
        try {
            File.Delete(path);
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        }
    }
}
