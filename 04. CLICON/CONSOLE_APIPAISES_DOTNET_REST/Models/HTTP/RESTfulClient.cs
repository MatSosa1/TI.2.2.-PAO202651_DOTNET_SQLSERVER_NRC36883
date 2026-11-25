using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.DTO;

namespace CONSOLE_APIPAISES_DOTNET_REST.Models.HTTP;

/// <summary>
/// Llama a los servicios RESTful del servidor y convierte la respuesta JSON en
/// <see cref="Respuesta{T}"/>. Nunca lanza excepciones: sin conexión o con una respuesta ilegible
/// devuelve un error con el mensaje para el usuario (ECUD10, alterno 4).
/// </summary>
public static class RESTfulClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static HttpClient? cliente;
    private static HttpMessageHandler? handlerDelCliente;

    public static Task<Respuesta<T>> GetAsync<T>(string ruta) => EnviarAsync<T>(HttpMethod.Get, ruta, null, ConfigHTTP.Timeout);

    public static Task<Respuesta<T>> PostAsync<T>(string ruta, object? cuerpo) => EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo, ConfigHTTP.Timeout);

    public static Task<Respuesta<T>> PostAsync<T>(string ruta, object? cuerpo, TimeSpan espera) => EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo, espera);

    public static Task<Respuesta<T>> PutAsync<T>(string ruta) => EnviarAsync<T>(HttpMethod.Put, ruta, null, ConfigHTTP.Timeout);

    public static Task<Respuesta<T>> DeleteAsync<T>(string ruta) => EnviarAsync<T>(HttpMethod.Delete, ruta, null, ConfigHTTP.Timeout);

    private static async Task<Respuesta<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, TimeSpan espera)
    {
        try
        {
            using var peticion = new HttpRequestMessage(metodo, ConfigHTTP.BaseUrl + ruta);
            peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (cuerpo != null)
                peticion.Content = new StringContent(JsonSerializer.Serialize(cuerpo, Json), Encoding.UTF8, "application/json");
            if (ConfigHTTP.HaySesion())
                peticion.Headers.TryAddWithoutValidation("Authorization", ConfigHTTP.Token);

            using var cancelacion = new CancellationTokenSource(espera);
            using HttpResponseMessage http = await Cliente().SendAsync(peticion, cancelacion.Token);
            string texto = await http.Content.ReadAsStringAsync(cancelacion.Token);
            int codigo = (int)http.StatusCode;

            Respuesta<T>? respuesta = string.IsNullOrWhiteSpace(texto) ? null : JsonSerializer.Deserialize<Respuesta<T>>(texto, Json);
            if (respuesta?.Estado == null)
                return ConCodigo(Respuesta<T>.Error(ErroresCliente.RespuestaIncompleta), codigo);
            return ConCodigo(respuesta, codigo);
        }
        catch (JsonException)
        {
            return Respuesta<T>.Error(ErroresCliente.RespuestaIncompleta);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            // Sin red, servidor apagado, tiempo agotado o dirección mal escrita
            return Respuesta<T>.Error(ErroresCliente.SinServidor);
        }
    }

    /// <summary>Un solo HttpClient; las pruebas cambian el handler por un servidor simulado.</summary>
    private static HttpClient Cliente()
    {
        HttpMessageHandler? handler = ConfigHTTP.Handler;
        if (cliente == null || !ReferenceEquals(handler, handlerDelCliente))
        {
            cliente = handler == null
                ? new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan }
                : new HttpClient(handler, disposeHandler: false) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            handlerDelCliente = handler;
        }
        return cliente;
    }

    private static Respuesta<T> ConCodigo<T>(Respuesta<T> respuesta, int codigo)
    {
        respuesta.CodigoHttp = codigo;
        return respuesta;
    }
}
