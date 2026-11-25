namespace MOV_APIPAISES_DOTNET_REST.Config;

public static class RestConfig
{
    /// <summary>
    /// Servidor REST de APIPaises (WS_APIPAISES_DOTNET_REST, http://*:5090/api).
    /// 10.0.2.2 es el equipo anfitrión visto desde el emulador; en un celular físico use la IP del servidor.
    /// </summary>
    public const string UrlServidor = "http://10.0.2.2:5090/api/";
}
