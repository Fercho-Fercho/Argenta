using System.Windows.Input;

namespace ContaSuite.Wpf.ViewModels.Navegacion;

/// <summary>Una tarjeta clicable en la pantalla de bienvenida de un menú padre.</summary>
public sealed class TarjetaLandingViewModel(string nombre, string descripcion, ICommand comando)
{
    public string Nombre { get; } = nombre;
    public string Descripcion { get; } = descripcion;
    public ICommand Comando { get; } = comando;
}
