namespace Shos.WpfCadSample.Services;

/// <summary>
/// 保存確認の答え（仕様 4.6）。
/// </summary>
public enum SaveChangesAnswer
{
    /// <summary>
    /// 保存してから続ける。
    /// </summary>
    Yes,

    /// <summary>
    /// 保存せずに続ける。
    /// </summary>
    No,

    /// <summary>
    /// 操作を中止する。
    /// </summary>
    Cancel
}
