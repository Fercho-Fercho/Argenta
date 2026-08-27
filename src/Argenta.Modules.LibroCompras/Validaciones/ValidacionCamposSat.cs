using Argenta.Core.Validacion;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Validaciones;

/// <summary>
/// Advertencias por factura: campos que la SAT normalmente trae en su valor
/// por defecto y que, si vienen distintos, el contador debe revisar antes de
/// generar el libro (no bloquean, pero deben reconocerse).
/// </summary>
public sealed class ValidacionCamposSat : IReglaValidacion<IReadOnlyList<FacturaSat>>
{
    public IEnumerable<HallazgoValidacion> Validar(IReadOnlyList<FacturaSat> contexto)
    {
        foreach (var factura in contexto)
        {
            if (factura.ClasificacionEmisor.Trim() != "0")
            {
                yield return new HallazgoValidacion(
                    SeveridadValidacion.Advertencia,
                    $"La factura {factura.ReferenciaCorta} del {factura.FechaEmision:dd/MM/yyyy} tiene Clasificación emisor = \"{factura.ClasificacionEmisor}\" (se esperaba 0).",
                    factura.ReferenciaCorta);
            }

            if (!string.Equals(factura.Exportacion.Trim(), "No", StringComparison.OrdinalIgnoreCase))
            {
                yield return new HallazgoValidacion(
                    SeveridadValidacion.Advertencia,
                    $"La factura {factura.ReferenciaCorta} del {factura.FechaEmision:dd/MM/yyyy} tiene Exportación = \"{factura.Exportacion}\" (se esperaba \"No\").",
                    factura.ReferenciaCorta);
            }

            if (!string.Equals(factura.UbicacionTemporal.Trim(), "No", StringComparison.OrdinalIgnoreCase))
            {
                yield return new HallazgoValidacion(
                    SeveridadValidacion.Advertencia,
                    $"La factura {factura.ReferenciaCorta} del {factura.FechaEmision:dd/MM/yyyy} tiene Ubicación temporal = \"{factura.UbicacionTemporal}\" (se esperaba \"No\").",
                    factura.ReferenciaCorta);
            }
        }
    }
}
