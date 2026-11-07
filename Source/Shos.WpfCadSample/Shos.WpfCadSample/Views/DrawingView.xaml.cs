using System.Windows.Controls;

namespace Shos.WpfCadSample.Views;

/// <summary>
/// 描画領域（論理領域の枠、図形、入力中の図形、ハンドル、入力を受ける面の重ね合わせ。設計書 5.7）。
/// </summary>
public partial class DrawingView : UserControl
{
    public DrawingView() => InitializeComponent();
}
