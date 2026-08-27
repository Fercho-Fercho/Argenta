using System.Globalization;
using System.Windows.Data;

namespace Argenta.Wpf.Converters;

/// <summary>Muestra un bool como "Si"/"No" (columna "Marca de Anulado" del libro de ventas).</summary>
public sealed class BoolASiNoConverter : IValueConverter
{
    public static readonly BoolASiNoConverter Instancia = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Si" : "No";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
