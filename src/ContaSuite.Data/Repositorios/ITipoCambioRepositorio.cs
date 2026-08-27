using ContaSuite.Data.Entidades;

namespace ContaSuite.Data.Repositorios;

public interface ITipoCambioRepositorio
{
    Task<List<TipoCambio>> ObtenerTodosAsync();
    Task<TipoCambio?> ObtenerPorFechaAsync(DateTime fecha);
    Task GuardarAsync(TipoCambio tipoCambio);
    Task EliminarAsync(int id);

    /// <summary>
    /// Inserta los tipos de cambio que no existen y actualiza (por fecha) los que
    /// ya existen. Usado al importar el CSV del Banguat. Devuelve (insertados, actualizados).
    /// </summary>
    Task<(int Insertados, int Actualizados)> ImportarUpsertAsync(IEnumerable<(DateTime Fecha, decimal Valor)> valores);
}
