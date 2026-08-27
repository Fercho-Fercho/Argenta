using System.Globalization;
using System.Windows.Data;
using ContaSuite.Data.Entidades;
using ContaSuite.Wpf.Modelos;

namespace ContaSuite.Wpf.Converters;

/// <summary>Muestra el enum AccionRevisar como texto legible ("Revisar" / "Excluir siempre") en la tabla.</summary>
public sealed class AccionRevisarATextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is AccionRevisar accion ? OpcionAccionRevisar.ATexto(accion) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
