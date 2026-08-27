namespace ContaSuite.Data.Entidades;

/// <summary>
/// Contribuyente cuyo libro/reporte se genera. Catálogo compartido entre
/// todos los módulos (Compras, y en el futuro Ventas, etc.). Tipo y Exporta
/// viven a nivel de <see cref="Establecimiento"/> (no aquí): un mismo cliente
/// puede tener varios establecimientos, cada uno de distinto tipo y con o
/// sin exportación.
/// </summary>
public class Cliente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>NIT tal como lo usa el contador para identificar al cliente, ej. "468783-3".</summary>
    public string Nit { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public List<Establecimiento> Establecimientos { get; set; } = [];
}
