namespace ContaSuite.Core.Moneda;

/// <summary>
/// Contrato para consultar el tipo de cambio del catálogo por fecha.
/// Lo implementa la capa Data (consultando SQLite); lo consumen los módulos
/// (Compras, y en el futuro Ventas) sin depender de EF Core.
/// </summary>
public interface IProveedorTipoCambio
{
    /// <summary>Busca el tipo de cambio vigente para una fecha exacta.</summary>
    bool TryObtener(DateTime fecha, out decimal valor);
}
