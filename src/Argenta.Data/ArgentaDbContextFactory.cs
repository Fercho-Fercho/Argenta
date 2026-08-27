using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Argenta.Data;

/// <summary>
/// Fábrica usada solo en tiempo de diseño por las herramientas de EF Core
/// (`dotnet ef migrations add`, etc.). La aplicación real registra el
/// DbContext mediante <c>ServiceCollectionExtensions.AddArgentaData</c>.
/// </summary>
public class ArgentaDbContextFactory : IDesignTimeDbContextFactory<ArgentaDbContext>
{
    public ArgentaDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<ArgentaDbContext>()
            .UseSqlite("Data Source=disenio.db")
            .Options;

        return new ArgentaDbContext(opciones);
    }
}
