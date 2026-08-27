using Argenta.Data.Entidades;

namespace Argenta.Data.Repositorios;

public interface IProveedorRepositorio
{
    Task<List<Proveedor>> ObtenerTodosAsync();
    Task<Proveedor?> ObtenerPorIdAsync(int id);

    /// <summary>Trae todo el catálogo indexado por NIT normalizado, para clasificar facturas en memoria.</summary>
    Task<Dictionary<string, Proveedor>> ObtenerDiccionarioPorNitAsync();

    Task GuardarAsync(Proveedor proveedor);
    Task EliminarAsync(int id);
}
