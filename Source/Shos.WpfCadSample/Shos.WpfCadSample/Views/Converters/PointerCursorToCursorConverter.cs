using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using Shos.WpfCadSample.ViewModels;

namespace Shos.WpfCadSample.Views.Converters;

/// <summary>
/// カーソルの種類を、WPF のカーソルにする（仕様 4.3、設計書 5.7）。
/// </summary>
public sealed class PointerCursorToCursorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch {
            PointerCursor.Cross => Cursors.Cross,
            PointerCursor.Move => Cursors.SizeAll,
            PointerCursor.Arrow => Cursors.Arrow,
            _ => null
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
