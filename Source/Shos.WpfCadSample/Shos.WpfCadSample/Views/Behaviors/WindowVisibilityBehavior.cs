using System.ComponentModel;
using System.Windows;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// 属性ウィンドウの表示・非表示（仕様 4.5、A-25、設計書 5.7）。
/// <see cref="OwnedWindowProperty"/>：メインウィンドウに付け、表示された後で、そのウィンドウの所有者にする（メインウィンドウの前面に表示するため）。
/// <see cref="IsShownProperty"/>：表示状態とバインドし、閉じる操作（閉じるボタン、Alt+F4）を、取り消して非表示に変える。
/// </summary>
public static class WindowVisibilityBehavior
{
    /// <summary>
    /// メインウィンドウが所有する属性ウィンドウ。
    /// </summary>
    public static readonly DependencyProperty OwnedWindowProperty =
        DependencyProperty.RegisterAttached("OwnedWindow", typeof(Window), typeof(WindowVisibilityBehavior), new(null, OnOwnedWindowChanged));

    /// <summary>
    /// 属性ウィンドウを表示するか（既定で双方向のバインド。閉じる操作で偽を書き戻す）。
    /// </summary>
    public static readonly DependencyProperty IsShownProperty =
        DependencyProperty.RegisterAttached("IsShown", typeof(bool), typeof(WindowVisibilityBehavior),
                                            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsShownChanged));

    public static Window? GetOwnedWindow(Window window) => (Window?)window.GetValue(OwnedWindowProperty);

    public static void SetOwnedWindow(Window window, Window? value) => window.SetValue(OwnedWindowProperty, value);

    public static bool GetIsShown(Window window) => (bool)window.GetValue(IsShownProperty);

    public static void SetIsShown(Window window, bool value) => window.SetValue(IsShownProperty, value);

    static void OnOwnedWindowChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Window owner || e.NewValue is not Window owned)
            return;
        owned.Closing += OnOwnedClosing;
        // 所有者は、表示された（ウィンドウのハンドルがある）後でなければ設定できない
        owner.Loaded += (_, _) => {
            owned.Owner = owner;
            UpdateVisibility(owned);
        };
    }

    static void OnIsShownChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is Window window)
            UpdateVisibility(window);
    }

    // 所有者が決まるまでは表示しない（起動時の表示は、メインウィンドウの表示の後）
    static void UpdateVisibility(Window window)
    {
        if (window.Owner is null)
            return;
        if (GetIsShown(window))
            window.Show();
        else
            window.Hide();
    }

    // 閉じる操作は、ウィンドウを破棄せず非表示にする。メインウィンドウが閉じるときは、所有者とともに閉じる
    static void OnOwnedClosing(object? sender, CancelEventArgs e)
    {
        if (sender is not Window window || window.Owner is not { IsVisible: true })
            return;
        e.Cancel = true;
        window.SetCurrentValue(IsShownProperty, false);
    }
}
