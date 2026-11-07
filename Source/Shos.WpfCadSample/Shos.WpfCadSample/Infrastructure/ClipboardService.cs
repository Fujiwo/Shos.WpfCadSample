using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Services;

namespace Shos.WpfCadSample.Infrastructure;

/// <summary>
/// クリップボード：独自形式と画像は WPF の <see cref="Clipboard"/>、EMF は <see cref="MetafileRenderer"/> と <see cref="NativeClipboard"/>（設計書 5.5）。
/// 生成と書き込みの失敗は <see cref="ClipboardOperationException"/> に包んで伝える（Core が GDI+ や COM の例外型に依存しないため。設計書 8.2）。
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    /// <summary>
    /// 独自形式の書式名（設計書 5.5）。データは UTF-8 の JSON テキスト。
    /// </summary>
    const string shapesFormat = "Shos.WpfCadSample.Shapes";

    public bool ContainsShapes => Run(() => Clipboard.ContainsData(shapesFormat));

    // 独自の書式の文字列は、型の直列化に頼らないよう、バイト列（MemoryStream）として渡す
    public void SetShapesText(string text)
        => Run(() => Clipboard.SetData(shapesFormat, new MemoryStream(Encoding.UTF8.GetBytes(text))));

    public string? GetShapesText()
        => Run(() => Clipboard.GetData(shapesFormat) is MemoryStream stream ? Encoding.UTF8.GetString(stream.ToArray()) : null);

    /// <summary>
    /// EMF を作ってからクリップボードを開くため、生成に失敗したときはクリップボードを変更しない。
    /// </summary>
    public void CopyAsMetafile(IReadOnlyList<Shape> shapes)
        => Run(() => {
            var handle = CreateMetafileHandle(shapes);
            NativeClipboard.SetEnhancedMetafile(OwnerWindowHandle(), handle);
        });

    /// <summary>
    /// 画像を作ってからクリップボードへ書くため、生成に失敗したときはクリップボードを変更しない。
    /// </summary>
    public void CopyAsImage(IReadOnlyList<Shape> shapes)
        => Run(() => {
            var image = CreateBitmapSource(MetafileRenderer.CreatePng(shapes));
            Clipboard.SetImage(image);
        });

    // GetHenhmetafile の後は、Metafile ではなくハンドルの所有者（クリップボード、または失敗時の削除）が責任を持つ
    static IntPtr CreateMetafileHandle(IReadOnlyList<Shape> shapes)
    {
        using var metafile = MetafileRenderer.CreateMetafile(shapes);
        return metafile.GetHenhmetafile();
    }

    static BitmapSource CreateBitmapSource(byte[] png)
    {
        using var stream = new MemoryStream(png);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    static IntPtr OwnerWindowHandle()
        => Application.Current?.MainWindow is { } window ? new WindowInteropHelper(window).Handle : IntPtr.Zero;

    static void Run(Action action) => Run(() => {
        action();
        return true;
    });

    // GDI+ の失敗（ExternalException。引数の不正を OutOfMemoryException で返す場合がある）、大きすぎるビットマップ（ArgumentException）、
    // クリップボードの使用中（COMException、Win32Exception。いずれも ExternalException の派生）を包む
    static T Run<T>(Func<T> function)
    {
        try {
            return function();
        } catch (Exception exception) when (exception is ExternalException or ArgumentException or OutOfMemoryException) {
            throw new ClipboardOperationException(exception.Message, exception);
        }
    }
}
