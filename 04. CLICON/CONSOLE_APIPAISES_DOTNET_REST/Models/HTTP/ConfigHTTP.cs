namespace CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;

/// <summary>
/// Dirección del servidor y datos de la sesión. El token se guarda mientras dure la sesión
/// (ECUD1, paso 7) y viaja en el encabezado Authorization.
/// <para>
/// La dirección se toma, en este orden, del argumento del programa, de la variable de entorno
/// <c>SERVIDOR_URL</c> o de <c>cliente.properties</c> (junto al ejecutable).
/// </para>
/// </summary>
public static class ConfigHTTP
{
    /// <summary>Espera máxima de una respuesta del servidor.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);
    /// <summary>Importar todos consulta varias páginas de la API y guarda cerca de 250 países (ERS: máx. 30 s).</summary>
    public static readonly TimeSpan TimeoutImportacion = TimeSpan.FromSeconds(90);

    private const string UrlPorDefecto = "http://localhost:5090/api";

    private static string baseUrl = LeerUrl();

    public static string BaseUrl => baseUrl;

    public static string Token { get; set; } = "";

    /// <summary>Nombre del usuario que devolvió el inicio de sesión.</summary>
    public static string Nombre { get; set; } = "";

    /// <summary>Solo para las pruebas de pruebas/: reemplaza la red por respuestas preparadas.</summary>
    public static HttpMessageHandler? Handler { get; set; }

    public static void SetBaseUrl(string url)
    {
        baseUrl = SinBarraFinal(url.Trim());
    }

    public static bool HaySesion() => Token.Length > 0;

    /// <summary>Borra los datos de la sesión (cerrar sesión o token no válido).</summary>
    public static void Limpiar()
    {
        Token = "";
        Nombre = "";
    }

    private static string LeerUrl()
    {
        string? entorno = Environment.GetEnvironmentVariable("SERVIDOR_URL");
        if (!string.IsNullOrWhiteSpace(entorno))
            return SinBarraFinal(entorno.Trim());

        string archivo = Path.Combine(AppContext.BaseDirectory, "cliente.properties");
        if (File.Exists(archivo))
        {
            foreach (string linea in File.ReadAllLines(archivo))
            {
                string limpia = linea.Trim();
                if (limpia.StartsWith("servidor.url=", StringComparison.Ordinal))
                    return SinBarraFinal(limpia["servidor.url=".Length..].Trim());
            }
        }
        return UrlPorDefecto;
    }

    private static string SinBarraFinal(string url) => url.EndsWith('/') ? url[..^1] : url;
}
