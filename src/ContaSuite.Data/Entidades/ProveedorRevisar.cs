namespace ContaSuite.Data.Entidades;

/// <summary>
/// Catálogo de proveedores a marcar en la pestaña "Libro de Compras (XML)":
/// distinto del catálogo de Proveedores normal (que define Compra/Servicio),
/// este solo controla el resaltado y el estado inicial de inclusión de sus
/// facturas. Ver <c>MotorClasificacionFel</c>.
/// </summary>
public class ProveedorRevisar
{
    public int Id { get; set; }

    /// <summary>NIT normalizado (ver <c>ContaSuite.Core.Utilidades.NitUtil.Normalizar</c>).</summary>
    public string Nit { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public AccionRevisar Accion { get; set; } = AccionRevisar.Revisar;
}
