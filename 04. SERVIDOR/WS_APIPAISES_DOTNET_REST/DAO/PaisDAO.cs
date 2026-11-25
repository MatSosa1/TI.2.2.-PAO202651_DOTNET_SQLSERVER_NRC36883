using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.Data;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;
using WS_APIPAISES_DOTNET_REST.Utilidades;

namespace WS_APIPAISES_DOTNET_REST.DAO;

/// <summary>
/// Acceso a la tabla Paises: mantenimiento (ECUD4), búsqueda y filtro (ECUD5, ECUD6),
/// importación (ECUD2) e informes (ECUD7).
/// <para>
/// Las comparaciones de texto las hace SQL Server con la collation Latin1_General_100_CI_AI,
/// así que no distinguen mayúsculas ni tildes.
/// </para>
/// </summary>
public class PaisDAO
{
    /// <summary>Resultado de <see cref="ImportarAsync"/>.</summary>
    public const string Registrado = "registrado";
    public const string Actualizado = "actualizado";

    private readonly AppDbContext db;
    private readonly ILogger<PaisDAO> log;

    public PaisDAO(AppDbContext db, ILogger<PaisDAO> log)
    {
        this.db = db;
        this.log = log;
    }

    // ------------------------------------------------------------------ Consultas

    /// <summary>Todos los países ordenados por nombre (ECUD4, Listar).</summary>
    public Task<List<Pais>> ListarAsync()
    {
        return Proyectar(db.Paises.AsNoTracking()).OrderBy(p => p.Nombre).ToListAsync();
    }

    /// <summary>
    /// Países que cumplen todos los criterios con valor: nombre y capital por coincidencia
    /// parcial, región por igualdad. Sin criterios devuelve todos (ECUD5, alterno 3).
    /// </summary>
    public Task<List<Pais>> BuscarAsync(string? nombre, string? capital, string? region)
    {
        IQueryable<Pais> consulta = db.Paises.AsNoTracking();
        if (!Textos.Vacio(nombre))
        {
            string patron = Contiene(nombre!);
            consulta = consulta.Where(p => EF.Functions.Like(p.Nombre, patron, "!"));
        }
        if (!Textos.Vacio(capital))
        {
            string patron = Contiene(capital!);
            consulta = consulta.Where(p => EF.Functions.Like(p.Capital, patron, "!"));
        }
        if (!Textos.Vacio(region))
        {
            string buscada = region!.Trim();
            consulta = consulta.Where(p => p.RegionCatalogo!.Nombre == buscada);
        }
        return Proyectar(consulta).OrderBy(p => p.Nombre).ToListAsync();
    }

    public Task<bool> ExisteAsync(string nombre)
    {
        return db.Paises.AsNoTracking().AnyAsync(p => p.Nombre == nombre);
    }

    /// <summary>
    /// Claves de comparación (<see cref="Textos.Clave"/>) de los nombres guardados, para marcar
    /// qué países de la API ya están en la base (ECUD3, paso 6).
    /// </summary>
    public async Task<HashSet<string>> ClavesGuardadasAsync()
    {
        List<string> nombres = await db.Paises.AsNoTracking().Select(p => p.Nombre).ToListAsync();
        return nombres.Select(Textos.Clave).ToHashSet();
    }

    public Task<int> ContarAsync()
    {
        return db.Paises.CountAsync();
    }

    // ------------------------------------------------------------------ Mantenimiento

    /// <summary>
    /// Registra un país nuevo (ECUD4, Registrar).
    /// </summary>
    /// <exception cref="ArgumentException">si la región no está en el catálogo</exception>
    /// <exception cref="DbUpdateException">si ya existe un país con ese nombre (clave primaria)</exception>
    public async Task RegistrarAsync(Pais pais)
    {
        int idRegion = await IdRegionObligatoriaAsync(pais.Region);
        db.Paises.Add(NuevaFila(pais, idRegion));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Actualiza capital, región, población y moneda del país con ese nombre (ECUD4, Actualizar).
    /// </summary>
    /// <returns>filas encontradas: 0 si el país no existe</returns>
    /// <exception cref="ArgumentException">si la región no está en el catálogo</exception>
    public async Task<int> ActualizarAsync(Pais pais)
    {
        int idRegion = await IdRegionObligatoriaAsync(pais.Region);
        return await db.Paises
            .Where(p => p.Nombre == pais.Nombre)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Capital, pais.Capital)
                .SetProperty(p => p.IdRegion, idRegion)
                .SetProperty(p => p.Poblacion, pais.Poblacion)
                .SetProperty(p => p.Moneda, pais.Moneda));
    }

    /// <returns>filas eliminadas: 0 si el país no existe (ECUD4, Eliminar).</returns>
    public Task<int> EliminarAsync(string nombre)
    {
        return db.Paises.Where(p => p.Nombre == nombre).ExecuteDeleteAsync();
    }

    // ------------------------------------------------------------------ Importación (ECUD2)

    /// <summary>
    /// Guarda un país dentro de una transacción: lo registra si no existe o actualiza sus datos
    /// si ya existe (ECUD2, paso 7). La fila queda bloqueada (UPDLOCK, HOLDLOCK) hasta confirmar.
    /// </summary>
    /// <returns><see cref="Registrado"/> o <see cref="Actualizado"/></returns>
    public async Task<string> ImportarAsync(Pais pais)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync();
        try
        {
            int idRegion = await IdRegionObligatoriaAsync(pais.Region);
            Pais? existente = await db.Paises
                .FromSqlInterpolated($"select * from Paises with (updlock, holdlock) where Nombre = {pais.Nombre}")
                .FirstOrDefaultAsync();
            if (existente == null)
            {
                db.Paises.Add(NuevaFila(pais, idRegion));
            }
            else
            {
                CopiarDatos(existente, pais, idRegion);
            }
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            return existente == null ? Registrado : Actualizado;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Importa todos los países en una sola transacción: carga una vez los países existentes,
    /// guarda todas las filas con un solo SaveChanges (EF Core las envía en lotes) y confirma al
    /// final. Si algo falla se deshace todo y ningún país queda importado a medias (ECUD2, alterno 5).
    /// </summary>
    public async Task<ResumenImportacion> ImportarTodosAsync(List<Pais> paises)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync();
        try
        {
            Dictionary<string, int> regiones = (await db.Regiones.AsNoTracking().ToListAsync())
                .ToDictionary(r => Textos.Clave(r.Nombre), r => r.IdRegion);
            // Filas existentes, con seguimiento para actualizarlas (la clave no distingue tildes ni mayúsculas)
            Dictionary<string, Pais> existentes = new();
            foreach (Pais fila in await db.Paises.ToListAsync())
            {
                existentes.TryAdd(Textos.Clave(fila.Nombre), fila);
            }

            // Un solo registro por nombre
            var unicos = new Dictionary<string, Pais>();
            foreach (Pais pais in paises)
            {
                unicos.TryAdd(Textos.Clave(pais.Nombre), pais);
            }

            int registrados = 0;
            int actualizados = 0;
            foreach (var (clave, pais) in unicos)
            {
                if (!regiones.TryGetValue(Textos.Clave(pais.Region), out int idRegion))
                {
                    log.LogWarning("País omitido por región desconocida: {Pais}", pais.Nombre);
                    continue;
                }
                if (existentes.TryGetValue(clave, out Pais? existente))
                {
                    CopiarDatos(existente, pais, idRegion);
                    actualizados++;
                }
                else
                {
                    Pais nueva = NuevaFila(pais, idRegion);
                    db.Paises.Add(nueva);
                    existentes[clave] = nueva;
                    registrados++;
                }
            }
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            return new ResumenImportacion(paises.Count, registrados, actualizados);
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    // ------------------------------------------------------------------ Informes (ECUD7)

    /// <summary>Incluye las regiones sin países y las ordena de mayor a menor (ECUD7, paso 4).</summary>
    public async Task<List<ConteoRegion>> ContarPorRegionAsync()
    {
        var filas = await db.Regiones.AsNoTracking()
            .Select(r => new { r.IdRegion, r.Nombre, Cantidad = r.Paises.Count() })
            .OrderByDescending(r => r.Cantidad).ThenBy(r => r.IdRegion)
            .ToListAsync();
        return filas.Select(r => new ConteoRegion(r.Nombre, r.Cantidad)).ToList();
    }

    /// <summary>País con mayor población, o null si no hay países. Empate: el primero por nombre (ECUD7, paso 5).</summary>
    public Task<Pais?> PaisMasPobladoAsync()
    {
        return Proyectar(db.Paises.AsNoTracking())
            .OrderByDescending(p => p.Poblacion).ThenBy(p => p.Nombre)
            .FirstOrDefaultAsync();
    }

    /// <summary>Redondeado a entero; 0 en las regiones sin países (ECUD7, paso 6 y alterno 3).</summary>
    public async Task<List<PromedioRegion>> PromedioPorRegionAsync()
    {
        var filas = await db.Regiones.AsNoTracking()
            .OrderBy(r => r.IdRegion)
            .Select(r => new
            {
                r.Nombre,
                Cantidad = r.Paises.Count(),
                Suma = r.Paises.Sum(p => (long?)p.Poblacion) ?? 0L,
            })
            .ToListAsync();
        return filas.Select(r => new PromedioRegion(r.Nombre,
                r.Cantidad == 0 ? 0 : (long)Math.Round((decimal)r.Suma / r.Cantidad, MidpointRounding.AwayFromZero)))
            .ToList();
    }

    // ------------------------------------------------------------------ Apoyo

    /// <summary>Pais con el nombre de su región (join con Regiones).</summary>
    private static IQueryable<Pais> Proyectar(IQueryable<Pais> consulta)
    {
        return consulta.Select(p => new Pais
        {
            Nombre = p.Nombre,
            Capital = p.Capital,
            IdRegion = p.IdRegion,
            Region = p.RegionCatalogo!.Nombre,
            Poblacion = p.Poblacion,
            Moneda = p.Moneda,
        });
    }

    private static Pais NuevaFila(Pais pais, int idRegion)
    {
        return new Pais
        {
            Nombre = pais.Nombre,
            Capital = pais.Capital,
            IdRegion = idRegion,
            Poblacion = Math.Max(0, pais.Poblacion),
            Moneda = pais.Moneda,
        };
    }

    private static void CopiarDatos(Pais destino, Pais origen, int idRegion)
    {
        destino.Capital = origen.Capital;
        destino.IdRegion = idRegion;
        destino.Poblacion = Math.Max(0, origen.Poblacion);
        destino.Moneda = origen.Moneda;
    }

    private async Task<int> IdRegionObligatoriaAsync(string? region)
    {
        int id = await RegionDAO.IdPorNombreAsync(db, region);
        if (id <= 0)
        {
            throw new ArgumentException("Región no válida: " + region);
        }
        return id;
    }

    /// <summary>Patrón LIKE «contiene», escapando los comodines que escriba el usuario.</summary>
    private static string Contiene(string texto)
    {
        string escapado = texto.Trim().Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![");
        return "%" + escapado + "%";
    }
}
