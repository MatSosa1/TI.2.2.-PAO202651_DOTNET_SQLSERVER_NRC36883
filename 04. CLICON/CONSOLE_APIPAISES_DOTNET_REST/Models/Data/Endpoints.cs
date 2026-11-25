namespace CONSOLE_APIPAISES_DOTNET_REST.Models.Data;

/// <summary>Rutas de los servicios del servidor, relativas a la dirección base (…/api).</summary>
public static class Endpoints
{
    public const string Login = "/Login/login";
    public const string Logout = "/Login/logout";
    public const string Paises = "/Paises";
    public const string Buscar = "/Paises/buscar";
    public const string Externo = "/Paises/externo/";
    public const string Importar = "/Paises/importar";
    public const string ImportarTodos = "/Paises/importar/todos";
    public const string Reportes = "/Reportes";

    /// <summary>País como segmento de la ruta (espacios y tildes codificados).</summary>
    public static string Segmento(string texto) => Codificar(texto);

    /// <summary>Parámetro de consulta «&amp;nombre=valor», o vacío si no tiene valor.</summary>
    public static string Parametro(string nombre, string? valor)
        => string.IsNullOrWhiteSpace(valor) ? "" : "&" + nombre + "=" + Codificar(valor.Trim());

    /// <summary>Une los parámetros: el primero lleva «?» en lugar de «&amp;».</summary>
    public static string Consulta(params string[] parametros)
    {
        string unidos = string.Concat(parametros);
        return unidos.Length == 0 ? "" : "?" + unidos[1..];
    }

    private static string Codificar(string texto) => Uri.EscapeDataString(texto.Trim());
}
