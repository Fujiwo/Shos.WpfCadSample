using System.Windows;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.Settings;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// ウィンドウの配置（位置・サイズ・最大化）の復元と保存（仕様 5.9、設計書 5.7、R-4）。
/// 表示の直前に <see cref="WindowPlacement"/> を適用し（現在のモニターに含まれなければ既定の配置）、
/// その後は、位置・サイズ・状態が変わるたびに、<see cref="Window.RestoreBounds"/>（最大化中も通常時の位置）で書き戻す。
/// </summary>
public static class WindowPlacementBehavior
{
    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.RegisterAttached("Placement", typeof(WindowPlacement), typeof(WindowPlacementBehavior),
                                            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPlacementChanged));

    /// <summary>
    /// 保存した配置が現在のモニターに含まれないときに使う、既定の配置。
    /// </summary>
    public static readonly DependencyProperty DefaultPlacementProperty =
        DependencyProperty.RegisterAttached("DefaultPlacement", typeof(WindowPlacement), typeof(WindowPlacementBehavior));

    static readonly DependencyProperty isAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(WindowPlacementBehavior));

    public static WindowPlacement? GetPlacement(Window window) => (WindowPlacement?)window.GetValue(PlacementProperty);

    public static void SetPlacement(Window window, WindowPlacement? value) => window.SetValue(PlacementProperty, value);

    public static WindowPlacement? GetDefaultPlacement(Window window) => (WindowPlacement?)window.GetValue(DefaultPlacementProperty);

    public static void SetDefaultPlacement(Window window, WindowPlacement? value) => window.SetValue(DefaultPlacementProperty, value);

    // 表示の前（SourceInitialized）に 1 回だけ適用する。それ以降の変化は、ウィンドウからの書き戻しだけ
    static void OnPlacementChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Window window || (bool)window.GetValue(isAttachedProperty))
            return;
        window.SetValue(isAttachedProperty, true);
        window.SourceInitialized += (_, _) => {
            Apply(window);
            window.LocationChanged += (_, _) => Store(window);
            window.SizeChanged += (_, _) => Store(window);
            window.StateChanged += (_, _) => Store(window);
        };
    }

    static void Apply(Window window)
    {
        if (GetPlacement(window) is not { } placement)
            return;
        if (!placement.IsWithin(VirtualScreen()) && GetDefaultPlacement(window) is { } defaultPlacement)
            placement = defaultPlacement;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        (window.Left, window.Top, window.Width, window.Height) = (placement.Left, placement.Top, placement.Width, placement.Height);
        if (placement.IsMaximized)
            window.WindowState = WindowState.Maximized;
    }

    // 最小化中は書き戻さない（次回は、最小化の前の状態で開く）
    static void Store(Window window)
    {
        var bounds = window.RestoreBounds;
        if (window.WindowState == WindowState.Minimized || bounds.IsEmpty)
            return;
        window.SetCurrentValue(PlacementProperty,
                               new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, window.WindowState == WindowState.Maximized));
    }

    // 画面座標（Y 下向き）の仮想スクリーン。Rect2 の Bottom が上端、Top が下端（WindowPlacement.IsWithin の約束）
    static Rect2 VirtualScreen()
        => new(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
               SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth,
               SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight);
}
