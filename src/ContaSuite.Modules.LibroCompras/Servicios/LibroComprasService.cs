using ContaSuite.Core.Validacion;
using ContaSuite.Data.Repositorios;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>Resultado de validar y clasificar el lote de facturas antes de generar el .xlsx.</summary>
public sealed class ResultadoProcesamiento
{
    public required IReadOnlyList<FilaLibroCompras> Filas { get; init; }

    /// <summary>Facturas tipo CIVA y NABN: van aparte del libro, sin afectar sus totales.</summary>
    public required IReadOnlyList<FilaLibroCompras> FilasAparte { get; init; }

    public required IReadOnlyList<HallazgoValidacion> Hallazgos { get; init; }

    public bool TieneBloqueantes => Hallazgos.Any(h => h.Severidad == SeveridadValidacion.Bloqueante);
    public IEnumerable<HallazgoValidacion> Advertencias => Hallazgos.Where(h => h.Severidad == SeveridadValidacion.Advertencia);
}

/// <summary>
/// Orquesta el flujo completo del módulo: valida el lote de facturas ya leído
/// del Excel, clasifica cada una y arma las filas del libro (numeradas y
/// ordenadas), listas para previsualizar o para exportar a .xlsx.
/// </summary>
public sealed class LibroComprasService(
    MotorClasificacion motorClasificacion,
    MotorValidaciones<IReadOnlyList<FacturaSat>> motorValidaciones,
    IProveedorRepositorio proveedorRepositorio)
{
    public async Task<ResultadoProcesamiento> ProcesarAsync(IReadOnlyList<FacturaSat> facturas)
    {
        var hallazgos = motorValidaciones.Evaluar(facturas);

        if (hallazgos.Any(h => h.Severidad == SeveridadValidacion.Bloqueante))
        {
            return new ResultadoProcesamiento { Filas = [], FilasAparte = [], Hallazgos = hallazgos };
        }

        var proveedores = await proveedorRepositorio.ObtenerDiccionarioPorNitAsync();

        // CIVA y NABN van aparte del libro (misma regla para los dos): no
        // entran a la clasificación normal ni a los totales, se listan en su
        // propia sección al final del archivo.
        var facturasAparte = facturas.Where(EsTipoAparte).ToList();
        var facturasLibro = facturas.Where(f => !EsTipoAparte(f)).ToList();

        var filas = facturasLibro
            .Select(f => motorClasificacion.Clasificar(f, proveedores))
            .OrderBy(f => f.Fecha)
            .ThenBy(f => f.Proveedor, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(f => f.Serie)
            .ToList();

        NumerarFilas(filas);

        var filasAparte = facturasAparte
            .Select(f => motorClasificacion.Clasificar(f, proveedores))
            .OrderBy(f => f.Fecha)
            .ToList();

        return new ResultadoProcesamiento { Filas = filas, FilasAparte = filasAparte, Hallazgos = hallazgos };
    }

    private static bool EsTipoAparte(FacturaSat factura) =>
        EsTipo(factura.TipoDte, "CIVA") || EsTipo(factura.TipoDte, "NABN");

    /// <summary>
    /// Sugerencia de cliente a partir del "ID del receptor" de la primera
    /// factura del Excel (fila 2): todas las facturas del libro son del mismo
    /// cliente, así que basta con leerlo del primer registro.
    /// </summary>
    public static string? DetectarIdReceptor(IReadOnlyList<FacturaSat> facturas) =>
        facturas.FirstOrDefault()?.IdReceptor;

    /// <summary>
    /// Asigna el correlativo 1, 2, 3... a las filas dadas (RECI/FPEQ no llevan
    /// número). Se usa tanto al procesar el lote completo para la vista previa
    /// como, de nuevo, sobre el subconjunto de filas incluidas justo antes de
    /// generar el archivo (Función 2: excluir facturas).
    /// </summary>
    public static void NumerarFilas(IReadOnlyList<FilaLibroCompras> filas)
    {
        int numero = 1;
        foreach (var fila in filas)
        {
            // RECI y FPEQ no llevan correlativo en el libro (así lo muestra el modelo oficial).
            if (EsTipo(fila.Docto, "RECI") || EsTipo(fila.Docto, "FPEQ")) continue;
            fila.Numero = numero++;
        }
    }

    private static bool EsTipo(string tipoDte, string esperado) =>
        string.Equals(tipoDte?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
