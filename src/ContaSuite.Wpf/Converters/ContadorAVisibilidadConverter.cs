using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ContaSuite.Wpf.Converters;

/// <summary>Oculta el control cuando el contador enlazado es 0.</summary>
public sealed class ContadorAVisibilidadConverter : IValueConverter
{
    public static readonly ContadorAVisibilidadConverter Instancia = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int cantidad && cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
