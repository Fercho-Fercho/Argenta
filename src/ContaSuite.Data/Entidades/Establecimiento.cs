namespace ContaSuite.Data.Entidades;

/// <summary>
/// Un establecimiento de un <see cref="Cliente"/> (vendedor): un mismo
/// contribuyente puede facturar desde varios establecimientos, cada uno con
/// su propio Tipo y Exporta — por eso el libro de Ventas se genera por
/// establecimiento, no por cliente. Ver <c>LibroVentasFelService</c>.
/// </summary>
public class Establecimiento
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    /// <summary>
    /// Código de establecimiento tal como lo trae el DTE (atributo
    /// CodigoEstablecimiento del emisor). Único dentro del mismo cliente,
    /// pero NO correlativo: puede ser 1, 2, 10, cualquier número.
    /// </summary>
    public int Numero { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Profesional (solo Servicios), Comercial (Bienes y Servicios) u Hotel (agrega columna INGUAT). Ver <see cref="Entidades.TipoCliente"/>.</summary>
    public TipoCliente Tipo { get; set; } = TipoCliente.Comercial;

    /// <summary>Si este establecimiento realiza exportaciones: agrega la columna "Exportaciones" a su libro de ventas.</summary>
    public bool Exporta { get; set; }
}
