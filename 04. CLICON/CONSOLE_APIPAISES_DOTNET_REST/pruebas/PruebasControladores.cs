using System.Net;
using System.Text;
using System.Text.Json;
using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.Enums;
using CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;

namespace CONSOLE_APIPAISES_DOTNET_REST.Pruebas;

/// <summary>Reemplaza la red: guarda la última petición y devuelve la respuesta preparada.</summary>
internal sealed class ServidorFalso : HttpMessageHandler
{
    public string Metodo = "";
    public string Url = "";
    public string? Cuerpo;
    public string? Authorization;
    public string? ContentType;
    public string Respuesta = """{"estado":"OK","mensaje":"ok","datos":null}""";
    public HttpStatusCode Codigo = HttpStatusCode.OK;
    public bool SinRed;

    public JsonElement Json => JsonDocument.Parse(Cuerpo!).RootElement;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Metodo = request.Method.Method;
        Url = request.RequestUri!.AbsoluteUri;
        Cuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
        ContentType = request.Content?.Headers.ContentType?.ToString();
        Authorization = request.Headers.TryGetValues("Authorization", out var valores) ? valores.First() : null;
        if (SinRed)
            throw new HttpRequestException("sin red");
        return new HttpResponseMessage(Codigo) { Content = new StringContent(Respuesta, Encoding.UTF8, "application/json") };
    }
}

internal static class PruebasControladores
{
    private const string Base = "http://servidor:5090/api";
    private const string Token = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string Ecuador = """{"nombre":"Ecuador","capital":"Quito","region":"América","poblacion":18001000,"moneda":"United States dollar (USD)"}""";

    private static ServidorFalso Preparar(string respuesta = """{"estado":"OK","mensaje":"ok","datos":null}""",
        HttpStatusCode codigo = HttpStatusCode.OK, bool conSesion = true)
    {
        var falso = new ServidorFalso { Respuesta = respuesta, Codigo = codigo };
        ConfigHTTP.Handler = falso;
        ConfigHTTP.SetBaseUrl(Base + "/");
        ConfigHTTP.Limpiar();
        if (conSesion)
        {
            ConfigHTTP.Token = Token;
            ConfigHTTP.Nombre = "Administrador";
        }
        return falso;
    }

    public static Ejecutor Crear()
    {
        var e = new Ejecutor();

        e.Caso("Login correcto guarda la sesión", async () =>
        {
            var s = Preparar("""{"estado":"OK","mensaje":"Ingreso de sesión exitoso","datos":{"token":"%T","usuario":"Administrador"}}""".Replace("%T", Token), conSesion: false);
            var r = await new LoginController().LoginAsync(" admin ", "admin123");

            Afirmar.Igual(EstadosEnum.OK, r.Estado!.Value);
            Afirmar.Igual("Ingreso de sesión exitoso", r.Mensaje);
            Afirmar.Igual("POST", s.Metodo);
            Afirmar.Igual(Base + "/Login/login", s.Url);
            Afirmar.Igual("admin", s.Json.GetProperty("usuario").GetString());
            Afirmar.Igual("admin123", s.Json.GetProperty("clave").GetString());
            Afirmar.Nulo(s.Authorization, "el login no lleva token");
            Afirmar.Igual(Token, ConfigHTTP.Token);
            Afirmar.Igual("Administrador", ConfigHTTP.Nombre);
        });

        e.Caso("Credenciales incorrectas no guardan sesión ni son sesión no válida", async () =>
        {
            Preparar("""{"estado":"ERROR","mensaje":"Credenciales incorrectas","datos":null}""", HttpStatusCode.Unauthorized, conSesion: false);
            var r = await new LoginController().LoginAsync("admin", "mala");

            Afirmar.Igual(EstadosEnum.ERROR, r.Estado!.Value);
            Afirmar.Igual("Credenciales incorrectas", r.Mensaje);
            Afirmar.Falso(ConfigHTTP.HaySesion(), "no debe quedar sesión");
        });

        e.Caso("Logout borra la sesión aunque no haya servidor", async () =>
        {
            var s = Preparar();
            s.SinRed = true;
            var r = await new LoginController().LogoutAsync();

            Afirmar.Igual(ErroresCliente.SinServidor, r.Mensaje);
            Afirmar.Igual(Base + "/Login/logout", s.Url);
            Afirmar.Falso(ConfigHTTP.HaySesion(), "ECUD1 alterno 4");
        });

        e.Caso("Las peticiones llevan el token sin prefijo", async () =>
        {
            var s = Preparar("""{"estado":"OK","mensaje":"Países obtenidos correctamente","datos":[%P]}""".Replace("%P", Ecuador));
            var r = await new PaisController().ListarAsync();

            Afirmar.Igual("GET", s.Metodo);
            Afirmar.Igual(Base + "/Paises", s.Url);
            Afirmar.Igual(Token, s.Authorization);
            Afirmar.Nulo(s.Cuerpo);
            var ecuador = r.Datos![0];
            Afirmar.Igual("Ecuador", ecuador.Nombre);
            Afirmar.Igual(18001000L, ecuador.Poblacion);
            Afirmar.Falso(ecuador.Guardado, "sin el campo, guardado es false");
        });

        e.Caso("Token 401 es sesión no válida", async () =>
        {
            Preparar("""{"estado":"ERROR","mensaje":"Token inválido o sesión no activa","datos":null}""", HttpStatusCode.Unauthorized);
            var r = await new PaisController().ListarAsync();

            Afirmar.Verdadero(r.SesionNoValida(), "401 debe ser sesión no válida");
            Afirmar.Igual("Token inválido o sesión no activa", r.Mensaje);
        });

        e.Caso("Sin red es «No se pudo conectar…»", async () =>
        {
            var s = Preparar();
            s.SinRed = true;
            var r = await new PaisController().ListarAsync();

            Afirmar.Igual(ErroresCliente.SinServidor, r.Mensaje);
            Afirmar.Falso(r.SesionNoValida(), "sin red no es 401");
        });

        e.Caso("Respuesta ilegible no detiene el cliente", async () =>
        {
            foreach (string cuerpo in new[] { "", "<html>", "{\"x\":1}", "{\"estado\":\"RARO\"}" })
            {
                Preparar(cuerpo, HttpStatusCode.InternalServerError);
                var r = await new PaisController().ListarAsync();
                Afirmar.Igual(EstadosEnum.ERROR, r.Estado!.Value, cuerpo);
                Afirmar.Igual(ErroresCliente.RespuestaIncompleta, r.Mensaje, cuerpo);
            }
        });

        e.Caso("Buscar codifica los criterios y omite los vacíos", async () =>
        {
            var s = Preparar("""{"estado":"OK","mensaje":"Búsqueda realizada correctamente","datos":{"paises":[%P],"total":1}}""".Replace("%P", Ecuador));
            var r = await new PaisController().BuscarAsync("san josé", " ", "América");

            Afirmar.Igual(Base + "/Paises/buscar?nombre=san%20jos%C3%A9&region=Am%C3%A9rica", s.Url);
            Afirmar.Igual(1, r.Datos!.Total);
            Afirmar.Igual("Ecuador", r.Datos.Paises![0].Nombre);
        });

        e.Caso("Buscar sin criterios no agrega parámetros", async () =>
        {
            var s = Preparar();
            await new PaisController().BuscarAsync(null, "", null);
            Afirmar.Igual(Base + "/Paises/buscar", s.Url);
        });

        e.Caso("Consultar en la API codifica el nombre en la ruta", async () =>
        {
            var s = Preparar("""{"estado":"OK","mensaje":"Consulta en la API exitosa","datos":[{"nombre":"Perú","guardado":true}]}""");
            var r = await new PaisController().ConsultarApiAsync("Perú del Sur");

            Afirmar.Igual(Base + "/Paises/externo/Per%C3%BA%20del%20Sur", s.Url);
            Afirmar.Verdadero(r.Datos![0].Guardado, "guardado=true");
        });

        e.Caso("Importar envía consulta y nombre", async () =>
        {
            var s = Preparar("""{"estado":"OK","mensaje":"País importado correctamente","datos":{"pais":%P,"accion":"registrado"}}""".Replace("%P", Ecuador));
            var r = await new PaisController().ImportarAsync(" ecuador ", "Ecuador");

            Afirmar.Igual("POST", s.Metodo);
            Afirmar.Igual(Base + "/Paises/importar", s.Url);
            Afirmar.Igual("ecuador", s.Json.GetProperty("consulta").GetString());
            Afirmar.Igual("Ecuador", s.Json.GetProperty("nombre").GetString());
            Afirmar.Igual("application/json; charset=utf-8", s.ContentType);
            Afirmar.Igual("registrado", r.Datos!.Accion);
        });

        e.Caso("Importar todos usa su propia espera", async () =>
        {
            var s = Preparar("""{"estado":"OK","mensaje":"Importación finalizada","datos":{"recibidos":250,"registrados":240,"actualizados":10}}""");
            var r = await new PaisController().ImportarTodosAsync();

            Afirmar.Igual(Base + "/Paises/importar/todos", s.Url);
            Afirmar.Verdadero(ConfigHTTP.TimeoutImportacion > ConfigHTTP.Timeout, "espera mayor que la normal");
            Afirmar.Igual(240, r.Datos!.Registrados);
        });

        e.Caso("Registrar, actualizar y eliminar", async () =>
        {
            var controller = new PaisController();
            var s = Preparar("""{"estado":"OK","mensaje":"País registrado correctamente","datos":%P}""".Replace("%P", Ecuador), HttpStatusCode.Created);
            Afirmar.Verdadero((await controller.RegistrarAsync("ecuador", "Ecuador")).EsOk(), "registrar OK");
            Afirmar.Igual("POST", s.Metodo);
            Afirmar.Igual(Base + "/Paises", s.Url);

            s = Preparar();
            await controller.ActualizarAsync("Países Bajos");
            Afirmar.Igual("PUT", s.Metodo);
            Afirmar.Igual(Base + "/Paises/Pa%C3%ADses%20Bajos", s.Url);

            s = Preparar("""{"estado":"ERROR","mensaje":"El país no existe","datos":null}""", HttpStatusCode.NotFound);
            var r = await controller.EliminarAsync("Narnia");
            Afirmar.Igual("DELETE", s.Metodo);
            Afirmar.Igual(Base + "/Paises/Narnia", s.Url);
            Afirmar.Igual("El país no existe", r.Mensaje);
            Afirmar.Falso(r.SesionNoValida(), "404 no es sesión no válida");
        });

        e.Caso("Informes con las tres secciones", async () =>
        {
            Preparar("""{"estado":"OK","mensaje":"Informes generados correctamente","datos":{"paisesPorRegion":[{"region":"América","cantidad":1}],"paisMasPoblado":%P,"promedioPorRegion":[{"region":"América","promedioPoblacion":18001000}]}}""".Replace("%P", Ecuador));
            var informe = (await new ReporteController().GenerarAsync()).Datos!;

            Afirmar.Igual("Ecuador", informe.PaisMasPoblado!.Nombre);
            Afirmar.Igual(1, informe.PaisesPorRegion![0].Cantidad);
            Afirmar.Igual(18001000L, informe.PromedioPorRegion![0].PromedioPoblacion);
        });

        e.Caso("Informes sin países es error con HTTP 200", async () =>
        {
            Preparar("""{"estado":"ERROR","mensaje":"No hay países registrados para generar informes","datos":null}""");
            var r = await new ReporteController().GenerarAsync();

            Afirmar.Igual(EstadosEnum.ERROR, r.Estado!.Value);
            Afirmar.Falso(r.SesionNoValida(), "200 no es sesión no válida");
            Afirmar.Nulo(r.Datos);
        });

        return e;
    }
}
