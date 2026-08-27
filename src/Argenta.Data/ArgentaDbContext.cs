using Argenta.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Argenta.Data;

/// <summary>
/// Contexto de EF Core. IMPORTANTE (regla de privacidad, no negociable):
/// esta base de datos SOLO contiene catálogos (Clientes, Proveedores, Tipos
/// de Cambio) y configuración. Nunca se debe agregar aquí una tabla que
/// persista datos de facturas de compras o de ventas: los documentos se
/// procesan en memoria al generar cada reporte y se descartan al terminar.
///
/// Única excepción, muy acotada: <see cref="SeleccionFactura"/> recuerda si
/// el usuario incluyó/excluyó una factura, identificándola solo por un hash
/// opaco (nunca datos de la factura en sí). Ver el comentario de esa clase
/// antes de tocarla.
/// </summary>
public class ArgentaDbContext(DbContextOptions<ArgentaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Establecimiento> Establecimientos => Set<Establecimiento>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();
    public DbSet<ProveedorRevisar> ProveedoresRevisar => Set<ProveedorRevisar>();
    public DbSet<SeleccionFactura> SeleccionesFactura => Set<SeleccionFactura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.Property(c => c.Nombre).IsRequired().HasMaxLength(300);
            e.Property(c => c.Nit).IsRequired().HasMaxLength(30);
            e.HasMany(c => c.Establecimientos)
                .WithOne()
                .HasForeignKey(est => est.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Establecimiento>(e =>
        {
            e.Property(est => est.Nombre).IsRequired().HasMaxLength(300);
            e.HasIndex(est => new { est.ClienteId, est.Numero }).IsUnique();
        });

        modelBuilder.Entity<Proveedor>(e =>
        {
            e.Property(p => p.Nit).IsRequired().HasMaxLength(30);
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(300);
            e.HasIndex(p => p.Nit).IsUnique();
        });

        modelBuilder.Entity<TipoCambio>(e =>
        {
            e.HasIndex(t => t.Fecha).IsUnique();
        });

        modelBuilder.Entity<ProveedorRevisar>(e =>
        {
            e.Property(p => p.Nit).IsRequired().HasMaxLength(30);
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(300);
            e.HasIndex(p => p.Nit).IsUnique();
        });

        modelBuilder.Entity<SeleccionFactura>(e =>
        {
            e.Property(s => s.NitCliente).IsRequired().HasMaxLength(30);
            e.Property(s => s.IdentificadorFactura).IsRequired().HasMaxLength(64); // hash SHA-256 en hexadecimal
            e.HasIndex(s => new { s.NitCliente, s.Anio, s.Mes, s.TipoLibro, s.IdentificadorFactura }).IsUnique();
        });
    }
}
