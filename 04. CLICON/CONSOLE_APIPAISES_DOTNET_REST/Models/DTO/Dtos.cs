using System.Text.Json.Serialization;
using CONSOLE_APIPAISES_DOTNET_REST.Models.Enums;

namespace CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;

/// <summary>
/// Respuesta estructurada del servidor: estado, mensaje y datos (ECUD10). <see cref="CodigoHttp"/>
/// no viene en el JSON: lo agrega el cliente para reconocer el 401 de sesión no válida.
/// </summary>
public class Respuesta<T>
{
    public EstadosEnum? Estado { get; set; }

    private string? mensaje;
    public string Mensaje
    {
        get => mensaje ?? "";
        set => mensaje = value;
    }

    public T? Datos { get; set; }

    [JsonIgnore]
    public int CodigoHttp { get; set; }

    public Respuesta()
    {
    }

    public Respuesta(EstadosEnum estado, string mensaje, T? datos)
    {
        Estado = estado;
        Mensaje = mensaje;
        Datos = datos;
    }

    /// <summary>Error generado en el cliente (sin conexión, respuesta ilegible...).</summary>
    public static Respuesta<T> Error(string mensaje) => new(EstadosEnum.ERROR, mensaje, default);

    public bool EsOk() => Estado == EstadosEnum.OK;

    /// <summary>ECUD8: el servidor rechazó el token (401); el cliente debe borrar la sesión.</summary>
    public bool SesionNoValida() => CodigoHttp == 401;
}

/// <summary>Datos de un inicio de sesión correcto: el token y el nombre del usuario (ECUD1, paso 6).</summary>
public record LoginDatos(string? Token, string? Usuario);

/// <summary>
/// País guardado o consultado en la API. <c>Guardado</c> solo llega en la consulta a la API
/// (ECUD3, paso 6): indica si ya está en la base de datos local.
/// </summary>
public record Pais(string? Nombre, string? Capital, string? Region, long Poblacion, string? Moneda, bool Guardado);

/// <summary>Países encontrados por Buscar o Filtrar y el total (ECUD5, ECUD6).</summary>
public record ResultadoBusqueda(List<Pais>? Paises, int Total);

/// <summary>País importado y si fue «registrado» o «actualizado» (ECUD2, paso 8).</summary>
public record ResultadoImportacion(Pais? Pais, string? Accion);

/// <summary>Resultado de «Importar todos»: países recibidos de la API, registrados y actualizados.</summary>
public record ResumenImportacion(int Recibidos, int Registrados, int Actualizados);

/// <summary>Los tres informes estadísticos de GET /api/Reportes (ECUD7).</summary>
public record Informe(List<ConteoRegion>? PaisesPorRegion, Pais? PaisMasPoblado, List<PromedioRegion>? PromedioPorRegion);

/// <summary>Cantidad de países de una región.</summary>
public record ConteoRegion(string? Region, int Cantidad);

/// <summary>Promedio de población de una región.</summary>
public record PromedioRegion(string? Region, long PromedioPoblacion);
