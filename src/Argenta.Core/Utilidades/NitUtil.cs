using System.Text;

namespace Argenta.Core.Utilidades;

/// <summary>
/// Normalización de NIT (Número de Identificación Tributaria) de Guatemala.
/// Se usa tanto al guardar proveedores/clientes en el catálogo como al leer
/// el Excel de la SAT, para que el cruce por NIT funcione sin importar si
/// el NIT trae guiones o ceros a la izquierda.
/// </summary>
public static class NitUtil
{
    /// <summary>
    /// Deja solo letras y dígitos (quita guiones, espacios, puntos), pasa a
    /// mayúsculas y elimina los ceros a la izquierda del NIT resultante.
    /// </summary>
    public static string Normalizar(string? nit)
    {
        if (string.IsNullOrWhiteSpace(nit)) return string.Empty;

        var sb = new StringBuilder(nit.Length);
        foreach (var c in nit)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        }

        var limpio = sb.ToString().TrimStart('0');
        return limpio;
    }
}
