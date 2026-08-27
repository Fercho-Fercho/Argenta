using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Argenta.Wpf.Converters;

/// <summary>Oculta el control cuando el string enlazado es nulo o vacío (ej. mensajes de advertencia opcionales).</summary>
public sealed class NuloOVacioAVisibilidadConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
