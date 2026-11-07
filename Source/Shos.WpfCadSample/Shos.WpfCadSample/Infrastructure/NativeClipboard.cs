using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Shos.WpfCadSample.Infrastructure;

/// <summary>
/// EMF のハンドルを、Win32 の <c>SetClipboardData</c> で <c>CF_ENHMETAFILE</c> として渡す（WPF に EMF をクリップボードへ書く標準の機能がないため。設計書 1 章、DD-6）。
/// </summary>
static class NativeClipboard
{
    const uint enhancedMetafileFormat = 14;     // CF_ENHMETAFILE

    // クリップボードは、ほかのプロセス（クリップボードの履歴、リモートデスクトップなど）が一瞬開いていることがある。
    // WPF の Clipboard.SetDataObject の既定と同じく、100 ms おきに 10 回まで試す（独自形式・画像のコピーと振る舞いをそろえる）
    const int openAttempts = 10;
    static readonly TimeSpan openRetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// クリップボードの内容を EMF に置き換える。成功するとクリップボードがハンドルを所有し、失敗するとハンドルを削除する。
    /// </summary>
    /// <exception cref="Win32Exception">再試行してもクリップボードを開けない（他のアプリが使用中など）、または書き込めない。</exception>
    public static void SetEnhancedMetafile(IntPtr ownerWindow, IntPtr enhancedMetafile)
    {
        if (!TryOpenClipboard(ownerWindow)) {
            DeleteEnhMetaFile(enhancedMetafile);
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        try {
            if (!EmptyClipboard() || SetClipboardData(enhancedMetafileFormat, enhancedMetafile) == IntPtr.Zero) {
                // エラー番号は、DeleteEnhMetaFile が上書きする前に読む
                var error = Marshal.GetLastPInvokeError();
                DeleteEnhMetaFile(enhancedMetafile);
                throw new Win32Exception(error);
            }
        } finally {
            // 開いたクリップボードは、成否にかかわらず閉じる（開いたままだと、他のアプリが使えない）
            CloseClipboard();
        }
    }

    // 開けたか。失敗のエラー番号は、最後の OpenClipboard のもの（Thread.Sleep は上書きしない）
    static bool TryOpenClipboard(IntPtr ownerWindow)
    {
        for (var attempt = 1; ; attempt++) {
            if (OpenClipboard(ownerWindow))
                return true;
            if (attempt == openAttempts)
                return false;
            Thread.Sleep(openRetryDelay);
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
