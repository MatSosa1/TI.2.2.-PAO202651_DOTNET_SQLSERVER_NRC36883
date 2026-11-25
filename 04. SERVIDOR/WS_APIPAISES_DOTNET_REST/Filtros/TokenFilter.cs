using Microsoft.AspNetCore.Mvc.Filters;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Vista;

namespace WS_APIPAISES_DOTNET_REST.Filtros;

/// <summary>Marca la única operación pública: el inicio de sesión.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SinTokenAttribute : Attribute
{
}

/// <summary>
/// Validar Token (RQF-008, ECUD8): toda petición, salvo el inicio de sesión, debe traer en el
/// encabezado Authorization un token de 64 caracteres hexadecimales que esté entre los activos.
/// Si no, la operación no se ejecuta y se responde 401 «Token inválido o sesión no activa».
/// <para>
/// Es un filtro global de MVC: corre después de elegir la ruta, así que una ruta inexistente
/// responde 404 y un método no permitido 405, igual que en el servidor Java.
/// </para>
/// </summary>
public class TokenFilter : IAuthorizationFilter
{
    /// <summary>Clave de HttpContext.Items con el usuario autenticado, por si un controlador lo necesita.</summary>
    public const string Usuario = "apipaises.usuario";

    private readonly TokenService tokenService;

    public TokenFilter(TokenService tokenService)
    {
        this.tokenService = tokenService;
    }

    public void OnAuthorization(AuthorizationFilterContext contexto)
    {
        if (contexto.ActionDescriptor.EndpointMetadata.OfType<SinTokenAttribute>().Any())
        {
            return;
        }
        string? token = TokenService.Extraer(contexto.HttpContext.Request.Headers.Authorization.ToString());
        if (!tokenService.EsValido(token))
        {
            contexto.Result = JsonResponseView.Error(StatusCodes.Status401Unauthorized, Mensajes.TokenInvalido);
            return;
        }
        contexto.HttpContext.Items[Usuario] = tokenService.UsuarioDe(token);
    }
}
