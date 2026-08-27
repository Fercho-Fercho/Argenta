using Argenta.Core.Moneda;
using Argenta.Core.Utilidades;
using Argenta.Data.Entidades;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Aplica el orden de precedencia del libro de compras (sección 8 de las
/// especificaciones) a cada factura para decidir en qué columna cae el monto:
/// 1) Anulada → todo en 0.
/// 2) RECI/FPEQ → Gran Total completo a Exento.
/// 3) Impuesto especial por categoría del proveedor (Gasolinera → Petróleo,
///    Empresa Eléctrica → Tasa Municipal) → ese impuesto a Exento, Neto a Compras.
/// 4) Tipo del proveedor del catálogo (Compra/Servicio).
/// 5) Proveedor no encontrado → Compras + marcar para revisión.
/// 6) NCRE → toda la fila en negativo.
/// (CIVA y NABN se manejan aparte, fuera de este motor, por el orquestador del módulo.)
/// </summary>
public sealed class MotorClasificacion(IConversorMoneda conversor)
{
    /// <summary>
    /// Umbral del filtro de IVA (sección 1.3): si la diferencia entre el IVA
    /// calculado y el IVA real de la factura supera este valor (en cualquier
    /// dirección), la fila se marca para revisión.
    /// </summary>
    public const decimal UmbralDiferenciaIva = 0.10m;

    public FilaLibroCompras Clasificar(FacturaSat factura, IReadOnlyDictionary<string, Proveedor> proveedoresPorNit)
    {
        var nitProveedor = NitUtil.Normalizar(factura.NitEmisor);
        proveedoresPorNit.TryGetValue(nitProveedor, out var proveedor);
        var esNotaCredito = EsTipo(factura.TipoDte, "NCRE");

        var fila = new FilaLibroCompras
        {
            Fecha = factura.FechaEmision,
            Docto = factura.TipoDte,
            Serie = factura.Serie,
            NoDoc = factura.NumeroDte,
            Nit = factura.NitEmisor,
            Proveedor = factura.NombreEmisor,
            ProveedorNoEncontrado = proveedor is null,
            EsNotaCredito = esNotaCredito,
        };

        // 1) Anulada: todos los montos quedan en 0 (valor por defecto de las propiedades).
        if (factura.EsAnulada) return fila;

        if (!TryConvertirTodosLosMontos(factura, out var granTotal, out var iva, out var petroleo, out var otrosImpuestos))
        {
            // No debería ocurrir: las validaciones bloqueantes ya deben haber
            // detenido la generación si falta algún tipo de cambio.
            throw new InvalidOperationException(
                $"Falta el tipo de cambio del {factura.FechaEmision:dd/MM/yyyy} para convertir la factura {factura.ReferenciaCorta}.");
        }

        var neto = RedondeoUtil.Redondear(granTotal - iva - petroleo - otrosImpuestos);

        var esReciOFpeq = EsTipo(factura.TipoDte, "RECI") || EsTipo(factura.TipoDte, "FPEQ");

        // Robustez: se usa el proveedor mal catalogado como respaldo — si la
        // categoría no está marcada en el catálogo pero la factura ya trae el
        // impuesto correspondiente (> 0), se aplica igual el impuesto especial.
        var esGasolina = proveedor?.Categoria == CategoriaProveedor.Gasolinera || factura.Petroleo > 0;
        var impuestoEspecial = ObtenerImpuestoEspecial(proveedor, factura);
        var tieneImpuestoEspecial = impuestoEspecial > 0;
        fila.EsGasolina = esGasolina;

        if (esReciOFpeq)
        {
            fila.Exento = granTotal;
            fila.Iva = 0m;
            fila.Total = granTotal;
        }
        else if (tieneImpuestoEspecial)
        {
            fila.Compras = neto;
            fila.Exento = impuestoEspecial;
            fila.Iva = iva;
            fila.Total = granTotal;
        }
        else
        {
            var tipo = proveedor?.Tipo ?? TipoProveedor.Compra;
            if (tipo == TipoProveedor.Servicio) fila.Servicios = neto;
            else fila.Compras = neto;

            fila.Iva = iva;
            fila.Total = granTotal;
        }

        if (esNotaCredito)
        {
            fila.Compras = -fila.Compras;
            fila.Servicios = -fila.Servicios;
            fila.Exento = -fila.Exento;
            fila.Iva = -fila.Iva;
            fila.Total = -fila.Total;
        }

        // Filtro de IVA: corre AL FINAL, después de aplicar todo el orden de
        // precedencia. Es puramente informativo/de revisión — no cambia
        // montos ni clasificación, solo activa el indicador DescuadreIva.
        // Solo aplica a FACT, FCAM y NCRE, y NUNCA a gasolineras (por
        // instrucción explícita: ni cálculo ni color). Para otras categorías
        // con impuesto especial (p. ej. Empresa Eléctrica) el filtro sí
        // corre, pero usando el Neto en vez del Gran Total.
        if ((EsTipo(factura.TipoDte, "FACT") || EsTipo(factura.TipoDte, "FCAM") || esNotaCredito) && !esGasolina)
        {
            var calculoIva = CalcularIvaTeorico(neto);
            var diferencia = RedondeoUtil.Redondear(calculoIva - iva);

            fila.CalculoIva = calculoIva;
            fila.DescuadreIva = Math.Abs(diferencia) > UmbralDiferenciaIva;
        }

        return fila;
    }

    /// <summary>
    /// Impuesto especial de la factura, ya convertido a quetzales, según la
    /// categoría del proveedor (Cambio 3): Petróleo para gasolineras, Tasa
    /// Municipal para empresas eléctricas, 0 para el resto. Por robustez, si
    /// el proveedor no está catalogado con la categoría pero la factura ya
    /// trae ese impuesto (> 0), se aplica igual — para no perder el caso de
    /// un proveedor mal catalogado. Agregar una categoría nueva con su
    /// propio impuesto especial es tan simple como sumar un caso más a este
    /// switch.
    /// </summary>
    private decimal ObtenerImpuestoEspecial(Proveedor? proveedor, FacturaSat factura)
    {
        var montoSinConvertir = proveedor?.Categoria == CategoriaProveedor.Gasolinera || factura.Petroleo > 0
            ? factura.Petroleo
            : proveedor?.Categoria == CategoriaProveedor.EmpresaElectrica || factura.TasaMunicipal > 0
                ? factura.TasaMunicipal
                : 0m;

        if (montoSinConvertir <= 0m) return 0m;

        conversor.TryConvertirAQuetzales(montoSinConvertir, factura.Moneda, factura.FechaEmision, out var convertido);
        return RedondeoUtil.Redondear(convertido);
    }

    /// <summary>
    /// IVA teórico a partir del Neto (la base imponible ya sin IVA ni
    /// impuestos especiales — la misma base que se usa para Compras /
    /// Servicios): Neto × 12%. No se parte del Gran Total porque en
    /// facturas con impuesto especial (gasolina, empresa eléctrica) el
    /// Total incluye ese impuesto además del IVA, así que Total ÷ 1.12 ya
    /// no equivale a la base real.
    /// </summary>
    private static decimal CalcularIvaTeorico(decimal baseImponible) =>
        RedondeoUtil.Redondear(baseImponible * 0.12m);

    private bool TryConvertirTodosLosMontos(
        FacturaSat factura, out decimal granTotal, out decimal iva, out decimal petroleo, out decimal otrosImpuestos)
    {
        granTotal = iva = petroleo = otrosImpuestos = 0m;

        if (!conversor.TryConvertirAQuetzales(factura.GranTotal, factura.Moneda, factura.FechaEmision, out granTotal)) return false;
        if (!conversor.TryConvertirAQuetzales(factura.Iva, factura.Moneda, factura.FechaEmision, out iva)) return false;
        if (!conversor.TryConvertirAQuetzales(factura.Petroleo, factura.Moneda, factura.FechaEmision, out petroleo)) return false;
        if (!conversor.TryConvertirAQuetzales(factura.OtrosImpuestos, factura.Moneda, factura.FechaEmision, out otrosImpuestos)) return false;

        granTotal = RedondeoUtil.Redondear(granTotal);
        iva = RedondeoUtil.Redondear(iva);
        petroleo = RedondeoUtil.Redondear(petroleo);
        otrosImpuestos = RedondeoUtil.Redondear(otrosImpuestos);
        return true;
    }

    private static bool EsTipo(string tipoDte, string esperado) =>
        string.Equals(tipoDte?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
