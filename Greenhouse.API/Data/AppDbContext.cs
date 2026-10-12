using Greenhouse.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Estufa> Estufas => Set<Estufa>();
    public DbSet<Sensor> Sensores => Set<Sensor>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<LeituraAmbiental> LeiturasAmbientais => Set<LeituraAmbiental>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Estufa>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).IsRequired().HasMaxLength(100);
            e.Property(x => x.Localizacao).IsRequired().HasMaxLength(200);
            e.Property(x => x.TipoCultivo).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Sensor>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Estufa)
             .WithMany(x => x.Sensores)
             .HasForeignKey(x => x.EstufaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alerta>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Estufa)
             .WithMany(x => x.Alertas)
             .HasForeignKey(x => x.EstufaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeituraAmbiental>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Estufa)
             .WithMany(x => x.LeiturasAmbientais)
             .HasForeignKey(x => x.EstufaId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
