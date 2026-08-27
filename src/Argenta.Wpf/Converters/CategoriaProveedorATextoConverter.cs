using System.Globalization;
using System.Windows.Data;
using Argenta.Data.Entidades;
using Argenta.Wpf.Modelos;

namespace Argenta.Wpf.Converters;

/// <summary>Muestra el enum CategoriaProveedor como texto legible ("Normal" / "Gasolinera" / "Empresa Eléctrica") en la tabla.</summary>
public sealed class CategoriaProveedorATextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CategoriaProveedor categoria ? OpcionCategoriaProveedor.ATexto(categoria) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
