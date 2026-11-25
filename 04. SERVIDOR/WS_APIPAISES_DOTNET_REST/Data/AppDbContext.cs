using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;

namespace WS_APIPAISES_DOTNET_REST.Data;

/// <summary>
/// Contexto de EF Core de APIPaisesDB (ERS §9.1.5, diagrama de clases Servidor_APIPaises).
/// <para>
/// El esquema se crea con la migración <c>InitialCreate</c> (<c>dotnet ef database update</c>,
/// o <c>Update-Database</c> en Visual Studio). <c>03. BDD/APIPaisesDB.sql</c> es el mismo script,
/// generado con <c>dotnet ef migrations script --idempotent</c>.
/// </para>
/// <para>
/// Todas las columnas de texto usan la collation <c>Latin1_General_100_CI_AI</c>: no distingue
/// mayúsculas ni tildes, así que la búsqueda «quito» encuentra «Quito» y el nombre del país es
/// único aunque cambie la tilde (ECUD5).
/// </para>
/// </summary>
public class AppDbContext : DbContext
{
    public const string Collation = "Latin1_General_100_CI_AI";

    public AppDbContext(DbContextOptions<AppDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Region> Regiones => Set<Region>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // Catálogo de las seis regiones en español. ConsumoAPIPaises traduce la región de la
        // API y RegionDAO la busca por nombre.
        modelo.Entity<Region>(region =>
        {
            region.ToTable("Regiones");
            region.HasKey(r => r.IdRegion).HasName("PK_REGIONES");
            region.Property(r => r.IdRegion).UseIdentityColumn();
            region.Property(r => r.Nombre).HasMaxLength(20).IsRequired().UseCollation(Collation);
            region.HasIndex(r => r.Nombre).IsUnique().HasDatabaseName("AK_REGIONES_NOMBRE");

            // Regiones válidas (ECUD, parámetros generales). El servidor las busca por nombre.
            region.HasData(
                new Region(1, "América"),
                new Region(2, "Europa"),
                new Region(3, "Asia"),
                new Region(4, "África"),
                new Region(5, "Oceanía"),
                new Region(6, "Antártida"));
        });

        // El nombre en español es la clave primaria (RNF-006). Los países sin capital o sin
        // moneda se guardan con «Sin capital» o «Sin moneda» (ECUD9), por eso ninguna columna es nula.
        modelo.Entity<Pais>(pais =>
        {
            pais.ToTable("Paises", tabla =>
                tabla.HasCheckConstraint("CK_PAISES_POBLACION", "[Poblacion] >= 0"));
            pais.HasKey(p => p.Nombre).HasName("PK_PAISES");
            pais.Property(p => p.Nombre).HasMaxLength(100).UseCollation(Collation);
            pais.Property(p => p.Capital).HasMaxLength(100).IsRequired().UseCollation(Collation);
            pais.Property(p => p.Moneda).HasMaxLength(100).IsRequired().UseCollation(Collation);
            pais.Property(p => p.Poblacion).HasColumnType("bigint");
            pais.HasIndex(p => p.IdRegion).HasDatabaseName("IX_PAISES_REGION");
            pais.HasIndex(p => p.Poblacion).HasDatabaseName("IX_PAISES_POBLACION");
            pais.HasOne(p => p.RegionCatalogo)
                .WithMany(r => r.Paises)
                .HasForeignKey(p => p.IdRegion)
                .HasConstraintName("FK_PAISES_REGIONES")
                // No se puede borrar una región que tenga países
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Solo se cargan por script (no hay mantenimiento de usuarios).
        // ClaveHash = SHA-256 de la clave en hexadecimal (64 caracteres).
        modelo.Entity<Usuario>(usuario =>
        {
            usuario.ToTable("Usuarios");
            usuario.HasKey(u => u.IdUsuario).HasName("PK_USUARIOS");
            usuario.Property(u => u.IdUsuario).UseIdentityColumn();
            usuario.Property(u => u.NombreUsuario).HasColumnName("Usuario").HasMaxLength(50).IsRequired()
                .UseCollation(Collation);
            usuario.Property(u => u.ClaveHash).HasColumnType("char(64)").IsRequired();
            usuario.Property(u => u.Nombre).HasMaxLength(100).IsRequired().UseCollation(Collation);
            usuario.HasIndex(u => u.NombreUsuario).IsUnique().HasDatabaseName("AK_USUARIOS_USUARIO");

            // Usuarios de prueba (usuario / clave): admin / admin123 y msosa / msosa.
            // El hash es el que produce HashUtil.Sha256 (y LOWER(CONVERT(char(64), HASHBYTES('SHA2_256', ...), 2))).
            usuario.HasData(
                new Usuario(1, "admin", "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", "Administrador"),
                new Usuario(2, "msosa", "cb84af049da6d367b89c4565c747db7a50054d15226dd353f36dc6c26bb3fb9e", "Mateo Sosa"));
        });
    }
}
