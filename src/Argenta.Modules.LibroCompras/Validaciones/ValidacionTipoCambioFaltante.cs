using Argenta.Core.Moneda;
using Argenta.Core.Validacion;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Validaciones;

/// <summary>
/// Regla bloqueante: si alguna factura está en dólares y no hay tipo de cambio
/// registrado para su fecha de emisión, no se puede generar el libro.
/// </summary>
public sealed class ValidacionTipoCambioFaltante(IProveedorTipoCambio proveedorTipoCambio)
    : IReglaValidacion<IReadOnlyList<FacturaSat>>
{
    public IEnumerable<HallazgoValidacion> Validar(IReadOnlyList<FacturaSat> contexto)
    {
        var fechasFaltantes = contexto
            .Where(f => !f.EsAnulada && string.Equals(f.Moneda.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.FechaEmision.Date)
            .Distinct()
            .Where(fecha => !proveedorTipoCambio.TryObtener(fecha, out _))
            .OrderBy(fecha => fecha)
            .ToList();

        if (fechasFaltantes.Count == 0) yield break;

        var listado = string.Join(", ", fechasFaltantes.Select(f => f.ToString("dd/MM/yyyy")));
        yield return new HallazgoValidacion(
            SeveridadValidacion.Bloqueante,
            $"Faltan tipos de cambio para convertir facturas en dólares. Cargue el tipo de cambio de estas fechas en el catálogo y vuelva a generar el libro: {listado}.");
    }
}
