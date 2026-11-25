using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;

namespace MOV_APIPAISES_DOTNET_REST.Modelos.Repositorios;

/// <summary>Indica si el dispositivo tiene red. Las pruebas lo reemplazan.</summary>
public interface IVerificadorRed
{
    bool HayRed();
}

/// <summary>Envía una petición HTTP. Las pruebas lo reemplazan por un servidor simulado.</summary>
public interface ITransporte
{
    /// <summary>Código HTTP y cuerpo de la respuesta.</summary>
    record Respuesta(int Codigo, string Cuerpo);

    Task<Respuesta> EnviarAsync(string metodo, string url, string? cuerpo, string? token, TimeSpan espera);
}

/// <summary>Transporte real con HttpClient.</summary>
public class TransporteHttp : ITransporte
{
    private readonly HttpClient http = new() { Timeout = Timeout.InfiniteTimeSpan };

    public async Task<ITransporte.Respuesta> EnviarAsync(string metodo, string url, string? cuerpo, string? token, TimeSpan espera)
    {
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), url);
        peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (cuerpo != null)
            peticion.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        if (token != null)
            peticion.Headers.TryAddWithoutValidation("Authorization", token);
        using var cancelacion = new CancellationTokenSource(espera);
        using HttpResponseMessage respuesta = await http.SendAsync(peticion, cancelacion.Token);
        return new ITransporte.Respuesta((int)respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync(cancelacion.Token));
    }
}

/// <summary>
/// Llama a los servicios RESTful del servidor y lee la respuesta estructurada {estado, mensaje,
/// datos} (ECUD10). Nunca lanza excepciones: devuelve un <see cref="Resultado{T}"/> con el mensaje
/// para el usuario (sin red, sin servidor, respuesta ilegible o token rechazado).
/// </summary>
public class ClienteRest
{
    public const string SinServidor = "No se pudo conectar con el servidor. Verifique la red e intente nuevamente.";
    public const string RespuestaIncompleta = "El servidor envió una respuesta vacía o incompleta.";
    public const string SesionNoValida = "Su sesión no es válida. Inicie sesión nuevamente.";

    public static readonly TimeSpan Espera = TimeSpan.FromSeconds(20);
    /// <summary>Importar todos consulta varias páginas de la API y guarda cerca de 250 países.</summary>
    public static readonly TimeSpan EsperaImportacion = TimeSpan.FromSeconds(90);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly string urlBase;
    private readonly ITransporte transporte;
    private readonly IVerificadorRed red;

    public ClienteRest(string urlBase, ITransporte transporte, IVerificadorRed red)
    {
        this.urlBase = urlBase.TrimEnd('/');
        this.transporte = transporte;
        this.red = red;
    }

    private sealed class RespuestaJson<T>
    {
        public string? Estado { get; set; }
        public string? Mensaje { get; set; }
        public T? Datos { get; set; }
    }

    public async Task<Resultado<T>> EnviarAsync<T>(string metodo, string ruta, string? token, object? cuerpo, TimeSpan espera)
    {
        if (!red.HayRed())
            return Resultado<T>.Error(SinServidor);

        ITransporte.Respuesta http;
        try
        {
            http = await transporte.EnviarAsync(metodo, urlBase + ruta, cuerpo == null ? null : JsonSerializer.Serialize(cuerpo, Json),
                token, espera);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException
                                      or IOException or InvalidOperationException or UriFormatException)
        {
            // Sin red, servidor apagado, tiempo agotado o dirección mal escrita
            return Resultado<T>.Error(SinServidor);
        }

        RespuestaJson<T>? respuesta;
        try
        {
            respuesta = string.IsNullOrWhiteSpace(http.Cuerpo) ? null : JsonSerializer.Deserialize<RespuestaJson<T>>(http.Cuerpo, Json);
        }
        catch (JsonException)
        {
            respuesta = null;
        }
        if (http.Codigo == 401 && token != null)
        {
            // ECUD8: token rechazado; el inicio de sesión (sin token) responde 401 por credenciales
            return Resultado<T>.Expirada(SesionNoValida);
        }
        if (respuesta?.Estado == null)
            return Resultado<T>.Error(RespuestaIncompleta);
        string mensaje = respuesta.Mensaje ?? "";
        if (respuesta.Estado != "OK")
            return Resultado<T>.Error(string.IsNullOrWhiteSpace(mensaje) ? RespuestaIncompleta : mensaje);
        return Resultado<T>.Ok(respuesta.Datos, mensaje);
    }

    /// <summary>Texto como segmento de la ruta o valor de un parámetro (espacios y tildes codificados).</summary>
    public static string Codificar(string texto) => Uri.EscapeDataString(texto.Trim());
}

/// <summary>Operaciones del servidor REST de APIPaises (contrato §3 del plan).</summary>
public class ApiPaisesRepositorio
{
    private readonly ClienteRest cliente;

    public ApiPaisesRepositorio(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    // ------------------------------------------------------------------ ECUD1

    public async Task<Resultado<Sesion>> LoginAsync(string usuario, string clave)
    {
        Resultado<Sesion> resultado = await cliente.EnviarAsync<Sesion>("POST", "/Login/login", null,
            new { usuario, clave }, ClienteRest.Espera);
        if (resultado.Exito && resultado.Datos?.Token == null)
            return Resultado<Sesion>.Error(ClienteRest.RespuestaIncompleta);
        return resultado;
    }

    public Task<Resultado<object>> LogoutAsync(string token)
        => cliente.EnviarAsync<object>("POST", "/Login/logout", token, null, ClienteRest.Espera);

    // ------------------------------------------------------------------ ECUD4 / ECUD5 / ECUD6

    public Task<Resultado<List<Pais>>> ListarAsync(string? token)
        => cliente.EnviarAsync<List<Pais>>("GET", "/Paises", token, null, ClienteRest.Espera);

    public Task<Resultado<Busqueda>> BuscarAsync(string? token, string? nombre, string? capital, string? region)
    {
        var consulta = new StringBuilder();
        Agregar(consulta, "nombre", nombre);
        Agregar(consulta, "capital", capital);
        Agregar(consulta, "region", region);
        return cliente.EnviarAsync<Busqueda>("GET", "/Paises/buscar" + consulta, token, null, ClienteRest.Espera);
    }

    public Task<Resultado<Pais>> RegistrarAsync(string? token, string? consulta, string nombre)
        => cliente.EnviarAsync<Pais>("POST", "/Paises", token, Solicitud(consulta, nombre), ClienteRest.Espera);

    public Task<Resultado<Pais>> ActualizarAsync(string? token, string nombre)
        => cliente.EnviarAsync<Pais>("PUT", "/Paises/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    public Task<Resultado<object>> EliminarAsync(string? token, string nombre)
        => cliente.EnviarAsync<object>("DELETE", "/Paises/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    // ------------------------------------------------------------------ ECUD3 / ECUD2

    public Task<Resultado<List<Pais>>> ConsultarApiAsync(string? token, string nombre)
        => cliente.EnviarAsync<List<Pais>>("GET", "/Paises/externo/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    public Task<Resultado<Importacion>> ImportarAsync(string? token, string? consulta, string nombre)
        => cliente.EnviarAsync<Importacion>("POST", "/Paises/importar", token, Solicitud(consulta, nombre), ClienteRest.Espera);

    public Task<Resultado<Resumen>> ImportarTodosAsync(string? token)
        => cliente.EnviarAsync<Resumen>("POST", "/Paises/importar/todos", token, null, ClienteRest.EsperaImportacion);

    // ------------------------------------------------------------------ ECUD7

    public Task<Resultado<Informe>> InformesAsync(string? token)
        => cliente.EnviarAsync<Informe>("GET", "/Reportes", token, null, ClienteRest.Espera);

    private static object Solicitud(string? consulta, string nombre)
        => new { consulta = consulta?.Trim() ?? "", nombre = nombre.Trim() };

    private static void Agregar(StringBuilder consulta, string clave, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return;
        consulta.Append(consulta.Length == 0 ? '?' : '&').Append(clave).Append('=').Append(ClienteRest.Codificar(valor));
    }
}
