using WEB_APIPAISES_DOTNET_REST.Models.Dto;
using WEB_APIPAISES_DOTNET_REST.Models.Servicios;
using WEB_APIPAISES_DOTNET_REST.Pruebas.Fakes;

namespace WEB_APIPAISES_DOTNET_REST.Pruebas.Modelos;

public class ModelosTest
{
    [Fact]
    public void BarrasProporcionalesALaRegionConMasPaises()
    {
        var informe = new Informe
        {
            PaisesPorRegion = [new() { Region = "Europa", Cantidad = 4 }, new() { Region = "Asia", Cantidad = 1 }],
        };
        List<Barra> barras = informe.Barras;

        // Dos columnas de 300: barra de 180 centrada (x = 60) y alto proporcional a 160
        Assert.Equal(new Barra("Europa", 4, 60, 40, 180, 160, 150), barras[0]);
        Assert.Equal(new Barra("Asia", 1, 360, 160, 180, 40, 450), barras[1]);
        Assert.Empty(new Informe().Barras);
    }

    [Fact]
    public void FormatosDeEntradaYPresentacion()
    {
        Assert.Equal("1.408.280.000", Formatos.Poblacion(1408280000));
        Assert.Equal("3 países encontrados", Formatos.Total(3));
        Assert.Equal("", Formatos.Region("Todas"));
        Assert.Equal("", Formatos.Region("Marte"));
        Assert.Equal("Asia", Formatos.Region(" Asia "));
        Assert.Null(Formatos.ErrorNombre(new string('x', 100)));
        Assert.Equal(Formatos.NombreMuyLargo, Formatos.ErrorNombre(new string('x', 101)));
    }

    [Fact]
    public async Task ClienteRestTraduceLosErroresDelServidor()
    {
        var servidor = new ServidorFalso();
        var cliente = new ClienteRest(new HttpClient(servidor), new ApiConfig("http://servidor:5090/api/"));
        var paises = new PaisService(cliente);

        servidor.Responder("POST /api/Paises", 409, """{"estado":"ERROR","mensaje":"Ya existe un país con ese nombre","datos":null}""");
        var duplicado = await Assert.ThrowsAsync<ServicioException>(() => paises.RegistrarAsync("T", "ecua", "Ecuador"));
        Assert.Equal("Ya existe un país con ese nombre", duplicado.Message);
        Assert.False(duplicado.SesionNoValida);

        servidor.Responder("GET /api/Paises", 401, """{"estado":"ERROR","mensaje":"Token inválido o sesión no activa","datos":null}""");
        Assert.True((await Assert.ThrowsAsync<ServicioException>(() => paises.ListarAsync("T"))).SesionNoValida);

        servidor.Responder("GET /api/Reportes", 500, "<html>");
        Assert.Equal(ServicioException.RespuestaIncompleta,
            (await Assert.ThrowsAsync<ServicioException>(() => new ReporteService(cliente).GenerarAsync("T"))).Message);

        servidor.SinRed = true;
        Assert.Equal(ServicioException.SinServidor, (await Assert.ThrowsAsync<ServicioException>(() => paises.ListarAsync("T"))).Message);
    }

    [Fact]
    public async Task LoginCon401EsCredencialesIncorrectas()
    {
        var servidor = new ServidorFalso();
        servidor.Responder("POST /api/Login/login", 401, """{"estado":"ERROR","mensaje":"Credenciales incorrectas","datos":null}""");
        var login = new LoginService(new ClienteRest(new HttpClient(servidor), new ApiConfig("http://servidor:5090/api")));

        var error = await Assert.ThrowsAsync<ServicioException>(() => login.IniciarSesionAsync("admin", "mala"));
        Assert.Equal("Credenciales incorrectas", error.Message);
        Assert.False(error.SesionNoValida);
    }
}
