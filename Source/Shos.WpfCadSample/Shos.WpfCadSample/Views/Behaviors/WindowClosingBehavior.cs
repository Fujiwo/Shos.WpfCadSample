using System.ComponentModel;
using System.Windows;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// メインウィンドウの終了の流れ（設計書 6.5）：<see cref="MainViewModel.CloseRequested"/> でウィンドウを閉じ、
/// 閉じる直前（Exit、タイトルバーの閉じるボタン、Alt+F4）に <see cref="MainViewModel.TryClose"/> を呼び、偽なら閉じるのを取り消す。
/// </summary>
public static class WindowClosingBehavior
{
    /// <summary>
    /// 閉じる流れを任せる <see cref="MainViewModel"/>（メインウィンドウの DataContext をバインドする）。
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.RegisterAttached("ViewModel", typeof(MainViewModel), typeof(WindowClosingBehavior), new(null, OnViewModelChanged));

    public static MainViewModel? GetViewModel(Window window) => (MainViewModel?)window.GetValue(ViewModelProperty);

    public static void SetViewModel(Window window, MainViewModel? value) => window.SetValue(ViewModelProperty, value);

    // ViewModel は起動時に 1 回だけ設定される（メインウィンドウと MainViewModel は、アプリケーションの生存期間で 1 組）
    static void OnViewModelChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Window window || e.NewValue is not MainViewModel viewModel)
            return;
        viewModel.CloseRequested += (_, _) => window.Close();
        window.Closing += OnClosing;
    }

    static void OnClosing(object? sender, CancelEventArgs e)
    {
        if (sender is Window window && GetViewModel(window) is { } viewModel && !viewModel.TryClose())
            e.Cancel = true;
    }
}
