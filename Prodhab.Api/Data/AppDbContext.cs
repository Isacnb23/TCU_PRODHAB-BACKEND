using Microsoft.EntityFrameworkCore;
using Prodhab.Api.Models;

namespace Prodhab.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Expediente> Expedientes => Set<Expediente>();
    public DbSet<DatosFormulario> DatosFormularios => Set<DatosFormulario>();
    public DbSet<Subsanacion> Subsanaciones => Set<Subsanacion>();
    public DbSet<HistorialExpediente> HistorialExpedientes => Set<HistorialExpediente>();
    public DbSet<Observacion> Observaciones => Set<Observacion>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Expediente>(entity =>
        {
            // El enum se guarda como texto, no como int.
            entity.Property(e => e.Estado)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Único solo cuando ya hay número asignado: los borradores conviven con null.
            entity.HasIndex(e => e.NumeroExpediente)
                .IsUnique()
                .HasFilter("[NumeroExpediente] IS NOT NULL");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DatosFormulario>(entity =>
        {
            entity.Property(d => d.DatosJson)
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(d => new { d.ExpedienteId, d.Paso })
                .IsUnique();

            entity.HasOne(d => d.Expediente)
                .WithMany(e => e.Datos)
                .HasForeignKey(d => d.ExpedienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subsanacion>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Subsanacion_JustificacionOArchivo",
                "([TextoJustificacion] IS NOT NULL AND LEN([TextoJustificacion]) > 0) OR [ArchivoRuta] IS NOT NULL"));

            entity.HasOne(s => s.Expediente)
                .WithMany(e => e.Subsanaciones)
                .HasForeignKey(s => s.ExpedienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<HistorialExpediente>(entity =>
        {
            entity.HasOne(h => h.Expediente)
                .WithMany(e => e.Historial)
                .HasForeignKey(h => h.ExpedienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Observacion>(entity =>
        {
            entity.HasOne(o => o.Expediente)
                .WithMany(e => e.Observaciones)
                .HasForeignKey(o => o.ExpedienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            // FK opcional: si el expediente se borra, no se pierden notificaciones antiguas.
            entity.HasOne(n => n.Expediente)
                .WithMany()
                .HasForeignKey(n => n.ExpedienteId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
