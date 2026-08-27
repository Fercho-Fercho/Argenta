namespace Argenta.Data.Entidades;

/// <summary>
/// Tipo de cambio (USD → GTQ) de un día. Catálogo compartido: sirve para
/// convertir montos en dólares en Compras y, en el futuro, en Ventas.
/// </summary>
public class TipoCambio
{
    public int Id { get; set; }

    /// <summary>Solo la fecha (sin hora).</summary>
    public DateTime Fecha { get; set; }

    public decimal Valor { get; set; }
}
