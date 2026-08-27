using Argenta.Core.Utilidades;
using Argenta.Core.Validacion;
using Argenta.Data.Entidades;
using Argenta.Data.Repositorios;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Orquesta el flujo del módulo para la fuente XML (FEL): valida el lote de
/// facturas ya leído del ZIP, clasifica cada una y arma las filas del libro
/// (numeradas y ordenadas), listas para previsualizar o exportar a .xlsx.
/// Reutiliza <see cref="ResultadoProcesamiento"/> y <see cref="LibroComprasService.NumerarFilas"/>
/// tal cual los usa la pestaña del Excel de la SAT.
///
/// También recuerda, por cliente y periodo, qué facturas dejó el usuario
/// incluidas/excluidas la última vez (ver <see cref="ISeleccionFacturaRepositorio"/>
/// y la regla de privacidad en <c>SeleccionFactura</c>): al procesar un pool
/// nuevo, esas decisiones guardadas se restauran; al llamar a
/// <see cref="GuardarSeleccionAsync"/> (desde "Generar" o el botón "Guardar
/// selección" de la pantalla), se guarda el estado final.
/// </summary>
public sealed class LibroComprasFelService(
    MotorClasificacionFel motorClasificacion,
    MotorValidaciones<IReadOnlyList<DteFel>> motorValidaciones,
    IProveedorRevisarRepositorio proveedorRevisarRepositorio,
    ISeleccionFacturaRepositorio seleccionFacturaRepositorio)
{
    public async Task<ResultadoProcesamiento> ProcesarAsync(IReadOnlyList<DteFel> facturas)
    {
        var hallazgos = motorValidaciones.Evaluar(facturas);

        if (hallazgos.Any(h => h.Severidad == SeveridadValidacion.Bloqueante))
        {
            return new ResultadoProcesamiento { Filas = [], FilasAparte = [], Hallazgos = hallazgos };
        }

        var proveedoresRevisar = await proveedorRevisarRepositorio.ObtenerDiccionarioPorNitAsync();

        // NABN y CIVA (Grupo D) van aparte del libro: no entran a la
        // clasificación normal ni a los totales, se listan en su propia
        // sección al final del archivo.
        var facturasAparte = facturas.Where(EsTipoAparte).ToList();
        var facturasLibro = facturas.Where(f => !EsTipoAparte(f)).ToList();

        var filas = facturasLibro
            .Select(f => motorClasificacion.Clasificar(f, proveedoresRevisar))
            .OrderBy(f => f.Fecha)
            .ThenBy(f => f.Proveedor, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(f => f.Serie)
            .ToList();

        LibroComprasService.NumerarFilas(filas);

        // Recuerda selección por cliente + mes (ver privacidad en SeleccionFactura):
        // una decisión guardada por el usuario manda sobre lo que haya decidido
        // el catálogo "Proveedores a revisar" (que solo aplica cuando NO hay
        // decisión previa guardada para esa factura en ese periodo).
        if (filas.Count > 0)
        {
            var nitCliente = facturas[0].IdReceptor;
            var (mes, anio) = PeriodoUtil.ObtenerMesAnioPredominante(filas);
            var decisiones = await seleccionFacturaRepositorio.ObtenerDecisionesAsync(nitCliente, anio, mes, TipoLibro.Compras);

            if (decisiones.Count > 0)
            {
                foreach (var fila in filas)
                {
                    if (fila.OrigenFel is null) continue;

                    var identificador = IdentificadorFacturaUtil.CalcularHash(fila.OrigenFel.NumeroAutorizacion);
                    if (decisiones.TryGetValue(identificador, out var incluidaGuardada))
                    {
                        fila.Incluida = incluidaGuardada;
                    }
                }
            }
        }

        var filasAparte = facturasAparte
            .Select(f => motorClasificacion.Clasificar(f, proveedoresRevisar))
            .OrderBy(f => f.Fecha)
            .ToList();

        return new ResultadoProcesamiento { Filas = filas, FilasAparte = filasAparte, Hallazgos = hallazgos };
    }

    /// <summary>
    /// Guarda/actualiza el estado incluida/excluida de todas las filas del
    /// libro (no las de la sección aparte, que no tienen checkbox) para el
    /// cliente y periodo de este pool. Se llama al generar el libro y desde
    /// el botón "Guardar selección".
    /// </summary>
    public async Task GuardarSeleccionAsync(IReadOnlyList<DteFel> facturas, IReadOnlyList<FilaLibroCompras> filas)
    {
        if (facturas.Count == 0 || filas.Count == 0) return;

        var nitCliente = facturas[0].IdReceptor;
        var (mes, anio) = PeriodoUtil.ObtenerMesAnioPredominante(filas);

        var decisiones = filas
            .Where(f => f.OrigenFel is not null)
            .Select(f => (IdentificadorFacturaUtil.CalcularHash(f.OrigenFel!.NumeroAutorizacion), f.Incluida))
            .ToList();

        await seleccionFacturaRepositorio.GuardarLoteAsync(nitCliente, anio, mes, TipoLibro.Compras, decisiones);
    }

    private static bool EsTipoAparte(DteFel factura) =>
        ComportamientoDteCatalogo.Obtener(factura.TipoDte) == ComportamientoDte.Aparte;

    /// <summary>Igual que en la otra pestaña: todas las facturas del ZIP deben ser del mismo receptor.</summary>
    public static string? DetectarIdReceptor(IReadOnlyList<DteFel> facturas) =>
        facturas.FirstOrDefault()?.IdReceptor;
}
