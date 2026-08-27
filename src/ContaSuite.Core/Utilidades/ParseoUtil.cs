using System.Globalization;

namespace ContaSuite.Core.Utilidades;

/// <summary>
/// Utilidades de parseo tolerantes a los formatos que trae el Excel de la SAT
/// y el CSV del Banguat (fechas ISO con o sin offset, fechas dd/MM/yyyy,
/// números en cultura invariante, valores "Sí"/"No").
/// </summary>
public static class ParseoUtil
{
    private static readonly string[] FormatosFechaSat =
    {
        "yyyy-MM-ddTHH:mm:ss.fffzzz",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd",
    };

    /// <summary>
    /// Extrae solo la fecha (sin hora) de la "Fecha de emisión" del Excel de la SAT,
    /// que puede venir como "2026-07-31T20:20:23" o con offset "2026-07-31T05:21:52.202-06:00".
    /// </summary>
    public static DateTime ParsearFechaSat(string valor)
    {
        valor = valor.Trim();

        if (DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out var conOffset))
        {
            return conOffset.Date;
        }

        if (DateTime.TryParseExact(valor, FormatosFechaSat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exacta))
        {
            return exacta.Date;
        }

        if (DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out var general))
        {
            return general.Date;
        }

        throw new FormatException($"No se pudo interpretar la fecha '{valor}' del Excel de la SAT.");
    }

    /// <summary>Parsea una fecha dd/MM/yyyy como la del CSV del Banguat.</summary>
    public static bool TryParsearFechaDiaMesAnio(string valor, out DateTime fecha) =>
        DateTime.TryParseExact(valor.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);

    public static bool TryParsearDecimal(string? valor, out decimal resultado) =>
        decimal.TryParse((valor ?? string.Empty).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out resultado);

    public static decimal ParsearDecimal(string? valor) =>
        TryParsearDecimal(valor, out var resultado) ? resultado : 0m;

    public static bool EsAfirmativo(string? valor) =>
        string.Equals(valor?.Trim(), "Sí", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(valor?.Trim(), "Si", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] NombresMeses =
    {
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
    };

    /// <summary>Nombre del mes en español (1 = enero ... 12 = diciembre).</summary>
    public static string NombreMesEspanol(int mes) => NombresMeses[mes - 1];

    private static readonly string[] AbreviaturasMeses =
    {
        "ENE", "FEB", "MAR", "ABR", "MAY", "JUN",
        "JUL", "AGO", "SEP", "OCT", "NOV", "DIC",
    };

    /// <summary>Abreviatura de 3 letras del mes en español, en mayúsculas (1 = ENE ... 12 = DIC).</summary>
    public static string AbreviaturaMesEspanol(int mes) => AbreviaturasMeses[mes - 1];
}
