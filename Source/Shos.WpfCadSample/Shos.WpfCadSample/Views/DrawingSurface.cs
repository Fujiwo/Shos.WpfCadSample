using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shos.WpfCadSample.Controllers;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Views;

/// <summary>
/// 描画領域の入力を受ける面（設計書 5.7、DD-5）。マウスとホイールを、WPF に依存しない入力に変換してコマンドへ渡すだけで、
/// アプリケーションの判断は含まない。外観は <c>Themes\DrawingSurface.xaml</c>。
/// </summary>
public sealed class DrawingSurface : Control
{
    const int wheelDeltaPerNotch = 120;     // Windows のホイール 1 ノッチの量（WHEEL_DELTA）

    public static readonly DependencyProperty PointerCommandProperty = RegisterCommand(nameof(PointerCommand));
    public static readonly DependencyProperty PointerLeaveCommandProperty = RegisterCommand(nameof(PointerLeaveCommand));
    public static readonly DependencyProperty ZoomCommandProperty = RegisterCommand(nameof(ZoomCommand));
    public static readonly DependencyProperty ViewSizeCommandProperty = RegisterCommand(nameof(ViewSizeCommand));

    static readonly DependencyPropertyKey viewSizePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(ViewSize), typeof(ViewSize), typeof(DrawingSurface), new(default(ViewSize)));

    public static readonly DependencyProperty ViewSizeProperty = viewSizePropertyKey.DependencyProperty;

    bool isReleasingCapture;

    static DrawingSurface()
        => DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingSurface), new FrameworkPropertyMetadata(typeof(DrawingSurface)));

    public DrawingSurface() => SizeChanged += (_, e) => OnViewSizeChanged(new(e.NewSize.Width, e.NewSize.Height));

    /// <summary>
    /// マウスの操作（<see cref="PointerEvent"/>）を受けるコマンド。
    /// </summary>
    public ICommand? PointerCommand {
        get => (ICommand?)GetValue(PointerCommandProperty);
        set => SetValue(PointerCommandProperty, value);
    }

    /// <summary>
    /// マウスが描画領域の外へ出たことを受けるコマンド（引数なし）。
    /// </summary>
    public ICommand? PointerLeaveCommand {
        get => (ICommand?)GetValue(PointerLeaveCommandProperty);
        set => SetValue(PointerLeaveCommandProperty, value);
    }

    /// <summary>
    /// <c>Ctrl+ホイール</c>（<see cref="ZoomRequest"/>）を受けるコマンド。
    /// </summary>
    public ICommand? ZoomCommand {
        get => (ICommand?)GetValue(ZoomCommandProperty);
        set => SetValue(ZoomCommandProperty, value);
    }

    /// <summary>
    /// 描画領域の大きさの変化（<see cref="ViewModels.ViewSize"/>）を受けるコマンド。
    /// </summary>
    public ICommand? ViewSizeCommand {
        get => (ICommand?)GetValue(ViewSizeCommandProperty);
        set => SetValue(ViewSizeCommandProperty, value);
    }

    /// <summary>
    /// 描画領域の大きさ（px）。
    /// </summary>
    public ViewSize ViewSize => (ViewSize)GetValue(ViewSizeProperty);

    // ボタンを押したときにキャプチャを取り、ウィンドウの外で離しても Up を受け取る（仕様 5.3、A-26）
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();    // Delete、Ctrl+矢印を受け取るため（仕様 4.3）
        CaptureMouse();
        Send(PointerAction.Down, e, e.ClickCount);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Send(PointerAction.Move, e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsMouseCaptured)
            return;
        Send(PointerAction.Up, e);
        isReleasingCapture = true;
        try {
            ReleaseMouseCapture();
        } finally {
            isReleasingCapture = false;
        }
        e.Handled = true;
    }

    // Up 以外でキャプチャを失った（他のウィンドウへの切り替えなど）ときは、操作が途切れたことを伝える（仕様 A-26）
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (!isReleasingCapture)
            Send(PointerAction.CaptureLost, e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (!IsMouseCaptured)
            Execute(PointerLeaveCommand, null);
    }

    // Ctrl なしのホイールは何もしない（仕様 5.6）
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || e.Delta == 0)
            return;
        var notches = Math.Sign(e.Delta) * Math.Max(1, Math.Abs(e.Delta) / wheelDeltaPerNotch);
        Execute(ZoomCommand, new ZoomRequest(ScreenPoint(e), notches));
        e.Handled = true;
    }

    void OnViewSizeChanged(ViewSize size)
    {
        SetValue(viewSizePropertyKey, size);
        Execute(ViewSizeCommand, size);
    }

    void Send(PointerAction action, MouseEventArgs e, int clickCount = 1)
        => Execute(PointerCommand, new PointerEvent(action, ScreenPoint(e), clickCount, Modifiers()));

    Point2 ScreenPoint(MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        return new(position.X, position.Y);
    }

    static PointerModifiers Modifiers()
    {
        var keys = Keyboard.Modifiers;
        return (keys.HasFlag(ModifierKeys.Control) ? PointerModifiers.Control : PointerModifiers.None) |
               (keys.HasFlag(ModifierKeys.Shift) ? PointerModifiers.Shift : PointerModifiers.None) |
               (keys.HasFlag(ModifierKeys.Alt) ? PointerModifiers.Alt : PointerModifiers.None);
    }

    static void Execute(ICommand? command, object? parameter)
    {
        if (command?.CanExecute(parameter) == true)
            command.Execute(parameter);
    }

    static DependencyProperty RegisterCommand(string name)
        => DependencyProperty.Register(name, typeof(ICommand), typeof(DrawingSurface));
}
