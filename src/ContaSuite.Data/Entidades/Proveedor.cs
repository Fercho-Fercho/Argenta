namespace ContaSuite.Data.Entidades;

/// <summary>
/// Proveedor del catálogo del contador. El NIT se guarda normalizado
/// (sin guiones ni ceros a la izquierda) para poder cruzarlo directamente
/// con el "NIT del emisor" del Excel de la SAT.
/// </summary>
public class Proveedor
{
    public int Id { get; set; }

    /// <summary>NIT normalizado (ver <c>ContaSuite.Core.Utilidades.NitUtil.Normalizar</c>).</summary>
    public string Nit { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public TipoProveedor Tipo { get; set; } = TipoProveedor.Compra;

    /// <summary>
    /// Categoría especial del proveedor (Gasolinera, Empresa Eléctrica, etc.):
    /// determina si alguno de sus impuestos específicos va a la columna
    /// Exento en lugar de la base neta. Ver <c>MotorClasificacion.ObtenerImpuestoEspecial</c>.
    /// </summary>
    public CategoriaProveedor Categoria { get; set; } = CategoriaProveedor.Normal;
}
