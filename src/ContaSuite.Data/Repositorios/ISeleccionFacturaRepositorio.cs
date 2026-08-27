using ContaSuite.Data.Entidades;

namespace ContaSuite.Data.Repositorios;

public interface ISeleccionFacturaRepositorio
{
    /// <summary>
    /// Decisiones guardadas para ese cliente/periodo/libro, indexadas por
    /// <see cref="SeleccionFactura.IdentificadorFactura"/> (hash) -> Incluida.
    /// </summary>
    Task<Dictionary<string, bool>> ObtenerDecisionesAsync(string nitCliente, int anio, int mes, TipoLibro tipoLibro);

    /// <summary>Upsert por (NitCliente, Anio, Mes, TipoLibro, IdentificadorFactura).</summary>
    Task GuardarLoteAsync(string nitCliente, int anio, int mes, TipoLibro tipoLibro, IEnumerable<(string IdentificadorFactura, bool Incluida)> decisiones);
}
