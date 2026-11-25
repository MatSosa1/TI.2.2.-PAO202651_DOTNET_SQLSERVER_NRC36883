using WEB_APIPAISES_DOTNET_REST.Filtros;
using WEB_APIPAISES_DOTNET_REST.Models.Servicios;

// Cliente web de APIPaises: http://localhost:5091/ (consume el servidor REST de 04. SERVIDOR).
var builder = WebApplication.CreateBuilder(args);

// Modelo: servicios REST sobre un HttpClient compartido (la URL del servidor sale de appsettings.json)
builder.Services.AddSingleton<ApiConfig>();
builder.Services.AddHttpClient<ClienteRest>(cliente => cliente.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddTransient<LoginService>();
builder.Services.AddTransient<PaisService>();
builder.Services.AddTransient<ReporteService>();

// La sesión guarda el token del servidor REST, el nombre del usuario, el token CSRF y los mensajes flash
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.Cookie.Name = "APIPAISES_SESION";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
    opciones.Cookie.SameSite = SameSiteMode.Lax;
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Controladores (uno por pantalla) con el filtro CSRF en todas las peticiones
builder.Services.AddControllersWithViews(opciones => opciones.Filters.Add<CsrfFilter>());

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.MapControllers();

await app.RunAsync();

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program
{
    protected Program()
    {
    }
}
