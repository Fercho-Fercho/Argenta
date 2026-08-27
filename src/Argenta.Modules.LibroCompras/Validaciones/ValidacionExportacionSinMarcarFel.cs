using Argenta.Core.Validacion;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Validaciones;

/// <summary>
/// Bloqueante: si el pool de un establecimiento trae facturas de exportación
/// (<c>Exp="SI"</c> en el XML) pero ESE establecimiento no está marcado como
/// exportador (<c>Exporta = No</c> en el catálogo), se bloquea desde el
/// principio — antes de intentar clasificar nada — para que el contador
/// decida si corrige el catálogo o revisa esas facturas.
/// </summary>
public sealed class ValidacionExportacionSinMarcarFel : IReglaValidacion<ContextoValidacionVentas>
{
    public IEnumerable<HallazgoValidacion> Validar(ContextoValidacionVentas contexto)
    {
        if (contexto.Establecimiento.Exporta) yield break;

        var facturasExportacion = contexto.Facturas.Where(f => f.EsExportacion).ToList();
        if (facturasExportacion.Count == 0) yield break;

        var listado = string.Join(", ", facturasExportacion.Select(f => f.ReferenciaCorta));
        yield return new HallazgoValidacion(
            SeveridadValidacion.Bloqueante,
            $"Hay {facturasExportacion.Count} factura(s) de exportación (Exp=\"SI\") en el ZIP para el establecimiento " +
            $"{contexto.Establecimiento.Numero} del cliente \"{contexto.Cliente.Nombre}\", pero ese establecimiento no está " +
            $"marcado como exportador (\"Exporta\" = No) en el catálogo. Márquelo como exportador o revise estas facturas " +
            $"antes de generar el libro: {listado}.");
    }
}
