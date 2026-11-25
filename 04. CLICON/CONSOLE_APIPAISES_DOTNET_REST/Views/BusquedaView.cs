using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views;

/// <summary>
/// ECUD5 Buscar Países: nombre y capital por coincidencia parcial y región de una lista
/// (Todas, América, Europa, Asia, África, Oceanía, Antártida). Los criterios se combinan.
/// </summary>
public static class BusquedaView
{
    private static readonly PaisController Controller = new();

    public static void Show()
    {
        MessagesUtils.PrintTitle("BUSCAR PAISES");
        string nombre = MessagesUtils.ReadText("Nombre (Enter para omitir)");
        if (!Valido(nombre))
            return;
        string capital = MessagesUtils.ReadText("Capital (Enter para omitir)");
        if (!Valido(capital))
            return;
        string? region = Tablas.ElegirRegion();
        if (region == null)
            return;

        MessagesUtils.PrintMessage("Procesando...");
        Respuesta<ResultadoBusqueda> respuesta = Controller.BuscarAsync(nombre, capital, region).GetAwaiter().GetResult();
        if (MenuView.RevisarSesion(respuesta))
            return;
        if (!respuesta.EsOk() || respuesta.Datos == null)
        {
            MessagesUtils.PrintError(respuesta.Mensaje);
            return;
        }

        ResultadoBusqueda resultado = respuesta.Datos;
        if (resultado.Paises == null || resultado.Paises.Count == 0)
        {
            MessagesUtils.PrintMessage("No se encontraron países con esos criterios");
            return;
        }
        MessagesUtils.PrintMessage(respuesta.Mensaje);
        Tablas.Paises(resultado.Paises, false);
        Console.WriteLine(Formato.Total(resultado.Total));
    }

    /// <summary>Un criterio vacío se omite; con texto no puede superar 100 caracteres.</summary>
    private static bool Valido(string criterio)
    {
        if (criterio.Length > ValidadorDatos.MaxNombre)
        {
            MessagesUtils.PrintError(ValidadorDatos.NombreMuyLargo);
            return false;
        }
        return true;
    }
}
