using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace ContaSuite.Data.Repositorios;

public class SeleccionFacturaRepositorio(IDbContextFactory<ContaSuiteDbContext> fabricaDb) : ISeleccionFacturaRepositorio
{
    public async Task<Dictionary<string, bool>> ObtenerDecisionesAsync(string nitCliente, int anio, int mes, TipoLibro tipoLibro)
    {
        var nitNormalizado = NitUtil.Normalizar(nitCliente);

        await using var db = await fabricaDb.CreateDbContextAsync();
        return await db.SeleccionesFactura
            .AsNoTracking()
            .Where(s => s.NitCliente == nitNormalizado && s.Anio == anio && s.Mes == mes && s.TipoLibro == tipoLibro)
            .ToDictionaryAsync(s => s.IdentificadorFactura, s => s.Incluida);
    }

    public async Task GuardarLoteAsync(
        string nitCliente, int anio, int mes, TipoLibro tipoLibro, IEnumerable<(string IdentificadorFactura, bool Incluida)> decisiones)
    {
        var nitNormalizado = NitUtil.Normalizar(nitCliente);

        await using var db = await fabricaDb.CreateDbContextAsync();

        var existentes = await db.SeleccionesFactura
            .Where(s => s.NitCliente == nitNormalizado && s.Anio == anio && s.Mes == mes && s.TipoLibro == tipoLibro)
            .ToDictionaryAsync(s => s.IdentificadorFactura);

        foreach (var (identificador, incluida) in decisiones)
        {
            if (existentes.TryGetValue(identificador, out var registro))
            {
                registro.Incluida = incluida;
            }
            else
            {
                db.SeleccionesFactura.Add(new SeleccionFactura
                {
                    NitCliente = nitNormalizado,
                    Anio = anio,
                    Mes = mes,
                    TipoLibro = tipoLibro,
                    IdentificadorFactura = identificador,
                    Incluida = incluida,
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
