using MOV_APIPAISES_DOTNET_REST.Modelos.Data;

namespace MOV_APIPAISES_DOTNET_REST.Servicios;

/// <summary>Sesión abierta mientras dura la app: el token se guarda en memoria (ECUD1, paso 7).</summary>
public class SesionServicio
{
    private volatile Sesion? sesion;

    public Sesion? Sesion => sesion;

    public string? Token => sesion?.Token;

    public bool EstaAutenticado => !string.IsNullOrWhiteSpace(Token);

    public void Iniciar(Sesion nueva) => sesion = nueva;

    public void Limpiar() => sesion = null;
}
