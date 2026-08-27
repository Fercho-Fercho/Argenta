using Argenta.Data.Entidades;

namespace Argenta.Data.Repositorios;

public interface IClienteRepositorio
{
    Task<List<Cliente>> ObtenerTodosAsync();
    Task<Cliente?> ObtenerPorIdAsync(int id);

    /// <summary>Busca por el "ID del receptor" del Excel de la SAT (NIT sin guiones).</summary>
    Task<Cliente?> ObtenerPorNitNormalizadoAsync(string nitNormalizado);

    Task GuardarAsync(Cliente cliente);
    Task EliminarAsync(int id);
}
