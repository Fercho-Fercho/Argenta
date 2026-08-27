using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace ContaSuite.Data.Repositorios;

public class ProveedorRevisarRepositorio(IDbContextFactory<ContaSuiteDbContext> fabricaDb) : IProveedorRevisarRepositorio
{
    public async Task<List<ProveedorRevisar>> ObtenerTodosAsync()
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.ProveedoresRevisar.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<ProveedorRevisar?> ObtenerPorIdAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.ProveedoresRevisar.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Dictionary<string, ProveedorRevisar>> ObtenerDiccionarioPorNitAsync()
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var proveedores = await db.ProveedoresRevisar.AsNoTracking().ToListAsync();
        return proveedores
            .GroupBy(p => NitUtil.Normalizar(p.Nit))
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task GuardarAsync(ProveedorRevisar proveedor)
    {
        proveedor.Nit = NitUtil.Normalizar(proveedor.Nit);

        await using var db = await fabricaDb.CreateDbContextAsync();

        if (proveedor.Id == 0)
        {
            db.ProveedoresRevisar.Add(proveedor);
        }
        else
        {
            db.ProveedoresRevisar.Update(proveedor);
        }

        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var proveedor = await db.ProveedoresRevisar.FindAsync(id);
        if (proveedor is null) return;

        db.ProveedoresRevisar.Remove(proveedor);
        await db.SaveChangesAsync();
    }
}
