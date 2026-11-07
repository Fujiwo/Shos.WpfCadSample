using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// メニューのショートカット（仕様 4.2。<c>Alt+F4</c> を除く）の 1 つ。<see cref="Command"/> は、ウィンドウの DataContext（MainViewModel）からのパス。
/// </summary>
public sealed class Shortcut
{
    public Key Key { get; set; }

    public ModifierKeys Modifiers { get; set; }

    public string Command { get; set; } = "";
}

/// <summary>
/// ショートカットの一覧。<c>App.xaml</c> のリソースに 1 つだけ定義し、メインウィンドウと属性ウィンドウが共有する（設計書 5.7）。
/// </summary>
public sealed class ShortcutCollection : List<Shortcut>;

/// <summary>
/// 共有のショートカットの一覧から、ウィンドウの <see cref="UIElement.InputBindings"/> を作る（仕様 4.5、A-25）。
/// <see cref="KeyBinding"/> は 1 つの要素にしか属せないため、一覧はデータとして共有し、ウィンドウごとに作る。
/// </summary>
public static class Shortcuts
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached("Source", typeof(ShortcutCollection), typeof(Shortcuts), new(null, OnSourceChanged));

    public static ShortcutCollection? GetSource(UIElement element) => (ShortcutCollection?)element.GetValue(SourceProperty);

    public static void SetSource(UIElement element, ShortcutCollection? value) => element.SetValue(SourceProperty, value);

    static void OnSourceChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not UIElement element || e.NewValue is not ShortcutCollection shortcuts)
            return;
        foreach (var shortcut in shortcuts) {
            var binding = new KeyBinding { Key = shortcut.Key, Modifiers = shortcut.Modifiers };
            BindingOperations.SetBinding(binding, InputBinding.CommandProperty, new Binding(shortcut.Command));
            element.InputBindings.Add(binding);
        }
    }
}
