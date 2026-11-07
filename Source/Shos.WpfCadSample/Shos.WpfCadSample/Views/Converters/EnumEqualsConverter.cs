using System.Globalization;
using System.Windows.Data;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// 列挙値がパラメーターと等しいか。ツールボタンの選択状態に使う（設計書 5.7）。
/// 選択の変更はコマンドで行うため、逆向きの変換はしない。
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Equals(value, parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
