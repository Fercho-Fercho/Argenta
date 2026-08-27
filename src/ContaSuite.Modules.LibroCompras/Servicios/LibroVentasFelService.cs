using ContaSuite.Core.Validacion;
using ContaSuite.Data.Entidades;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>Resultado de clasificar el pool de UN establecimiento: sus filas, listas para previsualizar o exportar a un .xlsx propio.</summary>
public sealed class ResultadoLibroEstablecimiento
{
    public required Establecimiento Establecimiento { get; init; }
    public required IReadOnlyList<FilaLibroVentas> Filas { get; init; }

    /// <summary>Facturas tipo RANT, RECI, CIVA, NABN (y cualquier otro tipo "No afecta"): van aparte del libro, sin afectar sus totales.</summary>
    public required IReadOnlyList<FilaLibroVentas> FilasAparte { get; init; }

    /// <summary>Hallazgos propios de este establecimiento (p. ej. Profesional con un bien, o exportación sin marcar Exporta).</summary>
    public required IReadOnlyList<HallazgoValidacion> Hallazgos { get; init; }

    public bool TieneBloqueantes => Hallazgos.Any(h => h.Severidad == SeveridadValidacion.Bloqueante);
}

/// <summary>Resultado de validar y agrupar el lote completo de facturas, por establecimiento.</summary>
public sealed class ResultadoProcesamientoVentasMultiple
{
    /// <summary>
    /// Un libro por cada establecimiento REGISTRADO en el catálogo del
    /// cliente (todos, no solo los que aparecieron en el pool): si un
    /// establecimiento registrado no tiene facturas en este ZIP, igual
    /// aparece aquí con sus filas en 0 — ver <see cref="LibroVentasFelService"/>.
    /// </summary>
    public required IReadOnlyList<ResultadoLibroEstablecimiento> Libros { get; init; }

    /// <summary>Hallazgos que aplican a TODO el pool (no a un establecimiento en particular): tipo de cambio faltante, tipo de DTE desconocido, cuadre de ítems, o establecimientos del pool que no están registrados en el catálogo.</summary>
    public required IReadOnlyList<HallazgoValidacion> HallazgosGenerales { get; init; }

    public bool TieneBloqueantesGenerales => HallazgosGenerales.Any(h => h.Severidad == SeveridadValidacion.Bloqueante);
}

/// <summary>
/// Orquesta el flujo del módulo Libro de Ventas: valida el lote de facturas ya
/// leído del ZIP (reglas compartidas con compras + las propias de ventas) y
/// las AGRUPA POR ESTABLECIMIENTO (código de establecimiento del emisor),
/// porque cada establecimiento del mismo cliente puede tener su propio
/// Tipo/Exporta y por lo tanto su propia hoja en el libro de Ventas.
///
/// SIEMPRE se produce un <see cref="ResultadoLibroEstablecimiento"/> por
/// CADA establecimiento REGISTRADO del cliente (todos, no solo los que
/// aparecieron en el ZIP): si uno no tiene facturas en este pool en
/// particular, igual se incluye con sus filas en 0 (el Excel de salida
/// siempre trae una hoja por cada establecimiento del cliente, con su
/// encabezado, aunque no haya vendido nada ese mes) — ver
/// <see cref="GeneradorLibroVentasXlsx"/>.
///
/// Si el ZIP trae facturas de un código de establecimiento que NO está
/// registrado en el catálogo del cliente, esas facturas se quedan fuera (no
/// hay Tipo/Exporta con qué clasificarlas) y se deja un hallazgo de
/// advertencia listando cuáles códigos faltan, para que el contador los
/// registre y vuelva a subir el pool — sin perder el trabajo de los
/// establecimientos que sí están registrados.
///
/// A diferencia de compras: no hay catálogo de "Proveedores a revisar" ni
/// memoria de selección (nada se incluye/excluye a mano en ventas, así que no
/// hay nada que recordar entre subidas — ver sección 5.5 del pedido).
/// </summary>
public sealed class LibroVentasFelService(
    MotorClasificacionVentasFel motorClasificacion,
    MotorValidaciones<IReadOnlyList<DteFel>> motorValidaciones,
    MotorValidaciones<ContextoValidacionVentas> motorValidacionesVentas)
{
    public ResultadoProcesamientoVentasMultiple Procesar(
        IReadOnlyList<DteFel> facturas, Cliente cliente, IReadOnlyDictionary<string, EstadoDocumentoSat> estados)
    {
        var hallazgosGenerales = motorValidaciones.Evaluar(facturas).ToList();

        if (hallazgosGenerales.Any(h => h.Severidad == SeveridadValidacion.Bloqueante))
        {
            return new ResultadoProcesamientoVentasMultiple { Libros = [], HallazgosGenerales = hallazgosGenerales };
        }

        var facturasPorEstablecimiento = facturas
            .GroupBy(f => f.CodigoEstablecimiento.Trim())
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DteFel>)g.ToList());

        // Un libro por CADA establecimiento registrado del cliente, tenga o
        // no facturas en este pool — así el Excel siempre trae la misma
        // cantidad de hojas que establecimientos tiene el cliente.
        var libros = cliente.Establecimientos
            .OrderBy(e => e.Numero)
            .Select(establecimiento =>
            {
                var facturasEstablecimiento = facturasPorEstablecimiento.TryGetValue(establecimiento.Numero.ToString(), out var lista)
                    ? lista
                    : [];
                return ProcesarEstablecimiento(facturasEstablecimiento, cliente, establecimiento, estados);
            })
            .ToList();

        var numerosRegistrados = cliente.Establecimientos.Select(e => e.Numero.ToString()).ToHashSet();
        var establecimientosFaltantes = facturasPorEstablecimiento.Keys
            .Where(codigo => !numerosRegistrados.Contains(codigo))
            .Select(codigo => string.IsNullOrWhiteSpace(codigo) ? "(sin código)" : codigo)
            .OrderBy(codigo => codigo)
            .ToList();

        if (establecimientosFaltantes.Count > 0)
        {
            hallazgosGenerales.Add(new HallazgoValidacion(
                SeveridadValidacion.Advertencia,
                $"El/los establecimiento(s) número {string.Join(", ", establecimientosFaltantes)} no está(n) registrado(s) " +
                $"para el cliente \"{cliente.Nombre}\" (NIT {cliente.Nit}). Regístrelo(s) en el catálogo de Clientes para " +
                "incluir sus facturas; mientras tanto, se genera el libro con los establecimientos que sí están registrados."));
        }

        return new ResultadoProcesamientoVentasMultiple { Libros = libros, HallazgosGenerales = hallazgosGenerales };
    }

    private ResultadoLibroEstablecimiento ProcesarEstablecimiento(
        IReadOnlyList<DteFel> facturas, Cliente cliente, Establecimiento establecimiento, IReadOnlyDictionary<string, EstadoDocumentoSat> estados)
    {
        var hallazgos = motorValidacionesVentas.Evaluar(new ContextoValidacionVentas(facturas, cliente, establecimiento)).ToList();

        if (hallazgos.Any(h => h.Severidad == SeveridadValidacion.Bloqueante))
        {
            return new ResultadoLibroEstablecimiento { Establecimiento = establecimiento, Filas = [], FilasAparte = [], Hallazgos = hallazgos };
        }

        bool EsAnulada(DteFel f) => estados.TryGetValue(f.NumeroAutorizacion.Trim(), out var e) && e.Anulado;

        // Las anuladas se quedan SIEMPRE en el cuerpo del libro (en 0), nunca
        // se mandan aparte, sin importar su tipo de DTE — así el conteo de
        // "facturas emitidas y anuladas" del libro las sigue contando a todas.
        bool EsAparte(DteFel f) => !EsAnulada(f) && MotorClasificacionVentasFel.ComportamientoParaVentas(f.TipoDte) == ComportamientoDte.Aparte;

        var facturasAparte = facturas.Where(EsAparte).ToList();
        var facturasLibro = facturas.Where(f => !EsAparte(f)).ToList();

        var filas = facturasLibro
            .Select(f => motorClasificacion.Clasificar(f, establecimiento, estados))
            .OrderBy(f => f.Fecha)
            .ThenBy(f => f.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(f => f.Serie)
            .ToList();

        var filasAparte = facturasAparte
            .Select(f => motorClasificacion.Clasificar(f, establecimiento, estados))
            .OrderBy(f => f.Fecha)
            .ToList();

        return new ResultadoLibroEstablecimiento { Establecimiento = establecimiento, Filas = filas, FilasAparte = filasAparte, Hallazgos = hallazgos };
    }

    /// <summary>Sugerencia de cliente (vendedor) a partir del NIT del EMISOR de las facturas — no del receptor, como en compras.</summary>
    public static string? DetectarNitEmisor(IReadOnlyList<DteFel> facturas) =>
        facturas.FirstOrDefault()?.NitEmisor;
}
