using System.Security.Cryptography;
using System.Text;

namespace Argenta.Core.Utilidades;

/// <summary>
/// Calcula el identificador opaco que se guarda para "recordar" la
/// inclusión/exclusión de una factura entre subidas (ver <c>SeleccionFactura</c>).
/// Es un hash SHA-256 del Número de Autorización del DTE (su UUID único e
/// inmutable): nunca se guarda el UUID en texto plano ni ningún otro dato de
/// la factura, solo esta huella.
/// </summary>
public static class IdentificadorFacturaUtil
{
    public static string CalcularHash(string numeroAutorizacion)
    {
        var normalizado = (numeroAutorizacion ?? string.Empty).Trim().ToUpperInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalizado);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
