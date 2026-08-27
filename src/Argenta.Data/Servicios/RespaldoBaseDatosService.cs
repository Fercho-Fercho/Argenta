using Argenta.Data.Semilla;
using Microsoft.EntityFrameworkCore;

namespace Argenta.Data.Servicios;

/// <summary>
/// Aplica las migraciones de EF Core al arrancar la aplicación, respaldando
/// primero el archivo SQLite. Si la migración falla, restaura el respaldo
/// para nunca dejar la base de datos en un estado corrupto.
/// </summary>
public static class RespaldoBaseDatosService
{
    public static async Task InicializarBaseDatosAsync(ArgentaDbContext db, string rutaArchivoSqlite)
    {
        string? rutaRespaldo = null;

        if (File.Exists(rutaArchivoSqlite))
        {
            rutaRespaldo = $"{rutaArchivoSqlite}.{DateTime.Now:yyyyMMddHHmmss}.bak";
            File.Copy(rutaArchivoSqlite, rutaRespaldo, overwrite: true);
        }

        try
        {
            await db.Database.MigrateAsync();
            await SembradorDatos.SembrarAsync(db);
        }
        catch
        {
            await db.Database.CloseConnectionAsync();

            if (rutaRespaldo is not null && File.Exists(rutaRespaldo))
            {
                File.Copy(rutaRespaldo, rutaArchivoSqlite, overwrite: true);
            }

            throw;
        }
    }
}
