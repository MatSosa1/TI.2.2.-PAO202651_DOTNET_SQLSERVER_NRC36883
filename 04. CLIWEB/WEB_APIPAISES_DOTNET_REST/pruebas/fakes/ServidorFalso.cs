using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WEB_APIPAISES_DOTNET_REST.Models.Servicios;

namespace WEB_APIPAISES_DOTNET_REST.Pruebas.Fakes;

/// <summary>
/// Servidor REST de APIPaises simulado: responde según «MÉTODO /ruta» con el JSON preparado y
/// guarda las peticiones recibidas (ruta, token y cuerpo).
/// </summary>
public sealed class ServidorFalso : HttpMessageHandler
{
    public sealed record Peticion(string Metodo, string Ruta, string? Token, string? Cuerpo);

    public const string Ecuador = """{"nombre":"Ecuador","capital":"Quito","region":"América","poblacion":18001000,"moneda":"United States dollar (USD)"}""";
    public const string Token = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private readonly ConcurrentDictionary<string, (int Codigo, string Cuerpo)> respuestas = new();

    public ConcurrentQueue<Peticion> Recibidas { get; } = new();

    public bool SinRed { get; set; }

    public ServidorFalso()
    {
        Responder("POST /api/Login/login", 200, """{"estado":"OK","mensaje":"Ingreso de sesión exitoso","datos":{"token":"%T","usuario":"Administrador"}}""".Replace("%T", Token));
        Responder("POST /api/Login/logout", 200, """{"estado":"OK","mensaje":"Sesión cerrada","datos":null}""");
        Responder("GET /api/Paises", 200, """{"estado":"OK","mensaje":"Países obtenidos correctamente","datos":[%P]}""".Replace("%P", Ecuador));
        Responder("GET /api/Paises/buscar", 200, """{"estado":"OK","mensaje":"Filtro aplicado correctamente","datos":{"paises":[%P],"total":1}}""".Replace("%P", Ecuador));
        Responder("GET /api/Paises/externo", 200, """{"estado":"OK","mensaje":"Consulta en la API exitosa","datos":[{"nombre":"Ecuador","capital":"Quito","region":"América","poblacion":18001000,"moneda":"USD","guardado":true}]}""");
        Responder("POST /api/Paises/importar", 200, """{"estado":"OK","mensaje":"País importado correctamente","datos":{"pais":%P,"accion":"registrado"}}""".Replace("%P", Ecuador));
        Responder("POST /api/Paises/importar/todos", 200, """{"estado":"OK","mensaje":"Importación finalizada","datos":{"recibidos":250,"registrados":240,"actualizados":10}}""");
        Responder("POST /api/Paises", 201, """{"estado":"OK","mensaje":"País registrado correctamente","datos":%P}""".Replace("%P", Ecuador));
        Responder("PUT /api/Paises", 200, """{"estado":"OK","mensaje":"País actualizado correctamente","datos":%P}""".Replace("%P", Ecuador));
        Responder("DELETE /api/Paises", 200, """{"estado":"OK","mensaje":"País eliminado correctamente","datos":null}""");
        Responder("GET /api/Reportes", 200, """{"estado":"OK","mensaje":"Informes generados correctamente","datos":{"paisesPorRegion":[{"region":"América","cantidad":2},{"region":"Europa","cantidad":1}],"paisMasPoblado":%P,"promedioPorRegion":[{"region":"América","promedioPoblacion":18001000}]}}""".Replace("%P", Ecuador));
    }

    /// <summary>Prepara la respuesta de «MÉTODO /ruta» (sin el último segmento si es un nombre).</summary>
    public void Responder(string clave, int codigo, string cuerpo) => respuestas[clave] = (codigo, cuerpo);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (SinRed)
            throw new HttpRequestException("sin red");
        string ruta = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
        string? cuerpo = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
        string? autorizacion = request.Headers.TryGetValues("Authorization", out var valores) ? valores.First() : null;
        Recibidas.Enqueue(new Peticion(request.Method.Method, ruta + request.RequestUri.Query, autorizacion, cuerpo));

        string clave = request.Method.Method + " " + ruta;
        // /api/Paises/{nombre} y /api/Paises/externo/{nombre}: se busca sin el nombre
        if (!respuestas.ContainsKey(clave))
            clave = request.Method.Method + " " + ruta[..ruta.LastIndexOf('/')];
        (int codigo, string json) = respuestas.TryGetValue(clave, out var r) ? r : (404, """{"estado":"ERROR","mensaje":"El recurso solicitado no existe","datos":null}""");
        return new HttpResponseMessage((HttpStatusCode)codigo) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

/// <summary>La aplicación web real, con el servidor REST reemplazado por <see cref="ServidorFalso"/>.</summary>
public sealed class AplicacionWeb : WebApplicationFactory<Program>
{
    public ServidorFalso Servidor { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(servicios =>
        {
            servicios.AddSingleton(new ApiConfig("http://servidor:5090/api"));
            servicios.AddHttpClient<ClienteRest>().ConfigurePrimaryHttpMessageHandler(() => Servidor);
        });
    }
}
