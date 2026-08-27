using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Argenta.Core.Validacion;

namespace Argenta.Wpf.Converters;

/// <summary>Pinta de rojo los hallazgos bloqueantes y de ámbar las advertencias.</summary>
public sealed class SeveridadAColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SeveridadValidacion.Bloqueante => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
        SeveridadValidacion.Advertencia => new SolidColorBrush(Color.FromRgb(0xB2, 0x6A, 0x00)),
        _ => Brushes.Black,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
