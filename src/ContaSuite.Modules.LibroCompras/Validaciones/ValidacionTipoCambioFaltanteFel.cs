using ContaSuite.Core.Moneda;
using ContaSuite.Core.Validacion;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Validaciones;

/// <summary>
/// Regla bloqueante: si alguna factura FEL está en dólares y no hay tipo de
/// cambio registrado para su fecha de CERTIFICACIÓN (no la de emisión — ver
/// <see cref="Servicios.MotorClasificacionFel"/>), no se puede generar el libro.
/// </summary>
public sealed class ValidacionTipoCambioFaltanteFel(IProveedorTipoCambio proveedorTipoCambio)
    : IReglaValidacion<IReadOnlyList<DteFel>>
{
    public IEnumerable<HallazgoValidacion> Validar(IReadOnlyList<DteFel> contexto)
    {
        var fechasFaltantes = contexto
            .Where(f => string.Equals(f.CodigoMoneda.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.FechaCertificacion.Date)
            .Distinct()
            .Where(fecha => !proveedorTipoCambio.TryObtener(fecha, out _))
            .OrderBy(fecha => fecha)
            .ToList();

        if (fechasFaltantes.Count == 0) yield break;

        var listado = string.Join(", ", fechasFaltantes.Select(f => f.ToString("dd/MM/yyyy")));
        yield return new HallazgoValidacion(
            SeveridadValidacion.Bloqueante,
            "Faltan tipos de cambio para convertir facturas en dólares (según su fecha de CERTIFICACIÓN, no la de emisión). " +
            $"Cargue el tipo de cambio de estas fechas en el catálogo y vuelva a generar el libro: {listado}.");
    }
}
