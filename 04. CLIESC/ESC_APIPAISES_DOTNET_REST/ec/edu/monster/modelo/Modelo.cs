using System.Globalization;

namespace ec.edu.monster.modelo;

/// <summary>Respuesta de un inicio de sesión correcto: el token y el nombre del usuario (ECUD1, paso 6).</summary>
public record DatosSesion(string? Token, string? Usuario);

/// <summary>Operación correcta: el mensaje del servidor (por ejemplo «País registrado correctamente») y los datos.</summary>
public record Resultado<T>(string Mensaje, T? Datos);

/// <summary>País guardado o consultado; <c>Guardado</c> solo llega en la consulta a la API (ECUD3).</summary>
public record Pais(string? Nombre, string? Capital, string? Region, long Poblacion, string? Moneda, bool Guardado)
{
    public Pais ComoGuardado() => this with { Guardado = true };
}

/// <summary>ECUD5 y ECUD6: países encontrados y total.</summary>
public record ResultadoBusqueda(List<Pais>? Paises, int Total);

/// <summary>ECUD2: país importado y si fue «registrado» o «actualizado».</summary>
public record ResultadoImportacion(Pais? Pais, string? Accion);

/// <summary>«Importar todos»: recibidos de la API, registrados y actualizados.</summary>
public record ResumenImportacion(int Recibidos, int Registrados, int Actualizados);

/// <summary>ECUD7: los tres informes.</summary>
public record Informe(List<ConteoRegion>? PaisesPorRegion, Pais? PaisMasPoblado, List<PromedioRegion>? PromedioPorRegion);

public record ConteoRegion(string? Region, int Cantidad);

public record PromedioRegion(string? Region, long PromedioPoblacion);

/// <summary>
/// La operación no se completó. El mensaje ya está listo para mostrarlo al usuario: es el del
/// servidor (estado ERROR) o uno del cliente (sin conexión, respuesta ilegible).
/// <see cref="SesionNoValida"/> indica que el servidor rechazó el token (ECUD8): hay que volver al login.
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

/// <summary>Iniciar y cerrar sesión (ECUD1).</summary>
public interface LoginServicio
{
    DatosSesion IniciarSesion(string usuario, string clave);
    void CerrarSesion(string? token);
}

/// <summary>Operaciones sobre países: ECUD2 a ECUD6.</summary>
public interface PaisServicio
{
    Resultado<List<Pais>> Listar(string? token);
    Resultado<ResultadoBusqueda> Buscar(string? token, string? nombre, string? capital, string? region);
    Resultado<List<Pais>> ConsultarApi(string? token, string nombre);
    Resultado<ResultadoImportacion> Importar(string? token, string? consulta, string nombre);
    Resultado<ResumenImportacion> ImportarTodos(string? token);
    Resultado<Pais> Registrar(string? token, string? consulta, string nombre);
    Resultado<Pais> Actualizar(string? token, string nombre);
    Resultado<object> Eliminar(string? token, string nombre);
}

/// <summary>Informes estadísticos (ECUD7).</summary>
public interface ReporteServicio
{
    Resultado<Informe> Generar(string? token);
}

/// <summary>Datos de la sesión abierta: el token se guarda mientras dure la sesión (ECUD1, paso 7).</summary>
public class Sesion
{
    public string? Usuario { get; private set; }
    private string? nombre;
    public string? Token { get; private set; }

    public void Iniciar(string usuario, DatosSesion datos)
    {
        Usuario = usuario;
        nombre = datos.Usuario;
        Token = datos.Token;
    }

    public void Cerrar()
    {
        Usuario = null;
        nombre = null;
        Token = null;
    }

    public bool EstaActiva() => !string.IsNullOrWhiteSpace(Token);

    /// <summary>Nombre que devolvió el servidor, o el usuario si no vino.</summary>
    public string? Nombre => string.IsNullOrWhiteSpace(nombre) ? Usuario : nombre;
}

/// <summary>Formatos y reglas de entrada compartidos por las pantallas.</summary>
public static class Formatos
{
    public const int MaxNombre = 100;

    /// <summary>Regiones válidas (ECUD, parámetros generales), en el orden del catálogo.</summary>
    public static readonly IReadOnlyList<string> Regiones = ["América", "Europa", "Asia", "África", "Oceanía", "Antártida"];

    public const string Todas = "Todas";

    public const string IngreseUsuario = "Ingrese el usuario";
    public const string IngreseClave = "Ingrese la clave";
    public const string IngreseNombrePais = "Ingrese el nombre del país";
    public const string NombreMuyLargo = "El nombre no puede superar 100 caracteres";

    private static readonly NumberFormatInfo Miles = new() { NumberGroupSeparator = ".", NumberGroupSizes = [3] };

    /// <summary>18001000 → «18.001.000» (la población se muestra con separador de miles).</summary>
    public static string Poblacion(long valor) => valor.ToString("#,##0", Miles);

    /// <summary>«1 país encontrado», «3 países encontrados» (ECUD5, paso 8).</summary>
    public static string Total(int cantidad) => cantidad == 1 ? "1 país encontrado" : cantidad + " países encontrados";

    /// <returns>el mensaje de error, o null si el nombre es válido (ECUD3, alterno 3).</returns>
    public static string? ErrorNombre(string? nombre)
    {
        string limpio = nombre?.Trim() ?? "";
        if (limpio.Length == 0)
            return IngreseNombrePais;
        return limpio.Length > MaxNombre ? NombreMuyLargo : null;
    }

    /// <summary>«Todas» o vacío significa sin filtro de región.</summary>
    public static string Region(string? seleccion)
        => string.IsNullOrWhiteSpace(seleccion) || seleccion == Todas ? "" : seleccion;
}
