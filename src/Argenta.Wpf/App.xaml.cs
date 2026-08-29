using System.IO;
using System.Net.Http;
using System.Windows;
using Argenta.Core.Modulos;
using Argenta.Data;
using Argenta.Data.DependencyInjection;
using Argenta.Data.Servicios;
using Argenta.Modules.LibroCompras.DependencyInjection;
using Argenta.Wpf.Modulos;
using Argenta.Wpf.Servicios;
using Argenta.Wpf.Servicios.Licencia;
using Argenta.Wpf.ViewModels;
using Argenta.Wpf.ViewModels.Catalogos;
using Argenta.Wpf.ViewModels.Configuraciones;
using Argenta.Wpf.ViewModels.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace Argenta.Wpf;

public partial class App : Application
{
    public static IServiceProvider Servicios { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Debe ejecutarse muy al inicio, antes de mostrar cualquier ventana:
        // gestiona los ganchos de instalación/actualización de Velopack.
        VelopackApp.Build().Run();

        base.OnStartup(e);

        Directory.CreateDirectory(RutasApp.CarpetaDatos);
        RutasApp.MigrarDatosCarpetaAnterior();

        var configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        Servicios = ConfigurarServicios(configuracion);

        try
        {
            InicializarBaseDatosAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo preparar la base de datos local. Se restauró el último respaldo disponible.\n\n" +
                $"Detalle: {ex.Message}",
                "Argenta", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Aplica el tema guardado ANTES de mostrar cualquier ventana, para
        // que no haya un parpadeo del tema por defecto al arrancar.
        var preferencias = Servicios.GetRequiredService<IPreferenciasService>().Cargar();
        Servicios.GetRequiredService<ITemaService>().AplicarTema(preferencias.Tema);

        // Validación de licencia: corre ANTES de mostrar el shell (ShellViewModel
        // lee ILicenciaEstadoActual.Actual de forma síncrona en su constructor,
        // así que ya debe estar calculado en este punto). Ver README, sección
        // "Licencia por computadora autorizada".
        var estadoLicencia = Servicios.GetRequiredService<ILicenciaEstadoActual>();
        try
        {
            estadoLicencia.Actual = Servicios.GetRequiredService<IValidadorLicenciaService>()
                .ValidarAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // ValidadorLicenciaService ya atrapa sus propios errores de red;
            // si algo inesperado revienta aquí de todos modos, no debe tronar
            // el arranque — se trata como bloqueada para no dejar pasar
            // módulos sin haber podido validar.
            estadoLicencia.Actual = new ResultadoLicencia(
                EstadoLicencia.Bloqueada, "desconocido", null, $"No se pudo validar la licencia: {ex.Message}");
        }

        var ventanaPrincipal = new MainWindow
        {
            DataContext = Servicios.GetRequiredService<ShellViewModel>(),
        };
        ventanaPrincipal.Show();
    }

    private static IServiceProvider ConfigurarServicios(IConfiguration configuracion)
    {
        var servicios = new ServiceCollection();

        servicios.AddSingleton(configuracion);

        // Catálogos compartidos (EF Core + SQLite).
        servicios.AddArgentaData(RutasApp.ArchivoBaseDatos);

        // Módulos Libro de Compras y Libro de Ventas (comparten el mismo registro de servicios: AddModuloLibroCompras).
        servicios.AddModuloLibroCompras();
        servicios.AddSingleton<IModuloContable, LibroComprasModulo>();
        servicios.AddSingleton<IModuloContable, LibroVentasModulo>();

        // Servicios propios del shell.
        servicios.AddSingleton<IActualizacionService, ActualizacionService>();
        servicios.AddSingleton<IPreferenciasService, PreferenciasService>();
        servicios.AddSingleton<ITemaService, TemaService>();

        // Licencia por computadora autorizada (ver README).
        servicios.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(6) });
        servicios.AddSingleton<IFingerprintService, FingerprintService>();
        servicios.AddSingleton<IAutorizacionService, AutorizacionService>();
        servicios.AddSingleton<ICacheLicenciaService, CacheLicenciaService>();
        servicios.AddSingleton<IValidadorLicenciaService, ValidadorLicenciaService>();
        servicios.AddSingleton<ILicenciaEstadoActual, LicenciaEstadoActual>();

        // ViewModels.
        servicios.AddTransient<ShellViewModel>();
        servicios.AddTransient<ClientesViewModel>();
        servicios.AddTransient<ProveedoresViewModel>();
        servicios.AddTransient<ProveedoresRevisarViewModel>();
        servicios.AddTransient<TiposCambioViewModel>();
        servicios.AddTransient<GenerarLibroComprasViewModel>();
        servicios.AddTransient<GenerarLibroComprasFelViewModel>();
        servicios.AddTransient<GenerarLibroVentasFelViewModel>();
        servicios.AddTransient<DatosViewModel>();

        return servicios.BuildServiceProvider();
    }

    private static async Task InicializarBaseDatosAsync()
    {
        var fabricaDb = Servicios.GetRequiredService<IDbContextFactory<ArgentaDbContext>>();
        await using var db = await fabricaDb.CreateDbContextAsync();
        await RespaldoBaseDatosService.InicializarBaseDatosAsync(db, RutasApp.ArchivoBaseDatos);
    }
}
