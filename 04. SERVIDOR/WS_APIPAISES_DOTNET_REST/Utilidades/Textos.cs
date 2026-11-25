using System.Globalization;
using System.Text;

namespace WS_APIPAISES_DOTNET_REST.Utilidades;

/// <summary>Utilidades de texto que comparten el servicio de la API y los DAO.</summary>
public static class Textos
{
    /// <summary>Longitud máxima de las columnas de texto de Paises.</summary>
    public const int MaxLongitud = 100;

    /// <summary>
    /// Clave de comparación sin tildes ni mayúsculas. Es la misma regla que aplica la
    /// collation Latin1_General_100_CI_AI de SQL Server: «Perú» y «peru» son el mismo país.
    /// </summary>
    public static string Clave(string? texto)
    {
        if (texto == null)
        {
            return "";
        }
        string descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(descompuesto.Length);
        foreach (char c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sinTildes.Append(c);
            }
        }
        return sinTildes.ToString().ToLowerInvariant();
    }

    public static bool Vacio(string? texto) => string.IsNullOrWhiteSpace(texto);

    /// <summary>Recorta el texto para que quepa en una columna NVARCHAR(100).</summary>
    public static string? Recortar(string? texto)
    {
        if (texto == null)
        {
            return null;
        }
        string limpio = texto.Trim();
        return limpio.Length <= MaxLongitud ? limpio : limpio[..MaxLongitud];
    }
}
