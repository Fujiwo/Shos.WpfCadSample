using Shos.WpfCadSample.Services;

namespace Shos.WpfCadSample.Tests.Support;

/// <summary>
/// 偽の <see cref="IDialogService"/>：応答を設定でき、呼び出しを記録する（テスト計画書 2.2）。
/// </summary>
sealed class FakeDialogService : IDialogService
{
    // ダイアログの応答。ファイルのダイアログでは、null がキャンセルを表す
    public string? OpenPath { get; set; }
    public string? SavePath { get; set; }
    public SaveChangesAnswer SaveChangesAnswer { get; set; } = SaveChangesAnswer.No;

    /// <summary>
    /// 保存確認を表示する時点で呼ばれる（その時点の状態を確かめるため）。
    /// </summary>
    public Action? AskingSaveChanges { get; set; }

    /// <summary>
    /// 呼ばれたメソッドの名前を、呼ばれた順に記録する（表示の有無と順序の確認に使う）。
    /// </summary>
    public List<string> Calls { get; } = [];
    public List<(string Title, string Message)> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> SuggestedNames { get; } = [];

    public string? ShowOpenFileDialog()
    {
        Calls.Add(nameof(ShowOpenFileDialog));
        return OpenPath;
    }

    public string? ShowSaveFileDialog(string suggestedName)
    {
        Calls.Add(nameof(ShowSaveFileDialog));
        SuggestedNames.Add(suggestedName);
        return SavePath;
    }

    public SaveChangesAnswer AskSaveChanges(string name)
    {
        Calls.Add(nameof(AskSaveChanges));
        AskingSaveChanges?.Invoke();
        return SaveChangesAnswer;
    }

    public void ShowError(string title, string message)
    {
        Calls.Add(nameof(ShowError));
        Errors.Add((title, message));
    }

    public void ShowWarning(string message)
    {
        Calls.Add(nameof(ShowWarning));
        Warnings.Add(message);
    }
}
