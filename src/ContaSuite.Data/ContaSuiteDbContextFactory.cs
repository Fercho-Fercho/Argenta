using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContaSuite.Data;

/// <summary>
/// Fábrica usada solo en tiempo de diseño por las herramientas de EF Core
/// (`dotnet ef migrations add`, etc.). La aplicación real registra el
/// DbContext mediante <c>ServiceCollectionExtensions.AddContaSuiteData</c>.
/// </summary>
public class ContaSuiteDbContextFactory : IDesignTimeDbContextFactory<ContaSuiteDbContext>
{
    public ContaSuiteDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<ContaSuiteDbContext>()
            .UseSqlite("Data Source=disenio.db")
            .Options;

        return new ContaSuiteDbContext(opciones);
    }
}
