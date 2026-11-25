using MOV_APIPAISES_DOTNET_REST.Modelos.Repositorios;

namespace MOV_APIPAISES_DOTNET_REST.Servicios;

/// <summary>Estado de la red del dispositivo con Connectivity de MAUI.</summary>
public class RedMaui : IVerificadorRed
{
    public bool HayRed()
    {
        try
        {
            return Connectivity.Current.NetworkAccess != NetworkAccess.None;
        }
        catch
        {
            return true;
        }
    }
}
