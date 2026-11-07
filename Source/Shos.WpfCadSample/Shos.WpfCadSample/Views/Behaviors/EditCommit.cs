using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Shos.WpfCadSample.Views.Behaviors;

/// <summary>
/// 連続的な編集の完了時に、確定のコマンドを呼ぶ（仕様 5.4、設計書 5.7）：
/// スライダーはドラッグの完了・クリック・キー操作の後、テキストボックスは <c>Enter</c> とフォーカスの移動、リストは選択の変更。
/// </summary>
public static class EditCommit
{
    /// <summary>
    /// 編集の完了時に実行するコマンド（引数なし）。
    /// </summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(EditCommit), new(null, OnCommandChanged));

    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommand(DependencyObject element, ICommand? value) => element.SetValue(CommandProperty, value);

    static void OnCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is not null)
            return;     // イベントの購読は、最初に設定されたときに 1 回だけ
        switch (target) {
            case Slider slider:
                // つまみのドラッグ、トラックのクリック、キー操作の、それぞれの終わりを完了とする
                slider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((_, _) => Commit(slider)));
                slider.PreviewMouseLeftButtonUp += (_, _) => Commit(slider);
                slider.KeyUp += (_, _) => Commit(slider);
                break;
            case TextBox textBox:
                textBox.KeyDown += (_, args) => {
                    if (args.Key == Key.Enter)
                        Commit(textBox);
                };
                textBox.LostKeyboardFocus += (_, _) => Commit(textBox);
                break;
            case Selector selector:
                selector.SelectionChanged += (_, _) => Commit(selector);
                break;
        }
    }

    // 確定済みの状態で呼んでも、何も記録されない（AttributeEditor.Commit）
    static void Commit(DependencyObject element)
    {
        if (GetCommand(element) is { } command && command.CanExecute(null))
            command.Execute(null);
    }
}
