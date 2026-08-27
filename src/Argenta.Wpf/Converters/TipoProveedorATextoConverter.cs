using System.Globalization;
using System.Windows.Data;
using Argenta.Data.Entidades;
using Argenta.Wpf.Modelos;

namespace Argenta.Wpf.Converters;

/// <summary>Muestra el enum TipoProveedor como texto legible ("Compra" / "Servicio") en la tabla.</summary>
public sealed class TipoProveedorATextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TipoProveedor tipo ? OpcionTipoProveedor.ATexto(tipo) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
