using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WS_APIPAISES_DOTNET_REST.Models.DTO;

namespace WS_APIPAISES_DOTNET_REST.Models.Entidades;

/// <summary>
/// Fila de la tabla Paises. <see cref="Region"/> es el nombre de la región (Regiones.Nombre),
/// que es lo que muestran los clientes; <see cref="IdRegion"/> es la clave foránea.
/// </summary>
public class Pais
{
    public string Nombre { get; set; } = "";
    public string Capital { get; set; } = "";

    /// <summary>Dato interno de la base: los clientes reciben el nombre de la región, no su id.</summary>
    [JsonIgnore]
    public int IdRegion { get; set; }

    /// <summary>Nombre de la región; lo llenan las consultas de PaisDAO (no es una columna).</summary>
    [NotMapped]
    public string Region { get; set; } = "";

    public long Poblacion { get; set; }
    public string Moneda { get; set; } = "";

    /// <summary>Navegación de EF Core hacia el catálogo (FK_PAISES_REGIONES).</summary>
    [JsonIgnore]
    public Region? RegionCatalogo { get; set; }

    public Pais()
    {
    }

    public Pais(string nombre, string capital, string region, long poblacion, string moneda)
    {
        Nombre = nombre;
        Capital = capital;
        Region = region;
        Poblacion = poblacion;
        Moneda = moneda;
    }

    /// <summary>País a guardar a partir de lo que devolvió la API.</summary>
    public static Pais Desde(PaisDTO dto)
    {
        return new Pais(dto.Nombre, dto.Capital, dto.Region, dto.Poblacion, dto.Moneda);
    }
}

/// <summary>Fila del catálogo Regiones (América, Europa, Asia, África, Oceanía, Antártida).</summary>
public class Region
{
    public int IdRegion { get; set; }
    public string Nombre { get; set; } = "";

    public List<Pais> Paises { get; set; } = new();

    public Region()
    {
    }

    public Region(int idRegion, string nombre)
    {
        IdRegion = idRegion;
        Nombre = nombre;
    }
}

/// <summary>Fila de la tabla Usuarios. La clave nunca se guarda en claro (RNF-006).</summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string ClaveHash { get; set; } = "";
    public string Nombre { get; set; } = "";

    public Usuario()
    {
    }

    public Usuario(int idUsuario, string usuario, string claveHash, string nombre)
    {
        IdUsuario = idUsuario;
        NombreUsuario = usuario;
        ClaveHash = claveHash;
        Nombre = nombre;
    }
}
