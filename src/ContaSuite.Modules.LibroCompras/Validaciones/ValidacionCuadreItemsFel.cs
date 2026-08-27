using ContaSuite.Core.Validacion;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Validaciones;

/// <summary>
/// Advertencia: la suma de los <c>Total</c> de los ítems de la factura debe
/// coincidir con su Gran Total. Se compara en la moneda original del XML
/// (antes de convertir a quetzales): si cuadra ahí, sigue cuadrando después
/// de la conversión, porque es un simple factor multiplicativo.
/// </summary>
public sealed class ValidacionCuadreItemsFel : IReglaValidacion<IReadOnlyList<DteFel>>
{
    private const decimal Tolerancia = 0.05m;

    public IEnumerable<HallazgoValidacion> Validar(IReadOnlyList<DteFel> contexto)
    {
        foreach (var factura in contexto)
        {
            var comportamiento = ComportamientoDteCatalogo.Obtener(factura.TipoDte);
            var usaItems = comportamiento == ComportamientoDte.Afecta || comportamiento == ComportamientoDte.Resta;
            if (!usaItems) continue; // Grupo B/D: todo va a Exento (o aparte) por regla; no hay nada que cuadrar contra los ítems.

            var sumaItems = factura.Items.Sum(i => i.Total);
            var diferencia = Math.Abs(sumaItems - factura.GranTotal);

            if (diferencia > Tolerancia)
            {
                yield return new HallazgoValidacion(
                    SeveridadValidacion.Advertencia,
                    $"La factura {factura.ReferenciaCorta} del {factura.FechaEmision:dd/MM/yyyy} tiene una diferencia " +
                    $"de {diferencia:N2} entre la suma de sus ítems y el Gran Total del XML; revísela.",
                    factura.ReferenciaCorta);
            }
        }
    }
}
