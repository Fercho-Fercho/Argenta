using System.Globalization;
using System.Text;
using ContaSuite.Core.Utilidades;
using CsvHelper;
using CsvHelper.Configuration;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>Resultado de leer el CSV de tipo de cambio del Banguat.</summary>
public sealed record ResultadoLecturaBanguat(
    IReadOnlyList<(DateTime Fecha, decimal Valor)> Valores,
    int FilasIgnoradas,
    int FilasLeidasTotal);

/// <summary>
/// Lee el CSV de tipo de cambio del Banguat. El archivo trae filas de título
/// y pie que no son datos, viene en UTF-8 con BOM, y puede usar coma o punto
/// y coma como separador según la exportación. Se toman solo las filas cuya
/// columna 0 es una fecha dd/MM/yyyy válida y cuya columna 1 es un número.
/// </summary>
public sealed class LectorTipoCambioBanguat
{
    /// <summary>Lee directamente desde un archivo en disco, respetando el BOM UTF-8.</summary>
    public ResultadoLecturaBanguat Leer(string rutaArchivo)
    {
        var delimitador = DetectarDelimitador(rutaArchivo);

        using var contenido = File.OpenRead(rutaArchivo);
        return Leer(contenido, delimitador);
    }

    public ResultadoLecturaBanguat Leer(Stream contenidoCsv, string delimitador = ",")
    {
        // UTF8Encoding con detección de BOM: si el archivo trae el BOM de UTF-8
        // (como lo exporta el Banguat) lo reconoce y lo descarta; si no lo trae,
        // igual lo interpreta como UTF-8.
        using var lector = new StreamReader(contenidoCsv, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);

        var configuracion = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            MissingFieldFound = null,
            BadDataFound = null,
            IgnoreBlankLines = true,
            Delimiter = delimitador,
        };

        using var csv = new CsvReader(lector, configuracion);
        var valores = new List<(DateTime, decimal)>();
        int filasLeidas = 0;
        int filasIgnoradas = 0;

        while (csv.Read())
        {
            filasLeidas++;

            var columna0 = ObtenerCampoSeguro(csv, 0);
            var columna1 = ObtenerCampoSeguro(csv, 1);

            if (columna0 is null || columna1 is null ||
                !ParseoUtil.TryParsearFechaDiaMesAnio(columna0, out var fecha) ||
                !ParseoUtil.TryParsearDecimal(columna1, out var valor))
            {
                filasIgnoradas++;
                continue;
            }

            valores.Add((fecha, valor));
        }

        return new ResultadoLecturaBanguat(valores, filasIgnoradas, filasLeidas);
    }

    private static string? ObtenerCampoSeguro(CsvReader csv, int indice)
    {
        try
        {
            return csv.GetField(indice);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// El Banguat normalmente exporta con coma, pero algunas configuraciones
    /// regionales de Excel lo guardan con punto y coma. Se detecta con la
    /// primera línea no vacía del archivo para no fallar por eso.
    /// </summary>
    private static string DetectarDelimitador(string rutaArchivo)
    {
        using var flujo = File.OpenRead(rutaArchivo);
        using var lector = new StreamReader(flujo, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);

        string? primeraLinea = null;
        while (!lector.EndOfStream)
        {
            var linea = lector.ReadLine();
            if (!string.IsNullOrWhiteSpace(linea))
            {
                primeraLinea = linea;
                break;
            }
        }

        primeraLinea ??= string.Empty;
        var comas = primeraLinea.Count(c => c == ',');
        var puntoYComa = primeraLinea.Count(c => c == ';');
        return puntoYComa > comas ? ";" : ",";
    }
}
