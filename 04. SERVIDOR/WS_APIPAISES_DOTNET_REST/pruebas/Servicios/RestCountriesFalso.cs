using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WS_APIPAISES_DOTNET_REST.Pruebas.Servicios;

/// <summary>
/// Servidor HTTP local que imita la API REST Countries v5, para probar ConsumoAPIPaises sin
/// internet ni API key. Cada petición se responde con la función <c>responder</c>.
/// </summary>
internal sealed class RestCountriesFalso : IAsyncDisposable
{
    /// <summary>Respuesta que devuelve el servidor falso.</summary>
    public sealed record Respuesta(int Codigo, string Cuerpo, int DemoraMs = 0)
    {
        public static Respuesta Ok(string cuerpo) => new(200, cuerpo);
    }

    /// <summary>Petición recibida: ruta, parámetros y encabezado Authorization.</summary>
    public sealed record Peticion(string Ruta, Dictionary<string, string> Parametros, string? Authorization);

    private readonly WebApplication app;

    public ConcurrentQueue<Peticion> Recibidas { get; } = new();

    public List<Peticion> RecibidasEnOrden => Recibidas.ToList();

    private RestCountriesFalso(Func<Peticion, Respuesta> responder)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        app = builder.Build();
        app.Run(async contexto =>
        {
            var parametros = contexto.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var peticion = new Peticion(contexto.Request.Path.Value ?? "", parametros,
                contexto.Request.Headers.Authorization.ToString());
            Recibidas.Enqueue(peticion);
            Respuesta respuesta = responder(peticion);
            if (respuesta.DemoraMs > 0)
            {
                try
                {
                    await Task.Delay(respuesta.DemoraMs, contexto.RequestAborted);
                }
                catch (TaskCanceledException)
                {
                    return; // El cliente cerró la conexión por tiempo agotado
                }
            }
            contexto.Response.StatusCode = respuesta.Codigo;
            contexto.Response.ContentType = "application/json; charset=utf-8";
            await contexto.Response.WriteAsync(respuesta.Cuerpo);
        });
    }

    public static async Task<RestCountriesFalso> IniciarAsync(Func<Peticion, Respuesta> responder)
    {
        var falso = new RestCountriesFalso(responder);
        await falso.app.StartAsync();
        return falso;
    }

    /// <summary>Después de iniciar, Urls tiene el puerto real que eligió Kestrel.</summary>
    public string UrlBase => app.Urls.First().TrimEnd('/') + "/countries/v5";

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }

    // ------------------------------------------------------------------ JSON de ejemplo (forma de la v5)

    /// <summary>Objeto país con la forma de la v5. <paramref name="espanol"/> null = sin traducción al español.</summary>
    public static string Pais(string comun, string? espanol, string capitales, string region, string poblacion,
        string monedas)
    {
        string traducciones = espanol == null
            ? "{\"fra\":{\"common\":\"" + comun + "\",\"official\":\"" + comun + "\"}}"
            : "{\"spa\":{\"common\":\"" + espanol + "\",\"official\":\"" + espanol + "\"},"
              + "\"fra\":{\"common\":\"" + comun + "\",\"official\":\"" + comun + "\"}}";
        return "{\"names\":{\"common\":\"" + comun + "\",\"official\":\"" + comun + "\",\"translations\":"
               + traducciones + "}," + "\"capitals\":" + capitales + ",\"region\":" + region
               + ",\"population\":" + poblacion + ",\"currencies\":" + monedas + "}";
    }

    public static string Capital(string nombre, bool principal)
    {
        return "{\"name\":\"" + nombre + "\",\"coordinates\":{\"lat\":0,\"lng\":0},"
               + "\"attributes\":{\"primary\":" + (principal ? "true" : "false") + ",\"administrative\":false}}";
    }

    public static string Moneda(string codigo, string nombre)
    {
        return "{\"code\":\"" + codigo + "\",\"name\":\"" + nombre + "\",\"symbol\":\"$\"}";
    }

    public static string Pagina(bool mas, params string[] paises)
    {
        return "{\"data\":{\"objects\":[" + string.Join(",", paises) + "],\"meta\":{\"total\":0,\"count\":"
               + paises.Length + ",\"limit\":100,\"offset\":0,\"more\":" + (mas ? "true" : "false") + "}}}";
    }

    public static readonly string Ecuador = Pais("Ecuador", "Ecuador", "[" + Capital("Quito", true) + "]",
        "\"Americas\"", "18001000", "[" + Moneda("USD", "United States dollar") + "]");
    public static readonly string Alemania = Pais("Germany", "Alemania", "[" + Capital("Berlin", true) + "]",
        "\"Europe\"", "83491249", "[" + Moneda("EUR", "Euro") + "]");
    public static readonly string Austria = Pais("Austria", "Austria", "[" + Capital("Vienna", true) + "]",
        "\"Europe\"", "9177982", "[" + Moneda("EUR", "Euro") + "]");
}
