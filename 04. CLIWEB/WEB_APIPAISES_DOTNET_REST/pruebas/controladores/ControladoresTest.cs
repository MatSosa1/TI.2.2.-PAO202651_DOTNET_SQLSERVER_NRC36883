using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using WEB_APIPAISES_DOTNET_REST.Models.Dto;
using WEB_APIPAISES_DOTNET_REST.Pruebas.Fakes;

namespace WEB_APIPAISES_DOTNET_REST.Pruebas.Controladores;

/// <summary>Navegador falso: guarda la cookie de sesión y lee el token CSRF de cada página.</summary>
public sealed partial class NavegadorFalso
{
    private readonly HttpClient http;
    public string Csrf { get; private set; } = "";

    public NavegadorFalso(AplicacionWeb aplicacion)
    {
        http = aplicacion.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    [GeneratedRegex("name=\"_csrf\" value=\"([^\"]*)\"")]
    private static partial Regex CampoCsrf();

    public async Task<(HttpStatusCode Codigo, string Html, string? Destino)> GetAsync(string ruta)
    {
        HttpResponseMessage respuesta = await http.GetAsync(ruta);
        string html = await respuesta.Content.ReadAsStringAsync();
        Match csrf = CampoCsrf().Match(html);
        if (csrf.Success && csrf.Groups[1].Value.Length > 0)
            Csrf = csrf.Groups[1].Value;
        return (respuesta.StatusCode, WebUtility.HtmlDecode(html), respuesta.Headers.Location?.OriginalString);
    }

    public async Task<(HttpStatusCode Codigo, string Html, string? Destino)> PostAsync(string ruta, params (string, string)[] campos)
    {
        var datos = new List<KeyValuePair<string, string>> { new("_csrf", Csrf) };
        datos.AddRange(campos.Select(c => new KeyValuePair<string, string>(c.Item1, c.Item2)));
        HttpResponseMessage respuesta = await http.PostAsync(ruta, new FormUrlEncodedContent(datos));
        return (respuesta.StatusCode, WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()),
            respuesta.Headers.Location?.OriginalString);
    }

    public void CambiarCsrf(string valor) => Csrf = valor;

    /// <summary>GET /login y POST /login con admin/admin123.</summary>
    public async Task IniciarSesionAsync()
    {
        await GetAsync("/login");
        var (_, _, destino) = await PostAsync("/login", ("usuario", "admin"), ("clave", "admin123"));
        Assert.Equal("/menu", destino);
        await GetAsync("/menu");
    }
}

public class ControladoresTest : IClassFixture<AplicacionWeb>
{
    private readonly AplicacionWeb aplicacion;

    public ControladoresTest(AplicacionWeb aplicacion)
    {
        this.aplicacion = aplicacion;
    }

    private NavegadorFalso Navegador() => new(aplicacion);

    // ------------------------------------------------------------------ ECUD1 y filtros

    [Fact]
    public async Task RaizYPantallasProtegidasLlevanAlLogin()
    {
        var nav = Navegador();
        Assert.Equal("/login", (await nav.GetAsync("/")).Destino);
        foreach (string ruta in new[] { "/menu", "/paises", "/paises/nuevo", "/consulta", "/busqueda", "/informes" })
            Assert.Equal("/login", (await nav.GetAsync(ruta)).Destino);
    }

    [Fact]
    public async Task LoginCorrectoAbreElMenuConElNombre()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();
        var (codigo, html, _) = await nav.GetAsync("/menu");

        Assert.Equal(HttpStatusCode.OK, codigo);
        Assert.Contains("Bienvenido, Administrador", html);
        Assert.Contains("badge__platform--dotnet", html);
    }

    [Fact]
    public async Task LoginConCamposVaciosNoLlamaAlServidor()
    {
        var nav = Navegador();
        await nav.GetAsync("/login");
        int antes = aplicacion.Servidor.Recibidas.Count;

        var (_, html, _) = await nav.PostAsync("/login", ("usuario", " "), ("clave", "x"));
        Assert.Contains(Formatos.IngreseUsuario, html);
        (_, html, _) = await nav.PostAsync("/login", ("usuario", "admin"), ("clave", ""));
        Assert.Contains(Formatos.IngreseClave, html);
        Assert.Contains("value=\"admin\"", html); // se mantiene el usuario
        Assert.Equal(antes, aplicacion.Servidor.Recibidas.Count);
    }

    [Fact]
    public async Task CsrfFalsoEnLoginDaFormularioNuevoYEnLoDemas403()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();
        nav.CambiarCsrf("falso");

        Assert.Equal(HttpStatusCode.Forbidden, (await nav.PostAsync("/paises", ("accion", "eliminar"), ("nombre", "Ecuador"))).Codigo);
        Assert.Equal("/login", (await nav.PostAsync("/login", ("usuario", "admin"), ("clave", "x"))).Destino);
    }

    [Fact]
    public async Task SesionVencidaVuelveAlLoginConElAviso()
    {
        // Con un servidor que rechaza el token, la pantalla redirige a /login?sesion=expirada
        using var servidor = new AplicacionWebConToken401();
        var navVencido = new NavegadorFalso(servidor);
        await navVencido.IniciarSesionAsync();
        var (_, _, destino) = await navVencido.GetAsync("/paises");
        Assert.Equal("/login?sesion=expirada", destino);
        var (_, html, _) = await navVencido.GetAsync(destino!);
        Assert.Contains("Su sesión no es válida. Inicie sesión nuevamente.", html);
        Assert.Equal("/login", (await navVencido.GetAsync("/menu")).Destino);
    }

    [Fact]
    public async Task CerrarSesionBorraLaSesion()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();
        Assert.Equal("/login", (await nav.PostAsync("/logout")).Destino);
        Assert.Equal("/login", (await nav.GetAsync("/menu")).Destino);
    }

    // ------------------------------------------------------------------ ECUD4 y ECUD6

    [Fact]
    public async Task PaisesListaYFiltraPorRegion()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();

        var (_, html, _) = await nav.GetAsync("/paises");
        Assert.Contains("18.001.000", html);
        Assert.Contains("1 país encontrado", html);

        (_, html, _) = await nav.GetAsync("/paises?region=Am%C3%A9rica");
        Assert.Contains("chip chip--active\" href=\"/paises?region=Am%C3%A9rica\"", html);
        Assert.EndsWith("/api/Paises/buscar?region=Am%C3%A9rica", aplicacion.Servidor.Recibidas.Last().Ruta);
        Assert.Equal(ServidorFalso.Token, aplicacion.Servidor.Recibidas.Last().Token);
    }

    [Fact]
    public async Task EliminarPideConfirmarYLuegoMuestraElMensajeUnaVez()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();
        await nav.GetAsync("/paises");

        var (_, html, _) = await nav.PostAsync("/paises", ("accion", "eliminar"), ("paso", "revisar"), ("nombre", "Ecuador"), ("region", "América"));
        Assert.Contains("¿Eliminar el país <strong>Ecuador</strong>?", html);

        var (_, _, destino) = await nav.PostAsync("/paises", ("accion", "eliminar"), ("paso", "confirmar"), ("nombre", "Ecuador"), ("region", "América"));
        Assert.Equal("/paises?region=Am%C3%A9rica", destino);
        Assert.Equal("DELETE", aplicacion.Servidor.Recibidas.Last().Metodo);

        (_, html, _) = await nav.GetAsync(destino!);
        Assert.Contains("País eliminado correctamente", html);
        (_, html, _) = await nav.GetAsync(destino!);
        Assert.DoesNotContain("País eliminado correctamente", html); // flash de una sola vez
    }

    [Fact]
    public async Task ActualizarVuelveALaTablaConElMensaje()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();
        await nav.GetAsync("/paises");

        var (_, _, destino) = await nav.PostAsync("/paises", ("accion", "actualizar"), ("nombre", "Países Bajos"));
        Assert.Equal("/paises", destino);
        Assert.Equal("PUT", aplicacion.Servidor.Recibidas.Last().Metodo);
        Assert.Equal("/api/Paises/Países Bajos", aplicacion.Servidor.Recibidas.Last().Ruta);
        Assert.Contains("País actualizado correctamente", (await nav.GetAsync(destino!)).Html);
    }

    [Fact]
    public async Task NuevoConsultaYRegistra()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();

        var (_, html, _) = await nav.GetAsync("/paises/nuevo?consulta=ecua");
        Assert.Contains("id=\"pais-0\" value=\"Ecuador\" checked", html);

        var (_, _, destino) = await nav.PostAsync("/paises/nuevo", ("consulta", "ecua"), ("nombre", "Ecuador"));
        Assert.Equal("/paises", destino);
        Assert.Contains("\"consulta\":\"ecua\"", aplicacion.Servidor.Recibidas.Last().Cuerpo);
        Assert.Contains("País registrado correctamente", (await nav.GetAsync("/paises")).Html);
    }

    // ------------------------------------------------------------------ ECUD3, ECUD2 e importar todos

    [Fact]
    public async Task ConsultarGuardarEImportarTodos()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();

        var (_, html, _) = await nav.GetAsync("/consulta?nombre=" + new string('x', 101));
        Assert.Contains(Formatos.NombreMuyLargo, html);

        (_, html, _) = await nav.GetAsync("/consulta?nombre=ecua");
        Assert.Contains("tag tag--si", html);

        (_, html, _) = await nav.PostAsync("/consulta", ("accion", "guardar"), ("paso", "revisar"), ("consulta", "ecua"), ("nombre", "Ecuador"));
        Assert.Contains("¿Guardar el país <strong>Ecuador</strong> en la base de datos local?", html);

        var (_, _, destino) = await nav.PostAsync("/consulta", ("accion", "guardar"), ("paso", "confirmar"), ("consulta", "ecua"), ("nombre", "Ecuador"));
        Assert.Equal("/consulta?nombre=ecua", destino);
        Assert.Contains("País importado correctamente (registrado)", (await nav.GetAsync(destino!)).Html);

        (_, html, _) = await nav.PostAsync("/consulta", ("accion", "importarTodos"), ("paso", "revisar"));
        Assert.Contains("¿Importar todos los países desde la API REST Countries?", html);
        (_, _, destino) = await nav.PostAsync("/consulta", ("accion", "importarTodos"), ("paso", "confirmar"));
        Assert.Contains("Importación finalizada: 250 recibidos, 240 registrados, 10 actualizados", (await nav.GetAsync(destino!)).Html);
    }

    // ------------------------------------------------------------------ ECUD5 y ECUD7

    [Fact]
    public async Task BuscarMuestraTotalYMensaje()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();

        var (_, html, _) = await nav.GetAsync("/busqueda?buscar=1&nombre=ecu&capital=&region=Europa");
        Assert.Contains("1 país encontrado", html);
        Assert.Contains("Filtro aplicado correctamente", html);
        Assert.EndsWith("/api/Paises/buscar?nombre=ecu&region=Europa", aplicacion.Servidor.Recibidas.Last().Ruta);
        Assert.Contains("<option value=\"Europa\" selected=\"selected\">", html);
    }

    [Fact]
    public async Task InformesConTablasYGraficoSvg()
    {
        var nav = Navegador();
        await nav.IniciarSesionAsync();

        var (_, html, _) = await nav.GetAsync("/informes");
        Assert.Contains("Informes generados correctamente", html);
        Assert.Contains("<p class=\"stat__name\">Ecuador</p>", html);
        Assert.Contains("<rect class=\"chart__bar\" x=\"60\" y=\"40\" width=\"180\" height=\"160\" rx=\"4\">", html);
    }
}

/// <summary>Variante cuyo servidor acepta el login pero rechaza el token en todo lo demás.</summary>
public sealed class AplicacionWebConToken401 : IDisposable
{
    private readonly AplicacionWeb interna = new();

    public AplicacionWebConToken401()
    {
        const string rechazo = """{"estado":"ERROR","mensaje":"Token inválido o sesión no activa","datos":null}""";
        interna.Servidor.Responder("GET /api/Paises", 401, rechazo);
    }

    public static implicit operator AplicacionWeb(AplicacionWebConToken401 v) => v.interna;

    public void Dispose() => interna.Dispose();
}
