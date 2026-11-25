using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views;

/// <summary>ECUD1: formulario de inicio de sesión con usuario y clave (la clave se lee oculta).</summary>
public static class LoginView
{
    private static readonly LoginController Controller = new();

    /// <summary>Devuelve true al iniciar sesión, o false si el usuario elige salir.</summary>
    public static bool UserHasLogin()
    {
        while (true)
        {
            MessagesUtils.PrintTitle("INICIAR SESION");
            Console.WriteLine("APIPaises - Servicio REST / .NET\n");
            Console.WriteLine("1. Ingresar");
            Console.WriteLine("2. Salir");

            string opcion = MessagesUtils.ReadText("\nSeleccione una opcion");
            switch (opcion)
            {
                case "1":
                    if (Ingresar())
                        return true;
                    break;
                case "2":
                    return false;
                default:
                    MessagesUtils.PrintError("Opcion invalida");
                    break;
            }
            Console.WriteLine();
        }
    }

    /// <summary>ECUD1 pasos 3 a 7 y alternos 1 a 3.</summary>
    private static bool Ingresar()
    {
        string usuario = MessagesUtils.ReadText("Usuario");
        if (usuario.Length == 0)
        {
            MessagesUtils.PrintError(ValidadorDatos.IngreseUsuario);
            return false;
        }
        string clave = MessagesUtils.ReadPassword("Clave");
        if (clave.Length == 0)
        {
            MessagesUtils.PrintError(ValidadorDatos.IngreseClave);
            return false;
        }

        MessagesUtils.PrintMessage("Procesando...");
        Respuesta<LoginDatos> respuesta = Controller.LoginAsync(usuario, clave).GetAwaiter().GetResult();
        if (!respuesta.EsOk())
        {
            // Credenciales incorrectas o sin servidor: se mantiene el formulario para otro intento
            MessagesUtils.PrintError(respuesta.Mensaje);
            return false;
        }
        MessagesUtils.PrintMessage(respuesta.Mensaje);
        MessagesUtils.Esperar(1);
        return true;
    }
}
