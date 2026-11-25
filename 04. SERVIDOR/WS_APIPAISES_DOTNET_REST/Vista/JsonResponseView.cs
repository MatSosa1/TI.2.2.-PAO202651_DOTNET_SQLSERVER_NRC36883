using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Mvc;
using WS_APIPAISES_DOTNET_REST.Models.DTO;

namespace WS_APIPAISES_DOTNET_REST.Vista;

/// <summary>
/// «Vista» del servidor según el diagrama de arquitectura MVC: convierte el resultado de un
/// controlador en una respuesta HTTP JSON con la estructura estado / mensaje / datos y el
/// código HTTP correspondiente (ECUD10). La serialización la hace System.Text.Json.
/// </summary>
public static class JsonResponseView
{
    /// <summary>
    /// Nombres en camelCase (estado, mensaje, datos) y tildes sin escapar, para que los clientes
    /// lean «Sesión» y no «Sesión». Lo usan también el filtro y el manejador de errores.
    /// </summary>
    public static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <summary>200 con estado OK.</summary>
    public static IActionResult Ok(string mensaje, object? datos)
    {
        return Construir(StatusCodes.Status200OK, RespuestaEstructurada.CrearOk(mensaje, datos));
    }

    /// <summary>201 con estado OK (ECUD4, Registrar).</summary>
    public static IActionResult Creado(string mensaje, object? datos)
    {
        return Construir(StatusCodes.Status201Created, RespuestaEstructurada.CrearOk(mensaje, datos));
    }

    /// <summary>Estado ERROR, sin datos, con el código HTTP indicado.</summary>
    public static IActionResult Error(int codigoHttp, string mensaje)
    {
        return Construir(codigoHttp, RespuestaEstructurada.CrearError(mensaje));
    }

    /// <summary>Escribe un ERROR directamente en la respuesta (middleware, fuera de MVC).</summary>
    public static Task EscribirErrorAsync(HttpContext contexto, int codigoHttp, string mensaje)
    {
        contexto.Response.StatusCode = codigoHttp;
        contexto.Response.ContentType = "application/json; charset=utf-8";
        return contexto.Response.WriteAsync(
            JsonSerializer.Serialize(RespuestaEstructurada.CrearError(mensaje), Opciones));
    }

    private static IActionResult Construir(int codigoHttp, RespuestaEstructurada respuesta)
    {
        return new JsonResult(respuesta, Opciones)
        {
            StatusCode = codigoHttp,
            ContentType = "application/json; charset=utf-8",
        };
    }
}
