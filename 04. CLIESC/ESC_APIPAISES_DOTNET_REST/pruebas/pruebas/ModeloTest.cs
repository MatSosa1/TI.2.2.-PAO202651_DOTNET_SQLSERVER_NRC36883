using System.Text.Json;
using ec.edu.monster.dobles;
using ec.edu.monster.modelo;

namespace ec.edu.monster.pruebas;

public class ServiciosRestTest
{
    private const string Base = "http://servidor:5090/api";
    private const string Ecuador = """{"nombre":"Ecuador","capital":"Quito","region":"América","poblacion":18001000,"moneda":"United States dollar (USD)"}""";

    private readonly TransporteFalso transporte = new();
    private readonly ServiciosRest servicios;

    public ServiciosRestTest()
    {
        servicios = new ServiciosRest(new ClienteRest(Base + "/", transporte));
    }

    [Fact]
    public void LoginEnviaElCuerpoSinToken()
    {
        transporte.Respuesta = """{"estado":"OK","mensaje":"Ingreso de sesión exitoso","datos":{"token":"abc","usuario":"Administrador"}}""";

        DatosSesion datos = servicios.IniciarSesion("admin", "admin123");

        Assert.Equal("POST", transporte.Metodo);
        Assert.Equal(Base + "/Login/login", transporte.Url);
        Assert.False(transporte.Cabeceras.ContainsKey("Authorization"));
        using var cuerpo = JsonDocument.Parse(transporte.Cuerpo!);
        Assert.Equal("admin", cuerpo.RootElement.GetProperty("usuario").GetString());
        Assert.Equal("admin123", cuerpo.RootElement.GetProperty("clave").GetString());
        Assert.Equal("abc", datos.Token);
    }

    [Fact]
    public void LoginCon401EsCredencialesYNoSesionNoValida()
    {
        transporte.Codigo = 401;
        transporte.Respuesta = """{"estado":"ERROR","mensaje":"Credenciales incorrectas","datos":null}""";

        var error = Assert.Throws<ServicioException>(() => servicios.IniciarSesion("admin", "mala"));
        Assert.Equal("Credenciales incorrectas", error.Message);
        Assert.False(error.SesionNoValida);
    }

    [Fact]
    public void TokenRechazadoEsSesionNoValida()
    {
        transporte.Codigo = 401;
        transporte.Respuesta = """{"estado":"ERROR","mensaje":"Token inválido o sesión no activa","datos":null}""";

        var error = Assert.Throws<ServicioException>(() => servicios.Listar("TOKEN"));
        Assert.True(error.SesionNoValida);
        Assert.Equal(ServicioException.SesionNoValidaMensaje, error.Message);
    }

    [Fact]
    public void ListarLlevaElTokenSinPrefijo()
    {
        transporte.Respuesta = """{"estado":"OK","mensaje":"Países obtenidos correctamente","datos":[%P]}""".Replace("%P", Ecuador);

        Resultado<List<Pais>> resultado = servicios.Listar("TOKEN");

        Assert.Equal("GET", transporte.Metodo);
        Assert.Equal(Base + "/Paises", transporte.Url);
        Assert.Equal("TOKEN", transporte.Cabeceras["Authorization"]);
        Assert.Null(transporte.Cuerpo);
        Assert.Equal(18001000, resultado.Datos![0].Poblacion);
    }

    [Fact]
    public void BuscarCodificaYOmiteLosVacios()
    {
        servicios.Buscar("T", "san josé", " ", "América");
        Assert.Equal(Base + "/Paises/buscar?nombre=san%20jos%C3%A9&region=Am%C3%A9rica", transporte.Url);

        servicios.Buscar("T", null, null, null);
        Assert.Equal(Base + "/Paises/buscar", transporte.Url);
    }

    [Fact]
    public void RutasYMetodosDeLasDemasOperaciones()
    {
        servicios.ConsultarApi("T", "Perú del Sur");
        Assert.Equal(Base + "/Paises/externo/Per%C3%BA%20del%20Sur", transporte.Url);

        servicios.Importar("T", " ecuador ", "Ecuador");
        Assert.Equal(("POST", Base + "/Paises/importar"), (transporte.Metodo, transporte.Url));
        Assert.Equal("application/json; charset=utf-8", transporte.Cabeceras["Content-Type"]);
        using (var cuerpo = JsonDocument.Parse(transporte.Cuerpo!))
            Assert.Equal("ecuador", cuerpo.RootElement.GetProperty("consulta").GetString());

        servicios.ImportarTodos("T");
        Assert.Equal(Base + "/Paises/importar/todos", transporte.Url);
        Assert.Equal(ClienteRest.EsperaImportacion, transporte.Espera);

        servicios.Registrar("T", "ecuador", "Ecuador");
        Assert.Equal(("POST", Base + "/Paises"), (transporte.Metodo, transporte.Url));

        servicios.Actualizar("T", "Países Bajos");
        Assert.Equal(("PUT", Base + "/Paises/Pa%C3%ADses%20Bajos"), (transporte.Metodo, transporte.Url));

        servicios.Eliminar("T", "Narnia");
        Assert.Equal(("DELETE", Base + "/Paises/Narnia"), (transporte.Metodo, transporte.Url));

        servicios.Generar("T");
        Assert.Equal(("GET", Base + "/Reportes"), (transporte.Metodo, transporte.Url));

        servicios.CerrarSesion("T");
        Assert.Equal(("POST", Base + "/Login/logout"), (transporte.Metodo, transporte.Url));
    }

    [Fact]
    public void ErrorDelServidorLlegaConSuMensaje()
    {
        transporte.Codigo = 409;
        transporte.Respuesta = """{"estado":"ERROR","mensaje":"Ya existe un país con ese nombre","datos":null}""";

        var error = Assert.Throws<ServicioException>(() => servicios.Registrar("T", "ecuador", "Ecuador"));
        Assert.Equal("Ya existe un país con ese nombre", error.Message);
        Assert.False(error.SesionNoValida);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>")]
    [InlineData("{\"x\":1}")]
    public void RespuestaIlegibleEsRespuestaIncompleta(string cuerpo)
    {
        transporte.Codigo = 500;
        transporte.Respuesta = cuerpo;

        var error = Assert.Throws<ServicioException>(() => servicios.Listar("T"));
        Assert.Equal(ServicioException.RespuestaIncompleta, error.Message);
    }

    [Fact]
    public void SinRedOTiempoAgotadoEsSinServidor()
    {
        transporte.Falla = new HttpRequestException("sin red");
        Assert.Equal(ServicioException.SinServidor, Assert.Throws<ServicioException>(() => servicios.Listar("T")).Message);

        transporte.Falla = new TaskCanceledException("tiempo");
        Assert.Equal(ServicioException.SinServidor, Assert.Throws<ServicioException>(() => servicios.Listar("T")).Message);
    }
}

public class ModeloTest
{
    [Fact]
    public void FormatosDePoblacionTotalYRegion()
    {
        Assert.Equal("18.001.000", Formatos.Poblacion(18001000));
        Assert.Equal("0", Formatos.Poblacion(0));
        Assert.Equal("1 país encontrado", Formatos.Total(1));
        Assert.Equal("3 países encontrados", Formatos.Total(3));
        Assert.Equal("", Formatos.Region(Formatos.Todas));
        Assert.Equal("", Formatos.Region(null));
        Assert.Equal("Asia", Formatos.Region("Asia"));
        Assert.Equal(Formatos.IngreseNombrePais, Formatos.ErrorNombre("  "));
        Assert.Equal(Formatos.NombreMuyLargo, Formatos.ErrorNombre(new string('x', 101)));
        Assert.Null(Formatos.ErrorNombre(new string('x', 100)));
    }

    [Fact]
    public void SesionUsaElUsuarioSiNoVinoElNombre()
    {
        var sesion = new Sesion();
        Assert.False(sesion.EstaActiva());
        sesion.Iniciar("msosa", new DatosSesion("T", null));
        Assert.Equal("msosa", sesion.Nombre);
        Assert.True(sesion.EstaActiva());
        sesion.Cerrar();
        Assert.Null(sesion.Token);
    }

    [Fact]
    public void ConfiguracionArmaLaUrlYAvisaLoQueFalta()
    {
        string archivo = Path.GetTempFileName();
        try
        {
            File.WriteAllText(archivo, "# comentario\nws.host=servidor\nws.port=5090\nws.context=/api/\n");
            Assert.Equal("http://servidor:5090/api", Configuracion.DesdeArchivo(archivo).UrlBase);

            File.WriteAllText(archivo, "ws.host=servidor\nws.port=abc\nws.context=api\n");
            Assert.Contains("ws.port", Assert.Throws<InvalidOperationException>(() => Configuracion.DesdeArchivo(archivo).UrlBase).Message);

            File.WriteAllText(archivo, "ws.port=5090\nws.context=api\n");
            Assert.Contains("Falta 'ws.host'", Assert.Throws<InvalidOperationException>(() => Configuracion.DesdeArchivo(archivo).UrlBase).Message);
        }
        finally
        {
            File.Delete(archivo);
        }
    }

    [Fact]
    public void PaisComoGuardado()
    {
        var pais = new Pais("Perú", "Lima", "América", 1, "Sol (PEN)", false);
        Assert.True(pais.ComoGuardado().Guardado);
        Assert.Equal("Perú", pais.ComoGuardado().Nombre);
    }

    [Fact]
    public void EstadoServidorSinServidorEsNoDisponible()
    {
        var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        socket.Start();
        int puerto = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        Assert.False(new EstadoServidor($"http://127.0.0.1:{puerto}/api").Disponible());
    }
}
