using Microsoft.Extensions.Configuration;

namespace Argenta.Wpf.Servicios.Licencia;

public enum EstadoLicencia
{
    Autorizada,
    EnGracia,
    Bloqueada,
}

public sealed record ResultadoLicencia(
    EstadoLicencia Estado,
    string CodigoMaquina,
    DateTime? UltimaValidacionExitosaUtc,
    string Mensaje);

public interface IValidadorLicenciaService
{
    Task<ResultadoLicencia> ValidarAsync(CancellationToken ct = default);
}

/// <summary>
/// Orquesta la validación de licencia: código de esta máquina + lista remota
/// de autorizadas + período de gracia offline. Corre en el arranque de la
/// app (antes de mostrar los módulos) y cada vez que el usuario presiona
/// "Validar ahora". Ver README, sección "Licencia por computadora
/// autorizada", para la explicación completa del mecanismo.
/// </summary>
public sealed class ValidadorLicenciaService(
    IFingerprintService fingerprint,
    IAutorizacionService autorizacion,
    ICacheLicenciaService cache,
    IConfiguration configuracion) : IValidadorLicenciaService
{
    private const int DiasGraciaPorDefecto = 7;

    public async Task<ResultadoLicencia> ValidarAsync(CancellationToken ct = default)
    {
        var codigo = fingerprint.ObtenerCodigoMaquina();
        var resultadoRemoto = await autorizacion.ValidarAsync(codigo, ct).ConfigureAwait(false);

        switch (resultadoRemoto)
        {
            case ResultadoValidacionRemota.Autorizada:
            {
                var ahora = DateTime.UtcNow;
                cache.GuardarValidacionExitosa(codigo, ahora);
                return new ResultadoLicencia(EstadoLicencia.Autorizada, codigo, ahora, "Computadora autorizada.");
            }

            case ResultadoValidacionRemota.NoAutorizada:
                // Explícitamente no autorizada (o inactiva): bloquea siempre,
                // sin importar si había gracia antes — así una revocación
                // (activa: false) surte efecto de inmediato en cuanto haya
                // internet, en vez de esperar a que expire la gracia.
                return new ResultadoLicencia(
                    EstadoLicencia.Bloqueada, codigo, cache.ObtenerUltimaValidacionExitosa(codigo),
                    "Esta computadora no está autorizada para usar Argenta.");

            default: // ErrorRed
            {
                var ultima = cache.ObtenerUltimaValidacionExitosa(codigo);
                var diasGracia = ObtenerDiasGracia();

                if (ultima is not null && DateTime.UtcNow - ultima.Value <= TimeSpan.FromDays(diasGracia))
                {
                    var vence = ultima.Value.AddDays(diasGracia).ToLocalTime();
                    return new ResultadoLicencia(
                        EstadoLicencia.EnGracia, codigo, ultima,
                        $"Sin conexión para validar la licencia. Modo de gracia activo hasta el {vence:dd/MM/yyyy HH:mm}.");
                }

                return new ResultadoLicencia(
                    EstadoLicencia.Bloqueada, codigo, ultima,
                    "No se pudo validar la licencia (sin conexión) y no hay período de gracia disponible. Conéctese a internet e intente de nuevo.");
            }
        }
    }

    private int ObtenerDiasGracia()
    {
        var texto = configuracion["Licencia:DiasGracia"];
        return int.TryParse(texto, out var dias) && dias > 0 ? dias : DiasGraciaPorDefecto;
    }
}
