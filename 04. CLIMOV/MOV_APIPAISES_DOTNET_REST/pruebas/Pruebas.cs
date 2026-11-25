using System.Text.Json;
using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Repositorios;
using MOV_APIPAISES_DOTNET_REST.Servicios;
using MOV_APIPAISES_DOTNET_REST.Utils;

namespace MOV_APIPAISES_DOTNET_REST.Pruebas;

/// <summary>Servidor REST simulado: guarda la última petición y devuelve la respuesta preparada.</summary>
internal sealed class ServidorFalso : ITransporte, IVerificadorRed
{
    public string Metodo = "";
    public string Url = "";
    public string? Cuerpo;
    public string? Token;
    public TimeSpan Espera;
    public int Codigo = 200;
    public string Respuesta = """{"estado":"OK","mensaje":"ok","datos":null}""";
    public bool SinServidor;
    public bool Red = true;
    public int Llamadas;

    public bool HayRed() => Red;

    public Task<ITransporte.Respuesta> EnviarAsync(string metodo, string url, string? cuerpo, string? token, TimeSpan espera)
    {
        Llamadas++;
        Metodo = metodo;
        Url = url;
        Cuerpo = cuerpo;
        Token = token;
        Espera = espera;
        if (SinServidor)
            throw new HttpRequestException("sin servidor");
        return Task.FromResult(new ITransporte.Respuesta(Codigo, Respuesta));
    }
}

internal static class Comun
{
    public const string Base = "http://10.0.2.2:5090/api";
    public const string Ecuador = """{"nombre":"Ecuador","capital":"Quito","region":"América","poblacion":18001000,"moneda":"United States dollar (USD)"}""";

    public static (ServidorFalso Servidor, ApiPaisesRepositorio Repositorio) Crear()
    {
        var servidor = new ServidorFalso();
        return (servidor, new ApiPaisesRepositorio(new ClienteRest(Base + "/", servidor, servidor)));
    }

    public static string Ok(string mensaje, string datos) => $$"""{"estado":"OK","mensaje":"{{mensaje}}","datos":{{datos}}}""";

    public static string Error(string mensaje) => $$"""{"estado":"ERROR","mensaje":"{{mensaje}}","datos":null}""";
}

internal static class PruebasFormato
{
    public static Ejecutor Crear()
    {
        var e = new Ejecutor();
        e.Caso("Población con separador de miles", () =>
        {
            Afirmar.Igual("18.001.000", Formato.Poblacion(18001000));
            Afirmar.Igual("0", Formato.Poblacion(0));
        });
        e.Caso("Total en singular y plural", () =>
        {
            Afirmar.Igual("1 país encontrado", Formato.Total(1));
            Afirmar.Igual("5 países encontrados", Formato.Total(5));
        });
        e.Caso("Texto vacío como guion", () => Afirmar.Igual("—", Formato.Texto("  ")));
        e.Caso("Nombre vacío, de 100 y de 101 caracteres", () =>
        {
            Afirmar.Igual(ValidadorDatos.IngreseNombrePais, ValidadorDatos.ErrorNombre(" "));
            Afirmar.Nulo(ValidadorDatos.ErrorNombre(new string('x', 100)));
            Afirmar.Igual(ValidadorDatos.NombreMuyLargo, ValidadorDatos.ErrorNombre(new string('x', 101)));
            Afirmar.Nulo(ValidadorDatos.ErrorCriterio(""));
        });
        return e;
    }
}

internal static class PruebasRepositorio
{
    public static Ejecutor Crear()
    {
        var e = new Ejecutor();

        e.Caso("Login envía usuario y clave sin token", async () =>
        {
            var (s, r) = Comun.Crear();
            s.Respuesta = Comun.Ok("Ingreso de sesión exitoso", """{"token":"abc","usuario":"Administrador"}""");
            var resultado = await r.LoginAsync("admin", "admin123");

            Afirmar.Verdadero(resultado.Exito, "login OK");
            Afirmar.Igual("POST", s.Metodo);
            Afirmar.Igual(Comun.Base + "/Login/login", s.Url);
            Afirmar.Nulo(s.Token);
            using var cuerpo = JsonDocument.Parse(s.Cuerpo!);
            Afirmar.Igual("admin123", cuerpo.RootElement.GetProperty("clave").GetString());
            Afirmar.Igual("Administrador", resultado.Datos!.Nombre);
        });

        e.Caso("Login con 401 es credenciales incorrectas, no sesión vencida", async () =>
        {
            var (s, r) = Comun.Crear();
            s.Codigo = 401;
            s.Respuesta = Comun.Error("Credenciales incorrectas");
            var resultado = await r.LoginAsync("admin", "mala");
            Afirmar.Igual("Credenciales incorrectas", resultado.Mensaje);
            Afirmar.Falso(resultado.SesionExpirada, "no es sesión vencida");
        });

        e.Caso("Token rechazado es sesión vencida", async () =>
        {
            var (s, r) = Comun.Crear();
            s.Codigo = 401;
            s.Respuesta = Comun.Error("Token inválido o sesión no activa");
            var resultado = await r.ListarAsync("T");
            Afirmar.Verdadero(resultado.SesionExpirada, "sesión vencida");
            Afirmar.Igual(ClienteRest.SesionNoValida, resultado.Mensaje);
            Afirmar.Igual("T", s.Token);
        });

        e.Caso("Buscar codifica y omite los criterios vacíos", async () =>
        {
            var (s, r) = Comun.Crear();
            await r.BuscarAsync("T", "san josé", " ", "América");
            Afirmar.Igual(Comun.Base + "/Paises/buscar?nombre=san%20jos%C3%A9&region=Am%C3%A9rica", s.Url);
        });

        e.Caso("Rutas y métodos de las demás operaciones", async () =>
        {
            var (s, r) = Comun.Crear();
            await r.ConsultarApiAsync("T", "Perú");
            Afirmar.Igual(Comun.Base + "/Paises/externo/Per%C3%BA", s.Url);
            await r.ActualizarAsync("T", "Países Bajos");
            Afirmar.Igual(("PUT", Comun.Base + "/Paises/Pa%C3%ADses%20Bajos"), (s.Metodo, s.Url));
            await r.EliminarAsync("T", "Narnia");
            Afirmar.Igual(("DELETE", Comun.Base + "/Paises/Narnia"), (s.Metodo, s.Url));
            await r.ImportarTodosAsync("T");
            Afirmar.Igual(ClienteRest.EsperaImportacion, s.Espera);
            await r.InformesAsync("T");
            Afirmar.Igual(("GET", Comun.Base + "/Reportes"), (s.Metodo, s.Url));
        });

        e.Caso("Sin red no llama al servidor", async () =>
        {
            var (s, r) = Comun.Crear();
            s.Red = false;
            var resultado = await r.ListarAsync("T");
            Afirmar.Igual(ClienteRest.SinServidor, resultado.Mensaje);
            Afirmar.Igual(0, s.Llamadas);
        });

        e.Caso("Servidor apagado y respuesta ilegible", async () =>
        {
            var (s, r) = Comun.Crear();
            s.SinServidor = true;
            Afirmar.Igual(ClienteRest.SinServidor, (await r.ListarAsync("T")).Mensaje);
            s.SinServidor = false;
            s.Codigo = 500;
            s.Respuesta = "<html>";
            Afirmar.Igual(ClienteRest.RespuestaIncompleta, (await r.ListarAsync("T")).Mensaje);
        });

        return e;
    }
}

internal static class PruebasControladores
{
    private static (ServidorFalso, PaisesControlador, SesionServicio) Preparar()
    {
        var (s, r) = Comun.Crear();
        var sesion = new SesionServicio();
        sesion.Iniciar(new Modelos.Data.Sesion { Token = "T", Usuario = "Administrador" });
        return (s, new PaisesControlador(r, sesion), sesion);
    }

    public static Ejecutor Crear()
    {
        var e = new Ejecutor();

        e.Caso("Login con campos vacíos no llama al servidor", async () =>
        {
            var (s, r) = Comun.Crear();
            var login = new LoginControlador(r, new SesionServicio());
            Afirmar.Igual(LoginControlador.Campo.Usuario, login.CampoInvalido(" ", "x"));
            Afirmar.Igual(LoginControlador.Campo.Clave, login.CampoInvalido("admin", ""));
            Afirmar.Igual(ValidadorDatos.IngreseClave, (await login.IniciarSesionAsync("admin", "")).Mensaje);
            Afirmar.Igual(0, s.Llamadas);
        });

        e.Caso("Login correcto guarda la sesión y cerrar la borra", async () =>
        {
            var (s, r) = Comun.Crear();
            s.Respuesta = Comun.Ok("Ingreso de sesión exitoso", """{"token":"abc","usuario":"Administrador"}""");
            var sesion = new SesionServicio();
            var login = new LoginControlador(r, sesion);
            await login.IniciarSesionAsync(" admin ", "admin123");
            Afirmar.Verdadero(login.HaySesion, "hay sesión");
            Afirmar.Igual("abc", login.CerrarSesion());
            Afirmar.Falso(sesion.EstaAutenticado, "sesión borrada");
        });

        e.Caso("Sin criterios lista; con región filtra", async () =>
        {
            var (s, c, _) = Preparar();
            s.Respuesta = Comun.Ok("Países obtenidos correctamente", "[" + Comun.Ecuador + "]");
            var todos = await c.BuscarAsync("", null, "");
            Afirmar.Igual(Comun.Base + "/Paises", s.Url);
            Afirmar.Igual(1, todos.Datos!.Count);

            s.Respuesta = Comun.Ok("Filtro aplicado correctamente", """{"paises":[""" + Comun.Ecuador + """],"total":1}""");
            var filtro = await c.BuscarAsync(null, null, "América");
            Afirmar.Igual(Comun.Base + "/Paises/buscar?region=Am%C3%A9rica", s.Url);
            Afirmar.Igual("Ecuador", filtro.Datos![0].Nombre);
        });

        e.Caso("Región fuera del catálogo se ignora y criterio largo se rechaza", async () =>
        {
            var (s, c, _) = Preparar();
            await c.BuscarAsync(null, null, "Marte");
            Afirmar.Igual(Comun.Base + "/Paises", s.Url);
            Afirmar.Igual(ValidadorDatos.NombreMuyLargo, (await c.BuscarAsync(new string('x', 101), null, null)).Mensaje);
        });

        e.Caso("Importar y importar todos arman el mensaje", async () =>
        {
            var (s, c, _) = Preparar();
            s.Respuesta = Comun.Ok("País importado correctamente", """{"pais":""" + Comun.Ecuador + ""","accion":"registrado"}""");
            Afirmar.Igual("País importado correctamente (registrado)", (await c.ImportarAsync("ecua", "Ecuador")).Mensaje);
            s.Respuesta = Comun.Ok("Importación finalizada", """{"recibidos":250,"registrados":240,"actualizados":10}""");
            Afirmar.Igual("Importación finalizada: 250 recibidos, 240 registrados, 10 actualizados", (await c.ImportarTodosAsync()).Mensaje);
        });

        e.Caso("Consultar en la API valida el nombre", async () =>
        {
            var (s, c, _) = Preparar();
            Afirmar.Igual(ValidadorDatos.IngreseNombrePais, (await c.ConsultarApiAsync(" ")).Mensaje);
            Afirmar.Igual(0, s.Llamadas);
        });

        e.Caso("Sesión vencida borra la sesión local", async () =>
        {
            var (s, c, sesion) = Preparar();
            s.Codigo = 401;
            s.Respuesta = Comun.Error("Token inválido o sesión no activa");
            var resultado = await c.InformesAsync();
            Afirmar.Verdadero(resultado.SesionExpirada, "sesión vencida");
            Afirmar.Falso(sesion.EstaAutenticado, "sesión borrada");
        });

        return e;
    }
}
