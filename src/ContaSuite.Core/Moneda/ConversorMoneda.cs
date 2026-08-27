namespace ContaSuite.Core.Moneda;

/// <summary>Convierte montos a quetzales usando el tipo de cambio del día de emisión.</summary>
public interface IConversorMoneda
{
    /// <summary>
    /// Convierte <paramref name="monto"/> a quetzales. Si la moneda ya es GTQ, lo
    /// devuelve tal cual. Si es USD, busca el tipo de cambio de <paramref name="fecha"/>;
    /// si no existe, devuelve false (la factura no puede procesarse todavía).
    /// </summary>
    bool TryConvertirAQuetzales(decimal monto, string moneda, DateTime fecha, out decimal resultado);
}

public sealed class ConversorMoneda(IProveedorTipoCambio proveedorTipoCambio) : IConversorMoneda
{
    public bool TryConvertirAQuetzales(decimal monto, string moneda, DateTime fecha, out decimal resultado)
    {
        if (string.Equals(moneda?.Trim(), "GTQ", StringComparison.OrdinalIgnoreCase))
        {
            resultado = monto;
            return true;
        }

        if (proveedorTipoCambio.TryObtener(fecha, out var tipoCambio))
        {
            resultado = monto * tipoCambio;
            return true;
        }

        resultado = 0m;
        return false;
    }
}
