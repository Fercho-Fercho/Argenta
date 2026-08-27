using ContaSuite.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace ContaSuite.Data.Repositorios;

public class TipoCambioRepositorio(IDbContextFactory<ContaSuiteDbContext> fabricaDb) : ITipoCambioRepositorio
{
    public async Task<List<TipoCambio>> ObtenerTodosAsync()
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.TiposCambio.AsNoTracking().OrderByDescending(t => t.Fecha).ToListAsync();
    }

    public async Task<TipoCambio?> ObtenerPorFechaAsync(DateTime fecha)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.TiposCambio.AsNoTracking().FirstOrDefaultAsync(t => t.Fecha == fecha.Date);
    }

    public async Task GuardarAsync(TipoCambio tipoCambio)
    {
        tipoCambio.Fecha = tipoCambio.Fecha.Date;

        await using var db = await fabricaDb.CreateDbContextAsync();

        if (tipoCambio.Id == 0)
        {
            db.TiposCambio.Add(tipoCambio);
        }
        else
        {
            db.TiposCambio.Update(tipoCambio);
        }

        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var tipoCambio = await db.TiposCambio.FindAsync(id);
        if (tipoCambio is null) return;

        db.TiposCambio.Remove(tipoCambio);
        await db.SaveChangesAsync();
    }

    public async Task<(int Insertados, int Actualizados)> ImportarUpsertAsync(IEnumerable<(DateTime Fecha, decimal Valor)> valores)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();

        var existentes = await db.TiposCambio.ToDictionaryAsync(t => t.Fecha.Date, t => t);
        int insertados = 0, actualizados = 0;

        foreach (var (fecha, valor) in valores)
        {
            var fechaSolo = fecha.Date;
            if (existentes.TryGetValue(fechaSolo, out var existente))
            {
                if (existente.Valor != valor)
                {
                    existente.Valor = valor;
                    actualizados++;
                }
            }
            else
            {
                db.TiposCambio.Add(new TipoCambio { Fecha = fechaSolo, Valor = valor });
                insertados++;
            }
        }

        await db.SaveChangesAsync();
        return (insertados, actualizados);
    }
}
