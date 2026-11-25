using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;
using CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Views;

/// <summary>
/// ECUD3 Consultar país en la API y ECUD2 Importar: tabla de resultados con la columna
/// «Guardado» y opción para guardar el país elegido. También «Importar todos».
/// </summary>
public static class ConsultaApiView
{
    private static readonly PaisController Controller = new();

    public static void Show()
    {
        while (ConfigHTTP.HaySesion())
        {
            MessagesUtils.PrintTitle("CONSULTAR PAIS EN LA API");
            Console.WriteLine("1. Consultar pais por nombre");
            Console.WriteLine("2. Importar todos los paises");
            Console.WriteLine("3. Regresar");

            string opcion = MessagesUtils.ReadText("\nSeleccione una opcion");
            switch (opcion)
            {
                case "1": Consultar(); break;
                case "2": ImportarTodos(); break;
                case "3": return;
                default: MessagesUtils.PrintError("Opcion invalida"); break;
            }
            Console.WriteLine();
        }
    }

    /// <summary>ECUD3 pasos 2 a 7; luego ECUD2 para cada país que el usuario decida guardar.</summary>
    private static void Consultar()
    {
        string consulta = MessagesUtils.ReadText("Nombre del pais");
        string? error = ValidadorDatos.ErrorNombre(consulta);
        if (error != null)
        {
            MessagesUtils.PrintError(error);
            return;
        }

        MessagesUtils.PrintMessage("Procesando...");
        Respuesta<List<Pais>> respuesta = Controller.ConsultarApiAsync(consulta).GetAwaiter().GetResult();
        if (MenuView.RevisarSesion(respuesta))
            return;
        if (!respuesta.EsOk())
        {
            MessagesUtils.PrintError(respuesta.Mensaje);
            return;
        }
        MessagesUtils.PrintMessage(respuesta.Mensaje);

        List<Pais> paises = new(respuesta.Datos ?? []);
        while (ConfigHTTP.HaySesion())
        {
            Tablas.Paises(paises, true);
            int numero = MessagesUtils.ReadOption("\nNumero del pais a guardar (Enter para regresar)", paises.Count);
            if (numero == 0)
                return;
            if (numero < 0)
            {
                MessagesUtils.PrintError("Opcion invalida");
                continue;
            }
            Pais elegido = paises[numero - 1];
            if (Guardar(consulta, elegido))
                paises[numero - 1] = elegido with { Guardado = true };
        }
    }

    /// <summary>ECUD2 pasos 2 a 9. Devuelve true si el país quedó guardado.</summary>
    private static bool Guardar(string consulta, Pais pais)
    {
        if (!MessagesUtils.Confirm("¿Guardar el país " + pais.Nombre + " en la base de datos local?"))
        {
            MessagesUtils.PrintMessage("Operacion cancelada");
            return false;
        }
        MessagesUtils.PrintMessage("Procesando...");
        Respuesta<ResultadoImportacion> respuesta = Controller.ImportarAsync(consulta, pais.Nombre ?? "").GetAwaiter().GetResult();
        if (MenuView.RevisarSesion(respuesta))
            return false;
        if (!respuesta.EsOk())
        {
            MessagesUtils.PrintError(respuesta.Mensaje);
            return false;
        }
        string accion = respuesta.Datos == null ? "" : " (" + respuesta.Datos.Accion + ")";
        MessagesUtils.PrintMessage(respuesta.Mensaje + accion);
        return true;
    }

    /// <summary>Importa todos los países de la API: los nuevos se registran y los existentes se actualizan.</summary>
    private static void ImportarTodos()
    {
        if (!MessagesUtils.Confirm("¿Importar todos los países desde la API REST Countries?"))
        {
            MessagesUtils.PrintMessage("Operacion cancelada");
            return;
        }
        MessagesUtils.PrintMessage("Procesando... (puede tardar unos segundos)");
        Respuesta<ResumenImportacion> respuesta = Controller.ImportarTodosAsync().GetAwaiter().GetResult();
        if (!MenuView.MostrarResultado(respuesta) || respuesta.Datos == null)
            return;
        ResumenImportacion resumen = respuesta.Datos;
        MessagesUtils.PrintMessage("Recibidos: " + resumen.Recibidos
            + " | Registrados: " + resumen.Registrados
            + " | Actualizados: " + resumen.Actualizados);
    }
}
