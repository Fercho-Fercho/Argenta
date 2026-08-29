namespace Argenta.Wpf.Servicios.Licencia;

/// <summary>
/// Mantiene el último <see cref="ResultadoLicencia"/> calculado, compartido
/// entre <c>ShellViewModel</c> y las pantallas de licencia/bloqueo. Se
/// actualiza en el arranque y cada vez que corre "Validar ahora"; el evento
/// permite que el shell reaccione (por ejemplo, desbloquear la app sin
/// reiniciar si la validación ya sale autorizada).
/// </summary>
public interface ILicenciaEstadoActual
{
    ResultadoLicencia? Actual { get; set; }
    event Action<ResultadoLicencia>? Cambiado;
}

public sealed class LicenciaEstadoActual : ILicenciaEstadoActual
{
    private ResultadoLicencia? _actual;

    public ResultadoLicencia? Actual
    {
        get => _actual;
        set
        {
            _actual = value;
            if (value is not null) Cambiado?.Invoke(value);
        }
    }

    public event Action<ResultadoLicencia>? Cambiado;
}
