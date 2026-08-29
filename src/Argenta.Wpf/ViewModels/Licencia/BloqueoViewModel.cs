using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Wpf.Servicios.Licencia;

namespace Argenta.Wpf.ViewModels.Licencia;

/// <summary>
/// Pantalla de bloqueo: se muestra en vez del menú normal cuando esta
/// computadora no está autorizada (o quedó bloqueada sin período de gracia
/// disponible). No da acceso a ningún módulo — solo el código de la máquina
/// para enviárselo al proveedor, y un botón para reintentar la validación.
/// </summary>
public partial class BloqueoViewModel : ObservableObject
{
    private readonly Func<Task> _validarAhora;

    public string CodigoMaquina { get; }

    [ObservableProperty]
    private string mensaje;

    [ObservableProperty]
    private bool copiado;

    [ObservableProperty]
    private bool validando;

    public BloqueoViewModel(ResultadoLicencia resultado, Func<Task> validarAhora)
    {
        CodigoMaquina = resultado.CodigoMaquina;
        mensaje = resultado.Mensaje;
        _validarAhora = validarAhora;
    }

    public void Actualizar(ResultadoLicencia resultado) => Mensaje = resultado.Mensaje;

    [RelayCommand]
    private void Copiar()
    {
        Clipboard.SetText(CodigoMaquina);
        Copiado = true;
    }

    [RelayCommand]
    private async Task ValidarAhoraAsync()
    {
        Validando = true;
        try
        {
            await _validarAhora();
        }
        finally
        {
            Validando = false;
        }
    }
}
