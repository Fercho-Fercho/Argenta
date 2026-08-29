using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Argenta.Wpf.Servicios.Licencia;

public interface IFingerprintService
{
    /// <summary>Código único y estable de esta computadora (el mismo en cada arranque).</summary>
    string ObtenerCodigoMaquina();
}

/// <summary>
/// Genera el código único de esta computadora combinando el MachineGuid de
/// Windows (se genera una sola vez al instalar Windows, no cambia entre
/// reinicios) con el número de serie de la placa base cuando está
/// disponible, y aplicando SHA-256. El resultado no es reversible: no se
/// puede recuperar el hardware real a partir del código.
/// </summary>
public sealed class FingerprintService : IFingerprintService
{
    private string? _codigoCacheado;

    public string ObtenerCodigoMaquina()
    {
        if (_codigoCacheado is not null) return _codigoCacheado;

        var combinado = $"{ObtenerMachineGuid()}|{ObtenerNumeroSerieBaseCard()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combinado));
        _codigoCacheado = Convert.ToHexString(hash).ToLowerInvariant();
        return _codigoCacheado;
    }

    private static string ObtenerMachineGuid()
    {
        try
        {
            using var claveRaiz = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var clave = claveRaiz.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var valor = clave?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrWhiteSpace(valor)) return valor;
        }
        catch
        {
            // Sigue con el respaldo de abajo si el registro no se puede leer.
        }

        // Respaldo si no se pudo leer el registro (no debería pasar en un
        // Windows normal): menos estable que el MachineGuid, pero evita que
        // la app truene por no poder calcular ningún código.
        return $"{Environment.MachineName}|{Environment.UserDomainName}|{Environment.ProcessorCount}";
    }

    private static string ObtenerNumeroSerieBaseCard()
    {
        try
        {
            using var buscador = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (ManagementBaseObject objeto in buscador.Get())
            {
                var serie = (objeto["SerialNumber"] as string)?.Trim();
                if (!string.IsNullOrWhiteSpace(serie) && !EsSerieGenerica(serie))
                {
                    return serie;
                }
            }
        }
        catch
        {
            // WMI puede fallar por permisos o virtualización: el MachineGuid solo ya es suficiente.
        }

        return string.Empty;
    }

    private static bool EsSerieGenerica(string serie)
    {
        string[] valoresGenericos =
        [
            "to be filled by o.e.m.", "default string", "system serial number",
            "none", "not specified", "0123456789", "n/a",
        ];
        return valoresGenericos.Contains(serie.ToLowerInvariant());
    }
}
