using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.Config;
using WS_APIPAISES_DOTNET_REST.DAO;
using WS_APIPAISES_DOTNET_REST.Data;
using WS_APIPAISES_DOTNET_REST.Filtros;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Vista;

// Servidor RESTful de APIPaises: http://<servidor>:5090/api (la usan los cuatro clientes).
// Escucha en IPv4 e IPv6 (http://*:5090): en Windows «localhost» prueba primero ::1 y, si nadie
// responde ahí, cada conexión tarda unos 2 s.
var builder = WebApplication.CreateBuilder(args);

// appsettings.Local.json guarda la API key de REST Countries y no se versiona.
// Las variables de entorno y la línea de comandos siguen teniendo prioridad.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var configuracion = new Configuracion(builder.Configuration);
builder.Services.AddSingleton(configuracion);

// Modelo (MVC): EF Core sobre SQL Server y los DAO del diagrama de clases.
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(configuracion.CadenaConexion));
builder.Services.AddScoped<UsuarioDAO>();
builder.Services.AddScoped<RegionDAO>();
builder.Services.AddScoped<PaisDAO>();

// Servicios: el cliente de REST Countries reutiliza su HttpClient; los tokens viven en memoria.
builder.Services.AddSingleton<HashUtil>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ConsumoAPIPaises>();

// Controladores con el filtro de token global y la respuesta uniforme ante JSON mal formado.
builder.Services.AddScoped<TokenFilter>();
builder.Services.AddControllers(opciones => opciones.Filters.AddService<TokenFilter>())
    .AddJsonOptions(opciones =>
        opciones.JsonSerializerOptions.Encoder = JsonResponseView.Opciones.Encoder);
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
    opciones.InvalidModelStateResponseFactory = _ =>
        JsonResponseView.Error(StatusCodes.Status400BadRequest, Mensajes.DatosNoValidos));

var app = builder.Build();

app.UseErrorInesperado();
app.MapControllers();

await app.RunAsync();

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program
{
    protected Program()
    {
    }
}
