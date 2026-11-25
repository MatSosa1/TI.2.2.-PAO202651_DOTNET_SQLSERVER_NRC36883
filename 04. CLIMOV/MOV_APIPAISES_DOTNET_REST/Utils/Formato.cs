using System.Globalization;

namespace MOV_APIPAISES_DOTNET_REST.Utils;

/// <summary>Formatos de presentación: la población con separador de miles (ECUD, datos del país).</summary>
public static class Formato
{
    public const string SinDato = "—";

    private static readonly NumberFormatInfo Miles = new() { NumberGroupSeparator = ".", NumberGroupSizes = [3] };

    /// <summary>18001000 → «18.001.000».</summary>
    public static string Poblacion(long valor) => valor.ToString("#,##0", Miles);

    /// <summary>«1 país encontrado», «3 países encontrados» (ECUD5, paso 8).</summary>
    public static string Total(int cantidad) => cantidad == 1 ? "1 país encontrado" : cantidad + " países encontrados";

    /// <summary>Texto o «—» si viene vacío.</summary>
    public static string Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? SinDato : valor;
}
