namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>Rutas del Shell y vuelta al inicio de sesión (NavegacionSesion del cliente Android).</summary>
internal static class NavegacionSesion
{
    public const string RutaInicio = "//principal/inicio";
    public const string RutaPaises = "//principal/paises";
    public const string RutaApi = "//principal/api";
    public const string RutaInformes = "//principal/informes";

    private static bool avisoVisible;

    /// <param name="aviso">Mensaje a mostrar antes de volver (sesión no válida); null para volver directamente.</param>
    public static async Task VolverAlLoginAsync(Page pagina, string? aviso)
    {
        if (aviso is not null)
        {
            // Varias pestañas pueden detectar a la vez la sesión vencida; el aviso se muestra una sola vez.
            if (avisoVisible)
                return;
            avisoVisible = true;
            try
            {
                await pagina.DisplayAlertAsync(Textos.Sesion, aviso, Textos.Aceptar);
            }
            finally
            {
                avisoVisible = false;
            }
        }

        // Un Shell nuevo descarta las pestañas abiertas y su estado, como FLAG_ACTIVITY_CLEAR_TASK en Android.
        if (Application.Current?.Windows.FirstOrDefault() is Window ventana)
            ventana.Page = new AppShell();
        else
            await Shell.Current.GoToAsync("//login");
    }
}
