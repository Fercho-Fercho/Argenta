using Argenta.Core.Utilidades;
using Argenta.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Argenta.Data.Repositorios;

public class ClienteRepositorio(IDbContextFactory<ArgentaDbContext> fabricaDb) : IClienteRepositorio
{
    public async Task<List<Cliente>> ObtenerTodosAsync()
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.Clientes.AsNoTracking().Include(c => c.Establecimientos).OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.Clientes.AsNoTracking().Include(c => c.Establecimientos).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ObtenerPorNitNormalizadoAsync(string nitNormalizado)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var clientes = await db.Clientes.AsNoTracking().Include(c => c.Establecimientos).ToListAsync();
        return clientes.FirstOrDefault(c => NitUtil.Normalizar(c.Nit) == nitNormalizado);
    }

    public async Task GuardarAsync(Cliente cliente)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();

        if (cliente.Id == 0)
        {
            // Alta: EF inserta el cliente y en cascada sus establecimientos (todos con Id = 0).
            db.Clientes.Add(cliente);
        }
        else
        {
            // EdiciÃ³n: db.Clientes.Update(cliente) sobre un grafo desconectado marca
            // todo como Modified, pero NO elimina establecimientos que el usuario haya
            // quitado de la lista. Se sincroniza a mano contra lo que ya hay en la BD.
            var existente = await db.Clientes.Include(c => c.Establecimientos).FirstAsync(c => c.Id == cliente.Id);

            existente.Nombre = cliente.Nombre;
            existente.Nit = cliente.Nit;
            existente.Activo = cliente.Activo;

            var idsEntrantes = cliente.Establecimientos.Where(e => e.Id != 0).Select(e => e.Id).ToHashSet();
            foreach (var actual in existente.Establecimientos.Where(e => !idsEntrantes.Contains(e.Id)).ToList())
            {
                db.Establecimientos.Remove(actual);
            }

            foreach (var entrante in cliente.Establecimientos)
            {
                if (entrante.Id == 0)
                {
                    existente.Establecimientos.Add(new Establecimiento
                    {
                        ClienteId = existente.Id,
                        Numero = entrante.Numero,
                        Nombre = entrante.Nombre,
                        Tipo = entrante.Tipo,
                        Exporta = entrante.Exporta,
                    });
                }
                else
                {
                    var actual = existente.Establecimientos.First(e => e.Id == entrante.Id);
                    actual.Numero = entrante.Numero;
                    actual.Nombre = entrante.Nombre;
                    actual.Tipo = entrante.Tipo;
                    actual.Exporta = entrante.Exporta;
                }
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await fabricaDb.CreateDbContextAsync();
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null) return;

        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync();
    }
}
