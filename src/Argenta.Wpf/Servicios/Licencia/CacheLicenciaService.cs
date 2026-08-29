using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Argenta.Wpf.Servicios.Licencia;

public interface ICacheLicenciaService
{
    DateTime? ObtenerUltimaValidacionExitosa(string codigoMaquina);
    void GuardarValidacionExitosa(string codigoMaquina, DateTime momentoUtc);
}

internal sealed class CacheLicenciaContenido
{
    public string Codigo { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}

/// <summary>
/// Guarda localmente la fecha de la última validación exitosa contra la lista
/// remota, para el período de gracia sin internet (ver
/// <see cref="ValidadorLicenciaService"/>). Se cifra con DPAPI, ligado al
/// usuario de Windows de esta máquina: no se puede editar el archivo a mano
/// para "desbloquear" la app (el descifrado falla y se ignora como si no
/// hubiera caché), ni copiarlo a otra computadora u otro usuario para que
/// siga sirviendo. La fuente de verdad siempre es el JSON remoto — esto es
/// solo una caché para no dejar sin servicio por una caída temporal de
/// internet.
/// </summary>
public sealed class CacheLicenciaService : ICacheLicenciaService
{
    private static string RutaArchivo => Path.Combine(RutasApp.CarpetaDatos, "licencia.cache");

    public DateTime? ObtenerUltimaValidacionExitosa(string codigoMaquina)
    {
        try
        {
            if (!File.Exists(RutaArchivo)) return null;

            var cifrado = File.ReadAllBytes(RutaArchivo);
            var claro = ProtectedData.Unprotect(cifrado, null, DataProtectionScope.CurrentUser);
            var contenido = JsonSerializer.Deserialize<CacheLicenciaContenido>(claro);

            if (contenido is null || !string.Equals(contenido.Codigo, codigoMaquina, StringComparison.OrdinalIgnoreCase))
            {
                return null; // Caché de otra máquina/código: no cuenta para la gracia de esta.
            }

            return DateTime.SpecifyKind(contenido.FechaUtc, DateTimeKind.Utc);
        }
        catch
        {
            // Archivo corrupto, editado a mano, o cifrado con otra cuenta de
            // Windows: se trata igual que "no hay caché".
            return null;
        }
    }

    public void GuardarValidacionExitosa(string codigoMaquina, DateTime momentoUtc)
    {
        try
        {
            Directory.CreateDirectory(RutasApp.CarpetaDatos);
            var contenido = new CacheLicenciaContenido { Codigo = codigoMaquina, FechaUtc = momentoUtc };
            var claro = JsonSerializer.SerializeToUtf8Bytes(contenido);
            var cifrado = ProtectedData.Protect(claro, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(RutaArchivo, cifrado);
        }
        catch
        {
            // Si no se pudo guardar la caché, en el peor caso el próximo
            // arranque sin internet no tendrá gracia disponible; no debe
            // tronar la app por esto.
        }
    }
}
