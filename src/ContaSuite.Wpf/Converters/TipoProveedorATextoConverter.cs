using System.Globalization;
using System.Windows.Data;
using ContaSuite.Data.Entidades;
using ContaSuite.Wpf.Modelos;

namespace ContaSuite.Wpf.Converters;

/// <summary>Muestra el enum TipoProveedor como texto legible ("Compra" / "Servicio") en la tabla.</summary>
public sealed class TipoProveedorATextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TipoProveedor tipo ? OpcionTipoProveedor.ATexto(tipo) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
