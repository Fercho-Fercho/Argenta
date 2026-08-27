using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContaSuite.Core.Modulos;
using ContaSuite.Wpf.Servicios;
using ContaSuite.Wpf.ViewModels.Catalogos;
using ContaSuite.Wpf.ViewModels.Configuraciones;
using ContaSuite.Wpf.ViewModels.Navegacion;
using Microsoft.Extensions.DependencyInjection;

namespace ContaSuite.Wpf.ViewModels;

/// <summary>
/// ViewModel del shell: arma el menú de navegación en árbol (padres
/// expandibles/colapsables con sus hijos). Catálogos son fijos y compartidos
/// por todos los módulos; Operaciones se arma dinámicamente a partir de los
/// <see cref="IModuloContable"/> registrados en el contenedor de DI, así que
/// un módulo nuevo aparece sin tocar esta clase. Resuelve la vista actual
/// mediante inyección de dependencias.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceProvider _servicios;
    private readonly IActualizacionService _actualizaciones;
    private readonly ITemaService _temaService;
    private readonly IPreferenciasService _preferenciasService;

    public ObservableCollection<MenuPadreViewModel> Menu { get; } = [];
    public ObservableCollection<ElementoMenu> ElementosAyuda { get; } = [];

    [ObservableProperty]
    private object? vistaActual;

    [ObservableProperty]
    private string? mensajeGlobal;

    /// <summary>Sidebar visible/oculto (botón ☰ del topbar), para ganar espacio en Operaciones/Catálogos.</summary>
    [ObservableProperty]
    private bool menuExpandido = true;

    /// <summary>Tema activo, para el botón ☀/🌙 del topbar. Se mantiene en sincronía vía ITemaService.TemaCambiado.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconoTema))]
    [NotifyPropertyChangedFor(nameof(ToolTipTema))]
    private TemaApp temaActual;

    /// <summary>☀ cuando el tema activo es Claro (al presionarlo se pasa a Oscuro); 🌙 cuando es Oscuro (al presionarlo se vuelve a Claro).</summary>
    public string IconoTema => TemaActual == TemaApp.Oscuro ? "🌙" : "☀";

    public string ToolTipTema => TemaActual == TemaApp.Oscuro ? "Cambiar a modo claro" : "Cambiar a modo oscuro";

    public ShellViewModel(
        IServiceProvider servicios, IEnumerable<IModuloContable> modulos, IActualizacionService actualizaciones,
        ITemaService temaService, IPreferenciasService preferenciasService)
    {
        _servicios = servicios;
        _actualizaciones = actualizaciones;
        _temaService = temaService;
        _preferenciasService = preferenciasService;

        TemaActual = _temaService.TemaActual;
        _temaService.TemaCambiado += tema => TemaActual = tema;

        var hijosCatalogos = new List<MenuHijoViewModel>
        {
            new("Clientes", typeof(ClientesViewModel), NavegarHijo),
            // "Proveedores" oculto temporalmente: no funciona bien todavía.
            // ProveedoresViewModel/View siguen intactos, solo se quitó del menú.
            new("Proveedores a revisar", typeof(ProveedoresRevisarViewModel), NavegarHijo),
            new("Tipo de Cambio", typeof(TiposCambioViewModel), NavegarHijo),
        };
        var padreCatalogos = new MenuPadreViewModel("Catálogos", hijosCatalogos, ClicPadre);
        Menu.Add(padreCatalogos);

        var hijosOperaciones = new List<MenuHijoViewModel>();
        foreach (var modulo in modulos.OrderBy(m => m.Orden))
        {
            foreach (var elemento in modulo.ObtenerElementosMenu())
            {
                hijosOperaciones.Add(new MenuHijoViewModel(elemento.Nombre, elemento.TipoViewModel, NavegarHijo));
            }
        }
        var padreOperaciones = new MenuPadreViewModel("Operaciones", hijosOperaciones, ClicPadre);
        Menu.Add(padreOperaciones);

        var hijosConfiguraciones = new List<MenuHijoViewModel>
        {
            new("Datos", typeof(DatosViewModel), NavegarHijo),
        };
        var padreConfiguraciones = new MenuPadreViewModel("Configuraciones", hijosConfiguraciones, ClicPadre);
        Menu.Add(padreConfiguraciones);

        ElementosAyuda.Add(new ElementoMenu("Buscar actualizaciones", BuscarActualizacionesAsync));

        // Estado inicial: Catálogos expandido, mostrando Clientes.
        padreCatalogos.EstaExpandido = true;
        NavegarHijo(hijosCatalogos[0]);
    }

    [RelayCommand]
    private void AlternarMenu() => MenuExpandido = !MenuExpandido;

    [RelayCommand]
    private void AlternarTema()
    {
        var nuevoTema = TemaActual == TemaApp.Claro ? TemaApp.Oscuro : TemaApp.Claro;
        _temaService.AplicarTema(nuevoTema);

        var preferencias = _preferenciasService.Cargar();
        preferencias.Tema = nuevoTema;
        _preferenciasService.Guardar(preferencias);
    }

    private void ClicPadre(MenuPadreViewModel padre)
    {
        padre.EstaExpandido = !padre.EstaExpandido;
        VistaActual = new CatalogoLandingViewModel(padre.Nombre, padre.Hijos, "Ir a");
        LimpiarHijosActivos();
    }

    private void NavegarHijo(MenuHijoViewModel hijo)
    {
        VistaActual = _servicios.GetRequiredService(hijo.TipoViewModel);
        LimpiarHijosActivos();
        hijo.EsActivo = true;
    }

    private void LimpiarHijosActivos()
    {
        foreach (var padre in Menu)
        {
            foreach (var hijo in padre.Hijos)
            {
                hijo.EsActivo = false;
            }
        }
    }

    private async Task BuscarActualizacionesAsync()
    {
        MensajeGlobal = "Buscando actualizaciones...";
        MensajeGlobal = await _actualizaciones.BuscarYAplicarAsync();
    }
}
