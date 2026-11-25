using System.Net;
using System.Net.Sockets;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Services;
using static WS_APIPAISES_DOTNET_REST.Pruebas.Servicios.RestCountriesFalso;
using Tipo = WS_APIPAISES_DOTNET_REST.Services.ApiExternaException.TipoFalla;

namespace WS_APIPAISES_DOTNET_REST.Pruebas.Servicios;

/// <summary>Conversión del JSON de la v5 (ECUD9, pasos 5 a 8).</summary>
public class ConsumoAPIPaisesMapeoTest
{
    private readonly ConsumoAPIPaises api = new("http://no-se-usa", "rc_prueba", 15);

    [Fact]
    public void ConvierteUnPaisCompleto()
    {
        PaisDTO ecuador = api.Mapear(Pagina(false, Ecuador))[0];

        Assert.Equal("Ecuador", ecuador.Nombre);
        Assert.Equal("Quito", ecuador.Capital);
        Assert.Equal("América", ecuador.Region);
        Assert.Equal(18001000L, ecuador.Poblacion);
        Assert.Equal("United States dollar (USD)", ecuador.Moneda);
        Assert.False(ecuador.Guardado);
    }

    [Fact]
    public void UsaElNombreEnEspanol()
    {
        Assert.Equal("Alemania", api.Mapear(Pagina(false, Alemania))[0].Nombre);
    }

    [Fact]
    public void SinTraduccionUsaElNombreComun()
    {
        string json = Pagina(false, Pais("Narnia", null, "[]", "\"Europe\"", "5", "[]"));
        Assert.Equal("Narnia", api.Mapear(json)[0].Nombre);
    }

    [Fact]
    public void SinCapitalNiMonedaUsaLosTextosDelEcud()
    {
        string antartida = Pais("Antarctica", "Antártida", "[]", "\"Antarctic\"", "1000", "[]");
        PaisDTO pais = api.Mapear(Pagina(false, antartida))[0];

        Assert.Equal("Sin capital", pais.Capital);
        Assert.Equal("Sin moneda", pais.Moneda);
        Assert.Equal("Antártida", pais.Region);
    }

    [Fact]
    public void TomaLaCapitalPrincipalAunqueNoSeaLaPrimera()
    {
        string sudafrica = Pais("South Africa", "Sudáfrica",
            "[" + Capital("Bloemfontein", false) + "," + Capital("Pretoria", true) + "]",
            "\"Africa\"", "60000000", "[" + Moneda("ZAR", "South African rand") + "]");
        Assert.Equal("Pretoria", api.Mapear(Pagina(false, sudafrica))[0].Capital);
    }

    [Fact]
    public void SinCapitalPrincipalTomaLaPrimera()
    {
        string bolivia = Pais("Bolivia", "Bolivia",
            "[" + Capital("Sucre", false) + "," + Capital("La Paz", false) + "]",
            "\"Americas\"", "12000000", "[" + Moneda("BOB", "Bolivian boliviano") + "]");
        Assert.Equal("Sucre", api.Mapear(Pagina(false, bolivia))[0].Capital);
    }

    [Fact]
    public void TomaLaPrimeraMoneda()
    {
        string panama = Pais("Panama", "Panamá", "[" + Capital("Panama City", true) + "]", "\"Americas\"",
            "4000000", "[" + Moneda("PAB", "Panamanian balboa") + "," + Moneda("USD", "United States dollar") + "]");
        Assert.Equal("Panamanian balboa (PAB)", api.Mapear(Pagina(false, panama))[0].Moneda);
    }

    [Fact]
    public void DescartaRegionDesconocidaYSigueConLosDemas()
    {
        string raro = Pais("Atlantis", "Atlántida", "[]", "\"Underwater\"", "1", "[]");
        List<PaisDTO> paises = api.Mapear(Pagina(false, raro, Ecuador));

        Assert.Single(paises); // ECUD9, alterno 4
        Assert.Equal("Ecuador", paises[0].Nombre);
    }

    [Fact]
    public void PoblacionAusenteONegativaQuedaEnCero()
    {
        string sinPoblacion = Pais("Nowhere", "Ningunaparte", "[]", "\"Asia\"", "null", "[]");
        string negativa = Pais("Minus", "Menos", "[]", "\"Asia\"", "-5", "[]");

        List<PaisDTO> paises = api.Mapear(Pagina(false, sinPoblacion, negativa));
        Assert.Equal(0L, paises[0].Poblacion);
        Assert.Equal(0L, paises[1].Poblacion);
    }

    [Fact]
    public void RecortaNombresDeMasDeCienCaracteres()
    {
        string largo = Pais("Largo", new string('L', 140), "[]", "\"Asia\"", "1", "[]");
        Assert.Equal(100, api.Mapear(Pagina(false, largo))[0].Nombre.Length);
    }

    [Fact]
    public void JsonQueNoEsDeLaV5EsRespuestaNoValida()
    {
        // Respuesta actual de la v3.1 dada de baja
        string legado = "{\"success\":false,\"data\":null,\"errors\":[{\"message\":\"This API version has been deprecated.\"}]}";
        foreach (string json in new[] { legado, "[1,2,3]", "{no es json", "" })
        {
            var error = Assert.Throws<ApiExternaException>(() => api.Mapear(json));
            Assert.Equal(Tipo.RespuestaInvalida, error.Tipo);
            Assert.Equal(Mensajes.ApiRespuestaInvalida, error.MensajeUsuario);
            Assert.Equal(502, error.CodigoHttp);
        }
    }

    [Fact]
    public void TraduceLasSeisRegiones()
    {
        Assert.Equal("América", ConsumoAPIPaises.TraducirRegion("Americas"));
        Assert.Equal("Europa", ConsumoAPIPaises.TraducirRegion("Europe"));
        Assert.Equal("Asia", ConsumoAPIPaises.TraducirRegion("Asia"));
        Assert.Equal("África", ConsumoAPIPaises.TraducirRegion("Africa"));
        Assert.Equal("Oceanía", ConsumoAPIPaises.TraducirRegion("Oceania"));
        Assert.Equal("Antártida", ConsumoAPIPaises.TraducirRegion("Antarctic"));
        Assert.Null(ConsumoAPIPaises.TraducirRegion("Mars"));
        Assert.Null(ConsumoAPIPaises.TraducirRegion(null));
    }

    [Fact]
    public void SeleccionaElPaisElegidoSinDistinguirTildes()
    {
        var paises = new List<PaisDTO>
        {
            new("Perú", "Lima", "América", 1, "Sol (PEN)"),
            new("Paraguay", "Asunción", "América", 1, "Guaraní (PYG)"),
        };

        Assert.Equal("Perú", ConsumoAPIPaises.Seleccionar(paises, "peru")!.Nombre);
        Assert.Null(ConsumoAPIPaises.Seleccionar(paises, "Per"));
    }
}

/// <summary>Peticiones HTTP a la v5 contra un servidor falso local (ECUD9, pasos 2 a 4).</summary>
public class ConsumoAPIPaisesHttpTest
{
    private const string Key = "rc_prueba";

    [Fact]
    public async Task ObtenerTodosRecorreLasPaginasYEnviaLaApiKey()
    {
        await using var falso = await IniciarAsync(p => p.Parametros.GetValueOrDefault("offset") == "0"
            ? Respuesta.Ok(Pagina(true, Ecuador, Alemania))
            : Respuesta.Ok(Pagina(false, Austria)));

        List<PaisDTO> paises = await new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerTodosAsync();

        Assert.Equal(new[] { "Ecuador", "Alemania", "Austria" }, paises.Select(p => p.Nombre));
        var recibidas = falso.RecibidasEnOrden;
        Assert.Equal(2, recibidas.Count);
        Assert.Equal("/countries/v5", recibidas[0].Ruta);
        Assert.Equal("Bearer " + Key, recibidas[0].Authorization);
        Assert.Equal("100", recibidas[0].Parametros["limit"]);
        Assert.Equal(ConsumoAPIPaises.Campos, recibidas[0].Parametros["response_fields"]);
        Assert.Equal("2", recibidas[1].Parametros["offset"]);
    }

    [Fact]
    public async Task ObtenerPorNombreUsaElAgregadoName()
    {
        await using var falso = await IniciarAsync(_ => Respuesta.Ok(Pagina(false, Alemania)));

        List<PaisDTO> paises = await new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPorNombreAsync("germany");

        Assert.Equal("Alemania", paises[0].Nombre);
        var recibidas = falso.RecibidasEnOrden;
        Assert.Single(recibidas);
        Assert.Equal("/countries/v5/name", recibidas[0].Ruta);
        Assert.Equal("germany", recibidas[0].Parametros["q"]);
    }

    [Fact]
    public async Task ObtenerPorNombreEnEspanolBuscaEnLasTraducciones()
    {
        await using var falso = await IniciarAsync(p => p.Ruta.EndsWith("/name")
            ? Respuesta.Ok(Pagina(false))
            // names.translations encuentra «alemania» en cualquier idioma; Austria no debe quedar
            : Respuesta.Ok(Pagina(false, Alemania, Austria)));

        List<PaisDTO> paises = await new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPorNombreAsync("alemania");

        Assert.Equal(new[] { "Alemania" }, paises.Select(p => p.Nombre));
        Assert.Equal("/countries/v5/names.translations", falso.RecibidasEnOrden[1].Ruta);
    }

    [Fact]
    public async Task ObtenerPaisTomaElElegidoDeLaConsulta()
    {
        await using var falso = await IniciarAsync(_ => Respuesta.Ok(Pagina(false, Alemania, Austria)));

        PaisDTO elegido = await new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPaisAsync("a", "austria");

        Assert.Equal("Austria", elegido.Nombre);
        Assert.Single(falso.RecibidasEnOrden);
    }

    [Fact]
    public async Task ObtenerPaisBuscaPorElNombreEnEspanolSiLaConsultaNoLoTrae()
    {
        // Actualizar «Alemania»: el agregado name devuelve otros países, la traducción sí lo trae
        await using var falso = await IniciarAsync(p => p.Ruta.EndsWith("/name")
            ? Respuesta.Ok(Pagina(false, Austria))
            : Respuesta.Ok(Pagina(false, Alemania)));

        PaisDTO elegido = await new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPaisAsync(null, "Alemania");

        Assert.Equal("Alemania", elegido.Nombre);
        var recibidas = falso.RecibidasEnOrden;
        Assert.Equal("alemania", recibidas[0].Parametros["q"].ToLowerInvariant());
        Assert.Equal("/countries/v5/names.translations", recibidas[1].Ruta);
    }

    [Fact]
    public async Task ObtenerPaisSinElElegidoEs404()
    {
        await using var falso = await IniciarAsync(_ => Respuesta.Ok(Pagina(false, Austria)));

        var error = await Assert.ThrowsAsync<ApiExternaException>(
            () => new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPaisAsync("aus", "Australia"));
        Assert.Equal(Tipo.SinCoincidencias, error.Tipo);
    }

    [Fact]
    public async Task CodificaEspaciosYTildesEnLaConsulta()
    {
        await using var falso = await IniciarAsync(_ => Respuesta.Ok(Pagina(false, Ecuador)));

        await new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPorNombreAsync("  Perú del Sur ");
        Assert.Equal("Perú del Sur", falso.RecibidasEnOrden[0].Parametros["q"]);
    }

    [Fact]
    public async Task SinCoincidenciasEs404()
    {
        await using var falso = await IniciarAsync(_ => Respuesta.Ok(Pagina(false)));

        var error = await Assert.ThrowsAsync<ApiExternaException>(
            () => new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerPorNombreAsync("zzz"));

        Assert.Equal(Tipo.SinCoincidencias, error.Tipo);
        Assert.Equal(Mensajes.SinCoincidenciasApi, error.MensajeUsuario);
        Assert.Equal(404, error.CodigoHttp);
    }

    [Theory]
    [InlineData(401)] // API key
    [InlineData(403)] // cuota agotada
    [InlineData(429)] // límite de peticiones
    [InlineData(500)] // error de la API
    public async Task CodigosDeErrorDeLaApiSonNoDisponible(int codigo)
    {
        string cuerpo = "{\"errors\":[{\"message\":\"Fallo " + codigo + "\",\"code\":\"prueba\"}]}";
        await using var falso = await IniciarAsync(_ => new Respuesta(codigo, cuerpo));

        var error = await Assert.ThrowsAsync<ApiExternaException>(
            () => new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerTodosAsync());

        Assert.Equal(Tipo.NoDisponible, error.Tipo);
        Assert.Equal(Mensajes.ApiNoDisponible, error.MensajeUsuario);
        Assert.Equal(502, error.CodigoHttp);
        Assert.Contains(codigo.ToString(), error.Message); // el detalle va al log
    }

    [Fact]
    public async Task TiempoAgotadoEsNoDisponible()
    {
        await using var falso = await IniciarAsync(_ => new Respuesta(200, Pagina(false, Ecuador), 3000));

        var error = await Assert.ThrowsAsync<ApiExternaException>(
            () => new ConsumoAPIPaises(falso.UrlBase, Key, 1).ObtenerPorNombreAsync("ecuador"));
        Assert.Equal(Tipo.NoDisponible, error.Tipo);
    }

    [Fact]
    public async Task SinConexionEsNoDisponible()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        int puertoLibre = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        var api = new ConsumoAPIPaises($"http://127.0.0.1:{puertoLibre}/countries/v5", Key, 2);

        var error = await Assert.ThrowsAsync<ApiExternaException>(() => api.ObtenerTodosAsync());
        Assert.Equal(Tipo.NoDisponible, error.Tipo);
        Assert.NotNull(error.InnerException);
    }

    [Fact]
    public async Task ListaCompletaVaciaEsRespuestaNoValida()
    {
        await using var falso = await IniciarAsync(_ => Respuesta.Ok(Pagina(false)));

        var error = await Assert.ThrowsAsync<ApiExternaException>(
            () => new ConsumoAPIPaises(falso.UrlBase, Key, 5).ObtenerTodosAsync());
        Assert.Equal(Tipo.RespuestaInvalida, error.Tipo);
    }
}
