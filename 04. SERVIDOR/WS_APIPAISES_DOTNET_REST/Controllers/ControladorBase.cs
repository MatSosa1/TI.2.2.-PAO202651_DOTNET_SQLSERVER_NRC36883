using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Utilidades;
using WS_APIPAISES_DOTNET_REST.Vista;

namespace WS_APIPAISES_DOTNET_REST.Controllers;

/// <summary>
/// Traducción común de las fallas a la respuesta estructurada (ECUD10):
/// <list type="bullet">
///   <item>falla de la API externa: 502 o 404 con el mensaje del ECUD9;</item>
///   <item>falla de SQL Server: 500 «Servicio no disponible, intente más tarde».</item>
/// </list>
/// El detalle técnico solo va al log del servidor.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ControladorBase : ControllerBase
{
    private readonly ILogger log;

    protected ControladorBase(ILogger log)
    {
        this.log = log;
    }

    protected async Task<IActionResult> ResponderAsync(string operacion, Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (ApiExternaException e)
        {
            log.LogWarning("{Operacion}: {Detalle}", operacion, e.Message);
            return JsonResponseView.Error(e.CodigoHttp, e.MensajeUsuario);
        }
        catch (Exception e) when (EsErrorBaseDatos(e))
        {
            log.LogError(e, "{Operacion}: error de base de datos", operacion);
            return JsonResponseView.Error(StatusCodes.Status500InternalServerError, Mensajes.ServicioNoDisponible);
        }
    }

    /// <summary>
    /// Mismas reglas que validan los clientes para el nombre de un país (ECUD3, alterno 3).
    /// </summary>
    /// <returns>el mensaje de error, o null si el nombre es válido</returns>
    protected static string? ErrorNombre(string? nombre)
    {
        if (Textos.Vacio(nombre))
        {
            return Mensajes.IngreseNombrePais;
        }
        if (nombre!.Trim().Length > Textos.MaxLongitud)
        {
            return Mensajes.NombreMuyLargo;
        }
        return null;
    }

    /// <summary>Clave duplicada en SQL Server (2627: PRIMARY KEY/UNIQUE, 2601: índice único).</summary>
    protected static bool EsClaveDuplicada(Exception e)
    {
        for (Exception? causa = e; causa != null; causa = causa.InnerException)
        {
            if (causa is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2627 || sql.Number == 2601))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>SQL Server no responde o rechazó la sentencia (incluye las fallas que EF Core envuelve).</summary>
    private static bool EsErrorBaseDatos(Exception e)
    {
        for (Exception? causa = e; causa != null; causa = causa.InnerException)
        {
            if (causa is DbException or DbUpdateException)
            {
                return true;
            }
        }
        return false;
    }
}
