using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// クリップボードの内容に合わせて Paste の有効状態を更新する契機（WPF にはクリップボードの変更通知がないため。設計書 5.4、R-5）：
/// ウィンドウのアクティブ化と、メニューを開いたとき。
/// </summary>
public static class CommandEvents
{
    /// <summary>
    /// ウィンドウがアクティブになったときに実行するコマンド。
    /// </summary>
    public static readonly DependencyProperty ActivatedProperty =
        DependencyProperty.RegisterAttached("Activated", typeof(ICommand), typeof(CommandEvents), new(null, OnActivatedChanged));

    /// <summary>
    /// メニューを開いたときに実行するコマンド（その下のサブメニューを開いたときは除く）。
    /// </summary>
    public static readonly DependencyProperty SubmenuOpenedProperty =
        DependencyProperty.RegisterAttached("SubmenuOpened", typeof(ICommand), typeof(CommandEvents), new(null, OnSubmenuOpenedChanged));

    public static ICommand? GetActivated(Window window) => (ICommand?)window.GetValue(ActivatedProperty);

    public static void SetActivated(Window window, ICommand? value) => window.SetValue(ActivatedProperty, value);

    public static ICommand? GetSubmenuOpened(MenuItem menuItem) => (ICommand?)menuItem.GetValue(SubmenuOpenedProperty);

    public static void SetSubmenuOpened(MenuItem menuItem, ICommand? value) => menuItem.SetValue(SubmenuOpenedProperty, value);

    // 購読は最初に設定されたときの 1 回だけ。実行時に、その時点のコマンドを読む（バインドの更新に追従する）
    static void OnActivatedChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is Window window && e.OldValue is null)
            window.Activated += (_, _) => Execute(GetActivated(window));
    }

    static void OnSubmenuOpenedChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is MenuItem menuItem && e.OldValue is null)
            menuItem.SubmenuOpened += (_, args) => {
                if (args.OriginalSource == menuItem)    // 子のメニューが開いたときは除く
                    Execute(GetSubmenuOpened(menuItem));
            };
    }

    static void Execute(ICommand? command)
    {
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
