using Argenta.Core.Moneda;
using Argenta.Data.Moneda;
using Argenta.Data.Repositorios;
using Argenta.Data.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Argenta.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el DbContext (SQLite) y los repositorios de catálogos.
    ///
    /// IMPORTANTE: se usa <see cref="IDbContextFactory{TContext}"/> (no
    /// <c>AddDbContext</c> directo) porque el shell WPF resuelve los
    /// ViewModels desde el contenedor raíz de DI, sin crear un scope por
    /// pantalla. Un DbContext "Scoped" resuelto así vive tanto como la app y
    /// su ChangeTracker va acumulando entidades entre pantallas, lo que
    /// provoca "cannot be tracked because another instance with the same
    /// key value is already being tracked" en cuanto se edita dos veces el
    /// mismo registro. Con la fábrica, cada repositorio crea y descarta un
    /// DbContext de vida corta por operación.
    /// </summary>
    public static IServiceCollection AddArgentaData(this IServiceCollection services, string rutaArchivoSqlite)
    {
        services.AddDbContextFactory<ArgentaDbContext>(opciones => opciones.UseSqlite($"Data Source={rutaArchivoSqlite}"));

        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
        services.AddScoped<IProveedorRevisarRepositorio, ProveedorRevisarRepositorio>();
        services.AddScoped<ISeleccionFacturaRepositorio, SeleccionFacturaRepositorio>();
        services.AddScoped<ITipoCambioRepositorio, TipoCambioRepositorio>();
        services.AddScoped<IProveedorTipoCambio, ProveedorTipoCambioData>();
        services.AddScoped<IConversorMoneda, ConversorMoneda>();
        services.AddScoped<IImportacionDatosService, ImportacionDatosService>();

        return services;
    }
}
