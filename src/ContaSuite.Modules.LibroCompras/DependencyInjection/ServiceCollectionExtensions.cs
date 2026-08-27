using ContaSuite.Core.Validacion;
using ContaSuite.Modules.LibroCompras.Modelos;
using ContaSuite.Modules.LibroCompras.Servicios;
using ContaSuite.Modules.LibroCompras.Validaciones;
using Microsoft.Extensions.DependencyInjection;

namespace ContaSuite.Modules.LibroCompras.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra toda la lógica de negocio del módulo Libro de Compras.</summary>
    public static IServiceCollection AddModuloLibroCompras(this IServiceCollection services)
    {
        services.AddScoped<LectorFacturasSat>();
        services.AddScoped<LectorTipoCambioBanguat>();
        services.AddScoped<MotorClasificacion>();
        services.AddScoped<GeneradorLibroComprasXlsx>();
        services.AddScoped<GeneradorLibroComprasPdf>();
        services.AddScoped<LibroComprasService>();

        services.AddScoped<IReglaValidacion<IReadOnlyList<FacturaSat>>, ValidacionTipoCambioFaltante>();
        services.AddScoped<IReglaValidacion<IReadOnlyList<FacturaSat>>, ValidacionCamposSat>();
        services.AddScoped<MotorValidaciones<IReadOnlyList<FacturaSat>>>();

        // Fuente alterna: Libro de Compras a partir de un ZIP con XML (FEL).
        services.AddScoped<LectorZipXmlFel>();
        services.AddScoped<MotorClasificacionFel>();
        services.AddScoped<LibroComprasFelService>();

        services.AddScoped<IReglaValidacion<IReadOnlyList<DteFel>>, ValidacionTipoCambioFaltanteFel>();
        services.AddScoped<IReglaValidacion<IReadOnlyList<DteFel>>, ValidacionTipoDteFel>();
        services.AddScoped<IReglaValidacion<IReadOnlyList<DteFel>>, ValidacionCuadreItemsFel>();
        services.AddScoped<MotorValidaciones<IReadOnlyList<DteFel>>>();

        // Módulo Libro de Ventas: a partir de un ZIP con XML (FEL), cruzado con
        // el .xls de "Consulta de documentos" del SAT para saber qué facturas
        // están anuladas (dato que el XML no trae). Reutiliza LectorZipXmlFel,
        // ComportamientoDteCatalogo y MotorValidaciones<IReadOnlyList<DteFel>> tal cual los usa compras.
        services.AddScoped<LectorEstadoDocumentosSat>();
        services.AddScoped<MotorClasificacionVentasFel>();
        services.AddScoped<GeneradorLibroVentasXlsx>();
        services.AddScoped<GeneradorLibroVentasPdf>();
        services.AddScoped<LibroVentasFelService>();

        services.AddScoped<IReglaValidacion<ContextoValidacionVentas>, ValidacionProfesionalSoloServiciosFel>();
        services.AddScoped<IReglaValidacion<ContextoValidacionVentas>, ValidacionExportacionSinMarcarFel>();
        services.AddScoped<MotorValidaciones<ContextoValidacionVentas>>();

        return services;
    }
}
