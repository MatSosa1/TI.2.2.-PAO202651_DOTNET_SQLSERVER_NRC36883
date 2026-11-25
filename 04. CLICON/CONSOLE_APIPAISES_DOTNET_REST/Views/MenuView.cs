using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views;

/// <summary>ECUD1 paso 7: menú principal con Países, Consultar país en la API, Buscar países, Informes y Cerrar sesión.</summary>
public static class MenuView
{
    public static void Show()
    {
        while (ConfigHTTP.HaySesion())
        {
            MessagesUtils.PrintTitle("MENU PRINCIPAL");
            Console.WriteLine(ConfigHTTP.Nombre + "\n");

            Console.WriteLine("1. Paises");
            Console.WriteLine("2. Consultar pais en la API");
            Console.WriteLine("3. Buscar paises");
            Console.WriteLine("4. Informes");
            Console.WriteLine("5. Cerrar sesion");

            string opcion = MessagesUtils.ReadText("\nSeleccione una opcion");
            switch (opcion)
            {
                case "1": PaisesView.Show(); break;
                case "2": ConsultaApiView.Show(); break;
                case "3": BusquedaView.Show(); break;
                case "4": InformesView.Show(); break;
                case "5": CerrarSesion(); break;
                default: MessagesUtils.PrintError("Opcion invalida"); break;
            }
            Console.WriteLine();
        }
    }

    /// <summary>ECUD1 alterno 4: se borra la sesión y se vuelve al inicio aunque el servidor no responda.</summary>
    private static void CerrarSesion()
    {
        Respuesta<object> respuesta = new LoginController().LogoutAsync().GetAwaiter().GetResult();
        MessagesUtils.PrintMessage(respuesta.EsOk() ? respuesta.Mensaje : "Sesión cerrada");
    }

    /// <summary>
    /// ECUD8 alterno 2: con el token rechazado se informa, se borra la sesión y se vuelve al
    /// inicio de sesión. Devuelve true si la sesión dejó de ser válida.
    /// </summary>
    public static bool RevisarSesion<T>(Respuesta<T> respuesta)
    {
        if (!respuesta.SesionNoValida())
            return false;
        MessagesUtils.PrintError(ErroresCliente.SesionNoValida);
        ConfigHTTP.Limpiar();
        return true;
    }

    /// <summary>Muestra el mensaje de una operación: normal si salió bien, como error si no.</summary>
    public static bool MostrarResultado<T>(Respuesta<T> respuesta)
    {
        if (RevisarSesion(respuesta))
            return false;
        if (respuesta.EsOk())
        {
            MessagesUtils.PrintMessage(respuesta.Mensaje);
            return true;
        }
        MessagesUtils.PrintError(respuesta.Mensaje);
        return false;
    }
}
