using Automotora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Automotora.API.Data;

/// <summary>Contexto de Entity Framework Core contra SQL Server.</summary>
public class AutomotoraDbContext : DbContext
{
    public AutomotoraDbContext(DbContextOptions<AutomotoraDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Marca> Marcas => Set<Marca>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Marca>(entidad =>
        {
            entidad.Property(m => m.Nombre)
                   .IsRequired()
                   .HasMaxLength(60);

            // No se permiten dos marcas con el mismo nombre.
            entidad.HasIndex(m => m.Nombre).IsUnique();
        });

        modelBuilder.Entity<Vehiculo>(entidad =>
        {
            entidad.Property(v => v.Modelo)
                   .IsRequired()
                   .HasMaxLength(80);

            entidad.Property(v => v.Color).HasMaxLength(40);
            entidad.Property(v => v.Descripcion).HasMaxLength(1000);
            entidad.Property(v => v.ImagenUrl).HasMaxLength(300);

            // decimal(10,2): hasta 99.999.999,99 — suficiente y evita el warning de precisión de EF.
            entidad.Property(v => v.Precio).HasColumnType("decimal(10,2)");

            entidad.HasOne(v => v.Marca)
                   .WithMany(m => m.Vehiculos)
                   .HasForeignKey(v => v.MarcaId)
                   // Restrict evita que borrar una marca arrastre sus vehículos.
                   .OnDelete(DeleteBehavior.Restrict);

            // Índices sobre los campos por los que más se filtra.
            entidad.HasIndex(v => v.MarcaId);
            entidad.HasIndex(v => v.Anio);
            entidad.HasIndex(v => v.Precio);
        });

        SembrarMarcas(modelBuilder);
    }

    /// <summary>Marcas iniciales para que la base no arranque vacía.</summary>
    private static void SembrarMarcas(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Marca>().HasData(
            new Marca { Id = 1, Nombre = "Chevrolet" },
            new Marca { Id = 2, Nombre = "Fiat" },
            new Marca { Id = 3, Nombre = "Ford" },
            new Marca { Id = 4, Nombre = "Peugeot" },
            new Marca { Id = 5, Nombre = "Renault" },
            new Marca { Id = 6, Nombre = "Suzuki" },
            new Marca { Id = 7, Nombre = "Toyota" },
            new Marca { Id = 8, Nombre = "Volkswagen" }
        );
    }
}
