using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Argenta.Wpf.ViewModels.Navegacion;

/// <summary>Un ítem padre del sidebar (ej. "Catálogos"), con sus hijos expandibles/colapsables.</summary>
public sealed partial class MenuPadreViewModel : ObservableObject
{
    public string Nombre { get; }
    public ObservableCollection<MenuHijoViewModel> Hijos { get; }
    public ICommand ComandoClic { get; }

    [ObservableProperty]
    private bool estaExpandido;

    public MenuPadreViewModel(string nombre, IEnumerable<MenuHijoViewModel> hijos, Action<MenuPadreViewModel> alHacerClic)
    {
        Nombre = nombre;
        Hijos = new ObservableCollection<MenuHijoViewModel>(hijos);
        ComandoClic = new RelayCommand(() => alHacerClic(this));
    }
}
