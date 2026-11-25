using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WS_APIPAISES_DOTNET_REST.DAO;
using WS_APIPAISES_DOTNET_REST.Filtros;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Utilidades;
using WS_APIPAISES_DOTNET_REST.Vista;

namespace WS_APIPAISES_DOTNET_REST.Controllers;

/// <summary>Iniciar y cerrar sesión (RQF-001, ECUD1).</summary>
[Route("api/Login")]
public class LoginController : ControladorBase
{
    private readonly UsuarioDAO usuarioDAO;
    private readonly TokenService tokenService;
    private readonly HashUtil hashUtil;

    public LoginController(UsuarioDAO usuarioDAO, TokenService tokenService, HashUtil hashUtil,
        ILogger<LoginController> log) : base(log)
    {
        this.usuarioDAO = usuarioDAO;
        this.tokenService = tokenService;
        this.hashUtil = hashUtil;
    }

    /// <summary>
    /// POST /api/Login/login {usuario, clave}. Compara el SHA-256 de la clave con el guardado y,
    /// si coincide, entrega un token nuevo y el nombre del usuario. No indica qué dato falló.
    /// </summary>
    [HttpPost("login")]
    [SinToken]
    public Task<IActionResult> Login([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LoginRequest? solicitud)
    {
        string? usuario = solicitud?.Usuario;
        string? clave = solicitud?.Clave;
        if (Textos.Vacio(usuario))
        {
            return Task.FromResult(JsonResponseView.Error(StatusCodes.Status400BadRequest, Mensajes.IngreseUsuario));
        }
        if (string.IsNullOrEmpty(clave))
        {
            return Task.FromResult(JsonResponseView.Error(StatusCodes.Status400BadRequest, Mensajes.IngreseClave));
        }

        return ResponderAsync("login", async () =>
        {
            Usuario? registrado = await usuarioDAO.BuscarPorUsuarioAsync(usuario!.Trim());
            if (registrado == null || !hashUtil.Coincide(clave, registrado.ClaveHash))
            {
                return JsonResponseView.Error(StatusCodes.Status401Unauthorized, Mensajes.CredencialesIncorrectas);
            }
            string token = tokenService.GenerarToken(registrado.NombreUsuario);
            return JsonResponseView.Ok(Mensajes.LoginExitoso, new LoginResponse(token, registrado.Nombre));
        });
    }

    /// <summary>POST /api/Login/logout. El TokenFilter ya comprobó que el token está activo.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        tokenService.Invalidar(TokenService.Extraer(Request.Headers.Authorization.ToString()));
        return JsonResponseView.Ok(Mensajes.SesionCerrada, null);
    }
}
