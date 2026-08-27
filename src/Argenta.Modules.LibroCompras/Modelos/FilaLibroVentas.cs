namespace Argenta.Modules.LibroCompras.Modelos;

/// <summary>
/// Una fila ya clasificada del libro de Ventas. A diferencia de
/// <see cref="FilaLibroCompras"/>: no lleva Exento (no existe en este
/// formato), sí lleva Exportaciones/INGUAT (columnas condicionales según el
/// <c>TipoCliente</c>/<c>Exporta</c> del vendedor), no tiene checkbox de
/// incluir/excluir ni colores naranja/rojo (nada se descarta en ventas), y el
/// Nit/Nombre son los del RECEPTOR (comprador), no los del emisor.
/// </summary>
public sealed class FilaLibroVentas
{
    public required DateTime Fecha { get; init; }
    public required string Docto { get; init; }
    public required string Serie { get; init; }
    public required string NoDoc { get; init; }

    /// <summary>NIT del receptor (comprador) — no del emisor, que es el propio cliente/vendedor del libro.</summary>
    public required string Nit { get; init; }

    /// <summary>Nombre del receptor (comprador).</summary>
    public required string Nombre { get; init; }

    /// <summary>Es una nota de crédito (NCRE): los valores ya vienen en negativo.</summary>
    public required bool EsNotaCredito { get; init; }

    /// <summary>
    /// true si el documento aparece anulado en el Excel "Consulta de
    /// documentos" del SAT (el ZIP de XML no trae este dato). La fila se
    /// mantiene en el libro, pero con todos sus montos en 0.
    /// </summary>
    public bool Anulada { get; set; }

    public decimal Ventas { get; set; }
    public decimal Servicios { get; set; }

    /// <summary>Solo tiene sentido si el cliente (vendedor) tiene Exporta = Sí; ver <c>GeneradorLibroVentasXlsx</c>.</summary>
    public decimal Exportaciones { get; set; }

    /// <summary>Impuesto de turismo/hospedaje (INGUAT); solo tiene sentido si el cliente es Tipo Hotel.</summary>
    public decimal Inguat { get; set; }

    public decimal Iva { get; set; }
    public decimal Total { get; set; }

    /// <summary>
    /// IVA teórico ((Ventas + Servicios) × 12%), solo informativo para la
    /// columna "Cálculo de IVA" de la vista previa. Nulo para documentos a
    /// los que no aplica el filtro (anuladas, o tipos sin ítems).
    /// </summary>
    public decimal? CalculoIva { get; set; }

    /// <summary>Filtro de IVA (único resaltado que aplica en ventas: amarillo).</summary>
    public bool DescuadreIva { get; set; }

    /// <summary>Factura de origen (para el detalle visual con el botón "Ver").</summary>
    public required DteFel OrigenFel { get; init; }
}
