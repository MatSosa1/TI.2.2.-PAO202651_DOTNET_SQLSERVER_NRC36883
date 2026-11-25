using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WEB_APIPAISES_DOTNET_REST.Models.Dto;

namespace WEB_APIPAISES_DOTNET_REST.Models.Servicios;

/// <summary>Operación correcta: el mensaje del servidor y los datos.</summary>
public record Resultado<T>(string Mensaje, T? Datos);

/// <summary>
/// La operación no se completó. El mensaje ya está listo para mostrarlo: es el del servidor
/// (estado ERROR) o uno del cliente (sin conexión, respuesta ilegible). <see cref="SesionNoValida"/>
/// indica que el servidor rechazó el token (ECUD8): hay que volver al login.
/// </summary>
public class ServicioException : Exception
{
    public const string SinServidor = "No se pudo conectar con el servidor. Verifique la red e intente nuevamente.";
    public const string RespuestaIncompleta = "El servidor envió una respuesta vacía o incompleta.";
    public const string SesionNoValidaMensaje = "Su sesión no es válida. Inicie sesión nuevamente.";

    public bool SesionNoValida { get; }

    public ServicioException(string mensaje, bool sesionNoValida = false) : base(mensaje)
    {
        SesionNoValida = sesionNoValida;
    }
}

/// <summary>
/// Ubicación del servidor REST (sección «ServidorRest» de appsettings.json) y URL base de los
/// servicios: http://host:puerto/contexto.
/// </summary>
public sealed class ApiConfig
{
    public string UrlBase { get; }

    public ApiConfig(IConfiguration configuracion)
    {
        IConfigurationSection seccion = configuracion.GetSection("ServidorRest");
        UrlBase = "http://" + Valor(seccion, "Host") + ":" + Valor(seccion, "Puerto") + "/" + Valor(seccion, "Contexto").Trim('/');
    }

    public ApiConfig(string urlBase)
    {
        UrlBase = urlBase.TrimEnd('/');
    }

    private static string Valor(IConfigurationSection seccion, string clave)
    {
        string? valor = seccion[clave];
        return string.IsNullOrWhiteSpace(valor)
            ? throw new InvalidOperationException("Falta 'ServidorRest:" + clave + "' en appsettings.json")
            : valor.Trim();
    }
}

/// <summary>
/// Llama a los servicios RESTful y lee la respuesta estructurada {estado, mensaje, datos}
/// (ECUD10). Con estado OK devuelve el mensaje y los datos; con ERROR, sin conexión o con una
/// respuesta ilegible lanza <see cref="ServicioException"/>.
/// </summary>
public class ClienteRest
{
    public static readonly TimeSpan Espera = TimeSpan.FromSeconds(20);
    /// <summary>Importar todos consulta varias páginas de la API y guarda cerca de 250 países.</summary>
    public static readonly TimeSpan EsperaImportacion = TimeSpan.FromSeconds(90);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient http;
    private readonly string urlBase;

    public ClienteRest(HttpClient http, ApiConfig config)
    {
        this.http = http;
        urlBase = config.UrlBase;
    }

    private sealed class RespuestaJson<T>
    {
        public string? Estado { get; set; }
        public string? Mensaje { get; set; }
        public T? Datos { get; set; }
    }

    public async Task<Resultado<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, string? token, object? cuerpo,
        TimeSpan espera)
    {
        int codigo;
        string texto;
        try
        {
            using var peticion = new HttpRequestMessage(metodo, urlBase + ruta);
            peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (cuerpo != null)
                peticion.Content = new StringContent(JsonSerializer.Serialize(cuerpo, Json), Encoding.UTF8, "application/json");
            if (token != null)
                peticion.Headers.TryAddWithoutValidation("Authorization", token);
            using var cancelacion = new CancellationTokenSource(espera);
            using HttpResponseMessage respuesta = await http.SendAsync(peticion, cancelacion.Token);
            codigo = (int)respuesta.StatusCode;
            texto = await respuesta.Content.ReadAsStringAsync(cancelacion.Token);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException
                                      or InvalidOperationException or UriFormatException)
        {
            // Sin red, servidor apagado, tiempo agotado o dirección mal escrita
            throw new ServicioException(ServicioException.SinServidor);
        }

        RespuestaJson<T>? leida;
        try
        {
            leida = string.IsNullOrWhiteSpace(texto) ? null : JsonSerializer.Deserialize<RespuestaJson<T>>(texto, Json);
        }
        catch (JsonException)
        {
            leida = null;
        }
        if (codigo == 401 && token != null)
        {
            // ECUD8: token rechazado; el inicio de sesión (sin token) responde 401 por credenciales
            throw new ServicioException(ServicioException.SesionNoValidaMensaje, true);
        }
        if (leida?.Estado == null)
            throw new ServicioException(ServicioException.RespuestaIncompleta);
        string mensaje = leida.Mensaje ?? "";
        if (leida.Estado != "OK")
            throw new ServicioException(string.IsNullOrWhiteSpace(mensaje) ? ServicioException.RespuestaIncompleta : mensaje);
        return new Resultado<T>(mensaje, leida.Datos);
    }

    /// <summary>Texto como segmento de la ruta o valor de un parámetro (espacios y tildes codificados).</summary>
    public static string Codificar(string texto) => Uri.EscapeDataString(texto.Trim());
}

/// <summary>Iniciar y cerrar sesión (ECUD1).</summary>
public class LoginService
{
    private readonly ClienteRest cliente;

    public LoginService(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    public async Task<DatosSesion> IniciarSesionAsync(string usuario, string clave)
    {
        Resultado<DatosSesion> resultado = await cliente.EnviarAsync<DatosSesion>(HttpMethod.Post, "/Login/login", null,
            new { usuario, clave }, ClienteRest.Espera);
        if (resultado.Datos?.Token == null)
            throw new ServicioException(ServicioException.RespuestaIncompleta);
        return resultado.Datos;
    }

    /// <summary>ECUD1 alterno 4: el token se descarta igual en el cliente aunque el servidor no responda.</summary>
    public async Task CerrarSesionAsync(string token)
    {
        try
        {
            await cliente.EnviarAsync<object>(HttpMethod.Post, "/Login/logout", token, null, ClienteRest.Espera);
        }
        catch (ServicioException)
        {
            // La sesión local se borra de todos modos
        }
    }
}

/// <summary>Operaciones sobre países: ECUD2 a ECUD6.</summary>
public class PaisService
{
    private readonly ClienteRest cliente;

    public PaisService(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    public Task<Resultado<List<Pais>>> ListarAsync(string? token)
        => cliente.EnviarAsync<List<Pais>>(HttpMethod.Get, "/Paises", token, null, ClienteRest.Espera);

    public Task<Resultado<ResultadoBusqueda>> BuscarAsync(string? token, string? nombre, string? capital, string? region)
    {
        var consulta = new StringBuilder();
        Agregar(consulta, "nombre", nombre);
        Agregar(consulta, "capital", capital);
        Agregar(consulta, "region", region);
        return cliente.EnviarAsync<ResultadoBusqueda>(HttpMethod.Get, "/Paises/buscar" + consulta, token, null, ClienteRest.Espera);
    }

    public Task<Resultado<List<Pais>>> ConsultarApiAsync(string? token, string nombre)
        => cliente.EnviarAsync<List<Pais>>(HttpMethod.Get, "/Paises/externo/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    public Task<Resultado<ResultadoImportacion>> ImportarAsync(string? token, string consulta, string nombre)
        => cliente.EnviarAsync<ResultadoImportacion>(HttpMethod.Post, "/Paises/importar", token, Solicitud(consulta, nombre), ClienteRest.Espera);

    public Task<Resultado<ResumenImportacion>> ImportarTodosAsync(string? token)
        => cliente.EnviarAsync<ResumenImportacion>(HttpMethod.Post, "/Paises/importar/todos", token, null, ClienteRest.EsperaImportacion);

    public Task<Resultado<Pais>> RegistrarAsync(string? token, string consulta, string nombre)
        => cliente.EnviarAsync<Pais>(HttpMethod.Post, "/Paises", token, Solicitud(consulta, nombre), ClienteRest.Espera);

    public Task<Resultado<Pais>> ActualizarAsync(string? token, string nombre)
        => cliente.EnviarAsync<Pais>(HttpMethod.Put, "/Paises/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    public Task<Resultado<object>> EliminarAsync(string? token, string nombre)
        => cliente.EnviarAsync<object>(HttpMethod.Delete, "/Paises/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    private static object Solicitud(string consulta, string nombre) => new { consulta = consulta.Trim(), nombre = nombre.Trim() };

    private static void Agregar(StringBuilder consulta, string clave, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return;
        consulta.Append(consulta.Length == 0 ? '?' : '&').Append(clave).Append('=').Append(ClienteRest.Codificar(valor));
    }
}

/// <summary>Informes estadísticos (ECUD7).</summary>
public class ReporteService
{
    private readonly ClienteRest cliente;

    public ReporteService(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    public Task<Resultado<Informe>> GenerarAsync(string? token)
        => cliente.EnviarAsync<Informe>(HttpMethod.Get, "/Reportes", token, null, ClienteRest.Espera);
}
