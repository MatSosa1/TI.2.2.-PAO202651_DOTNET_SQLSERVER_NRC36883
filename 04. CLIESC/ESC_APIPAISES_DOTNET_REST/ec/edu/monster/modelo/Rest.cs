using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ec.edu.monster.modelo;

/// <summary>Envía una petición HTTP. Las pruebas lo reemplazan por un servidor simulado.</summary>
public interface Transporte
{
    /// <summary>Código HTTP y cuerpo de la respuesta.</summary>
    record Respuesta(int Codigo, string Cuerpo);

    /// <exception cref="HttpRequestException">sin red o servidor apagado</exception>
    /// <exception cref="TaskCanceledException">tiempo agotado</exception>
    Respuesta Enviar(string metodo, string url, string? cuerpo, IDictionary<string, string> cabeceras, TimeSpan espera);
}

/// <summary>Transporte real con HttpClient.</summary>
public class TransporteHttp : Transporte
{
    private readonly HttpClient http = new(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(5) })
    {
        Timeout = Timeout.InfiniteTimeSpan,
    };

    public Transporte.Respuesta Enviar(string metodo, string url, string? cuerpo, IDictionary<string, string> cabeceras,
        TimeSpan espera)
    {
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), url);
        if (cuerpo != null)
            peticion.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        foreach (var (clave, valor) in cabeceras)
        {
            if (clave.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue; // va en el contenido
            peticion.Headers.TryAddWithoutValidation(clave, valor);
        }
        using var cancelacion = new CancellationTokenSource(espera);
        using HttpResponseMessage respuesta = http.Send(peticion, cancelacion.Token);
        using var lector = new StreamReader(respuesta.Content.ReadAsStream(cancelacion.Token), Encoding.UTF8);
        return new Transporte.Respuesta((int)respuesta.StatusCode, lector.ReadToEnd());
    }
}

/// <summary>
/// Llama a los servicios RESTful del servidor y lee la respuesta estructurada
/// {estado, mensaje, datos} (ECUD10). Con estado OK devuelve el mensaje y los datos; con
/// ERROR, sin conexión o con una respuesta ilegible lanza <see cref="ServicioException"/>.
/// </summary>
public class ClienteRest
{
    public static readonly TimeSpan Espera = TimeSpan.FromSeconds(20);
    /// <summary>Importar todos consulta varias páginas de la API y guarda cerca de 250 países.</summary>
    public static readonly TimeSpan EsperaImportacion = TimeSpan.FromSeconds(90);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Transporte transporte;

    public string UrlBase { get; }

    public ClienteRest(string urlBase, Transporte transporte)
    {
        UrlBase = urlBase.EndsWith('/') ? urlBase[..^1] : urlBase;
        this.transporte = transporte;
    }

    /// <summary>Respuesta JSON tal como la envía el servidor.</summary>
    private sealed class RespuestaJson<T>
    {
        public string? Estado { get; set; }
        public string? Mensaje { get; set; }
        public T? Datos { get; set; }
    }

    /// <param name="token">token de la sesión, o null en el inicio de sesión</param>
    /// <param name="cuerpo">objeto a enviar como JSON, o null si la petición no lleva cuerpo</param>
    public Resultado<T> Enviar<T>(string metodo, string ruta, string? token, object? cuerpo, TimeSpan espera)
    {
        var cabeceras = new Dictionary<string, string> { ["Accept"] = "application/json" };
        if (cuerpo != null)
            cabeceras["Content-Type"] = "application/json; charset=utf-8";
        if (token != null)
            cabeceras["Authorization"] = token;

        Transporte.Respuesta http;
        try
        {
            http = transporte.Enviar(metodo, UrlBase + ruta, cuerpo == null ? null : JsonSerializer.Serialize(cuerpo, Json),
                cabeceras, espera);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException
                                      or InvalidOperationException or UriFormatException or IOException)
        {
            // Sin red, servidor apagado, tiempo agotado o dirección mal escrita
            throw new ServicioException(ServicioException.SinServidor);
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
            throw new ServicioException(ServicioException.SesionNoValidaMensaje, true);
        }
        if (respuesta?.Estado == null)
            throw new ServicioException(ServicioException.RespuestaIncompleta);
        string mensaje = respuesta.Mensaje ?? "";
        if (respuesta.Estado != "OK")
            throw new ServicioException(string.IsNullOrWhiteSpace(mensaje) ? ServicioException.RespuestaIncompleta : mensaje);
        return new Resultado<T>(mensaje, respuesta.Datos);
    }

    /// <summary>Texto como segmento de la ruta o valor de un parámetro (espacios y tildes codificados).</summary>
    public static string Codificar(string texto) => Uri.EscapeDataString(texto.Trim());
}

/// <summary>Implementación de los servicios contra el servidor RESTful (contrato del plan, §3).</summary>
public class ServiciosRest : LoginServicio, PaisServicio, ReporteServicio
{
    private readonly ClienteRest cliente;

    public ServiciosRest(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    // ------------------------------------------------------------------ ECUD1

    public DatosSesion IniciarSesion(string usuario, string clave)
    {
        Resultado<DatosSesion> resultado = cliente.Enviar<DatosSesion>("POST", "/Login/login", null,
            new { usuario, clave }, ClienteRest.Espera);
        if (resultado.Datos?.Token == null)
            throw new ServicioException(ServicioException.RespuestaIncompleta);
        return resultado.Datos;
    }

    public void CerrarSesion(string? token) => cliente.Enviar<object>("POST", "/Login/logout", token, null, ClienteRest.Espera);

    // ------------------------------------------------------------------ ECUD2 a ECUD6

    public Resultado<List<Pais>> Listar(string? token)
        => cliente.Enviar<List<Pais>>("GET", "/Paises", token, null, ClienteRest.Espera);

    public Resultado<ResultadoBusqueda> Buscar(string? token, string? nombre, string? capital, string? region)
    {
        var consulta = new StringBuilder();
        Agregar(consulta, "nombre", nombre);
        Agregar(consulta, "capital", capital);
        Agregar(consulta, "region", region);
        return cliente.Enviar<ResultadoBusqueda>("GET", "/Paises/buscar" + consulta, token, null, ClienteRest.Espera);
    }

    public Resultado<List<Pais>> ConsultarApi(string? token, string nombre)
        => cliente.Enviar<List<Pais>>("GET", "/Paises/externo/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    public Resultado<ResultadoImportacion> Importar(string? token, string? consulta, string nombre)
        => cliente.Enviar<ResultadoImportacion>("POST", "/Paises/importar", token, Solicitud(consulta, nombre), ClienteRest.Espera);

    public Resultado<ResumenImportacion> ImportarTodos(string? token)
        => cliente.Enviar<ResumenImportacion>("POST", "/Paises/importar/todos", token, null, ClienteRest.EsperaImportacion);

    public Resultado<Pais> Registrar(string? token, string? consulta, string nombre)
        => cliente.Enviar<Pais>("POST", "/Paises", token, Solicitud(consulta, nombre), ClienteRest.Espera);

    public Resultado<Pais> Actualizar(string? token, string nombre)
        => cliente.Enviar<Pais>("PUT", "/Paises/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    public Resultado<object> Eliminar(string? token, string nombre)
        => cliente.Enviar<object>("DELETE", "/Paises/" + ClienteRest.Codificar(nombre), token, null, ClienteRest.Espera);

    // ------------------------------------------------------------------ ECUD7

    public Resultado<Informe> Generar(string? token)
        => cliente.Enviar<Informe>("GET", "/Reportes", token, null, ClienteRest.Espera);

    private static object Solicitud(string? consulta, string nombre)
        => new { consulta = consulta?.Trim() ?? "", nombre = nombre.Trim() };

    /// <summary>Agrega «?clave=valor» o «&amp;clave=valor» si el valor no está vacío.</summary>
    private static void Agregar(StringBuilder consulta, string clave, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return;
        consulta.Append(consulta.Length == 0 ? '?' : '&').Append(clave).Append('=').Append(ClienteRest.Codificar(valor));
    }
}

/// <summary>
/// Comprueba si el servidor responde. Pide GET …/api/Paises sin token: cualquier respuesta HTTP
/// (normalmente 401 «Token inválido») significa que el servicio está en línea; sin respuesta
/// en 2 segundos, que no está disponible.
/// </summary>
public class EstadoServidor
{
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(2);

    private readonly Uri destino;
    private readonly HttpClient cliente = new(new SocketsHttpHandler { ConnectTimeout = Espera }) { Timeout = Espera };

    public EstadoServidor(string urlBase) : this(new Uri(urlBase + "/Paises"))
    {
    }

    public EstadoServidor(Uri destino)
    {
        this.destino = destino;
    }

    public bool Disponible()
    {
        try
        {
            using HttpResponseMessage respuesta = cliente.Send(new HttpRequestMessage(HttpMethod.Get, destino));
            int codigo = (int)respuesta.StatusCode;
            // Un 404 sin la aplicación significa que el servidor está, pero no el servicio
            return codigo != 404 && codigo < 500;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return false;
        }
    }
}

/// <summary>
/// Dirección del servidor, leída de cliente.properties. Se busca, en este orden: el archivo
/// indicado en la variable de entorno CLIENTE_CONFIG y el que está junto al ejecutable.
/// </summary>
public sealed class Configuracion
{
    public const string Archivo = "cliente.properties";
    public const string VariableRuta = "CLIENTE_CONFIG";

    private readonly Dictionary<string, string> propiedades;
    private readonly string origen;

    public Configuracion(Dictionary<string, string> propiedades, string origen = Archivo)
    {
        this.propiedades = propiedades;
        this.origen = origen;
    }

    public static Configuracion Cargar() => Cargar(AppContext.BaseDirectory);

    public static Configuracion Cargar(string directorio)
    {
        string? ruta = Environment.GetEnvironmentVariable(VariableRuta);
        if (!string.IsNullOrWhiteSpace(ruta))
        {
            if (!File.Exists(ruta.Trim()))
                throw new InvalidOperationException("No se encontró el archivo indicado en " + VariableRuta + ": "
                                                    + Path.GetFullPath(ruta.Trim()));
            return DesdeArchivo(ruta.Trim());
        }
        string juntoAlExe = Path.Combine(directorio, Archivo);
        if (!File.Exists(juntoAlExe))
            throw new InvalidOperationException("No se encontró " + Archivo + " junto al ejecutable: " + juntoAlExe);
        return DesdeArchivo(juntoAlExe);
    }

    public static Configuracion DesdeArchivo(string archivo)
    {
        var propiedades = new Dictionary<string, string>();
        foreach (string linea in File.ReadAllLines(archivo, Encoding.UTF8))
        {
            string limpia = linea.Trim();
            int igual = limpia.IndexOf('=');
            if (limpia.Length == 0 || limpia.StartsWith('#') || igual < 0)
                continue;
            propiedades[limpia[..igual].Trim()] = limpia[(igual + 1)..].Trim();
        }
        return new Configuracion(propiedades, Path.GetFullPath(archivo));
    }

    /// <summary>http://host:puerto/contexto</summary>
    public string UrlBase => "http://" + Valor("ws.host") + ":" + Puerto + "/" + Valor("ws.context").Trim('/');

    public int Puerto
    {
        get
        {
            string texto = Valor("ws.port");
            return int.TryParse(texto, out int puerto)
                ? puerto
                : throw new InvalidOperationException("'ws.port' no es un número en " + origen + ": " + texto);
        }
    }

    private string Valor(string clave)
    {
        return propiedades.TryGetValue(clave, out string? valor) && !string.IsNullOrWhiteSpace(valor)
            ? valor.Trim()
            : throw new InvalidOperationException("Falta '" + clave + "' en " + origen);
    }
}
