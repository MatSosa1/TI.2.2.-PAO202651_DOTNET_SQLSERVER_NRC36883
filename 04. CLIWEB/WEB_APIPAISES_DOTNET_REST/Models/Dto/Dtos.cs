using System.Globalization;

namespace WEB_APIPAISES_DOTNET_REST.Models.Dto;

/// <summary>Formatos y reglas de entrada comunes (ERS 9.1.1 y ECUD).</summary>
public static class Formatos
{
    public const int MaxNombre = 100;

    /// <summary>Regiones válidas (ECUD, parámetros generales), en el orden del catálogo.</summary>
    public static readonly IReadOnlyList<string> Regiones = ["América", "Europa", "Asia", "África", "Oceanía", "Antártida"];

    public const string IngreseUsuario = "Ingrese el usuario";
    public const string IngreseClave = "Ingrese la clave";
    public const string IngreseNombrePais = "Ingrese el nombre del país";
    public const string NombreMuyLargo = "El nombre no puede superar 100 caracteres";

    private static readonly NumberFormatInfo Miles = new() { NumberGroupSeparator = ".", NumberGroupSizes = [3] };

    /// <summary>18001000 → «18.001.000».</summary>
    public static string Poblacion(long valor) => valor.ToString("#,##0", Miles);

    /// <summary>«1 país encontrado», «3 países encontrados» (ECUD5, paso 8).</summary>
    public static string Total(int cantidad) => cantidad == 1 ? "1 país encontrado" : cantidad + " países encontrados";

    /// <returns>el mensaje de error, o null si el nombre es válido (ECUD3, alterno 3).</returns>
    public static string? ErrorNombre(string? nombre)
    {
        string limpio = Limpiar(nombre);
        if (limpio.Length == 0)
            return IngreseNombrePais;
        return limpio.Length > MaxNombre ? NombreMuyLargo : null;
    }

    /// <summary>Región de la lista, o "" si es «Todas», vacía o no es una de las seis.</summary>
    public static string Region(string? valor)
    {
        string limpio = Limpiar(valor);
        return Regiones.Contains(limpio) ? limpio : "";
    }

    public static string Limpiar(string? texto) => texto?.Trim() ?? "";
}

/// <summary>
/// País guardado o consultado en la API, con los campos del JSON del servidor.
/// <c>Guardado</c> solo llega en la consulta a la API (ECUD3, paso 6).
/// </summary>
public class Pais
{
    public string? Nombre { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }
    public long Poblacion { get; set; }
    public string? Moneda { get; set; }
    public bool Guardado { get; set; }

    /// <summary>Población con separador de miles (ECUD, datos del país).</summary>
    public string PoblacionFormateada => Formatos.Poblacion(Poblacion);
}

/// <summary>Datos de un inicio de sesión correcto.</summary>
public class DatosSesion
{
    public string? Token { get; set; }
    public string? Usuario { get; set; }
}

/// <summary>Datos de GET /api/Paises/buscar (ECUD5, ECUD6).</summary>
public class ResultadoBusqueda
{
    public List<Pais>? Paises { get; set; }
    public int Total { get; set; }
}

/// <summary>País importado y si fue «registrado» o «actualizado» (ECUD2).</summary>
public class ResultadoImportacion
{
    public Pais? Pais { get; set; }
    public string? Accion { get; set; }
}

/// <summary>Resultado de «Importar todos».</summary>
public class ResumenImportacion
{
    public int Recibidos { get; set; }
    public int Registrados { get; set; }
    public int Actualizados { get; set; }
}

/// <summary>
/// Los tres informes de GET /api/Reportes (ECUD7) y las barras del gráfico de países por
/// región, calculadas aquí para dibujarlas en la vista como SVG (sin librerías de JavaScript).
/// </summary>
public class Informe
{
    /// <summary>Medidas del gráfico en unidades del viewBox del SVG.</summary>
    public const int AnchoGrafico = 600;
    public const int AltoGrafico = 240;
    public const int BaseGrafico = 200;
    private const int AltoMaximo = 160;

    public List<ConteoRegion>? PaisesPorRegion { get; set; }
    public Pais? PaisMasPoblado { get; set; }
    public List<PromedioRegion>? PromedioPorRegion { get; set; }

    /// <summary>Una barra por región, proporcional a la región con más países.</summary>
    public List<Barra> Barras
    {
        get
        {
            List<ConteoRegion> datos = PaisesPorRegion ?? [];
            var barras = new List<Barra>();
            if (datos.Count == 0)
                return barras;
            int maximo = Math.Max(1, datos.Max(d => d.Cantidad));
            int columna = AnchoGrafico / datos.Count;
            int ancho = (int)(columna * 0.6);
            for (int i = 0; i < datos.Count; i++)
            {
                ConteoRegion dato = datos[i];
                int alto = (int)Math.Round((double)dato.Cantidad / maximo * AltoMaximo, MidpointRounding.AwayFromZero);
                int x = i * columna + (columna - ancho) / 2;
                barras.Add(new Barra(dato.Region ?? "", dato.Cantidad, x, BaseGrafico - alto, ancho, alto, i * columna + columna / 2));
            }
            return barras;
        }
    }
}

/// <summary>Cantidad de países de una región.</summary>
public class ConteoRegion
{
    public string? Region { get; set; }
    public int Cantidad { get; set; }
}

/// <summary>Promedio de población de una región.</summary>
public class PromedioRegion
{
    public string? Region { get; set; }
    public long PromedioPoblacion { get; set; }
    public string PromedioFormateado => Formatos.Poblacion(PromedioPoblacion);
}

/// <summary>Rectángulo de una barra y posición de sus textos (centro de la columna).</summary>
public record Barra(string Region, int Cantidad, int X, int Y, int Ancho, int Alto, int Centro);
