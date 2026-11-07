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

    /// <summary>
    /// 独自形式のテキストを置く（クリップボードの以前の内容は消える）。
    /// </summary>
    void SetShapesText(string text);

    /// <summary>
    /// 独自形式のテキスト。なければ null。
    /// </summary>
    string? GetShapesText();

    /// <summary>
    /// 図形をメタファイル（ベクター）としてクリップボードへ置く（ほかのアプリケーションへの貼り付けのため）。
    /// </summary>
    void CopyAsMetafile(IReadOnlyList<Shape> shapes);

    /// <summary>
    /// 図形をビットマップ画像としてクリップボードへ置く。
    /// </summary>
    void CopyAsImage(IReadOnlyList<Shape> shapes);
}
