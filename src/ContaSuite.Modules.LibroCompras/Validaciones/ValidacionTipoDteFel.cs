using ContaSuite.Core.Validacion;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Validaciones;

/// <summary>
/// Advertencia: tipos de DTE que no están en la tabla de referencia de
/// <see cref="ComportamientoDteCatalogo"/>. Se procesan igual (por defecto,
/// como Grupo A: normal con IVA) para no romper las sumas del libro, pero se
/// marcan para que el contador los revise.
/// </summary>
public sealed class ValidacionTipoDteFel : IReglaValidacion<IReadOnlyList<DteFel>>
{
    public IEnumerable<HallazgoValidacion> Validar(IReadOnlyList<DteFel> contexto)
    {
        foreach (var factura in contexto)
        {
            if (ComportamientoDteCatalogo.EsConocido(factura.TipoDte)) continue;

            yield return new HallazgoValidacion(
                SeveridadValidacion.Advertencia,
                $"Tipo de DTE no reconocido: {factura.TipoDte} (factura {factura.ReferenciaCorta} del " +
                $"{factura.FechaEmision:dd/MM/yyyy}). Se procesó como Grupo A (afecta, con IVA) por defecto; revísela.",
                factura.ReferenciaCorta);
        }
    }
}
