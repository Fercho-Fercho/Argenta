using System.IO;
using System.Windows;
using ContaSuite.Core.Modulos;
using ContaSuite.Data;
using ContaSuite.Data.DependencyInjection;
using ContaSuite.Data.Servicios;
using ContaSuite.Modules.LibroCompras.DependencyInjection;
using ContaSuite.Wpf.Modulos;
using ContaSuite.Wpf.Servicios;
using ContaSuite.Wpf.ViewModels;
using ContaSuite.Wpf.ViewModels.Catalogos;
using ContaSuite.Wpf.ViewModels.Configuraciones;
using ContaSuite.Wpf.ViewModels.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace ContaSuite.Wpf;

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
                "ContaSuite", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Aplica el tema guardado ANTES de mostrar cualquier ventana, para
        // que no haya un parpadeo del tema por defecto al arrancar.
        var preferencias = Servicios.GetRequiredService<IPreferenciasService>().Cargar();
        Servicios.GetRequiredService<ITemaService>().AplicarTema(preferencias.Tema);

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
        servicios.AddContaSuiteData(RutasApp.ArchivoBaseDatos);

        // Módulos Libro de Compras y Libro de Ventas (comparten el mismo registro de servicios: AddModuloLibroCompras).
        servicios.AddModuloLibroCompras();
        servicios.AddSingleton<IModuloContable, LibroComprasModulo>();
        servicios.AddSingleton<IModuloContable, LibroVentasModulo>();

        // Servicios propios del shell.
        servicios.AddSingleton<IActualizacionService, ActualizacionService>();
        servicios.AddSingleton<IPreferenciasService, PreferenciasService>();
        servicios.AddSingleton<ITemaService, TemaService>();

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
        var fabricaDb = Servicios.GetRequiredService<IDbContextFactory<ContaSuiteDbContext>>();
        await using var db = await fabricaDb.CreateDbContextAsync();
        await RespaldoBaseDatosService.InicializarBaseDatosAsync(db, RutasApp.ArchivoBaseDatos);
    }
}
