using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WEB_APIPAISES_DOTNET_REST.Filtros;

/// <summary>
/// Control de acceso a todas las pantallas protegidas, ANTES de que corra el controlador:
/// sin usuario con sesión (sin token) se va a /login. Hay un solo tipo de usuario, así que no
/// hay pantallas restringidas por rol.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext contexto)
    {
        if (string.IsNullOrWhiteSpace(contexto.HttpContext.Session.GetString(SesionWeb.Token)))
        {
            contexto.Result = new RedirectResult("/login");
            return;
        }
        // no-store: después de cerrar sesión, «Atrás» no muestra la página desde la caché
        contexto.HttpContext.Response.Headers.CacheControl = "no-store";
    }
}

/// <summary>
/// Protección CSRF con el patrón «synchronizer token», igual que en el cliente web de TI.2.1.
/// <list type="number">
///   <item>GET (y todo lo que no es POST): la sesión tiene un token para que los formularios lo
///         incluyan (parcial _Csrf).</item>
///   <item>POST: el campo «_csrf» debe ser igual al token de la sesión; si no, se rechaza antes
///         del controlador (POST /login → formulario nuevo; el resto → 403).</item>
/// </list>
/// </summary>
public sealed class CsrfFilter : IAsyncActionFilter
{
    public const string SessionAttribute = "csrfToken";
    public const string Parameter = "_csrf";
    private const int TokenBytes = 32;

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        HttpRequest peticion = contexto.HttpContext.Request;
        ISession sesion = contexto.HttpContext.Session;
        bool esLogin = peticion.Path.Equals("/login", StringComparison.OrdinalIgnoreCase);
        if (HttpMethods.IsPost(peticion.Method))
        {
            if (!await EsValidoAsync(peticion, sesion))
            {
                // Formulario de login vencido o falsificado: se muestra uno nuevo
                contexto.Result = esLogin ? new RedirectResult("/login") : new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }
        }
        else
        {
            // El login necesita sesión para guardar su token; las demás pantallas solo la reutilizan
            await sesion.LoadAsync();
            bool haySesion = sesion.Keys.Any();
            if ((esLogin || haySesion) && string.IsNullOrWhiteSpace(sesion.GetString(SessionAttribute)))
                sesion.SetString(SessionAttribute, NuevoToken());
        }
        await siguiente();
    }

    private static async Task<bool> EsValidoAsync(HttpRequest peticion, ISession sesion)
    {
        string? esperado = sesion.GetString(SessionAttribute);
        if (!peticion.HasFormContentType || string.IsNullOrWhiteSpace(esperado))
            return false;
        IFormCollection formulario = await peticion.ReadFormAsync();
        string enviado = formulario[Parameter].ToString();
        if (enviado.Length == 0)
            return false;
        // Comparación en tiempo constante
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(enviado));
    }

    private static string NuevoToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>Claves de la sesión HTTP del cliente web.</summary>
public static class SesionWeb
{
    public const string Token = "token";
    public const string Usuario = "usuario";
    public const string Nombre = "nombre";
    public const string FlashMensaje = "flashMensaje";
    public const string FlashError = "flashError";
}
