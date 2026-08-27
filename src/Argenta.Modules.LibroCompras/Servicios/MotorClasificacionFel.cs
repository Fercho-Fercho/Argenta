using Argenta.Core.Moneda;
using Argenta.Core.Utilidades;
using Argenta.Data.Entidades;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Clasifica cada factura FEL (XML) en las columnas del libro de compras,
/// según el <see cref="ComportamientoDte"/> de su tipo (ver <see cref="ComportamientoDteCatalogo"/>):
/// 1) Afecta (Grupo A) → se recorren los ítems: los que sí llevan IVA
///    reparten su neto (Total − IVA − impuestos especiales) entre Compras
///    ("B") o Servicios ("S") según corresponda, el IVA de todos ellos se
///    unifica en Iva, y sus impuestos especiales (Petróleo, Tasa Municipal,
///    Turismo Hospedaje, etc. — cualquiera que no sea IVA) se restan y van a
///    Exento. Los ítems SIN IVA (aunque otros de la misma factura sí lleven)
///    van completos a Exento, sin importar si son bien o servicio: así el
///    filtro de IVA (base × 12% vs IVA real) solo compara contra los ítems
///    que de verdad generan IVA.
/// 2) NoAfecta (Grupo B) → el Gran Total completo va a Exento; Compras,
///    Servicios e Iva quedan en 0.
/// 3) Resta (Grupo C) → igual que Afecta, pero toda la fila en negativo.
/// (Aparte / Grupo D: NABN y CIVA se manejan fuera de este motor, por el
/// orquestador del módulo — igual que aquí, solo que el resultado nunca
/// entra a los totales del libro.)
///
/// A diferencia de <see cref="MotorClasificacion"/> (Excel de la SAT), aquí
/// NO se usa el catálogo de Proveedores para decidir Compra/Servicio (cada
/// factura ya lo indica por ítem). Sí se usa el catálogo separado
/// "Proveedores a revisar" (<see cref="ProveedorRevisar"/>) para resaltar
/// filas específicas en naranja o excluirlas por defecto — sin afectar
/// nunca los montos calculados.
///
/// La conversión de moneda usa la fecha de CERTIFICACIÓN (no la de emisión):
/// la fecha de emisión solo se usa para ubicar la factura en el libro.
/// </summary>
public sealed class MotorClasificacionFel(IConversorMoneda conversor)
{
    public FilaLibroCompras Clasificar(DteFel factura, IReadOnlyDictionary<string, ProveedorRevisar> proveedoresRevisar)
    {
        var comportamiento = ComportamientoDteCatalogo.Obtener(factura.TipoDte);
        var esResta = comportamiento == ComportamientoDte.Resta;
        var esAfecta = comportamiento == ComportamientoDte.Afecta || esResta;

        var nitNormalizado = NitUtil.Normalizar(factura.NitEmisor);
        proveedoresRevisar.TryGetValue(nitNormalizado, out var proveedorRevisar);

        var fila = new FilaLibroCompras
        {
            Fecha = factura.FechaEmision,
            Docto = factura.TipoDte,
            Serie = factura.Serie,
            NoDoc = factura.NumeroDte,
            Nit = factura.NitEmisor,
            Proveedor = factura.NombreEmisor,
            // No se usa el catálogo de Proveedores normal en esta fuente: nunca hay marcado naranja por "no encontrado".
            ProveedorNoEncontrado = false,
            EsNotaCredito = esResta,
            OrigenFel = factura,
        };

        fila.EsGasolina = factura.Items
            .SelectMany(i => i.Impuestos)
            .Any(imp => string.Equals(imp.NombreCorto.Trim(), "PETROLEO", StringComparison.OrdinalIgnoreCase));

        // Catálogo "Proveedores a revisar": solo resalta / preselecciona,
        // nunca cambia los montos calculados abajo.
        fila.MarcadoParaRevisar = proveedorRevisar?.Accion == AccionRevisar.Revisar;
        fila.ExcluidoPorCatalogo = proveedorRevisar?.Accion == AccionRevisar.ExcluirSiempre;
        if (fila.ExcluidoPorCatalogo) fila.Incluida = false;

        decimal comprasRaw = 0m, serviciosRaw = 0m, exentoRaw = 0m, ivaRaw = 0m;

        if (esAfecta)
        {
            foreach (var item in factura.Items)
            {
                var itemIva = item.TotalIva;

                if (itemIva == 0m)
                {
                    // Ítem sin IVA, aunque otros ítems de la misma factura sí
                    // lo lleven (p. ej. una factura con bienes y servicios
                    // mezclados, donde solo algunos están gravados): va
                    // completo a Exento, para que el filtro de IVA (base ×
                    // 12% vs IVA real, más abajo) solo compare contra los
                    // ítems que realmente generan IVA y así cuadre.
                    exentoRaw += item.Total;
                    continue;
                }

                var itemEspecial = item.TotalEspeciales;
                var itemNeto = item.Neto;

                if (string.Equals(item.BienOServicio, "S", StringComparison.OrdinalIgnoreCase))
                {
                    serviciosRaw += itemNeto;
                }
                else
                {
                    // "B" (bien) y cualquier valor no reconocido caen a Compras por defecto.
                    comprasRaw += itemNeto;
                }

                ivaRaw += itemIva;
                exentoRaw += itemEspecial;
            }
        }
        else
        {
            // NoAfecta (Grupo B) y Aparte (Grupo D): el Gran Total completo va a Exento.
            exentoRaw = factura.GranTotal;
        }

        if (!TryConvertir(comprasRaw, factura, out var compras) ||
            !TryConvertir(serviciosRaw, factura, out var servicios) ||
            !TryConvertir(exentoRaw, factura, out var exento) ||
            !TryConvertir(ivaRaw, factura, out var iva) ||
            !TryConvertir(factura.GranTotal, factura, out var granTotal))
        {
            throw new InvalidOperationException(
                $"Falta el tipo de cambio del {factura.FechaCertificacion:dd/MM/yyyy} (fecha de CERTIFICACIÓN) " +
                $"para convertir la factura {factura.ReferenciaCorta}.");
        }

        compras = RedondeoUtil.Redondear(compras);
        servicios = RedondeoUtil.Redondear(servicios);
        exento = RedondeoUtil.Redondear(exento);
        iva = RedondeoUtil.Redondear(iva);
        // Prioriza el Gran Total real del XML (columna L), no la suma de las demás columnas.
        granTotal = RedondeoUtil.Redondear(granTotal);

        fila.Compras = compras;
        fila.Servicios = servicios;
        fila.Exento = exento;
        fila.Iva = iva;
        fila.Total = granTotal;

        if (esResta)
        {
            fila.Compras = -fila.Compras;
            fila.Servicios = -fila.Servicios;
            fila.Exento = -fila.Exento;
            fila.Iva = -fila.Iva;
            fila.Total = -fila.Total;
        }

        // Filtro de IVA (misma regla que la otra pestaña): base × 12% vs IVA
        // real, diferencia > Q0.10 → amarillo. Aplica a cualquier factura que
        // pase por el flujo normal con IVA (Grupo A y Grupo C), sin importar
        // el tipo exacto — todas usan el mismo cálculo de Neto por ítem.
        if (esAfecta)
        {
            var baseImponible = compras + servicios;
            var calculoIva = RedondeoUtil.Redondear(baseImponible * 0.12m);
            var diferencia = RedondeoUtil.Redondear(calculoIva - iva);

            fila.CalculoIva = calculoIva;
            fila.DescuadreIva = Math.Abs(diferencia) > MotorClasificacion.UmbralDiferenciaIva;
        }

        return fila;
    }

    private bool TryConvertir(decimal monto, DteFel factura, out decimal resultado) =>
        conversor.TryConvertirAQuetzales(monto, factura.CodigoMoneda, factura.FechaCertificacion, out resultado);
}
