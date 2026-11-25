using CONSOLE_APIPAISES_DOTNET_REST.Models.Data;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;

namespace CONSOLE_APIPAISES_DOTNET_REST.Controllers;

/// <summary>Mensajes del cliente cuando no hay una respuesta válida del servidor (textos del ECUD).</summary>
public static class ErroresCliente
{
    /// <summary>ECUD1 alterno 3 (y todos los casos de uso): sin conexión o sin respuesta a tiempo.</summary>
    public const string SinServidor = "No se pudo conectar con el servidor. Verifique la red e intente nuevamente.";
    /// <summary>ECUD10 alterno 4: la respuesta no se pudo interpretar; la aplicación sigue en ejecución.</summary>
    public const string RespuestaIncompleta = "El servidor envió una respuesta vacía o incompleta.";
    /// <summary>ECUD8 alterno 2: el token ya no es válido.</summary>
    public const string SesionNoValida = "Su sesión no es válida. Inicie sesión nuevamente.";
}

/// <summary>
/// Validación en el cliente antes de enviar (ERS 9.1.1): usuario y clave no vacíos, nombre de
/// país no vacío y de hasta 100 caracteres, región elegida de la lista. El servidor repite las reglas.
/// </summary>
public static class ValidadorDatos
{
    public const int MaxNombre = 100;

    public const string IngreseUsuario = "Ingrese el usuario";
    public const string IngreseClave = "Ingrese la clave";
    public const string IngreseNombrePais = "Ingrese el nombre del país";
    public const string NombreMuyLargo = "El nombre no puede superar 100 caracteres";

    /// <summary>Regiones válidas (ECUD, parámetros generales), en el orden del catálogo.</summary>
    public static readonly IReadOnlyList<string> Regiones = ["América", "Europa", "Asia", "África", "Oceanía", "Antártida"];

    public static string Limpiar(string? texto) => texto?.Trim() ?? "";

    /// <returns>el mensaje de error, o null si el nombre es válido (ECUD3, alterno 3).</returns>
    public static string? ErrorNombre(string? nombre)
    {
        string limpio = Limpiar(nombre);
        if (limpio.Length == 0)
            return IngreseNombrePais;
        return limpio.Length > MaxNombre ? NombreMuyLargo : null;
    }
}

/// <summary>Iniciar y cerrar sesión (ECUD1).</summary>
public class LoginController
{
    /// <summary>ECUD1 pasos 4 a 7: envía las credenciales y, si son correctas, guarda el token de la sesión.</summary>
    public async Task<Respuesta<LoginDatos>> LoginAsync(string? usuario, string? clave)
    {
        var respuesta = await RESTfulClient.PostAsync<LoginDatos>(Endpoints.Login,
            new { usuario = ValidadorDatos.Limpiar(usuario), clave = clave ?? "" });

        if (respuesta.EsOk() && respuesta.Datos != null)
        {
            ConfigHTTP.Token = respuesta.Datos.Token ?? "";
            ConfigHTTP.Nombre = respuesta.Datos.Usuario ?? "";
        }
        return respuesta;
    }

    /// <summary>ECUD1 alterno 4: cierra la sesión; los datos locales se borran aunque el servidor no responda.</summary>
    public async Task<Respuesta<object>> LogoutAsync()
    {
        try
        {
            return await RESTfulClient.PostAsync<object>(Endpoints.Logout, null);
        }
        finally
        {
            ConfigHTTP.Limpiar();
        }
    }
}

/// <summary>Países: gestionar (ECUD4), buscar y filtrar (ECUD5, ECUD6), consultar en la API (ECUD3) e importar (ECUD2).</summary>
public class PaisController
{
    /// <summary>ECUD4 Listar: todos los países guardados, ordenados por nombre.</summary>
    public Task<Respuesta<List<Pais>>> ListarAsync() => RESTfulClient.GetAsync<List<Pais>>(Endpoints.Paises);

    /// <summary>ECUD5 y ECUD6: criterios vacíos se omiten; sin criterios devuelve todos.</summary>
    public Task<Respuesta<ResultadoBusqueda>> BuscarAsync(string? nombre, string? capital, string? region)
    {
        string consulta = Endpoints.Consulta(
            Endpoints.Parametro("nombre", nombre),
            Endpoints.Parametro("capital", capital),
            Endpoints.Parametro("region", region));
        return RESTfulClient.GetAsync<ResultadoBusqueda>(Endpoints.Buscar + consulta);
    }

    /// <summary>ECUD3: países de la API que coinciden con el nombre, marcados si ya están guardados.</summary>
    public Task<Respuesta<List<Pais>>> ConsultarApiAsync(string nombre)
        => RESTfulClient.GetAsync<List<Pais>>(Endpoints.Externo + Endpoints.Segmento(nombre));

    /// <summary>ECUD2: guarda el país elegido de la consulta (lo registra o lo actualiza).</summary>
    public Task<Respuesta<ResultadoImportacion>> ImportarAsync(string consulta, string nombre)
        => RESTfulClient.PostAsync<ResultadoImportacion>(Endpoints.Importar, Solicitud(consulta, nombre));

    /// <summary>Importa todos los países de la API; puede tardar varios segundos.</summary>
    public Task<Respuesta<ResumenImportacion>> ImportarTodosAsync()
        => RESTfulClient.PostAsync<ResumenImportacion>(Endpoints.ImportarTodos, null, ConfigHTTP.TimeoutImportacion);

    /// <summary>ECUD4 Registrar: registra un país nuevo elegido de la consulta (409 si ya existe).</summary>
    public Task<Respuesta<Pais>> RegistrarAsync(string consulta, string nombre)
        => RESTfulClient.PostAsync<Pais>(Endpoints.Paises, Solicitud(consulta, nombre));

    /// <summary>ECUD4 Actualizar: el servidor vuelve a consultar la API y actualiza el país.</summary>
    public Task<Respuesta<Pais>> ActualizarAsync(string nombre)
        => RESTfulClient.PutAsync<Pais>(Endpoints.Paises + "/" + Endpoints.Segmento(nombre));

    /// <summary>ECUD4 Eliminar: borra el país de la base local.</summary>
    public Task<Respuesta<object>> EliminarAsync(string nombre)
        => RESTfulClient.DeleteAsync<object>(Endpoints.Paises + "/" + Endpoints.Segmento(nombre));

    private static object Solicitud(string consulta, string nombre)
        => new { consulta = ValidadorDatos.Limpiar(consulta), nombre = ValidadorDatos.Limpiar(nombre) };
}

/// <summary>Informes estadísticos (ECUD7).</summary>
public class ReporteController
{
    public Task<Respuesta<Informe>> GenerarAsync() => RESTfulClient.GetAsync<Informe>(Endpoints.Reportes);
}
