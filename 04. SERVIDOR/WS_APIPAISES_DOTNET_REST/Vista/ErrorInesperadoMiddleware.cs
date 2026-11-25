using Microsoft.AspNetCore.Diagnostics;
using WS_APIPAISES_DOTNET_REST.Models;

namespace WS_APIPAISES_DOTNET_REST.Vista;

/// <summary>
/// Última red de seguridad (ECUD10, alterno 1): cualquier excepción no prevista se registra en
/// el log y se responde con la estructura uniforme, sin exponer sentencias SQL, excepciones de
/// .NET ni trazas. También da formato a los errores del propio protocolo (ruta inexistente,
/// método no permitido) que ASP.NET Core responde sin cuerpo.
/// </summary>
public static class ErrorInesperadoMiddleware
{
    public static IApplicationBuilder UseErrorInesperado(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(manejador => manejador.Run(async contexto =>
        {
            Exception? error = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
            contexto.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ErrorInesperado").LogError(error, "Error inesperado");
            await JsonResponseView.EscribirErrorAsync(contexto, StatusCodes.Status500InternalServerError,
                Mensajes.ErrorInesperado);
        }));

        // 404 (ruta inexistente), 405 (método no permitido), 415... sin cuerpo: estructura uniforme
        app.UseStatusCodePages(async pagina =>
        {
            HttpContext contexto = pagina.HttpContext;
            int codigo = contexto.Response.StatusCode;
            string mensaje = codigo switch
            {
                StatusCodes.Status404NotFound => Mensajes.RecursoNoEncontrado,
                StatusCodes.Status405MethodNotAllowed => Mensajes.MetodoNoPermitido,
                StatusCodes.Status400BadRequest => Mensajes.DatosNoValidos,
                _ => Mensajes.ErrorInesperado,
            };
            await JsonResponseView.EscribirErrorAsync(contexto, codigo, mensaje);
        });
        return app;
    }
}
