using WS_APIPAISES_DOTNET_REST.Models.Entidades;

namespace WS_APIPAISES_DOTNET_REST.Models.DTO;

/// <summary>
/// Respuesta uniforme de todas las operaciones del servidor (RQF-010, ECUD10):
/// estado OK o ERROR, un mensaje en español y los datos. Con ERROR los datos van vacíos
/// (<c>"datos": null</c>, que siempre se serializa para que las respuestas tengan los tres campos).
/// </summary>
public class RespuestaEstructurada
{
    public const string Ok = "OK";
    public const string Error = "ERROR";

    public string Estado { get; set; } = Ok;
    public string Mensaje { get; set; } = "";
    public object? Datos { get; set; }

    public RespuestaEstructurada()
    {
    }

    public RespuestaEstructurada(string estado, string mensaje, object? datos)
    {
        Estado = estado;
        Mensaje = mensaje;
        Datos = datos;
    }

    public static RespuestaEstructurada CrearOk(string mensaje, object? datos) => new(Ok, mensaje, datos);

    public static RespuestaEstructurada CrearError(string mensaje) => new(Error, mensaje, null);
}

/// <summary>Cuerpo de POST /api/Login/login. Los campos pueden faltar: el controlador los valida.</summary>
public class LoginRequest
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
}

/// <summary>Datos de un inicio de sesión correcto: el token y el nombre del usuario (ECUD1, paso 6).</summary>
public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Usuario { get; set; } = "";

    public LoginResponse()
    {
    }

    public LoginResponse(string token, string usuario)
    {
        Token = token;
        Usuario = usuario;
    }
}

/// <summary>
/// País tal como llega de la API REST Countries, ya convertido (ECUD9).
/// <see cref="Guardado"/> indica si ya existe en la base de datos local (ECUD3, paso 6).
/// </summary>
public class PaisDTO
{
    public string Nombre { get; set; } = "";
    public string Capital { get; set; } = "";
    public string Region { get; set; } = "";
    public long Poblacion { get; set; }
    public string Moneda { get; set; } = "";
    public bool Guardado { get; set; }

    public PaisDTO()
    {
    }

    public PaisDTO(string nombre, string capital, string region, long poblacion, string moneda)
    {
        Nombre = nombre;
        Capital = capital;
        Region = region;
        Poblacion = poblacion;
        Moneda = moneda;
    }
}

/// <summary>
/// Cuerpo de POST /api/Paises y POST /api/Paises/importar: el texto que el usuario
/// consultó en la API y el nombre del país que eligió de los resultados (ECUD2, ECUD4).
/// </summary>
public class SolicitudPais
{
    public string? Consulta { get; set; }
    public string? Nombre { get; set; }
}

/// <summary>Datos de GET /api/Paises/buscar: los países encontrados y el total (ECUD5 paso 7, ECUD6 paso 5).</summary>
public class ResultadoBusqueda
{
    public List<Pais> Paises { get; set; } = new();
    public int Total { get; set; }

    public ResultadoBusqueda()
    {
    }

    public ResultadoBusqueda(List<Pais> paises)
    {
        Paises = paises;
        Total = paises.Count;
    }
}

/// <summary>
/// Datos de POST /api/Paises/importar: el país guardado y si fue «registrado» o «actualizado»
/// (ECUD2, paso 8).
/// </summary>
public class ResultadoImportacion
{
    public Pais? Pais { get; set; }
    public string Accion { get; set; } = "";

    public ResultadoImportacion()
    {
    }

    public ResultadoImportacion(Pais pais, string accion)
    {
        Pais = pais;
        Accion = accion;
    }
}

/// <summary>Resultado de importar todos los países: cuántos llegaron, cuántos se registraron y cuántos se actualizaron.</summary>
public class ResumenImportacion
{
    public int Recibidos { get; set; }
    public int Registrados { get; set; }
    public int Actualizados { get; set; }

    public ResumenImportacion()
    {
    }

    public ResumenImportacion(int recibidos, int registrados, int actualizados)
    {
        Recibidos = recibidos;
        Registrados = registrados;
        Actualizados = actualizados;
    }
}

/// <summary>Cantidad de países de una región (ECUD7, paso 4).</summary>
public class ConteoRegion
{
    public string Region { get; set; } = "";
    public int Cantidad { get; set; }

    public ConteoRegion()
    {
    }

    public ConteoRegion(string region, int cantidad)
    {
        Region = region;
        Cantidad = cantidad;
    }
}

/// <summary>Promedio de población de una región, redondeado a entero; 0 si no tiene países (ECUD7, paso 6).</summary>
public class PromedioRegion
{
    public string Region { get; set; } = "";
    public long PromedioPoblacion { get; set; }

    public PromedioRegion()
    {
    }

    public PromedioRegion(string region, long promedioPoblacion)
    {
        Region = region;
        PromedioPoblacion = promedioPoblacion;
    }
}

/// <summary>Los tres informes de GET /api/Reportes (RQF-007, ECUD7).</summary>
public class InformeEstadistico
{
    public List<ConteoRegion> PaisesPorRegion { get; set; } = new();
    public Pais? PaisMasPoblado { get; set; }
    public List<PromedioRegion> PromedioPorRegion { get; set; } = new();

    public InformeEstadistico()
    {
    }

    public InformeEstadistico(List<ConteoRegion> paisesPorRegion, Pais? paisMasPoblado,
        List<PromedioRegion> promedioPorRegion)
    {
        PaisesPorRegion = paisesPorRegion;
        PaisMasPoblado = paisMasPoblado;
        PromedioPorRegion = promedioPorRegion;
    }
}
