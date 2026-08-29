using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Core.Modulos;
using Argenta.Wpf.Servicios;
using Argenta.Wpf.Servicios.Licencia;
using Argenta.Wpf.ViewModels.Catalogos;
using Argenta.Wpf.ViewModels.Configuraciones;
using Argenta.Wpf.ViewModels.Licencia;
using Argenta.Wpf.ViewModels.Navegacion;
using Microsoft.Extensions.DependencyInjection;

namespace Argenta.Wpf.ViewModels;

/// <summary>
/// ViewModel del shell: arma el menú de navegación en árbol (padres
/// expandibles/colapsables con sus hijos). Catálogos son fijos y compartidos
/// por todos los módulos; Operaciones se arma dinámicamente a partir de los
/// <see cref="IModuloContable"/> registrados en el contenedor de DI, así que
/// un módulo nuevo aparece sin tocar esta clase. Resuelve la vista actual
/// mediante inyección de dependencias.
///
/// Si la computadora no está autorizada (ver <see cref="ILicenciaEstadoActual"/>),
/// el menú se arma vacío y <see cref="VistaActual"/> muestra la pantalla de
/// bloqueo en su lugar — ningún módulo queda accesible.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceProvider _servicios;
    private readonly IEnumerable<IModuloContable> _modulos;
    private readonly IActualizacionService _actualizaciones;
    private readonly ITemaService _temaService;
    private readonly IPreferenciasService _preferenciasService;
    private readonly ILicenciaEstadoActual _licenciaEstado;
    private readonly IValidadorLicenciaService _validadorLicencia;

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

    /// <summary>Estado de licencia vigente (Autorizada/EnGracia/Bloqueada), para decidir qué se arma en el menú.</summary>
    [ObservableProperty]
    private EstadoLicencia estadoLicenciaActual;

    /// <summary>☀ cuando el tema activo es Claro (al presionarlo se pasa a Oscuro); 🌙 cuando es Oscuro (al presionarlo se vuelve a Claro).</summary>
    public string IconoTema => TemaActual == TemaApp.Oscuro ? "🌙" : "☀";

    public string ToolTipTema => TemaActual == TemaApp.Oscuro ? "Cambiar a modo claro" : "Cambiar a modo oscuro";

    /// <summary>Versión mostrada en el sidebar (toma el <c>&lt;Version&gt;</c> del .csproj); se actualiza sola en cada release.</summary>
    public string VersionTexto => $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";

    public ShellViewModel(
        IServiceProvider servicios, IEnumerable<IModuloContable> modulos, IActualizacionService actualizaciones,
        ITemaService temaService, IPreferenciasService preferenciasService,
        ILicenciaEstadoActual licenciaEstado, IValidadorLicenciaService validadorLicencia)
    {
        _servicios = servicios;
        _modulos = modulos;
        _actualizaciones = actualizaciones;
        _temaService = temaService;
        _preferenciasService = preferenciasService;
        _licenciaEstado = licenciaEstado;
        _validadorLicencia = validadorLicencia;

        TemaActual = _temaService.TemaActual;
        _temaService.TemaCambiado += tema => TemaActual = tema;
        _licenciaEstado.Cambiado += OnLicenciaCambiada;

        ReconstruirSegunLicencia();
    }

    /// <summary>Arma el menú completo (Autorizada/EnGracia) o lo deja vacío y muestra la pantalla de bloqueo (Bloqueada).</summary>
    private void ReconstruirSegunLicencia()
    {
        var resultado = _licenciaEstado.Actual!;
        EstadoLicenciaActual = resultado.Estado;

        Menu.Clear();
        ElementosAyuda.Clear();

        if (resultado.Estado == EstadoLicencia.Bloqueada)
        {
            MenuExpandido = false;
            VistaActual = new BloqueoViewModel(resultado, ValidarLicenciaAhoraAsync);
            return;
        }

        MenuExpandido = true;

        if (resultado.Estado == EstadoLicencia.EnGracia)
        {
            MensajeGlobal = resultado.Mensaje;
        }

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
        foreach (var modulo in _modulos.OrderBy(m => m.Orden))
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
        ElementosAyuda.Add(new ElementoMenu("Licencia / Acerca de", AbrirLicencia));

        // Estado inicial: Operaciones expandido, mostrando su primer elemento
        // (con Catálogos como respaldo si todavía no hay módulos registrados).
        if (hijosOperaciones.Count > 0)
        {
            padreOperaciones.EstaExpandido = true;
            NavegarHijo(hijosOperaciones[0]);
        }
        else
        {
            padreCatalogos.EstaExpandido = true;
            NavegarHijo(hijosCatalogos[0]);
        }
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

    private void AbrirLicencia()
    {
        VistaActual = new LicenciaViewModel(_licenciaEstado.Actual!, ValidarLicenciaAhoraAsync);
        LimpiarHijosActivos();
    }

    private async Task ValidarLicenciaAhoraAsync()
    {
        var resultado = await _validadorLicencia.ValidarAsync();
        _licenciaEstado.Actual = resultado; // Dispara OnLicenciaCambiada.
    }

    /// <summary>
    /// Reacciona a una revalidación de licencia ("Validar ahora"). Si cambió
    /// entre "puede usar módulos" y "no puede", reconstruye todo el menú
    /// (así se desbloquea/bloquea sin reiniciar la app); si el estado
    /// utilizable no cambió, solo refresca la pantalla de licencia/bloqueo
    /// que esté abierta, sin perder la navegación actual del usuario.
    /// </summary>
    private void OnLicenciaCambiada(ResultadoLicencia resultado)
    {
        var eraBloqueada = EstadoLicenciaActual == EstadoLicencia.Bloqueada;
        var esBloqueada = resultado.Estado == EstadoLicencia.Bloqueada;

        if (eraBloqueada != esBloqueada)
        {
            ReconstruirSegunLicencia();
            return;
        }

        EstadoLicenciaActual = resultado.Estado;
        switch (VistaActual)
        {
            case LicenciaViewModel licenciaVm:
                licenciaVm.Actualizar(resultado);
                break;
            case BloqueoViewModel bloqueoVm:
                bloqueoVm.Actualizar(resultado);
                break;
        }
    }

    private async Task BuscarActualizacionesAsync()
    {
        MensajeGlobal = "Buscando actualizaciones...";
        MensajeGlobal = await _actualizaciones.BuscarYAplicarAsync();
    }
}
