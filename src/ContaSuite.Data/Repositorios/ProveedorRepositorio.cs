using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace ContaSuite.Data.Repositorios;

public class ProveedorRepositorio(IDbContextFactory<ContaSuiteDbContext> fabricaDb) : IProveedorRepositorio
{
    public async Task<List<Proveedor>> ObtenerTodosAsync()
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.Proveedores.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<Proveedor?> ObtenerPorIdAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Dictionary<string, Proveedor>> ObtenerDiccionarioPorNitAsync()
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var proveedores = await db.Proveedores.AsNoTracking().ToListAsync();
        return proveedores
            .GroupBy(p => NitUtil.Normalizar(p.Nit))
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task GuardarAsync(Proveedor proveedor)
    {
        proveedor.Nit = NitUtil.Normalizar(proveedor.Nit);

        await using var db = await fabricaDb.CreateDbContextAsync();

        if (proveedor.Id == 0)
        {
            db.Proveedores.Add(proveedor);
        }
        else
        {
            db.Proveedores.Update(proveedor);
        }

        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var proveedor = await db.Proveedores.FindAsync(id);
        if (proveedor is null) return;

        db.Proveedores.Remove(proveedor);
        await db.SaveChangesAsync();
    }
}
