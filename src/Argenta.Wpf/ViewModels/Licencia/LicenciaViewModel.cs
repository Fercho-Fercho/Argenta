using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Wpf.Servicios.Licencia;

namespace Argenta.Wpf.ViewModels.Licencia;

/// <summary>Pantalla "Ayuda → Licencia / Acerca de": estado de activación de esta computadora.</summary>
public partial class LicenciaViewModel : ObservableObject
{
    private readonly Func<Task> _validarAhora;

    public string CodigoMaquina { get; }

    [ObservableProperty]
    private string estadoTexto = string.Empty;

    [ObservableProperty]
    private string ultimaValidacionTexto = string.Empty;

    [ObservableProperty]
    private bool copiado;

    [ObservableProperty]
    private bool validando;

    public LicenciaViewModel(ResultadoLicencia resultado, Func<Task> validarAhora)
    {
        CodigoMaquina = resultado.CodigoMaquina;
        _validarAhora = validarAhora;
        Actualizar(resultado);
    }

    public void Actualizar(ResultadoLicencia resultado)
    {
        EstadoTexto = resultado.Estado switch
        {
            EstadoLicencia.Autorizada => "Autorizada",
            EstadoLicencia.EnGracia => "En período de gracia (sin conexión con el servidor de licencias)",
            EstadoLicencia.Bloqueada => "Bloqueada",
            _ => "Desconocido",
        };
        UltimaValidacionTexto = resultado.UltimaValidacionExitosaUtc is { } fecha
            ? fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : "Nunca";
    }

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
