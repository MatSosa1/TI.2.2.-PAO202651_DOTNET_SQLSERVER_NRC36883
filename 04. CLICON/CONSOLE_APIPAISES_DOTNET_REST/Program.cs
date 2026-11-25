using CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;
using CONSOLE_APIPAISES_DOTNET_REST.Views;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

// Cliente de consola de APIPaises. Uso: CONSOLE_APIPAISES_DOTNET_REST [url del servidor].
// La url por defecto está en cliente.properties.
MessagesUtils.PrepararConsola();
if (args.Length > 0)
    ConfigHTTP.SetBaseUrl(args[0]);

// Arte de inicio
MessagesUtils.PrintMonster();
MessagesUtils.Esperar(2);

// Inicio de sesión y menú; al cerrar sesión (o si el token deja de valer) se vuelve al inicio de sesión
while (LoginView.UserHasLogin())
{
    MenuView.Show();
}

MessagesUtils.PrintMessage("Hasta Luego!");
MessagesUtils.Esperar(1);
