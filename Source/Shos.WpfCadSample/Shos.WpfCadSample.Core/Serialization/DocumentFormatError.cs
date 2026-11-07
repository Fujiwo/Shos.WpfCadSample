namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// 読み込めない理由の種類（仕様 7 章）。
/// </summary>
public enum DocumentFormatError
{
    /// <summary>
    /// 不正とみなす条件に該当する。
    /// </summary>
    Invalid,

    /// <summary>
    /// 現行より新しいバージョンの形式。
    /// </summary>
    NewerVersion
}
