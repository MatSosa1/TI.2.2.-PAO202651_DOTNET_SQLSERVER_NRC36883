using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.Data;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;

namespace WS_APIPAISES_DOTNET_REST.DAO;

/// <summary>
/// Acceso al catálogo Regiones. La búsqueda por nombre no distingue mayúsculas ni tildes
/// (collation de la columna), así que «europa» y «Europa» son la misma región.
/// </summary>
public class RegionDAO
{
    private readonly AppDbContext db;

    public RegionDAO(AppDbContext db)
    {
        this.db = db;
    }

    /// <summary>true si la región está en el catálogo (ECUD5 y ECUD6, «Región no válida»).</summary>
    public async Task<bool> ExisteAsync(string nombre)
    {
        return await ObtenerIdPorNombreAsync(nombre) > 0;
    }

    /// <summary>Id de la región, o -1 si no existe.</summary>
    public Task<int> ObtenerIdPorNombreAsync(string? nombre)
    {
        return IdPorNombreAsync(db, nombre);
    }

    public Task<List<Region>> ListarAsync()
    {
        return db.Regiones.AsNoTracking().OrderBy(r => r.IdRegion).ToListAsync();
    }

    /// <summary>Versión que reutiliza un contexto abierto (por ejemplo, dentro de una transacción de PaisDAO).</summary>
    internal static async Task<int> IdPorNombreAsync(AppDbContext db, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return -1;
        }
        string buscado = nombre.Trim();
        int? id = await db.Regiones.AsNoTracking()
            .Where(r => r.Nombre == buscado)
            .Select(r => (int?)r.IdRegion)
            .FirstOrDefaultAsync();
        return id ?? -1;
    }
}
