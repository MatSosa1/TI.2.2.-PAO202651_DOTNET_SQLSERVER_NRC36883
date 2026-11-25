namespace MOV_APIPAISES_DOTNET_REST.Modelos.Data;

/// <summary>Sesión abierta: el token y el nombre del usuario que devolvió el servidor (ECUD1, paso 6).</summary>
public class Sesion
{
    public string? Token { get; set; }
    public string? Usuario { get; set; }

    /// <summary>Nombre visible del usuario.</summary>
    public string Nombre => Usuario ?? "";
}

/// <summary>
/// País guardado o consultado en la API. <c>Guardado</c> solo llega en la consulta a la API
/// (ECUD3, paso 6): indica si ya está en la base de datos local.
/// </summary>
public class Pais
{
    public string? Nombre { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }
    public long Poblacion { get; set; }
    public string? Moneda { get; set; }
    public bool Guardado { get; set; }
}

/// <summary>Países encontrados por Buscar o Filtrar y el total (ECUD5, ECUD6).</summary>
public class Busqueda
{
    public List<Pais>? Paises { get; set; }
    public int Total { get; set; }
}

/// <summary>País importado y si fue «registrado» o «actualizado» (ECUD2, paso 8).</summary>
public class Importacion
{
    public Pais? Pais { get; set; }
    public string? Accion { get; set; }
}

/// <summary>Resultado de «Importar todos»: recibidos de la API, registrados y actualizados.</summary>
public class Resumen
{
    public int Recibidos { get; set; }
    public int Registrados { get; set; }
    public int Actualizados { get; set; }
}

/// <summary>Los tres informes estadísticos (ECUD7).</summary>
public class Informe
{
    public List<ConteoRegion> PaisesPorRegion { get; set; } = [];
    public Pais? PaisMasPoblado { get; set; }
    public List<PromedioRegion> PromedioPorRegion { get; set; } = [];
}

public class ConteoRegion
{
    public string? Region { get; set; }
    public int Cantidad { get; set; }
}

public class PromedioRegion
{
    public string? Region { get; set; }
    public long PromedioPoblacion { get; set; }
}

/// <summary>
/// Resultado de una operación: éxito con datos y el mensaje del servidor, error con el mensaje
/// para el usuario, o sesión expirada (el servidor rechazó el token, ECUD8).
/// </summary>
public sealed class Resultado<T>
{
    public bool Exito { get; }
    public T? Datos { get; }
    public string Mensaje { get; }
    public bool SesionExpirada { get; }

    private Resultado(bool exito, T? datos, string? mensaje, bool sesionExpirada)
    {
        Exito = exito;
        Datos = datos;
        Mensaje = mensaje ?? "";
        SesionExpirada = sesionExpirada;
    }

    public static Resultado<T> Ok(T? datos, string? mensaje) => new(true, datos, mensaje, false);

    public static Resultado<T> Error(string mensaje) => new(false, default, mensaje, false);

    public static Resultado<T> Expirada(string mensaje) => new(false, default, mensaje, true);
}
