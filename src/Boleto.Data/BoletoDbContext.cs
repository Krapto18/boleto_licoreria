using Boleto.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Boleto.Data;

public class BoletoDbContext(DbContextOptions<BoletoDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Tienda> Tienda => Set<Tienda>();
    public DbSet<CambioPrecio> CambiosPrecio => Set<CambioPrecio>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Producto>(e =>
        {
            e.HasKey(p => p.Id);
            // Sin precisión explícita EF Core avisa y SQL Server redondea mal
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Precio).HasPrecision(10, 2);
            e.Property(p => p.PrecioCombo).HasPrecision(10, 2);
            e.HasIndex(p => p.Grupo);
            e.HasIndex(p => new { p.Stock, p.Orden });
        });

        b.Entity<Tienda>(e =>
        {
            e.HasKey(t => t.Id);
            // Fila única con Id = 1 fijo, no autogenerado.
            e.Property(t => t.Id).ValueGeneratedNever();
        });

        b.Entity<CambioPrecio>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.FechaUtc);
        });
    }
}
