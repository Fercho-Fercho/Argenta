using Argenta.Core.Validacion;
using Argenta.Data.Entidades;
using Argenta.Modules.LibroCompras.Modelos;
using Argenta.Modules.LibroCompras.Servicios;

namespace Argenta.Modules.LibroCompras.Validaciones;

/// <summary>
/// Bloqueante: un establecimiento Profesional solo puede emitir facturas de
/// Servicios. Si alguna factura del pool de ESE establecimiento trae un
/// ítem Bien ("B"), se bloquea la generación de ese libro e indica cuál(es)
/// factura(s).
/// </summary>
public sealed class ValidacionProfesionalSoloServiciosFel : IReglaValidacion<ContextoValidacionVentas>
{
    public IEnumerable<HallazgoValidacion> Validar(ContextoValidacionVentas contexto)
    {
        if (contexto.Establecimiento.Tipo != TipoCliente.Profesional) yield break;

        foreach (var factura in contexto.Facturas)
        {
            var comportamiento = MotorClasificacionVentasFel.ComportamientoParaVentas(factura.TipoDte);
            var usaItems = comportamiento == ComportamientoDte.Afecta || comportamiento == ComportamientoDte.Resta;
            if (!usaItems) continue;

            var tieneBien = factura.Items.Any(i => string.Equals(i.BienOServicio, "B", StringComparison.OrdinalIgnoreCase));
            if (!tieneBien) continue;

            yield return new HallazgoValidacion(
                SeveridadValidacion.Bloqueante,
                $"El establecimiento {contexto.Establecimiento.Numero} del cliente \"{contexto.Cliente.Nombre}\" es Profesional " +
                $"(solo puede vender Servicios), pero la factura {factura.ReferenciaCorta} del {factura.FechaEmision:dd/MM/yyyy} " +
                "tiene al menos un ítem tipo Bien. Corrija el tipo del establecimiento en el catálogo de Clientes o excluya esa " +
                "factura del ZIP antes de generar el libro.",
                factura.ReferenciaCorta);
        }
    }
}
