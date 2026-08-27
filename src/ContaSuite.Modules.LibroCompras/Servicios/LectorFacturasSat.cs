using System.Globalization;
using ContaSuite.Core.Utilidades;
using ContaSuite.Modules.LibroCompras.Modelos;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>
/// Lee el .xls binario de facturas recibidas que se descarga del portal de la
/// SAT. Las columnas se ubican por NOMBRE de encabezado (no por posición),
/// para tolerar que la SAT cambie el orden de las columnas.
/// </summary>
public sealed class LectorFacturasSat
{
    // Nombres tal como los usa el spec/portal de la SAT. Se buscan con
    // "empieza con" para tolerar sufijos como "(monto de este impuesto)".
    private static readonly string[] NombresColumnas =
    [
        "Fecha de emisión",
        "Número de Autorización",
        "Tipo de DTE (nombre)",
        "Serie",
        "Número del DTE",
        "Clasificación emisor",
        "Exportación",
        "Ubicación temporal",
        "NIT del emisor",
        "Nombre completo del emisor",
        "Código de establecimiento",
        "Nombre del establecimiento",
        "ID del receptor",
        "Nombre completo del receptor",
        "NIT del Certificador",
        "Nombre completo del Certificador",
        "Estado",
        "Moneda",
        "Gran Total (Moneda Original)",
        "IVA (monto de este impuesto)",
        "Marca de anulado",
        "Fecha de anulación",
        "Petróleo (monto de este impuesto)",
        "Turismo Hospedaje",
        "Turismo Pasajes",
        "Timbre de Prensa",
        "Bomberos",
        "Tasa Municipal",
        "Bebidas alcohólicas",
        "Tabaco",
        "Cemento",
        "Bebidas no Alcohólicas",
        "Tarifa Portuaria",
    ];

    public IReadOnlyList<FacturaSat> Leer(Stream contenidoXls)
    {
        var workbook = new HSSFWorkbook(contenidoXls);
        var hoja = workbook.GetSheetAt(0);
        var encabezado = hoja.GetRow(0)
            ?? throw new InvalidDataException("El archivo no tiene encabezado en la primera fila.");

        var ix = ObtenerIndices(encabezado);
        var facturas = new List<FacturaSat>();

        for (int f = 1; f <= hoja.LastRowNum; f++)
        {
            var fila = hoja.GetRow(f);
            if (fila is null) continue;

            var celdaClave = fila.GetCell(ix["Número del DTE"]);
            if (celdaClave is null || string.IsNullOrWhiteSpace(LeerTexto(celdaClave))) continue;

            facturas.Add(MapearFila(fila, ix, f + 1));
        }

        return facturas;
    }

    private static Dictionary<string, int> ObtenerIndices(IRow encabezado)
    {
        var indices = new Dictionary<string, int>();

        foreach (var nombre in NombresColumnas)
        {
            int? encontrado = null;
            for (int c = 0; c < encabezado.LastCellNum; c++)
            {
                var texto = LeerTexto(encabezado.GetCell(c)).Trim();
                if (texto.StartsWith(nombre, StringComparison.OrdinalIgnoreCase))
                {
                    encontrado = c;
                    break;
                }
            }

            if (encontrado is null)
            {
                throw new InvalidDataException(
                    $"El Excel de facturas de la SAT no tiene la columna \"{nombre}\". " +
                    "Verifique que sea el archivo correcto exportado del portal de la SAT.");
            }

            indices[nombre] = encontrado.Value;
        }

        return indices;
    }

    private static FacturaSat MapearFila(IRow fila, Dictionary<string, int> ix, int numeroFilaExcel)
    {
        string Texto(string col) => LeerTexto(fila.GetCell(ix[col])).Trim();
        decimal Monto(string col) => LeerDecimal(fila.GetCell(ix[col]));

        return new FacturaSat
        {
            NumeroFilaExcel = numeroFilaExcel,
            FechaEmision = ParseoUtil.ParsearFechaSat(Texto("Fecha de emisión")),
            TipoDte = Texto("Tipo de DTE (nombre)"),
            Serie = Texto("Serie"),
            NumeroDte = Texto("Número del DTE"),
            ClasificacionEmisor = Texto("Clasificación emisor"),
            Exportacion = Texto("Exportación"),
            UbicacionTemporal = Texto("Ubicación temporal"),
            NitEmisor = Texto("NIT del emisor"),
            NombreEmisor = Texto("Nombre completo del emisor"),
            IdReceptor = Texto("ID del receptor"),
            NombreReceptor = Texto("Nombre completo del receptor"),
            Estado = Texto("Estado"),
            Moneda = Texto("Moneda"),
            GranTotal = Monto("Gran Total (Moneda Original)"),
            Iva = Monto("IVA (monto de este impuesto)"),
            Petroleo = Monto("Petróleo (monto de este impuesto)"),
            TurismoHospedaje = Monto("Turismo Hospedaje"),
            TurismoPasajes = Monto("Turismo Pasajes"),
            TimbrePrensa = Monto("Timbre de Prensa"),
            Bomberos = Monto("Bomberos"),
            TasaMunicipal = Monto("Tasa Municipal"),
            BebidasAlcoholicas = Monto("Bebidas alcohólicas"),
            Tabaco = Monto("Tabaco"),
            Cemento = Monto("Cemento"),
            BebidasNoAlcoholicas = Monto("Bebidas no Alcohólicas"),
            TarifaPortuaria = Monto("Tarifa Portuaria"),
        };
    }

    /// <summary>
    /// Lee el valor de una celda sin usar <c>ICell.ToString()</c> ni el
    /// <c>DataFormatter</c> de NPOI, porque en este entorno ese camino
    /// depende de SkiaSharp (para medir fuentes) y falla si no está presente.
    /// </summary>
    private static string LeerTexto(ICell? celda)
    {
        if (celda is null) return string.Empty;

        return celda.CellType switch
        {
            CellType.String => celda.StringCellValue,
            CellType.Numeric => DateUtil.IsCellDateFormatted(celda)
                ? celda.DateCellValue?.ToString("O") ?? string.Empty
                : celda.NumericCellValue.ToString(CultureInfo.InvariantCulture),
            CellType.Boolean => celda.BooleanCellValue ? "Sí" : "No",
            CellType.Formula => LeerTextoFormula(celda),
            _ => string.Empty,
        };
    }

    private static string LeerTextoFormula(ICell celda)
    {
        try
        {
            return celda.CachedFormulaResultType == CellType.Numeric
                ? celda.NumericCellValue.ToString(CultureInfo.InvariantCulture)
                : celda.StringCellValue;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static decimal LeerDecimal(ICell? celda)
    {
        if (celda is null) return 0m;
        if (celda.CellType == CellType.Numeric) return (decimal)celda.NumericCellValue;
        return ParseoUtil.ParsearDecimal(LeerTexto(celda));
    }
}
