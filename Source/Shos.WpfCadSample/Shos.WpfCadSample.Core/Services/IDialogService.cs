namespace Shos.WpfCadSample.Services;

/// <summary>
/// ファイルダイアログ・保存確認・エラー表示（UI に依存する境界。設計書 5.5、DD-13）。
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 開くファイルを選ばせる。キャンセルなら null。
    /// </summary>
    string? ShowOpenFileDialog();

    /// <summary>
    /// 保存先を選ばせる。キャンセルなら null。
    /// </summary>
    string? ShowSaveFileDialog(string suggestedName);

    SaveChangesAnswer AskSaveChanges(string name);

    void ShowError(string title, string message);

    void ShowWarning(string message);
}
