using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views;

/// <summary>
/// ECUD4 Gestionar Países: tabla de países con Nuevo, Actualizar y Eliminar, y el filtro por
/// región (ECUD6), que queda activo mientras el usuario está en esta pantalla.
/// </summary>
public static class PaisesView
{
    private static readonly PaisController Controller = new();

    public static void Show()
    {
        string region = "";
        while (ConfigHTTP.HaySesion())
        {
            MessagesUtils.PrintTitle("PAISES");
            List<Pais>? paises = Cargar(region);
            if (paises == null)
                return;

            if (region.Length > 0)
                Console.WriteLine("Filtro de region: " + region);
            if (paises.Count == 0)
            {
                MessagesUtils.PrintMessage(region.Length == 0
                    ? "No hay países registrados. Use “Consultar país en la API” para guardarlos"
                    : "No se encontraron países en esa región");
            }
            else
            {
                Tablas.Paises(paises, false);
                Console.WriteLine(Formato.Total(paises.Count));
            }

            Console.WriteLine();
            Console.WriteLine("1. Nuevo");
            Console.WriteLine("2. Actualizar");
            Console.WriteLine("3. Eliminar");
            Console.WriteLine("4. Filtrar por region");
            Console.WriteLine("5. Regresar");

            string opcion = MessagesUtils.ReadText("\nSeleccione una opcion");
            switch (opcion)
            {
                case "1": Nuevo(); break;
                case "2": Actualizar(paises); break;
                case "3": Eliminar(paises); break;
                case "4":
                    string? elegida = Tablas.ElegirRegion();
                    if (elegida != null)
                        region = elegida;
                    break;
                case "5": return;
                default: MessagesUtils.PrintError("Opcion invalida"); break;
            }
            Console.WriteLine();
        }
    }

    /// <summary>Listar (ECUD4) o, con una región activa, Filtrar (ECUD6). Null si hubo un error.</summary>
    private static List<Pais>? Cargar(string region)
    {
        MessagesUtils.PrintMessage("Procesando...");
        if (region.Length == 0)
        {
            Respuesta<List<Pais>> respuesta = Controller.ListarAsync().GetAwaiter().GetResult();
            return Datos(respuesta, respuesta.Datos);
        }
        Respuesta<ResultadoBusqueda> filtro = Controller.BuscarAsync(null, null, region).GetAwaiter().GetResult();
        return Datos(filtro, filtro.Datos?.Paises);
    }

    private static List<Pais>? Datos<T>(Respuesta<T> respuesta, List<Pais>? paises)
    {
        if (MenuView.RevisarSesion(respuesta))
            return null;
        if (!respuesta.EsOk())
        {
            MessagesUtils.PrintError(respuesta.Mensaje);
            return null;
        }
        return paises ?? [];
    }

    /// <summary>ECUD4 Registrar: consulta en la API, elige un país de los resultados y lo registra.</summary>
    private static void Nuevo()
    {
        MessagesUtils.PrintTitle("NUEVO PAIS");
        string consulta = MessagesUtils.ReadText("Nombre del pais a consultar en la API");
        string? error = ValidadorDatos.ErrorNombre(consulta);
        if (error != null)
        {
            MessagesUtils.PrintError(error);
            return;
        }

        MessagesUtils.PrintMessage("Procesando...");
        Respuesta<List<Pais>> resultados = Controller.ConsultarApiAsync(consulta).GetAwaiter().GetResult();
        if (MenuView.RevisarSesion(resultados))
            return;
        if (!resultados.EsOk())
        {
            MessagesUtils.PrintError(resultados.Mensaje);
            return;
        }
        List<Pais> paises = resultados.Datos ?? [];
        Tablas.Paises(paises, true);

        int numero = MessagesUtils.ReadOption("\nNumero del pais a registrar (Enter para cancelar)", paises.Count);
        if (numero == 0)
        {
            MessagesUtils.PrintMessage("Operacion cancelada");
            return;
        }
        if (numero < 0)
        {
            MessagesUtils.PrintError("Opcion invalida");
            return;
        }

        MessagesUtils.PrintMessage("Procesando...");
        MenuView.MostrarResultado(Controller.RegistrarAsync(consulta, paises[numero - 1].Nombre ?? "").GetAwaiter().GetResult());
    }

    /// <summary>ECUD4 Actualizar: el servidor vuelve a consultar la API y actualiza el país elegido.</summary>
    private static void Actualizar(List<Pais> paises)
    {
        Pais? pais = Elegir(paises, "Numero del pais a actualizar");
        if (pais == null)
            return;
        MessagesUtils.PrintMessage("Procesando...");
        MenuView.MostrarResultado(Controller.ActualizarAsync(pais.Nombre ?? "").GetAwaiter().GetResult());
    }

    /// <summary>ECUD4 Eliminar: pide confirmación antes de enviar (alterno 6).</summary>
    private static void Eliminar(List<Pais> paises)
    {
        Pais? pais = Elegir(paises, "Numero del pais a eliminar");
        if (pais == null)
            return;
        if (!MessagesUtils.Confirm("¿Eliminar el país " + pais.Nombre + "?"))
        {
            MessagesUtils.PrintMessage("Operacion cancelada");
            return;
        }
        MessagesUtils.PrintMessage("Procesando...");
        MenuView.MostrarResultado(Controller.EliminarAsync(pais.Nombre ?? "").GetAwaiter().GetResult());
    }

    private static Pais? Elegir(List<Pais> paises, string etiqueta)
    {
        if (paises.Count == 0)
        {
            MessagesUtils.PrintError("No hay países en la tabla");
            return null;
        }
        int numero = MessagesUtils.ReadOption(etiqueta + " (Enter para cancelar)", paises.Count);
        if (numero == 0)
        {
            MessagesUtils.PrintMessage("Operacion cancelada");
            return null;
        }
        if (numero < 0)
        {
            MessagesUtils.PrintError("Opcion invalida");
            return null;
        }
        return paises[numero - 1];
    }
}
