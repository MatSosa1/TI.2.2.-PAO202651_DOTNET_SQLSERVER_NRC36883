using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WS_APIPAISES_DOTNET_REST.Config;
using WS_APIPAISES_DOTNET_REST.DAO;
using WS_APIPAISES_DOTNET_REST.Data;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Utilidades;

namespace WS_APIPAISES_DOTNET_REST.Pruebas.DAO;

/// <summary>
/// Pruebas de los DAO contra la base real de 03. BDD (docker compose up -d). Si SQL Server no
/// está disponible, terminan sin comprobar nada (se avisa en la salida). Solo tocan países cuyo
/// nombre empieza por «ZZ Prueba» y los borran al terminar, así que no alteran los datos importados.
/// </summary>
[Collection("BaseDeDatos")]
public sealed class DaoIntegracionTest : IAsyncLifetime
{
    private const string Prefijo = "ZZ Prueba";

    private static readonly Lazy<bool> Disponible = new(() =>
    {
        try
        {
            using AppDbContext db = NuevoContexto();
            return db.Database.CanConnect();
        }
        catch (Exception)
        {
            return false;
        }
    });

    private readonly AppDbContext db = NuevoContexto();
    private readonly PaisDAO paisDAO;
    private readonly RegionDAO regionDAO;
    private readonly UsuarioDAO usuarioDAO;

    public DaoIntegracionTest()
    {
        paisDAO = new PaisDAO(db, NullLogger<PaisDAO>.Instance);
        regionDAO = new RegionDAO(db);
        usuarioDAO = new UsuarioDAO(db);
    }

    private static AppDbContext NuevoContexto()
    {
        string conexion = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServerConnection")
                          ?? Configuracion.ConexionPorDefecto;
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conexion).Options);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (Disponible.Value)
        {
            await db.Paises.Where(p => p.Nombre.StartsWith(Prefijo)).ExecuteDeleteAsync();
        }
        await db.DisposeAsync();
    }

    private static Pais Pais(string sufijo, string capital, string region, long poblacion)
    {
        return new Pais(Prefijo + " " + sufijo, capital, region, poblacion, "Moneda de prueba (XXX)");
    }

    /// <summary>Equivale a assumeTrue de JUnit: sin base, la prueba no comprueba nada.</summary>
    private static bool SinBase()
    {
        if (!Disponible.Value)
        {
            Console.WriteLine("SQL Server no disponible: se omite la prueba de integración");
        }
        return !Disponible.Value;
    }

    // ------------------------------------------------------------------ Usuarios y regiones

    [Fact]
    public async Task BuscaUsuarioDeLaMigracionYSuHashCoincide()
    {
        if (SinBase()) return;
        Usuario? admin = await usuarioDAO.BuscarPorUsuarioAsync("admin");

        Assert.NotNull(admin);
        Assert.Equal("Administrador", admin.Nombre);
        Assert.True(new HashUtil().Coincide("admin123", admin.ClaveHash));
        Assert.Null(await usuarioDAO.BuscarPorUsuarioAsync("no-existe"));
    }

    [Fact]
    public async Task RegionesDelCatalogo()
    {
        if (SinBase()) return;
        Assert.Equal(6, (await regionDAO.ListarAsync()).Count);
        Assert.True(await regionDAO.ExisteAsync("Europa"));
        Assert.True(await regionDAO.ExisteAsync("europa")); // sin distinguir mayúsculas
        Assert.True(await regionDAO.ExisteAsync("Africa")); // sin distinguir tildes
        Assert.False(await regionDAO.ExisteAsync("Marte"));
        Assert.Equal(-1, await regionDAO.ObtenerIdPorNombreAsync(""));
    }

    // ------------------------------------------------------------------ Mantenimiento (ECUD4)

    [Fact]
    public async Task RegistrarActualizarEliminar()
    {
        if (SinBase()) return;
        Pais pais = Pais("Uno", "Capital uno", "Europa", 100);
        await paisDAO.RegistrarAsync(pais);
        Assert.True(await paisDAO.ExisteAsync(pais.Nombre));

        db.ChangeTracker.Clear();
        // Nombre repetido (sin distinguir mayúsculas): 409 en el controlador
        await Assert.ThrowsAsync<DbUpdateException>(() => paisDAO.RegistrarAsync(Pais("UNO", "Otra", "Asia", 1)));
        db.ChangeTracker.Clear();

        pais.Capital = "Capital nueva";
        pais.Region = "Asia";
        pais.Poblacion = 200;
        Assert.Equal(1, await paisDAO.ActualizarAsync(pais));
        Pais leido = (await paisDAO.BuscarAsync(pais.Nombre, null, null))[0];
        Assert.Equal("Capital nueva", leido.Capital);
        Assert.Equal("Asia", leido.Region);
        Assert.Equal(200, leido.Poblacion);

        Assert.Equal(1, await paisDAO.ActualizarAsync(pais)); // sin cambios sigue contando la fila encontrada
        Assert.Equal(1, await paisDAO.EliminarAsync(pais.Nombre));
        Assert.Equal(0, await paisDAO.EliminarAsync(pais.Nombre)); // país inexistente: 404 en el controlador
        Assert.Equal(0, await paisDAO.ActualizarAsync(pais));
    }

    [Fact]
    public async Task RegistrarConRegionInvalidaFalla()
    {
        if (SinBase()) return;
        await Assert.ThrowsAsync<ArgumentException>(() => paisDAO.RegistrarAsync(Pais("Dos", "X", "Marte", 1)));
    }

    // ------------------------------------------------------------------ Búsqueda (ECUD5, ECUD6)

    [Fact]
    public async Task BuscarPorNombreCapitalYRegion()
    {
        if (SinBase()) return;
        await paisDAO.RegistrarAsync(Pais("Andino", "San Francisco de Quito", "América", 10));
        await paisDAO.RegistrarAsync(Pais("Europeo", "Lisboa", "Europa", 20));
        await paisDAO.RegistrarAsync(Pais("Asiatico", "Tokio", "Asia", 30));

        Assert.Equal(3, (await paisDAO.BuscarAsync(Prefijo, null, null)).Count);
        // Coincidencia parcial sin mayúsculas (con el prefijo: la base puede tener Ecuador importado)
        Assert.Single(await paisDAO.BuscarAsync(Prefijo, "quito", null));
        Assert.Single(await paisDAO.BuscarAsync(Prefijo, null, "america")); // región sin tilde
        Assert.Empty(await paisDAO.BuscarAsync(Prefijo, "lisboa", "Asia")); // todos los criterios a la vez
        Assert.DoesNotContain(await paisDAO.BuscarAsync("%", null, null), p => p.Nombre.StartsWith(Prefijo)); // comodín escapado
        Assert.DoesNotContain(await paisDAO.BuscarAsync("[Z]", null, null), p => p.Nombre.StartsWith(Prefijo));

        List<Pais> ordenados = await paisDAO.BuscarAsync(Prefijo, null, null);
        Assert.Equal(Prefijo + " Andino", ordenados[0].Nombre); // ordenados por nombre
    }

    // ------------------------------------------------------------------ Importación (ECUD2)

    [Fact]
    public async Task ImportarRegistraYLuegoActualiza()
    {
        if (SinBase()) return;
        Pais pais = Pais("Importado", "Capital", "Oceanía", 5);
        Assert.Equal(PaisDAO.Registrado, await paisDAO.ImportarAsync(pais));

        pais.Poblacion = 6;
        Assert.Equal(PaisDAO.Actualizado, await paisDAO.ImportarAsync(pais));
        Assert.Equal(6, (await paisDAO.BuscarAsync(pais.Nombre, null, null))[0].Poblacion);
    }

    [Fact]
    public async Task ImportarTodosSinDuplicarYEnUnaTransaccion()
    {
        if (SinBase()) return;
        await paisDAO.RegistrarAsync(Pais("Existente", "Vieja", "Europa", 1));
        db.ChangeTracker.Clear();

        ResumenImportacion resumen = await paisDAO.ImportarTodosAsync(new List<Pais>
        {
            Pais("Existente", "Nueva", "Europa", 2),
            Pais("Nuevo", "Capital", "África", 3),
            Pais("nuevo", "Repetido por la collation", "África", 4),
        });

        Assert.Equal(3, resumen.Recibidos);
        Assert.Equal(1, resumen.Registrados);
        Assert.Equal(1, resumen.Actualizados);
        Assert.Equal("Nueva", (await paisDAO.BuscarAsync(Prefijo + " Existente", null, null))[0].Capital);
        Assert.Equal(2, (await paisDAO.BuscarAsync(Prefijo, null, null)).Count);
    }

    [Fact]
    public async Task ImportarTodosDeshaceTodoSiFallaUnPais()
    {
        if (SinBase()) return;
        string demasiadoLargo = new('x', 150); // no cabe en NVARCHAR(100)
        var lote = new List<Pais>
        {
            Pais("Valido", "Capital", "Asia", 1),
            new(Prefijo + " Invalido", demasiadoLargo, "Asia", 1, "M"),
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => paisDAO.ImportarTodosAsync(lote));
        Assert.False(await paisDAO.ExisteAsync(Prefijo + " Valido")); // ECUD2, alterno 5: nada queda a medias
    }

    [Fact]
    public async Task ClavesGuardadasParaMarcarLosPaisesDeLaApi()
    {
        if (SinBase()) return;
        await paisDAO.RegistrarAsync(Pais("Marcado", "C", "Asia", 1));
        Assert.Contains(Textos.Clave(Prefijo + " MARCADO"), await paisDAO.ClavesGuardadasAsync());
    }

    // ------------------------------------------------------------------ Informes (ECUD7)

    [Fact]
    public async Task InformesIncluyenLasSeisRegionesYElMasPoblado()
    {
        if (SinBase()) return;
        await paisDAO.RegistrarAsync(Pais("Gigante", "C", "Antártida", 9_000_000_000_000L));
        await paisDAO.RegistrarAsync(Pais("Gigante bis", "C", "Antártida", 9_000_000_000_000L));

        List<ConteoRegion> conteos = await paisDAO.ContarPorRegionAsync();
        Assert.Equal(6, conteos.Count); // regiones sin países incluidas
        for (int i = 1; i < conteos.Count; i++)
        {
            Assert.True(conteos[i - 1].Cantidad >= conteos[i].Cantidad); // de mayor a menor
        }

        Pais? masPoblado = await paisDAO.PaisMasPobladoAsync();
        Assert.Equal(Prefijo + " Gigante", masPoblado!.Nombre); // empate: el primero por nombre
        Assert.Equal("Antártida", masPoblado.Region);

        List<PromedioRegion> promedios = await paisDAO.PromedioPorRegionAsync();
        Assert.Equal(6, promedios.Count);
        Assert.Contains(promedios, p => p.Region == "Antártida" && p.PromedioPoblacion >= 9_000_000_000_000L / 2);
        Assert.True(await paisDAO.ContarAsync() >= 2);
    }
}
