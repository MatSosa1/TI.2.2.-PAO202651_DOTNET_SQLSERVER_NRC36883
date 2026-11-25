using MOV_APIPAISES_DOTNET_REST.Modelos.Data;
using MOV_APIPAISES_DOTNET_REST.Modelos.Repositorios;
using MOV_APIPAISES_DOTNET_REST.Servicios;

namespace MOV_APIPAISES_DOTNET_REST.Controlador;

/// <summary>
/// Validación en el cliente antes de enviar (ERS 9.1.1): usuario y clave no vacíos, nombre de
/// país no vacío y de hasta 100 caracteres, región elegida de la lista.
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

    /// <summary>Un criterio de búsqueda puede ir vacío, pero no superar 100 caracteres.</summary>
    public static string? ErrorCriterio(string? criterio) => Limpiar(criterio).Length > MaxNombre ? NombreMuyLargo : null;
}

/// <summary>
/// Base de los controladores con sesión: si el servidor rechaza el token (ECUD8), se borra la
/// sesión local para que la vista vuelva al inicio de sesión.
/// </summary>
public abstract class ControladorConSesion
{
    public const string MensajeSesionNoValida = "Su sesión no es válida. Inicie sesión nuevamente.";

    protected readonly SesionServicio sesion;

    protected ControladorConSesion(SesionServicio sesion)
    {
        this.sesion = sesion;
    }

    protected string? Token => sesion.Token;

    public bool HaySesion => sesion.EstaAutenticado;

    public Sesion? Sesion => sesion.Sesion;

    protected Resultado<T> RevisarSesion<T>(Resultado<T> resultado)
    {
        if (resultado.SesionExpirada)
            sesion.Limpiar();
        return resultado;
    }
}

/// <summary>ECUD1 Iniciar y cerrar sesión.</summary>
public class LoginControlador : ControladorConSesion
{
    public enum Campo { Usuario, Clave }

    private readonly ApiPaisesRepositorio repositorio;

    public LoginControlador(ApiPaisesRepositorio repositorio, SesionServicio sesion) : base(sesion)
    {
        this.repositorio = repositorio;
    }

    /// <summary>ECUD1 alterno 2: el primer campo vacío, o null si los dos tienen valor.</summary>
    public Campo? CampoInvalido(string? usuario, string? clave)
    {
        if (ValidadorDatos.Limpiar(usuario).Length == 0)
            return Campo.Usuario;
        return string.IsNullOrEmpty(clave) ? Campo.Clave : null;
    }

    public static string Mensaje(Campo campo) => campo == Campo.Usuario ? ValidadorDatos.IngreseUsuario : ValidadorDatos.IngreseClave;

    /// <summary>ECUD1 pasos 4 a 7: si las credenciales son correctas, guarda la sesión.</summary>
    public async Task<Resultado<Sesion>> IniciarSesionAsync(string? usuario, string? clave)
    {
        if (CampoInvalido(usuario, clave) is Campo campo)
            return Resultado<Sesion>.Error(Mensaje(campo));
        Resultado<Sesion> resultado = await repositorio.LoginAsync(ValidadorDatos.Limpiar(usuario), clave!);
        if (resultado.Exito)
            sesion.Iniciar(resultado.Datos!);
        return resultado;
    }

    /// <summary>ECUD1 alterno 4: borra la sesión local y devuelve el token para avisar al servidor.</summary>
    public string? CerrarSesion()
    {
        string? token = Token;
        sesion.Limpiar();
        return token;
    }

    public async Task NotificarCierreAsync(string? token)
    {
        if (!string.IsNullOrEmpty(token))
            await repositorio.LogoutAsync(token);
    }
}

/// <summary>
/// ECUD2 a ECUD7: listar, buscar y filtrar, consultar en la API, importar, registrar, actualizar,
/// eliminar e informes.
/// </summary>
public class PaisesControlador : ControladorConSesion
{
    private readonly ApiPaisesRepositorio repositorio;

    public PaisesControlador(ApiPaisesRepositorio repositorio, SesionServicio sesion) : base(sesion)
    {
        this.repositorio = repositorio;
    }

    /// <summary>Listar (sin criterios), Buscar (nombre o capital) o Filtrar (solo la región).</summary>
    public async Task<Resultado<List<Pais>>> BuscarAsync(string? nombre, string? capital, string? region)
    {
        string? error = ValidadorDatos.ErrorCriterio(nombre) ?? ValidadorDatos.ErrorCriterio(capital);
        if (error != null)
            return Resultado<List<Pais>>.Error(error);
        string regionValida = ValidadorDatos.Regiones.Contains(ValidadorDatos.Limpiar(region)) ? ValidadorDatos.Limpiar(region) : "";
        bool sinCriterios = ValidadorDatos.Limpiar(nombre).Length == 0 && ValidadorDatos.Limpiar(capital).Length == 0
                            && regionValida.Length == 0;
        if (sinCriterios)
        {
            Resultado<List<Pais>> todos = RevisarSesion(await repositorio.ListarAsync(Token));
            return todos.Exito ? Resultado<List<Pais>>.Ok(todos.Datos ?? [], todos.Mensaje) : todos;
        }
        Resultado<Busqueda> resultado = RevisarSesion(await repositorio.BuscarAsync(Token, nombre, capital, regionValida));
        if (!resultado.Exito)
            return CopiarError<List<Pais>>(resultado);
        return Resultado<List<Pais>>.Ok(resultado.Datos?.Paises ?? [], resultado.Mensaje);
    }

    public async Task<Resultado<Pais>> ActualizarAsync(string nombre) => RevisarSesion(await repositorio.ActualizarAsync(Token, nombre));

    public async Task<Resultado<object>> EliminarAsync(string nombre) => RevisarSesion(await repositorio.EliminarAsync(Token, nombre));

    public async Task<Resultado<List<Pais>>> ConsultarApiAsync(string? nombre)
    {
        string? error = ValidadorDatos.ErrorNombre(nombre);
        if (error != null)
            return Resultado<List<Pais>>.Error(error);
        Resultado<List<Pais>> resultado = RevisarSesion(await repositorio.ConsultarApiAsync(Token, nombre!.Trim()));
        return resultado.Exito ? Resultado<List<Pais>>.Ok(resultado.Datos ?? [], resultado.Mensaje) : resultado;
    }

    public async Task<Resultado<Pais>> RegistrarAsync(string? consulta, string nombre)
        => RevisarSesion(await repositorio.RegistrarAsync(Token, consulta, nombre));

    /// <summary>ECUD2: el mensaje lleva la acción realizada, p. ej. «País importado correctamente (registrado)».</summary>
    public async Task<Resultado<Importacion>> ImportarAsync(string? consulta, string nombre)
    {
        Resultado<Importacion> resultado = RevisarSesion(await repositorio.ImportarAsync(Token, consulta, nombre));
        if (resultado.Exito && resultado.Datos?.Accion != null)
            return Resultado<Importacion>.Ok(resultado.Datos, resultado.Mensaje + " (" + resultado.Datos.Accion + ")");
        return resultado;
    }

    public async Task<Resultado<Resumen>> ImportarTodosAsync()
    {
        Resultado<Resumen> resultado = RevisarSesion(await repositorio.ImportarTodosAsync(Token));
        Resumen? resumen = resultado.Datos;
        if (resultado.Exito && resumen != null)
            return Resultado<Resumen>.Ok(resumen, resultado.Mensaje + ": " + resumen.Recibidos + " recibidos, "
                                                   + resumen.Registrados + " registrados, " + resumen.Actualizados + " actualizados");
        return resultado;
    }

    public async Task<Resultado<Informe>> InformesAsync() => RevisarSesion(await repositorio.InformesAsync(Token));

    private static Resultado<T> CopiarError<T>(Resultado<Busqueda> original)
        => original.SesionExpirada ? Resultado<T>.Expirada(original.Mensaje) : Resultado<T>.Error(original.Mensaje);
}
