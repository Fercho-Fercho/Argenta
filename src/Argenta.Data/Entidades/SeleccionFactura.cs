namespace Argenta.Data.Entidades;

/// <summary>
/// Recuerda si el contador dejó una factura marcada (incluida) o la
/// desmarcó (excluida) al generar un libro, por cliente y periodo, para no
/// perder esa elección al volver a subir el mismo pool de facturas.
///
/// REGLA DE PRIVACIDAD (no negociable): esta tabla NUNCA debe tener una
/// columna con datos de la factura (montos, IVA, proveedor, NIT del emisor,
/// descripciones, tipo de bien/servicio, etc.). Solo guarda un identificador
/// OPACO (hash SHA-256 del Número de Autorización, ver
/// <c>Argenta.Core.Utilidades.IdentificadorFacturaUtil</c>) y el estado
/// incluida/excluida. Quien vea esta tabla no debe poder reconstruir ninguna
/// factura ni saber cuánto se gastó ni con quién.
/// </summary>
public class SeleccionFactura
{
    public int Id { get; set; }

    /// <summary>NIT del cliente del libro, normalizado.</summary>
    public string NitCliente { get; set; } = string.Empty;

    public int Anio { get; set; }
    public int Mes { get; set; }

    /// <summary>Por ahora siempre Compras; el campo ya existe para reutilizar esta misma tabla cuando exista el libro de Ventas.</summary>
    public TipoLibro TipoLibro { get; set; } = TipoLibro.Compras;

    /// <summary>Hash SHA-256 del Número de Autorización (UUID) del DTE. Nunca el UUID en texto plano.</summary>
    public string IdentificadorFactura { get; set; } = string.Empty;

    /// <summary>true = el contador la dejó marcada (incluida); false = la desmarcó (excluida).</summary>
    public bool Incluida { get; set; } = true;
}
