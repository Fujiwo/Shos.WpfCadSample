using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Services;

namespace Shos.WpfCadSample.Tests.Support;

/// <summary>
/// 偽の <see cref="IClipboardService"/>：内容をメモリーに持ち、出力を記録する。失敗させることもできる（テスト計画書 2.2）。
/// </summary>
sealed class FakeClipboardService : IClipboardService
{
    /// <summary>
    /// クリップボードにある独自形式のテキスト。null なら、図形の内容はない。
    /// </summary>
    public string? ShapesText { get; set; }

    /// <summary>
    /// true にすると、すべての操作が <see cref="ClipboardOperationException"/> を投げる（ほかのアプリケーションが使用中の状態）。
    /// </summary>
    public bool Fails { get; set; }

    // Copy as Metafile / Copy as Image で出力した図形の記録
    public List<IReadOnlyList<Shape>> Metafiles { get; } = [];
    public List<IReadOnlyList<Shape>> Images { get; } = [];

    public bool ContainsShapes {
        get {
            ThrowIfFails();
            return ShapesText is not null;
        }
    }

    public void SetShapesText(string text)
    {
        ThrowIfFails();
        ShapesText = text;
    }

    public string? GetShapesText()
    {
        ThrowIfFails();
        return ShapesText;
    }

    public void CopyAsMetafile(IReadOnlyList<Shape> shapes)
    {
        ThrowIfFails();
        Metafiles.Add(shapes);
    }

    public void CopyAsImage(IReadOnlyList<Shape> shapes)
    {
        ThrowIfFails();
        Images.Add(shapes);
    }

    void ThrowIfFails()
    {
        if (Fails)
            throw new ClipboardOperationException("The clipboard is used by another application.");
    }
}
