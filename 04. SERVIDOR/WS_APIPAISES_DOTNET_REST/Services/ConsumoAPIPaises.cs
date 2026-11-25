using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WS_APIPAISES_DOTNET_REST.Config;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Utilidades;
using Tipo = WS_APIPAISES_DOTNET_REST.Services.ApiExternaException.TipoFalla;

namespace WS_APIPAISES_DOTNET_REST.Services;

/// <summary>
/// Único punto del sistema que conoce la API REST Countries (ECUD9).
/// <para>
/// Usa la versión v5 (<c>https://api.restcountries.com/countries/v5</c>) con la API key en
/// <c>Authorization: Bearer</c>, porque la v3.1 del ERS fue dada de baja. Convierte cada país
/// a <see cref="PaisDTO"/> con las reglas del ERS §9.1.5:
/// </para>
/// <list type="bullet">
///   <item>nombre: <c>names.translations.spa.common</c>, o <c>names.common</c> si no hay traducción;</item>
///   <item>capital: la capital principal (<c>attributes.primary</c>), o la primera; «Sin capital» si no tiene;</item>
///   <item>región: traducida al español; si no es una de las seis, el país se descarta (ECUD9, alterno 4);</item>
///   <item>población: <c>population</c> como entero largo;</item>
///   <item>moneda: la primera, como «nombre (código)»; «Sin moneda» si no tiene.</item>
/// </list>
/// El JSON se lee con Newtonsoft.Json, como pide el taller.
/// </summary>
public class ConsumoAPIPaises
{
    public const string SinCapital = "Sin capital";
    public const string SinMoneda = "Sin moneda";

    /// <summary>Solo los campos que se necesitan (RNF de rendimiento: la importación pide lo mínimo).</summary>
    internal const string Campos = "names.common,names.translations,capitals,region,population,currencies";
    /// <summary>Tamaño máximo de página del plan gratuito de la v5.</summary>
    internal const int Limite = 100;
    /// <summary>Tope de páginas por si la API nunca dijera que terminó (≈249 países caben en 3).</summary>
    internal const int MaxPaginas = 10;

    private readonly string urlBase;
    private readonly string apiKey;
    private readonly int timeoutSegundos;
    private readonly HttpClient http;
    private readonly ILogger log;

    public ConsumoAPIPaises(Configuracion configuracion, ILogger<ConsumoAPIPaises> log)
        : this(configuracion.ApiUrl, configuracion.ApiKey, configuracion.ApiTimeoutSegundos, log)
    {
    }

    public ConsumoAPIPaises(string urlBase, string? apiKey, int timeoutSegundos, ILogger? log = null)
    {
        this.urlBase = urlBase.EndsWith('/') ? urlBase[..^1] : urlBase;
        this.apiKey = apiKey?.Trim() ?? "";
        this.timeoutSegundos = timeoutSegundos;
        this.log = log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        // HttpClient sigue las redirecciones por defecto; un solo cliente por instancia (singleton en Program)
        http = new HttpClient(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(timeoutSegundos) })
        {
            Timeout = TimeSpan.FromSeconds(timeoutSegundos),
        };
        if (this.apiKey.Length == 0)
        {
            this.log.LogWarning("RestCountries:ApiKey está vacía: la API REST Countries v5 responderá 401");
        }
    }

    /// <summary>Todos los países (equivale a la antigua /v3.1/all), recorriendo las páginas de la v5.</summary>
    public async Task<List<PaisDTO>> ObtenerTodosAsync()
    {
        var paises = new Dictionary<string, PaisDTO>();
        var orden = new List<string>();
        int offset = 0;
        for (int pagina = 0; pagina < MaxPaginas; pagina++)
        {
            Pagina actual = await PedirAsync($"{urlBase}?limit={Limite}&offset={offset}&response_fields={Campos}");
            AgregarSinRepetir(paises, orden, actual.Paises);
            offset += actual.CantidadObjetos;
            if (!actual.HayMas || actual.CantidadObjetos == 0)
            {
                break;
            }
        }
        if (paises.Count == 0)
        {
            throw new ApiExternaException(Tipo.RespuestaInvalida,
                "La API no devolvió ningún país al pedir la lista completa");
        }
        return orden.Select(c => paises[c]).ToList();
    }

    /// <summary>
    /// Países cuyo nombre contiene el texto (equivale a la antigua /v3.1/name/{nombre}).
    /// Primero busca en los nombres en inglés y nativos; si no hay coincidencias, busca en las
    /// traducciones y se queda con los países cuyo nombre en español contiene el texto,
    /// para que «alemania» encuentre Alemania.
    /// </summary>
    public async Task<List<PaisDTO>> ObtenerPorNombreAsync(string? nombre)
    {
        string texto = nombre?.Trim() ?? "";
        if (texto.Length == 0)
        {
            throw new ArgumentException("El nombre a consultar está vacío");
        }
        var paises = new Dictionary<string, PaisDTO>();
        var orden = new List<string>();
        AgregarSinRepetir(paises, orden, (await PedirAsync(urlBase + "/name" + Consulta(texto))).Paises);

        if (paises.Count == 0)
        {
            AgregarSinRepetir(paises, orden, await PorTraduccionAsync(texto));
        }
        if (paises.Count == 0)
        {
            throw new ApiExternaException(Tipo.SinCoincidencias, $"Sin coincidencias para '{texto}'");
        }
        return orden.Select(c => paises[c]).ToList();
    }

    /// <summary>
    /// País elegido por el usuario entre los resultados de su consulta (ECUD2 paso 6, ECUD4
    /// Registrar y Actualizar). Si la consulta original no lo trae (por ejemplo, al actualizar
    /// un país guardado con su nombre en español), lo busca por ese nombre en las traducciones.
    /// </summary>
    /// <param name="consulta">texto que el usuario consultó; si está vacío se usa el nombre</param>
    /// <param name="nombre">nombre exacto del país elegido</param>
    public async Task<PaisDTO> ObtenerPaisAsync(string? consulta, string nombre)
    {
        string texto = Textos.Vacio(consulta) ? nombre : consulta!;
        PaisDTO? elegido;
        try
        {
            elegido = Seleccionar(await ObtenerPorNombreAsync(texto), nombre);
        }
        catch (ApiExternaException e) when (e.Tipo == Tipo.SinCoincidencias)
        {
            elegido = null;
        }
        elegido ??= Seleccionar(await PorTraduccionAsync(nombre.Trim()), nombre);
        if (elegido == null)
        {
            throw new ApiExternaException(Tipo.SinCoincidencias,
                $"La API no devolvió el país '{nombre}' para la consulta '{texto}'");
        }
        return elegido;
    }

    /// <summary>Países cuyo nombre en español contiene el texto, buscando en names.translations.</summary>
    private async Task<List<PaisDTO>> PorTraduccionAsync(string texto)
    {
        string buscado = Textos.Clave(texto);
        var paises = new List<PaisDTO>();
        foreach (PaisDTO pais in (await PedirAsync(urlBase + "/names.translations" + Consulta(texto))).Paises)
        {
            // names.translations encuentra el texto en cualquier idioma: se filtra por el nombre en español
            if (Textos.Clave(pais.Nombre).Contains(buscado))
            {
                paises.Add(pais);
            }
        }
        return paises;
    }

    private static string Consulta(string texto)
    {
        return "?q=" + Uri.EscapeDataString(texto) + "&limit=" + Limite + "&response_fields=" + Campos;
    }

    /// <summary>
    /// País de la lista cuyo nombre es exactamente el elegido, sin distinguir mayúsculas ni
    /// tildes (ECUD9, paso 8: el invocador toma el que coincide). Null si no está.
    /// </summary>
    public static PaisDTO? Seleccionar(IEnumerable<PaisDTO> paises, string nombre)
    {
        string buscado = Textos.Clave(nombre);
        return paises.FirstOrDefault(p => Textos.Clave(p.Nombre) == buscado);
    }

    /// <summary>Convierte el cuerpo JSON de una respuesta de la v5 en la lista de países.</summary>
    public List<PaisDTO> Mapear(string? json)
    {
        return MapearPagina(json).Paises;
    }

    /// <summary>Traduce la región de la API al nombre del catálogo Regiones; null si no es ninguna de las seis.</summary>
    public static string? TraducirRegion(string? region)
    {
        if (region == null)
        {
            return null;
        }
        return region.Trim().ToLowerInvariant() switch
        {
            "americas" or "america" or "north america" or "south america" => "América",
            "europe" => "Europa",
            "asia" => "Asia",
            "africa" => "África",
            "oceania" => "Oceanía",
            "antarctic" or "antarctica" or "polar" => "Antártida",
            _ => null,
        };
    }

    // ------------------------------------------------------------------ HTTP

    private async Task<Pagina> PedirAsync(string url)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, url);
        peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        peticion.Headers.UserAgent.ParseAdd("APIPaises/1.0");
        if (apiKey.Length > 0)
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        int codigo;
        string cuerpo;
        try
        {
            using HttpResponseMessage respuesta = await http.SendAsync(peticion);
            codigo = (int)respuesta.StatusCode;
            cuerpo = await respuesta.Content.ReadAsStringAsync();
        }
        catch (TaskCanceledException e)
        {
            throw new ApiExternaException(Tipo.NoDisponible,
                $"Tiempo de espera agotado ({timeoutSegundos} s) en {SinConsulta(url)}", e);
        }
        catch (HttpRequestException e)
        {
            throw new ApiExternaException(Tipo.NoDisponible, $"Sin conexión con {SinConsulta(url)}: {e.Message}", e);
        }

        if (codigo != 200)
        {
            // 401 (API key), 403 (cuota agotada), 429 (límite de peticiones), 5xx...: la API no está disponible
            string detalle = $"REST Countries respondió {codigo} en {SinConsulta(url)}: {ResumirError(cuerpo)}";
            log.LogWarning("{Detalle}", detalle);
            throw new ApiExternaException(Tipo.NoDisponible, detalle);
        }
        return MapearPagina(cuerpo);
    }

    private static string SinConsulta(string url)
    {
        int i = url.IndexOf('?');
        return i < 0 ? url : url[..i];
    }

    /// <summary>Primer mensaje del arreglo errors de la v5, para el log.</summary>
    private static string ResumirError(string? cuerpo)
    {
        try
        {
            if (JToken.Parse(cuerpo ?? "") is JObject raiz && raiz["errors"] is JArray { Count: > 0 } errores
                && errores[0] is JObject error)
            {
                string? codigo = Texto(error, "code");
                return Texto(error, "message") + (codigo == null ? "" : " [" + codigo + "]");
            }
        }
        catch (JsonException)
        {
            // Cuerpo que no es JSON: se resume tal cual
        }
        string texto = cuerpo?.Trim() ?? "";
        return texto.Length <= 200 ? texto : texto[..200] + "...";
    }

    // ------------------------------------------------------------------ JSON

    private sealed record Pagina(List<PaisDTO> Paises, int CantidadObjetos, bool HayMas);

    private Pagina MapearPagina(string? json)
    {
        JArray objetos;
        JObject? meta;
        try
        {
            JToken raiz = JToken.Parse(json ?? "");
            JObject? datos = raiz is JObject r ? Objeto(r, "data") : null;
            if (datos?["objects"] is not JArray lista)
            {
                throw new ApiExternaException(Tipo.RespuestaInvalida,
                    "La respuesta no tiene data.objects: " + ResumirError(json));
            }
            objetos = lista;
            meta = Objeto(datos, "meta");
        }
        catch (JsonException e)
        {
            throw new ApiExternaException(Tipo.RespuestaInvalida, "JSON no válido", e);
        }

        var paises = new List<PaisDTO>();
        foreach (JToken elemento in objetos)
        {
            if (elemento is not JObject objeto)
            {
                continue;
            }
            try
            {
                PaisDTO? pais = MapearPais(objeto);
                if (pais != null)
                {
                    paises.Add(pais);
                }
            }
            catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                // Un país con un campo de tipo inesperado no detiene a los demás
                log.LogWarning(e, "País descartado por formato inesperado: {Pais}", elemento.ToString(Formatting.None));
            }
        }

        bool hayMas = meta?["more"] is JValue { Type: JTokenType.Boolean } mas && (bool)mas;
        return new Pagina(paises, objetos.Count, hayMas);
    }

    private PaisDTO? MapearPais(JObject json)
    {
        JObject? nombres = Objeto(json, "names");
        string? comun = Texto(nombres, "common");
        string? espanol = Texto(Objeto(Objeto(nombres, "translations"), "spa"), "common");
        string? nombre = Textos.Vacio(espanol) ? comun : espanol;
        if (Textos.Vacio(nombre))
        {
            log.LogWarning("País descartado: no tiene nombre");
            return null;
        }

        string? regionApi = Texto(json, "region");
        string? region = TraducirRegion(regionApi);
        if (region == null)
        {
            // ECUD9, alterno 4: región que no está en la tabla Regiones
            log.LogWarning("País descartado por región desconocida: {Pais} ({Region})", nombre, regionApi);
            return null;
        }

        return new PaisDTO(Textos.Recortar(nombre)!, Textos.Recortar(Capital(json["capitals"]))!, region,
            Poblacion(json["population"]), Textos.Recortar(Moneda(json["currencies"]))!);
    }

    private static string Capital(JToken? capitales)
    {
        if (capitales is not JArray lista)
        {
            return SinCapital;
        }
        string? primera = null;
        foreach (JToken elemento in lista)
        {
            string? nombre;
            bool principal = false;
            if (elemento is JObject capital)
            {
                nombre = Texto(capital, "name");
                principal = Objeto(capital, "attributes")?["primary"] is JValue { Type: JTokenType.Boolean } p && (bool)p;
            }
            else if (elemento is JValue valor && valor.Type != JTokenType.Null)
            {
                nombre = valor.ToString();
            }
            else
            {
                continue;
            }
            if (Textos.Vacio(nombre))
            {
                continue;
            }
            if (principal)
            {
                return nombre!.Trim();
            }
            primera ??= nombre!.Trim();
        }
        return primera ?? SinCapital;
    }

    private static long Poblacion(JToken? valor)
    {
        if (valor is JValue { Type: JTokenType.Integer or JTokenType.Float } numero)
        {
            long poblacion = (long)Math.Round((double)numero, MidpointRounding.AwayFromZero);
            return Math.Max(0, poblacion);
        }
        return 0;
    }

    private static string Moneda(JToken? monedas)
    {
        if (monedas is JArray lista)
        {
            // v5: [{ "code": "USD", "name": "United States dollar", "symbol": "$" }]
            foreach (JToken elemento in lista)
            {
                if (elemento is JObject moneda)
                {
                    string? texto = FormatoMoneda(Texto(moneda, "name"), Texto(moneda, "code"));
                    if (texto != null)
                    {
                        return texto;
                    }
                }
            }
        }
        else if (monedas is JObject mapa)
        {
            // Forma antigua { "USD": { "name": ... } }, por si la API la devolviera
            foreach (JProperty entrada in mapa.Properties())
            {
                string? nombre = entrada.Value is JObject datos ? Texto(datos, "name") : null;
                string? texto = FormatoMoneda(nombre, entrada.Name);
                if (texto != null)
                {
                    return texto;
                }
            }
        }
        return SinMoneda;
    }

    private static string? FormatoMoneda(string? nombre, string? codigo)
    {
        bool sinNombre = Textos.Vacio(nombre);
        bool sinCodigo = Textos.Vacio(codigo);
        if (sinNombre && sinCodigo)
        {
            return null;
        }
        if (sinCodigo)
        {
            return nombre!.Trim();
        }
        if (sinNombre)
        {
            return codigo!.Trim();
        }
        return nombre!.Trim() + " (" + codigo!.Trim() + ")";
    }

    private static JObject? Objeto(JObject? padre, string clave)
    {
        return padre?[clave] as JObject;
    }

    private static string? Texto(JObject? padre, string clave)
    {
        return padre?[clave] is JValue valor && valor.Type is not (JTokenType.Null or JTokenType.Undefined)
            ? valor.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;
    }

    private static void AgregarSinRepetir(Dictionary<string, PaisDTO> destino, List<string> orden,
        IEnumerable<PaisDTO> paises)
    {
        foreach (PaisDTO pais in paises)
        {
            string clave = Textos.Clave(pais.Nombre);
            if (destino.TryAdd(clave, pais))
            {
                orden.Add(clave);
            }
        }
    }
}
