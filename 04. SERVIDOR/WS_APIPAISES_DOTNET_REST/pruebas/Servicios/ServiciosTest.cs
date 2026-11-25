using Microsoft.Extensions.Configuration;
using WS_APIPAISES_DOTNET_REST.Config;
using WS_APIPAISES_DOTNET_REST.Filtros;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Services;
using WS_APIPAISES_DOTNET_REST.Utilidades;

namespace WS_APIPAISES_DOTNET_REST.Pruebas.Servicios;

public class HashUtilTest
{
    private readonly HashUtil hash = new();

    [Fact]
    public void Sha256DeUnVectorConocido()
    {
        // Mismo valor que LOWER(CONVERT(char(64), HASHBYTES('SHA2_256', 'admin123'), 2)) en SQL Server
        Assert.Equal("240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", hash.Sha256("admin123"));
        Assert.Equal(64, hash.Sha256("").Length);
    }

    [Fact]
    public void CoincideSinDistinguirMayusculasDelHex()
    {
        string guardado = hash.Sha256("msosa").ToUpperInvariant();
        Assert.True(hash.Coincide("msosa", guardado));
        Assert.False(hash.Coincide("MSOSA", guardado));
        Assert.False(hash.Coincide(null, guardado));
        Assert.False(hash.Coincide("msosa", null));
    }
}

public class TokenServiceTest
{
    private readonly TokenService tokens = new();

    [Fact]
    public void GeneraTokensDistintosDe64Hex()
    {
        string uno = tokens.GenerarToken("admin");
        string dos = tokens.GenerarToken("admin");

        Assert.Matches("^[0-9a-f]{64}$", uno);
        Assert.NotEqual(uno, dos);
        Assert.True(tokens.EsValido(uno));
        Assert.Equal("admin", tokens.UsuarioDe(uno));
    }

    [Fact]
    public void InvalidarCierraLaSesion()
    {
        string token = tokens.GenerarToken("msosa");
        tokens.Invalidar(token);

        Assert.False(tokens.EsValido(token));
        Assert.Null(tokens.UsuarioDe(token));
    }

    [Fact]
    public void RechazaTokensMalFormadosONoRegistrados()
    {
        Assert.False(tokens.EsValido(null));
        Assert.False(tokens.EsValido("abc"));
        Assert.False(tokens.EsValido(new string('a', 64)));
    }

    [Fact]
    public void CompartenLosTokensEntreInstancias()
    {
        string token = tokens.GenerarToken("admin");
        Assert.True(new TokenService().EsValido(token));
    }

    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("  abc  ", "abc")]
    [InlineData("Bearer abc", "abc")]
    [InlineData("bearer  abc", "abc")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ExtraeElTokenDelEncabezado(string? encabezado, string? esperado)
    {
        Assert.Equal(esperado, TokenService.Extraer(encabezado));
    }
}

public class TextosTest
{
    [Fact]
    public void ClaveSinTildesNiMayusculas()
    {
        Assert.Equal("peru", Textos.Clave(" Perú "));
        Assert.Equal("africa", Textos.Clave("ÁFRICA"));
        Assert.Equal("", Textos.Clave(null));
    }

    [Fact]
    public void RecortaACienCaracteres()
    {
        Assert.Equal(100, Textos.Recortar(new string('x', 150))!.Length);
        Assert.Equal("abc", Textos.Recortar("  abc "));
        Assert.Null(Textos.Recortar(null));
        Assert.True(Textos.Vacio("  "));
    }
}

public class ConfiguracionTest
{
    private static Configuracion Crear(Dictionary<string, string?> valores)
    {
        return new Configuracion(new ConfigurationBuilder().AddInMemoryCollection(valores).Build());
    }

    [Fact]
    public void UsaLosValoresPorDefecto()
    {
        Configuracion config = Crear(new());

        Assert.Equal(Configuracion.ConexionPorDefecto, config.CadenaConexion);
        Assert.Equal("https://api.restcountries.com/countries/v5", config.ApiUrl);
        Assert.Equal("", config.ApiKey);
        Assert.Equal(15, config.ApiTimeoutSegundos);
    }

    [Fact]
    public void LeeLasClavesYQuitaLaBarraFinal()
    {
        Configuracion config = Crear(new()
        {
            ["ConnectionStrings:SqlServerConnection"] = "Server=otro",
            ["RestCountries:Url"] = "http://localhost:9999/countries/v5/",
            ["RestCountries:ApiKey"] = "  rc_clave ",
            ["RestCountries:TimeoutSegundos"] = "3",
        });

        Assert.Equal("Server=otro", config.CadenaConexion);
        Assert.Equal("http://localhost:9999/countries/v5", config.ApiUrl);
        Assert.Equal("rc_clave", config.ApiKey);
        Assert.Equal(3, config.ApiTimeoutSegundos);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-4")]
    [InlineData("abc")]
    public void TimeoutNoValidoUsa15(string valor)
    {
        Assert.Equal(15, Crear(new() { ["RestCountries:TimeoutSegundos"] = valor }).ApiTimeoutSegundos);
    }
}

public class RespuestaEstructuradaTest
{
    [Fact]
    public void OkLlevaLosDatosYErrorLosDejaVacios()
    {
        var ok = RespuestaEstructurada.CrearOk("Hecho", 5);
        var error = RespuestaEstructurada.CrearError("Falló");

        Assert.Equal("OK", ok.Estado);
        Assert.Equal(5, ok.Datos);
        Assert.Equal("ERROR", error.Estado);
        Assert.Equal("Falló", error.Mensaje);
        Assert.Null(error.Datos);
    }

    [Fact]
    public void SerializaLosTresCamposConTildes()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(RespuestaEstructurada.CrearError("Sesión cerrada"),
            Vista.JsonResponseView.Opciones);
        Assert.Equal("{\"estado\":\"ERROR\",\"mensaje\":\"Sesión cerrada\",\"datos\":null}", json);
    }
}

public class TokenFilterTest
{
    [Fact]
    public void ElLoginEsLaUnicaOperacionPublica()
    {
        var publicas = typeof(Program).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(SinTokenAttribute), false).Length > 0)
            .Select(m => m.DeclaringType!.Name + "." + m.Name)
            .ToList();
        Assert.Equal(new[] { "LoginController.Login" }, publicas);
    }
}
