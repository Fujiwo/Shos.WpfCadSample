using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Shos.WpfCadSample.Infrastructure;

/// <summary>
/// EMF のハンドルを、Win32 の <c>SetClipboardData</c> で <c>CF_ENHMETAFILE</c> として渡す（WPF に EMF をクリップボードへ書く標準の機能がないため。設計書 1 章、DD-6）。
/// </summary>
static class NativeClipboard
{
    const uint enhancedMetafileFormat = 14;     // CF_ENHMETAFILE

    /// <summary>
    /// クリップボードの内容を EMF に置き換える。成功するとクリップボードがハンドルを所有し、失敗するとハンドルを削除する。
    /// </summary>
    /// <exception cref="Win32Exception">クリップボードを開けない（他のアプリが使用中など）、または書き込めない。</exception>
    public static void SetEnhancedMetafile(IntPtr ownerWindow, IntPtr enhancedMetafile)
    {
        if (!OpenClipboard(ownerWindow)) {
            DeleteEnhMetaFile(enhancedMetafile);
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        try {
            if (!EmptyClipboard() || SetClipboardData(enhancedMetafileFormat, enhancedMetafile) == IntPtr.Zero) {
                var error = Marshal.GetLastPInvokeError();
                DeleteEnhMetaFile(enhancedMetafile);
                throw new Win32Exception(error);
            }
        } finally {
            CloseClipboard();
        }
    }

    // LibraryImport は unsafe のコンパイルを要するため、プロジェクトの設定を変えずに使える DllImport にする
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseClipboard();

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DeleteEnhMetaFile(IntPtr enhancedMetafile);
}
