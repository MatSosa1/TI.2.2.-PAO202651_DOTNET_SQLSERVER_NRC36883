using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.DAO;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Utilidades;
using WS_APIPAISES_DOTNET_REST.Vista;

namespace WS_APIPAISES_DOTNET_REST.Controllers;

/// <summary>
/// Países: listar, registrar, actualizar y eliminar (ECUD4), buscar y filtrar (ECUD5, ECUD6),
/// consultar en la API (ECUD3) e importar (ECUD2).
/// </summary>
[Route("api/Paises")]
public class PaisesController : ControladorBase
{
    private readonly ConsumoAPIPaises consumoAPIPaises;
    private readonly PaisDAO paisDAO;
    private readonly RegionDAO regionDAO;

    public PaisesController(ConsumoAPIPaises consumoAPIPaises, PaisDAO paisDAO, RegionDAO regionDAO,
        ILogger<PaisesController> log) : base(log)
    {
        this.consumoAPIPaises = consumoAPIPaises;
        this.paisDAO = paisDAO;
        this.regionDAO = regionDAO;
    }

    /// <summary>GET /api/Paises: todos los países guardados, ordenados por nombre (ECUD4, Listar).</summary>
    [HttpGet]
    public Task<IActionResult> Listar()
    {
        return ResponderAsync("listar", async () =>
            JsonResponseView.Ok(Mensajes.PaisesListados, await paisDAO.ListarAsync()));
    }

    /// <summary>
    /// GET /api/Paises/buscar?nombre=&amp;capital=&amp;region=. Buscar (ECUD5) cuando hay nombre o
    /// capital; Filtrar (ECUD6) cuando solo llega la región.
    /// </summary>
    [HttpGet("buscar")]
    public Task<IActionResult> Buscar([FromQuery] string? nombre, [FromQuery] string? capital,
        [FromQuery] string? region)
    {
        return ResponderAsync("buscar", async () =>
        {
            if (!Textos.Vacio(region) && !await regionDAO.ExisteAsync(region!))
            {
                return JsonResponseView.Error(StatusCodes.Status400BadRequest, Mensajes.RegionNoValida);
            }
            List<Pais> paises = await paisDAO.BuscarAsync(nombre, capital, region);
            bool soloFiltro = Textos.Vacio(nombre) && Textos.Vacio(capital) && !Textos.Vacio(region);
            return JsonResponseView.Ok(soloFiltro ? Mensajes.FiltroAplicado : Mensajes.BusquedaRealizada,
                new ResultadoBusqueda(paises));
        });
    }

    /// <summary>
    /// GET /api/Paises/externo/{nombre}: consulta en la API REST Countries y marca los países
    /// que ya están guardados (ECUD3).
    /// </summary>
    [HttpGet("externo/{nombre}")]
    public Task<IActionResult> ConsultarExterno(string nombre)
    {
        string? error = ErrorNombre(nombre);
        if (error != null)
        {
            return Task.FromResult(JsonResponseView.Error(StatusCodes.Status400BadRequest, error));
        }
        return ResponderAsync("consultarExterno", async () =>
        {
            List<PaisDTO> paises = await consumoAPIPaises.ObtenerPorNombreAsync(nombre);
            HashSet<string> guardados = await paisDAO.ClavesGuardadasAsync();
            foreach (PaisDTO pais in paises)
            {
                pais.Guardado = guardados.Contains(Textos.Clave(pais.Nombre));
            }
            return JsonResponseView.Ok(Mensajes.ConsultaApiExitosa, paises);
        });
    }

    /// <summary>
    /// POST /api/Paises/importar {consulta, nombre}: guarda el país elegido; lo registra si es
    /// nuevo o actualiza sus datos si ya existe (ECUD2).
    /// </summary>
    [HttpPost("importar")]
    public Task<IActionResult> Importar([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SolicitudPais? solicitud)
    {
        string? error = ErrorSolicitud(solicitud);
        if (error != null)
        {
            return Task.FromResult(JsonResponseView.Error(StatusCodes.Status400BadRequest, error));
        }
        return ResponderAsync("importar", async () =>
        {
            Pais pais = Pais.Desde(await consumoAPIPaises.ObtenerPaisAsync(solicitud!.Consulta, solicitud.Nombre!));
            string accion = await paisDAO.ImportarAsync(pais);
            return JsonResponseView.Ok(Mensajes.PaisImportado, new ResultadoImportacion(pais, accion));
        });
    }

    /// <summary>
    /// POST /api/Paises/importar/todos: importa todos los países de la API en una sola
    /// transacción, sin duplicar (ERS §7.2; endpoint agregado en el punto de control 1).
    /// </summary>
    [HttpPost("importar/todos")]
    public Task<IActionResult> ImportarTodos()
    {
        return ResponderAsync("importarTodos", async () =>
        {
            List<Pais> paises = (await consumoAPIPaises.ObtenerTodosAsync()).Select(Pais.Desde).ToList();
            ResumenImportacion resumen = await paisDAO.ImportarTodosAsync(paises);
            return JsonResponseView.Ok(Mensajes.ImportacionFinalizada, resumen);
        });
    }

    /// <summary>
    /// POST /api/Paises {consulta, nombre}: registra un país nuevo con los datos de la API.
    /// 409 si ya existe (ECUD4, Registrar).
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Registrar([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SolicitudPais? solicitud)
    {
        string? error = ErrorSolicitud(solicitud);
        if (error != null)
        {
            return Task.FromResult(JsonResponseView.Error(StatusCodes.Status400BadRequest, error));
        }
        return ResponderAsync("registrar", async () =>
        {
            Pais pais = Pais.Desde(await consumoAPIPaises.ObtenerPaisAsync(solicitud!.Consulta, solicitud.Nombre!));
            if (await paisDAO.ExisteAsync(pais.Nombre))
            {
                return JsonResponseView.Error(StatusCodes.Status409Conflict, Mensajes.PaisDuplicado);
            }
            try
            {
                await paisDAO.RegistrarAsync(pais);
            }
            catch (DbUpdateException e) when (EsClaveDuplicada(e))
            {
                // Otro usuario lo registró entre la verificación y la inserción
                return JsonResponseView.Error(StatusCodes.Status409Conflict, Mensajes.PaisDuplicado);
            }
            return JsonResponseView.Creado(Mensajes.PaisRegistrado, pais);
        });
    }

    /// <summary>
    /// PUT /api/Paises/{nombre}: vuelve a consultar el país en la API y actualiza su capital,
    /// región, población y moneda (ECUD4, Actualizar).
    /// </summary>
    [HttpPut("{nombre}")]
    public Task<IActionResult> Actualizar(string nombre)
    {
        string? error = ErrorNombre(nombre);
        if (error != null)
        {
            return Task.FromResult(JsonResponseView.Error(StatusCodes.Status400BadRequest, error));
        }
        return ResponderAsync("actualizar", async () =>
        {
            if (!await paisDAO.ExisteAsync(nombre))
            {
                return JsonResponseView.Error(StatusCodes.Status404NotFound, Mensajes.PaisNoExiste);
            }
            Pais pais = Pais.Desde(await consumoAPIPaises.ObtenerPaisAsync(null, nombre));
            pais.Nombre = nombre.Trim();
            if (await paisDAO.ActualizarAsync(pais) == 0)
            {
                return JsonResponseView.Error(StatusCodes.Status404NotFound, Mensajes.PaisNoExiste);
            }
            return JsonResponseView.Ok(Mensajes.PaisActualizado, pais);
        });
    }

    /// <summary>DELETE /api/Paises/{nombre}: elimina el país de la base local (ECUD4, Eliminar).</summary>
    [HttpDelete("{nombre}")]
    public Task<IActionResult> Eliminar(string nombre)
    {
        return ResponderAsync("eliminar", async () => await paisDAO.EliminarAsync(nombre) == 0
            ? JsonResponseView.Error(StatusCodes.Status404NotFound, Mensajes.PaisNoExiste)
            : JsonResponseView.Ok(Mensajes.PaisEliminado, null));
    }

    /// <summary>Valida el cuerpo {consulta, nombre}; la consulta puede faltar (se usa el nombre).</summary>
    private static string? ErrorSolicitud(SolicitudPais? solicitud)
    {
        if (solicitud == null)
        {
            return Mensajes.IngreseNombrePais;
        }
        string? error = ErrorNombre(solicitud.Nombre);
        if (error == null && !Textos.Vacio(solicitud.Consulta))
        {
            error = ErrorNombre(solicitud.Consulta);
        }
        return error;
    }
}
