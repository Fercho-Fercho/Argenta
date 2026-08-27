using Argenta.Data.Entidades;

namespace Argenta.Data.Repositorios;

public interface IProveedorRevisarRepositorio
{
    Task<List<ProveedorRevisar>> ObtenerTodosAsync();
    Task<ProveedorRevisar?> ObtenerPorIdAsync(int id);

    /// <summary>Trae todo el catálogo indexado por NIT normalizado, para clasificar facturas en memoria.</summary>
    Task<Dictionary<string, ProveedorRevisar>> ObtenerDiccionarioPorNitAsync();

    Task GuardarAsync(ProveedorRevisar proveedor);
    Task EliminarAsync(int id);
}
