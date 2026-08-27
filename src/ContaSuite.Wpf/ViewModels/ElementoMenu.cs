using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ContaSuite.Wpf.ViewModels;

/// <summary>Una entrada de la navegación del shell (Catálogos, Operaciones o Ayuda).</summary>
public sealed class ElementoMenu
{
    public string Nombre { get; }
    public ICommand Comando { get; }

    public ElementoMenu(string nombre, Action accion)
    {
        Nombre = nombre;
        Comando = new RelayCommand(accion);
    }

    public ElementoMenu(string nombre, Func<Task> accionAsync)
    {
        Nombre = nombre;
        Comando = new AsyncRelayCommand(accionAsync);
    }
}
