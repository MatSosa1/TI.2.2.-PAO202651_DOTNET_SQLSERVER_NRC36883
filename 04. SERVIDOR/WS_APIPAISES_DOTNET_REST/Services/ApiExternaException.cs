using WS_APIPAISES_DOTNET_REST.Models;

namespace WS_APIPAISES_DOTNET_REST.Services;

/// <summary>
/// Falla al consumir la API REST Countries (ECUD9, flujos alternos). El tipo decide el
/// mensaje y el código HTTP que devuelve el caso de uso invocador.
/// </summary>
public class ApiExternaException : Exception
{
    public enum TipoFalla
    {
        /// <summary>Sin conexión, tiempo agotado o código de error de la API (incluye 401/403/429): HTTP 502.</summary>
        NoDisponible,
        /// <summary>La API no tiene países con ese nombre: HTTP 404.</summary>
        SinCoincidencias,
        /// <summary>El cuerpo no tiene el formato esperado: HTTP 502.</summary>
        RespuestaInvalida,
    }

    public TipoFalla Tipo { get; }

    public ApiExternaException(TipoFalla tipo, string detalle, Exception? causa = null) : base(detalle, causa)
    {
        Tipo = tipo;
    }

    /// <summary>Mensaje para el usuario (el detalle técnico queda en Message, solo para el log).</summary>
    public string MensajeUsuario => Tipo switch
    {
        TipoFalla.SinCoincidencias => Mensajes.SinCoincidenciasApi,
        TipoFalla.RespuestaInvalida => Mensajes.ApiRespuestaInvalida,
        _ => Mensajes.ApiNoDisponible,
    };

    public int CodigoHttp => Tipo == TipoFalla.SinCoincidencias ? 404 : 502;
}
