using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WS_APIPAISES_DOTNET_REST.Services;

/// <summary>
/// Tokens de sesión (ECUD1, ECUD8). El token es el SHA-256 de usuario + hora actual +
/// valor aleatorio, y se guarda en memoria entre los tokens activos. No vence: deja de
/// valer al cerrar sesión o al reiniciar el servidor (decisión del punto de control 1).
/// <para>
/// El registro es estático: todas las instancias (filtro y controladores) comparten
/// los mismos tokens activos.
/// </para>
/// </summary>
public partial class TokenService
{
    /// <summary>64 caracteres hexadecimales: el formato de un SHA-256 (ECUD8, paso 2).</summary>
    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex Formato();

    private static readonly ConcurrentDictionary<string, string> TokensActivos = new();

    private readonly HashUtil hashUtil;

    public TokenService() : this(new HashUtil())
    {
    }

    public TokenService(HashUtil hashUtil)
    {
        this.hashUtil = hashUtil;
    }

    /// <summary>Genera un token nuevo para el usuario y lo registra como activo.</summary>
    public string GenerarToken(string usuario)
    {
        byte[] aleatorio = RandomNumberGenerator.GetBytes(16);
        string semilla = usuario + "|" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "|"
            + Stopwatch.GetTimestamp() + "|" + Convert.ToHexStringLower(aleatorio);
        string token = hashUtil.Sha256(semilla);
        TokensActivos[token] = usuario;
        return token;
    }

    /// <summary>true si el token tiene el formato esperado y está entre los tokens activos.</summary>
    public bool EsValido(string? token)
    {
        return token != null && Formato().IsMatch(token) && TokensActivos.ContainsKey(token);
    }

    /// <summary>Usuario dueño del token, o null si no está activo.</summary>
    public string? UsuarioDe(string? token)
    {
        return token != null && TokensActivos.TryGetValue(token, out string? usuario) ? usuario : null;
    }

    /// <summary>Cierra la sesión: el token deja de ser válido.</summary>
    public void Invalidar(string? token)
    {
        if (token != null)
        {
            TokensActivos.TryRemove(token, out _);
        }
    }

    /// <summary>
    /// Extrae el token del encabezado Authorization. El contrato es «&lt;TOKEN&gt;» (ERS 8.1.4);
    /// también se acepta el prefijo «Bearer » por comodidad de las herramientas de prueba.
    /// </summary>
    public static string? Extraer(string? authorization)
    {
        if (authorization == null)
        {
            return null;
        }
        string valor = authorization.Trim();
        if (valor.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            valor = valor[7..].Trim();
        }
        return valor.Length == 0 ? null : valor;
    }
}
