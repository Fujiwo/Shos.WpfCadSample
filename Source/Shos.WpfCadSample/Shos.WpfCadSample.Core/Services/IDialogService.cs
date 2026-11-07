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

    /// <summary>
    /// 未保存の変更を保存するかを尋ねる（<paramref name="name"/> はドキュメントの表示名）。
    /// </summary>
    SaveChangesAnswer AskSaveChanges(string name);

    /// <summary>
    /// 操作の失敗を表示する（文言は英語）。
    /// </summary>
    void ShowError(string title, string message);

    /// <summary>
    /// 操作は続けられるが、利用者に知らせることを表示する（設定の読み込みの失敗など）。
    /// </summary>
    void ShowWarning(string message);
}
