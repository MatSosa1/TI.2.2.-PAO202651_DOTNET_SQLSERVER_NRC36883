namespace WS_APIPAISES_DOTNET_REST.Config;

/// <summary>
/// Configuración del servidor: base de datos y API REST Countries.
/// <para>
/// Cada clave se busca con el orden normal de ASP.NET Core (el último gana):
/// <list type="number">
///   <item><c>appsettings.json</c> (versionado, sin la API key);</item>
///   <item><c>appsettings.{Entorno}.json</c>;</item>
///   <item><c>appsettings.Local.json</c> (no se versiona porque guarda la API key; la plantilla es
///         <c>appsettings.Local.example.json</c>);</item>
///   <item>variables de entorno (por ejemplo <c>RestCountries__ApiKey</c>);</item>
///   <item>argumentos de la línea de comandos (<c>--RestCountries:ApiKey=...</c>).</item>
/// </list>
/// Si una clave no aparece en ninguna fuente se usa el valor por defecto de esta clase.
/// </para>
/// </summary>
public sealed class Configuracion
{
    public const string ConexionSqlServer = "SqlServerConnection";
    public const string ApiUrlClave = "RestCountries:Url";
    public const string ApiKeyClave = "RestCountries:ApiKey";
    public const string ApiTimeoutClave = "RestCountries:TimeoutSegundos";

    /// <summary>Coincide con 03. BDD/docker-compose.yml (usuario sa / clave Root1234!).</summary>
    public const string ConexionPorDefecto =
        "Server=localhost,1433;Database=APIPaisesDB;User Id=sa;Password=Root1234!;"
        + "TrustServerCertificate=True;Encrypt=False;Connect Timeout=5";

    public const string ApiUrlPorDefecto = "https://api.restcountries.com/countries/v5";

    /// <summary>ECUD: el servidor espera la respuesta de la API pública como máximo 15 segundos.</summary>
    public const int TimeoutPorDefecto = 15;

    private readonly IConfiguration configuracion;

    public Configuracion(IConfiguration configuracion)
    {
        this.configuracion = configuracion;
    }

    public string CadenaConexion => Valor(configuracion.GetConnectionString(ConexionSqlServer), ConexionPorDefecto);

    public string ApiUrl
    {
        get
        {
            string url = Valor(configuracion[ApiUrlClave], ApiUrlPorDefecto);
            // Sin barra final para poder concatenar rutas
            return url.EndsWith('/') ? url[..^1] : url;
        }
    }

    public string ApiKey => Valor(configuracion[ApiKeyClave], "");

    public int ApiTimeoutSegundos
    {
        get
        {
            return int.TryParse(configuracion[ApiTimeoutClave], out int segundos) && segundos > 0
                ? segundos
                : TimeoutPorDefecto;
        }
    }

    private static string Valor(string? valor, string porDefecto)
    {
        return string.IsNullOrWhiteSpace(valor) ? porDefecto : valor.Trim();
    }
}
