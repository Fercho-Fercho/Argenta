using Argenta.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Argenta.Data.Semilla;

/// <summary>
/// Siembra los catálogos iniciales (proveedores + cliente de prueba) la primera
/// vez que arranca la aplicación. Es idempotente: si ya hay datos, no hace nada.
/// </summary>
public static class SembradorDatos
{
    public static async Task SembrarAsync(ArgentaDbContext db)
    {
        if (!await db.Proveedores.AnyAsync())
        {
            db.Proveedores.AddRange(ProveedoresSemilla.ObtenerProveedores());
        }

        if (!await db.Clientes.AnyAsync())
        {
            db.Clientes.Add(new Cliente
            {
                Nombre = "Randall Manuel Lou Meda",
                Nit = "468783-3",
                Activo = true,
                Establecimientos = [new Establecimiento { Numero = 1, Nombre = "Principal", Tipo = TipoCliente.Profesional }],
            });
        }

        await db.SaveChangesAsync();
    }
}
