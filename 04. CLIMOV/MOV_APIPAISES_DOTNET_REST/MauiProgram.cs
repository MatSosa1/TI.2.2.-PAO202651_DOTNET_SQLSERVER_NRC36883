using Microsoft.Extensions.Logging;
using MOV_APIPAISES_DOTNET_REST.Config;
using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Repositorios;
using MOV_APIPAISES_DOTNET_REST.Servicios;
using MOV_APIPAISES_DOTNET_REST.Vista;

namespace MOV_APIPAISES_DOTNET_REST;

/// <summary>Raíz de composición (AplicacionAPIPaises del cliente Android).</summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Modelo: la sesión y el repositorio REST viven mientras dura la app.
        builder.Services.AddSingleton<SesionServicio>();
        builder.Services.AddSingleton<IVerificadorRed, RedMaui>();
        builder.Services.AddSingleton<ITransporte, TransporteHttp>();
        builder.Services.AddSingleton(s => new ApiPaisesRepositorio(
            new ClienteRest(RestConfig.UrlServidor, s.GetRequiredService<ITransporte>(), s.GetRequiredService<IVerificadorRed>())));

        // Controladores y vistas
        builder.Services.AddTransient<LoginControlador>();
        builder.Services.AddTransient<PaisesControlador>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<InicioPage>();
        builder.Services.AddTransient<PaisesPage>();
        builder.Services.AddTransient<ApiPage>();
        builder.Services.AddTransient<InformesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Sin la línea inferior que Android dibuja en los Entry: el contorno lo pone CampoTexto.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinSubrayado", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        return builder.Build();
    }
}
