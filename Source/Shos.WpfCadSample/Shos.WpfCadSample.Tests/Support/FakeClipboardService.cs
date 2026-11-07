using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Services;

namespace Shos.WpfCadSample.Tests.Support;

/// <summary>
/// 偽の <see cref="IClipboardService"/>：内容をメモリーに持ち、出力を記録する。失敗させることもできる（テスト計画書 2.2）。
/// </summary>
sealed class FakeClipboardService : IClipboardService
{
    public string? ShapesText { get; set; }
    public bool Fails { get; set; }

    public List<IReadOnlyList<Shape>> Metafiles { get; } = [];
    public List<IReadOnlyList<Shape>> Images { get; } = [];

    public bool ContainsShapes => ShapesText is not null;

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
