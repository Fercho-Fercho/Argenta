using ContaSuite.Core.Moneda;
using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>
/// Clasifica cada factura FEL (XML) en las columnas del libro de Ventas.
/// Mismo esquema de comportamiento por tipo de DTE que <see cref="MotorClasificacionFel"/>
/// (compras), con las diferencias del libro de Ventas:
/// 1) El Nit/Nombre de la fila son los del RECEPTOR (comprador), no del emisor.
/// 2) No existe la columna Exento: los tipos "No afecta" del catálogo
///    compartido (que en compras van a Exento) aquí se tratan como Aparte
///    (se listan abajo, sin afectar los totales) — ver <see cref="ComportamientoParaVentas"/>.
/// 3) Exportación (<c>Exp="SI"</c>): el Gran Total completo va a Exportaciones,
///    no a Ventas/Servicios. El IVA se calcula igual (suma de los ítems).
/// 4) INGUAT (impuesto de turismo/hospedaje) va a su propia columna cuando el
///    establecimiento es Tipo Hotel; se separa igual que un impuesto especial.
/// 5) Anulada (según el Excel de consulta de documentos, ver
///    <see cref="EstadoDocumentoSat"/>): la fila se mantiene en el libro,
///    pero con todos los montos en 0 — nunca se manda a la sección aparte.
/// </summary>
public sealed class MotorClasificacionVentasFel(IConversorMoneda conversor)
{
    /// <summary>
    /// Código corto (dte:NombreCorto) del impuesto de turismo/hospedaje
    /// (INGUAT). En el XML real del SAT viene con espacio ("TURISMO
    /// HOSPEDAJE"); se compara sin espacios para no depender de ese detalle.
    /// </summary>
    private const string CodigoTurismoHospedaje = "TURISMOHOSPEDAJE";

    private static bool EsTurismoHospedaje(ImpuestoItemFel impuesto) =>
        string.Equals(
            impuesto.NombreCorto.Replace(" ", "").Trim(),
            CodigoTurismoHospedaje,
            StringComparison.OrdinalIgnoreCase);

    public FilaLibroVentas Clasificar(DteFel factura, Establecimiento establecimiento, IReadOnlyDictionary<string, EstadoDocumentoSat> estados)
    {
        var comportamiento = ComportamientoParaVentas(factura.TipoDte);
        var esResta = comportamiento == ComportamientoDte.Resta;
        var esAfecta = comportamiento == ComportamientoDte.Afecta || esResta;

        var anulada = estados.TryGetValue(factura.NumeroAutorizacion.Trim(), out var estado) && estado.Anulado;

        var fila = new FilaLibroVentas
        {
            Fecha = factura.FechaEmision,
            Docto = factura.TipoDte,
            Serie = factura.Serie,
            NoDoc = factura.NumeroDte,
            Nit = factura.IdReceptor,
            Nombre = factura.NombreReceptor,
            EsNotaCredito = esResta,
            Anulada = anulada,
            OrigenFel = factura,
        };

        // Anulada: se queda en el libro, pero todo en 0 (valores por defecto de las propiedades).
        if (anulada) return fila;

        decimal ventasRaw = 0m, serviciosRaw = 0m, exportacionesRaw = 0m, inguatRaw = 0m, ivaRaw = 0m;

        if (esAfecta)
        {
            if (factura.EsExportacion)
            {
                // Todo el Gran Total va a Exportaciones; el IVA se calcula
                // igual, sumando los ítems (normalmente 0 en exportaciones).
                exportacionesRaw = factura.GranTotal;
                ivaRaw = factura.Items.Sum(i => i.TotalIva);
            }
            else
            {
                foreach (var item in factura.Items)
                {
                    var itemTurismo = item.Impuestos
                        .Where(EsTurismoHospedaje)
                        .Sum(imp => imp.MontoImpuesto);

                    if (establecimiento.Tipo == TipoCliente.Hotel) inguatRaw += itemTurismo;

                    if (string.Equals(item.BienOServicio, "B", StringComparison.OrdinalIgnoreCase))
                    {
                        ventasRaw += item.Neto;
                    }
                    else
                    {
                        // "S" (servicio) y cualquier valor no reconocido caen a Servicios por defecto.
                        serviciosRaw += item.Neto;
                    }

                    ivaRaw += item.TotalIva;
                }
            }
        }
        else
        {
            // Aparte (RANT/RECI/CIVA/NABN y cualquier "No afecta" del catálogo
            // compartido, que en ventas no tiene columna Exento a donde ir):
            // el orquestador (LibroVentasFelService) ya la separó antes de
            // llamar a este método, pero si llega aquí igual no debe sumar.
            return fila;
        }

        if (!TryConvertir(ventasRaw, factura, out var ventas) ||
            !TryConvertir(serviciosRaw, factura, out var servicios) ||
            !TryConvertir(exportacionesRaw, factura, out var exportaciones) ||
            !TryConvertir(inguatRaw, factura, out var inguat) ||
            !TryConvertir(ivaRaw, factura, out var iva) ||
            !TryConvertir(factura.GranTotal, factura, out var granTotal))
        {
            throw new InvalidOperationException(
                $"Falta el tipo de cambio del {factura.FechaCertificacion:dd/MM/yyyy} (fecha de CERTIFICACIÓN) " +
                $"para convertir la factura {factura.ReferenciaCorta}.");
        }

        fila.Ventas = RedondeoUtil.Redondear(ventas);
        fila.Servicios = RedondeoUtil.Redondear(servicios);
        fila.Exportaciones = RedondeoUtil.Redondear(exportaciones);
        fila.Inguat = RedondeoUtil.Redondear(inguat);
        fila.Iva = RedondeoUtil.Redondear(iva);
        // Prioriza el Gran Total real del XML, no la suma de las demás columnas.
        fila.Total = RedondeoUtil.Redondear(granTotal);

        if (esResta)
        {
            fila.Ventas = -fila.Ventas;
            fila.Servicios = -fila.Servicios;
            fila.Exportaciones = -fila.Exportaciones;
            fila.Inguat = -fila.Inguat;
            fila.Iva = -fila.Iva;
            fila.Total = -fila.Total;
        }

        // Filtro de IVA: base = Ventas + Servicios (las exportaciones quedan
        // fuera de la base — normalmente van con IVA 0, así que el cálculo
        // ya cuadra solo). Único resaltado que aplica en ventas (amarillo).
        var baseImponible = fila.Ventas + fila.Servicios;
        var calculoIva = RedondeoUtil.Redondear(baseImponible * 0.12m);
        var diferencia = RedondeoUtil.Redondear(calculoIva - fila.Iva);

        fila.CalculoIva = calculoIva;
        fila.DescuadreIva = Math.Abs(diferencia) > MotorClasificacion.UmbralDiferenciaIva;

        return fila;
    }

    /// <summary>
    /// En ventas no existe la columna Exento: los tipos "No afecta" del
    /// catálogo compartido (RANT, RECI, FPEQ, etc. — que en compras van
    /// completos a Exento) se tratan como Aparte, igual que CIVA/NABN.
    /// </summary>
    public static ComportamientoDte ComportamientoParaVentas(string tipoDte)
    {
        var comportamiento = ComportamientoDteCatalogo.Obtener(tipoDte);
        return comportamiento == ComportamientoDte.NoAfecta ? ComportamientoDte.Aparte : comportamiento;
    }

    private bool TryConvertir(decimal monto, DteFel factura, out decimal resultado) =>
        conversor.TryConvertirAQuetzales(monto, factura.CodigoMoneda, factura.FechaCertificacion, out resultado);
}
