using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ContaSuite.Wpf.ViewModels.Navegacion;

/// <summary>Un ítem hijo del sidebar (ej. "Clientes" dentro de "Catálogos").</summary>
public sealed partial class MenuHijoViewModel : ObservableObject
{
    public string Nombre { get; }
    public Type TipoViewModel { get; }
    public ICommand ComandoClic { get; }

    [ObservableProperty]
    private bool esActivo;

    public MenuHijoViewModel(string nombre, Type tipoViewModel, Action<MenuHijoViewModel> alHacerClic)
    {
        Nombre = nombre;
        TipoViewModel = tipoViewModel;
        ComandoClic = new RelayCommand(() => alHacerClic(this));
    }
}
