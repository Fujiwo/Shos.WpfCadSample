using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// ドキュメントのファイルの読み書き（仕様 5.8、7 章、設計書 5.6）。I/O の例外は呼び出し側（MainViewModel）へ伝える。
/// </summary>
public sealed class DocumentStorage
{
    /// <summary>
    /// 失敗しても元のファイルを壊さない。
    /// </summary>
    public void Save(string path, IEnumerable<Shape> shapes) => SafeFile.WriteAllText(path, DocumentSerializer.Serialize(shapes));

    /// <exception cref="DocumentFormatException">内容が不正、または現行より新しいバージョン。</exception>
    public IReadOnlyList<Shape> Load(string path) => DocumentSerializer.Deserialize(File.ReadAllText(path));
}
