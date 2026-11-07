using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Views.Converters;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// ツールボタン（<see cref="RadioButton"/>）に、そのボタンのツールを 1 か所で指定する（設計書 5.7）。
/// <see cref="ToolProperty"/> を設定すると、<c>CommandParameter</c> をそのツールにし、<c>IsChecked</c> を
/// <see cref="CurrentToolProperty"/>（スタイルで <c>CurrentTool</c> にバインドする）と <see cref="EnumEqualsConverter"/> でバインドする。
/// </summary>
/// <remarks>
/// <c>ConverterParameter</c> はバインドできないため、スタイルだけでは、ボタンごとに異なるツールとの比較を書けない。ここでバインディングを作って補う。
/// </remarks>
public static class ToolSelection
{
    static readonly EnumEqualsConverter enumEquals = new();

    /// <summary>
    /// このボタンが選ぶツール。
    /// </summary>
    public static readonly DependencyProperty ToolProperty =
        DependencyProperty.RegisterAttached("Tool", typeof(Tool?), typeof(ToolSelection), new(null, OnToolChanged));   // 既定値と同じツールの指定でも、変更として扱われるよう null を既定にする

    /// <summary>
    /// 現在のツール。ボタンの選択状態（IsChecked）を決める比較の元になる。
    /// </summary>
    public static readonly DependencyProperty CurrentToolProperty =
        DependencyProperty.RegisterAttached("CurrentTool", typeof(Tool), typeof(ToolSelection), new(Tool.Select));

    public static Tool? GetTool(RadioButton button) => (Tool?)button.GetValue(ToolProperty);

    public static void SetTool(RadioButton button, Tool? value) => button.SetValue(ToolProperty, value);

    public static Tool GetCurrentTool(RadioButton button) => (Tool)button.GetValue(CurrentToolProperty);

    public static void SetCurrentTool(RadioButton button, Tool value) => button.SetValue(CurrentToolProperty, value);

    static void OnToolChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not RadioButton button || e.NewValue is not Tool tool)
            return;
        button.CommandParameter = tool;
        // 選択の変更はコマンドで行うため、IsChecked からは書き戻さない（OneWay）
        button.SetBinding(ToggleButton.IsCheckedProperty, new Binding {
            Path = new PropertyPath(CurrentToolProperty),
            RelativeSource = RelativeSource.Self,
            Mode = BindingMode.OneWay,
            Converter = enumEquals,
            ConverterParameter = tool
        });
    }
}
