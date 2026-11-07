namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// ツールごとの入力の解釈（描画・選択）。EditorController が、現在のツールに応じて 1 つを使う（設計書 5.3）。
/// </summary>
interface IInteraction
{
    /// <summary>
    /// 入力中（描画）またはドラッグ中（選択）か。
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 論理座標へ変換済みのマウス操作を 1 つ解釈する。
    /// </summary>
    void Handle(PointerInput input);

    /// <summary>
    /// 入力中の図形、またはドラッグを取り消す（Esc）。
    /// </summary>
    void Cancel();
}
