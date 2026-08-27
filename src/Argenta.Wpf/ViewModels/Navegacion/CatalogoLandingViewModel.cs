using System.Collections.ObjectModel;

namespace Argenta.Wpf.ViewModels.Navegacion;

/// <summary>
/// Pantalla de bienvenida de un menú padre (Catálogos, Operaciones): muestra
/// una tarjeta por cada hijo. Es genérica y la reutiliza cualquier padre del
/// sidebar, así que un módulo nuevo no necesita crear su propia landing.
/// </summary>
public sealed class CatalogoLandingViewModel
{
    public string Titulo { get; }
    public ObservableCollection<TarjetaLandingViewModel> Tarjetas { get; }

    public CatalogoLandingViewModel(string titulo, IEnumerable<MenuHijoViewModel> hijos, string descripcionPrefijo)
    {
        Titulo = titulo;
        Tarjetas = new ObservableCollection<TarjetaLandingViewModel>(
            hijos.Select(h => new TarjetaLandingViewModel(h.Nombre, $"{descripcionPrefijo} {h.Nombre}", h.ComandoClic)));
    }
}
