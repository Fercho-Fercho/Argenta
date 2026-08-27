using System.Globalization;
using Argenta.Modules.LibroCompras.Modelos;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Lee el .xls "Consulta de documentos" del portal de la SAT (el mismo
/// formato que ya lee <see cref="LectorFacturasSat"/> para compras), pero
/// solo para sacar el estado de anulación de cada documento por su Número de
/// Autorización — es el único dato que el ZIP de XML no trae. Las columnas se
/// ubican por NOMBRE de encabezado, igual que el otro lector.
/// </summary>
public sealed class LectorEstadoDocumentosSat
{
    private const string ColNumeroAutorizacion = "Número de Autorización";
    private const string ColMarcaAnulado = "Marca de anulado";
    private const string ColEstado = "Estado";

    public IReadOnlyDictionary<string, EstadoDocumentoSat> Leer(Stream contenidoXls)
    {
        var workbook = new HSSFWorkbook(contenidoXls);
        var hoja = workbook.GetSheetAt(0);
        var encabezado = hoja.GetRow(0)
            ?? throw new InvalidDataException("El archivo no tiene encabezado en la primera fila.");

        var ixAutorizacion = BuscarColumna(encabezado, ColNumeroAutorizacion);
        var ixMarca = BuscarColumna(encabezado, ColMarcaAnulado);
        var ixEstado = BuscarColumna(encabezado, ColEstado);

        var resultado = new Dictionary<string, EstadoDocumentoSat>(StringComparer.OrdinalIgnoreCase);

        for (int f = 1; f <= hoja.LastRowNum; f++)
        {
            var fila = hoja.GetRow(f);
            if (fila is null) continue;

            var numeroAutorizacion = LeerTexto(fila.GetCell(ixAutorizacion)).Trim();
            if (numeroAutorizacion.Length == 0) continue;

            var marca = LeerTexto(fila.GetCell(ixMarca)).Trim();
            var estado = LeerTexto(fila.GetCell(ixEstado)).Trim();
            var anulado = string.Equals(marca, "Si", StringComparison.OrdinalIgnoreCase)
                || string.Equals(estado, "Anulado", StringComparison.OrdinalIgnoreCase);

            resultado[numeroAutorizacion] = new EstadoDocumentoSat(numeroAutorizacion, anulado, estado);
        }

        return resultado;
    }

    private static int BuscarColumna(IRow encabezado, string nombre)
    {
        for (int c = 0; c < encabezado.LastCellNum; c++)
        {
            var texto = LeerTexto(encabezado.GetCell(c)).Trim();
            if (texto.StartsWith(nombre, StringComparison.OrdinalIgnoreCase)) return c;
        }

        throw new InvalidDataException(
            $"El Excel de consulta de documentos de la SAT no tiene la columna \"{nombre}\". " +
            "Verifique que sea el archivo correcto exportado del portal de la SAT.");
    }

    /// <summary>Igual que en <see cref="LectorFacturasSat"/>: sin ToString()/DataFormatter de NPOI (dependen de SkiaSharp).</summary>
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
}
