using Argenta.Core.Moneda;
using Microsoft.EntityFrameworkCore;

namespace Argenta.Data.Moneda;

/// <summary>Implementación de <see cref="IProveedorTipoCambio"/> respaldada por SQLite.</summary>
public class ProveedorTipoCambioData(IDbContextFactory<ArgentaDbContext> fabricaDb) : IProveedorTipoCambio
{
    public bool TryObtener(DateTime fecha, out decimal valor)
    {
        using var db = fabricaDb.CreateDbContext();
        var tipoCambio = db.TiposCambio.AsNoTracking().FirstOrDefault(t => t.Fecha == fecha.Date);
        valor = tipoCambio?.Valor ?? 0m;
        return tipoCambio is not null;
    }
}
