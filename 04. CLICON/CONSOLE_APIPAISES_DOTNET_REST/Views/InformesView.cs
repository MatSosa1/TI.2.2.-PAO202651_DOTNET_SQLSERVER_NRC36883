using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views;

/// <summary>
/// ECUD7 Generar Informes: países por región, país más poblado y promedio poblacional por región.
/// La consola muestra solo tablas de texto, sin gráfico (ECUD7, paso 8).
/// </summary>
public static class InformesView
{
    /// <summary>Mensaje del servidor cuando no hay países (ECUD7, alterno 2).</summary>
    private const string SinPaises = "No hay países registrados para generar informes";

    private static readonly ReporteController Controller = new();

    public static void Show()
    {
        MessagesUtils.PrintTitle("INFORMES");
        MessagesUtils.PrintMessage("Procesando...");
        Respuesta<Informe> respuesta = Controller.GenerarAsync().GetAwaiter().GetResult();
        if (MenuView.RevisarSesion(respuesta))
            return;
        if (!respuesta.EsOk() || respuesta.Datos == null)
        {
            MessagesUtils.PrintError(respuesta.Mensaje);
            if (respuesta.Mensaje == SinPaises)
                MessagesUtils.PrintMessage("Use “Consultar país en la API” para guardar países.");
            return;
        }
        MessagesUtils.PrintMessage(respuesta.Mensaje);
        Informe informe = respuesta.Datos;

        Console.WriteLine("\nPaises por region");
        var conteos = (informe.PaisesPorRegion ?? []).Select(c => new[] { c.Region ?? "", c.Cantidad.ToString() }).ToList();
        MessagesUtils.PrintTable(["Región", "Países"], conteos, [false, true]);

        Pais? mayor = informe.PaisMasPoblado;
        if (mayor != null)
        {
            Console.WriteLine("\nPais mas poblado");
            Console.WriteLine(mayor.Nombre + " (" + mayor.Region + "): " + Formato.Poblacion(mayor.Poblacion) + " habitantes");
        }

        Console.WriteLine("\nPromedio poblacional por region");
        var promedios = (informe.PromedioPorRegion ?? [])
            .Select(p => new[] { p.Region ?? "", Formato.Poblacion(p.PromedioPoblacion) }).ToList();
        MessagesUtils.PrintTable(["Región", "Promedio de población"], promedios, [false, true]);
    }
}
