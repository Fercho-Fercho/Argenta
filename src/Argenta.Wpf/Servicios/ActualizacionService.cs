using Microsoft.Extensions.Configuration;
using Velopack;
using Velopack.Sources;

namespace Argenta.Wpf.Servicios;

public sealed class ActualizacionService(IConfiguration configuracion) : IActualizacionService
{
    public async Task<string> BuscarYAplicarAsync()
    {
        var urlFeed = configuracion["Actualizaciones:UrlFeed"];
        if (string.IsNullOrWhiteSpace(urlFeed))
        {
            return "No hay una dirección de actualizaciones configurada (appsettings.json).";
        }

        try
        {
            var administrador = new UpdateManager(CrearFuente(urlFeed));

            if (!administrador.IsInstalled)
            {
                return "Esta copia no fue instalada con el instalador de Argenta; no se puede actualizar automáticamente.";
            }

            var actualizacionDisponible = await administrador.CheckForUpdatesAsync();
            if (actualizacionDisponible is null)
            {
                return "Ya tiene instalada la última versión de Argenta.";
            }

            await administrador.DownloadUpdatesAsync(actualizacionDisponible);
            administrador.ApplyUpdatesAndRestart(actualizacionDisponible.TargetFullRelease);
            return "Actualización descargada. Argenta se reiniciará para aplicarla.";
        }
        catch (Exception ex)
        {
            return $"No se pudo buscar actualizaciones: {ex.Message}";
        }
    }

    /// <summary>
    /// Si <c>UrlFeed</c> apunta a un repo de GitHub, se usa <see cref="GithubSource"/>
    /// (necesario para que Velopack sepa buscar los "Releases" del repo en vez
    /// de tratarlo como una carpeta HTTP con un feed plano). El repo es
    /// público, así que <c>Actualizaciones:GithubToken</c> puede quedar vacío;
    /// solo hace falta llenarlo si el repo vuelve a ponerse privado. Cualquier
    /// otra URL se trata como el feed HTTP simple de siempre.
    /// </summary>
    private IUpdateSource CrearFuente(string urlFeed)
    {
        if (urlFeed.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
        {
            var token = configuracion["Actualizaciones:GithubToken"];
            return new GithubSource(urlFeed, string.IsNullOrWhiteSpace(token) ? null : token, prerelease: false);
        }

        return new SimpleWebSource(urlFeed);
    }
}
