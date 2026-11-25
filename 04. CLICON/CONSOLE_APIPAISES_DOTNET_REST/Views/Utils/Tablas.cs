using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

/// <summary>Tablas de texto de países y selección de región, comunes a las pantallas.</summary>
public static class Tablas
{
    /// <summary>
    /// Tabla con N.º, Nombre, Capital, Región, Población y Moneda (ECUD4 paso 4). Con
    /// <paramref name="conGuardado"/> agrega la columna «Guardado» de la consulta a la API (ECUD3 paso 7).
    /// </summary>
    public static void Paises(List<Pais> paises, bool conGuardado)
    {
        string[] encabezados = conGuardado
            ? ["N.º", "Nombre", "Capital", "Región", "Población", "Moneda", "Guardado"]
            : ["N.º", "Nombre", "Capital", "Región", "Población", "Moneda"];
        bool[] derecha = conGuardado
            ? [true, false, false, false, true, false, false]
            : [true, false, false, false, true, false];

        var filas = new List<string[]>();
        for (int i = 0; i < paises.Count; i++)
        {
            Pais p = paises[i];
            var fila = new List<string>
            {
                (i + 1).ToString(),
                Formato.Texto(p.Nombre),
                Formato.Texto(p.Capital),
                Formato.Texto(p.Region),
                Formato.Poblacion(p.Poblacion),
                Formato.Texto(p.Moneda),
            };
            if (conGuardado)
                fila.Add(Formato.SiNo(p.Guardado));
            filas.Add(fila.ToArray());
        }
        Console.WriteLine();
        MessagesUtils.PrintTable(encabezados, filas, derecha);
    }

    /// <summary>Lista numerada de regiones (ECUD5 paso 2, ECUD6 paso 1).</summary>
    /// <returns>la región elegida, "" para «Todas» o Enter, o null si la opción no es válida</returns>
    public static string? ElegirRegion()
    {
        IReadOnlyList<string> regiones = ValidadorDatos.Regiones;
        Console.WriteLine("0. Todas");
        for (int i = 0; i < regiones.Count; i++)
            Console.WriteLine((i + 1) + ". " + regiones[i]);

        string texto = MessagesUtils.ReadText("Region (Enter = Todas)");
        if (texto.Length == 0 || texto == "0")
            return "";
        if (int.TryParse(texto, out int opcion) && opcion >= 1 && opcion <= regiones.Count)
            return regiones[opcion - 1];
        MessagesUtils.PrintError("Opcion invalida");
        return null;
    }
}
