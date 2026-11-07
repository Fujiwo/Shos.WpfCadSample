using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Services;

/// <summary>
/// クリップボード（UI に依存する境界。設計書 5.5、DD-13）。失敗はすべて <see cref="ClipboardOperationException"/> で伝える。
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// 独自形式のデータがあるか。
    /// </summary>
    bool ContainsShapes { get; }

    void SetShapesText(string text);

    /// <summary>
    /// 独自形式のテキスト。なければ null。
    /// </summary>
    string? GetShapesText();

    void CopyAsMetafile(IReadOnlyList<Shape> shapes);

    void CopyAsImage(IReadOnlyList<Shape> shapes);
}
