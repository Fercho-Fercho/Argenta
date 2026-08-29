using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Argenta.Wpf.Servicios.Licencia;

public enum ResultadoValidacionRemota
{
    Autorizada,
    NoAutorizada,

    /// <summary>No se pudo consultar la lista remota (sin internet, timeout, JSON inválido, etc.).</summary>
    ErrorRed,
}

public interface IAutorizacionService
{
    Task<ResultadoValidacionRemota> ValidarAsync(string codigoMaquina, CancellationToken ct = default);
}

internal sealed class ListaAutorizadasJson
{
    public int Version { get; set; }
    public List<MaquinaAutorizadaJson> Maquinas { get; set; } = [];
}

internal sealed class MaquinaAutorizadaJson
{
    public string Codigo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public bool Activa { get; set; }
}

/// <summary>
/// Descarga la lista de computadoras autorizadas desde un archivo JSON público
/// en GitHub (raw.githubusercontent.com) — no hace falta token porque el
/// repositorio de licencias es público. La URL sale de
/// <c>Licencia:UrlListaAutorizadas</c> en appsettings.json. Ver README,
/// sección "Licencia por computadora autorizada", para el mantenimiento.
/// </summary>
public sealed class AutorizacionService(HttpClient http, IConfiguration configuracion) : IAutorizacionService
{
    public async Task<ResultadoValidacionRemota> ValidarAsync(string codigoMaquina, CancellationToken ct = default)
    {
        var urlLista = configuracion["Licencia:UrlListaAutorizadas"];
        if (string.IsNullOrWhiteSpace(urlLista))
        {
            return ResultadoValidacionRemota.ErrorRed;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(http.Timeout);

            var lista = await http.GetFromJsonAsync<ListaAutorizadasJson>(urlLista, cts.Token).ConfigureAwait(false);
            var maquina = lista?.Maquinas.FirstOrDefault(m =>
                string.Equals(m.Codigo.Trim(), codigoMaquina, StringComparison.OrdinalIgnoreCase));

            return maquina is { Activa: true } ? ResultadoValidacionRemota.Autorizada : ResultadoValidacionRemota.NoAutorizada;
        }
        catch
        {
            // Sin internet, DNS caído, timeout, HTTP con error, JSON corrupto,
            // etc.: no es "no autorizada", es que no se pudo consultar. La
            // lógica de gracia decide si igual se puede usar la app (ver
            // ValidadorLicenciaService).
            return ResultadoValidacionRemota.ErrorRed;
        }
    }
}
