using ec.edu.monster.controlador;
using ec.edu.monster.dobles;
using ec.edu.monster.modelo;
using ec.edu.monster.vista;

namespace ec.edu.monster.pruebas;

public class ControladoresTest
{
    private static readonly Pais Ecuador = new("Ecuador", "Quito", "América", 18001000, "United States dollar (USD)", false);

    private readonly ServicioFalso servicio = new();
    private readonly Sesion sesion = new();
    private int sesionesNoValidas;

    public ControladoresTest()
    {
        sesion.Iniciar("admin", new DatosSesion("TOKEN", "Administrador"));
    }

    private void SesionNoValida() => sesionesNoValidas++;

    // ------------------------------------------------------------------ ECUD1

    [Fact]
    public void LoginCorrectoGuardaLaSesionYAbreElMenu()
    {
        var vista = new LoginVistaFalsa { Usuario = " admin ", Clave = "admin123" };
        var nueva = new Sesion();
        _ = new LoginControlador(vista, servicio, nueva);

        vista.Ingresar();

        Assert.True(vista.MenuAbierto);
        Assert.Equal("login:admin", servicio.Llamadas.Single());
        Assert.Equal("Administrador", nueva.Nombre);
        Assert.True(nueva.EstaActiva());
    }

    [Theory]
    [InlineData("", "x", Formatos.IngreseUsuario)]
    [InlineData("  ", "x", Formatos.IngreseUsuario)]
    [InlineData("admin", "", Formatos.IngreseClave)]
    public void CamposVaciosNoEnvianLaPeticion(string usuario, string clave, string mensaje)
    {
        var vista = new LoginVistaFalsa { Usuario = usuario, Clave = clave };
        _ = new LoginControlador(vista, servicio, new Sesion());

        vista.Ingresar();

        Assert.Equal(mensaje, vista.Errores.Single());
        Assert.Empty(servicio.Llamadas);
    }

    [Fact]
    public void CredencialesIncorrectasLimpianLaClave()
    {
        servicio.Falla = new ServicioException("Credenciales incorrectas");
        var vista = new LoginVistaFalsa { Usuario = "admin", Clave = "mala" };
        _ = new LoginControlador(vista, servicio, new Sesion());

        vista.Ingresar();

        Assert.True(vista.ClaveLimpia);
        Assert.Equal("Credenciales incorrectas", vista.Errores.Single());
        Assert.False(vista.MenuAbierto);
    }

    // ------------------------------------------------------------------ ECUD4 y ECUD6

    [Fact]
    public void CargarSinRegionListaYConRegionFiltra()
    {
        servicio.Paises = [Ecuador];
        var vista = new PaisesVistaFalsa();
        _ = new PaisesControlador(vista, servicio, sesion, SesionNoValida);

        vista.Cargar("");
        Assert.Equal("listar", servicio.Llamadas.Last());
        Assert.Equal("TOKEN", servicio.UltimoToken);
        Assert.Equal("", vista.Region);

        vista.Cargar("América");
        Assert.Equal("buscar:||América", servicio.Llamadas.Last());
        Assert.Equal("América", vista.Region);
        Assert.Single(vista.Paises!);
    }

    [Fact]
    public void ActualizarYEliminarRecarganConLaRegionActiva()
    {
        var vista = new PaisesVistaFalsa();
        _ = new PaisesControlador(vista, servicio, sesion, SesionNoValida);
        vista.Cargar("Europa");

        vista.Actualizar("Alemania");
        Assert.Equal(["actualizar:Alemania", "buscar:||Europa"], servicio.Llamadas.Skip(1));
        Assert.Equal("País actualizado correctamente", vista.Mensajes.Last());

        vista.Eliminar("Alemania");
        Assert.Equal("buscar:||Europa", servicio.Llamadas.Last());
        Assert.Equal("País eliminado correctamente", vista.Mensajes.Last());
    }

    [Fact]
    public void NuevoValidaElNombreConsultaYRegistra()
    {
        servicio.Paises = [Ecuador];
        var vista = new PaisesVistaFalsa();
        _ = new PaisesControlador(vista, servicio, sesion, SesionNoValida);

        vista.ConsultarApi(" ");
        Assert.Equal(Formatos.IngreseNombrePais, vista.ErroresRegistro.Single());
        Assert.Empty(servicio.Llamadas);

        vista.ConsultarApi(" ecua ");
        Assert.Equal("consultarApi:ecua", servicio.Llamadas.Last());
        Assert.Single(vista.ResultadosApi!);

        vista.Registrar("ecua", "Ecuador");
        Assert.Equal("País registrado correctamente", vista.Registro);
        Assert.Equal("listar", servicio.Llamadas.Last());
    }

    [Fact]
    public void RegistrarDuplicadoMuestraElErrorEnElDialogo()
    {
        servicio.Falla = new ServicioException("Ya existe un país con ese nombre");
        var vista = new PaisesVistaFalsa();
        _ = new PaisesControlador(vista, servicio, sesion, SesionNoValida);

        vista.Registrar("ecuador", "Ecuador");

        Assert.Equal("Ya existe un país con ese nombre", vista.ErroresRegistro.Single());
        Assert.Null(vista.Registro);
    }

    [Fact]
    public void TokenRechazadoCierraLaSesionYAvisa()
    {
        servicio.Falla = new ServicioException(ServicioException.SesionNoValidaMensaje, true);
        var vista = new PaisesVistaFalsa();
        _ = new PaisesControlador(vista, servicio, sesion, SesionNoValida);

        vista.Cargar("");

        Assert.Equal(1, sesionesNoValidas);
        Assert.False(sesion.EstaActiva());
        Assert.Empty(vista.Errores); // no se muestra en la pantalla: se vuelve al login
    }

    // ------------------------------------------------------------------ ECUD3 y ECUD2

    [Fact]
    public void ConsultarGuardarEImportarTodos()
    {
        servicio.Paises = [Ecuador];
        var vista = new ConsultaApiVistaFalsa();
        _ = new ConsultaApiControlador(vista, servicio, sesion, SesionNoValida);

        vista.Consultar(new string('x', 101));
        Assert.Equal(Formatos.NombreMuyLargo, vista.Errores.Single());

        vista.Consultar(" ecuador ");
        Assert.Equal("consultarApi:ecuador", servicio.Llamadas.Last());
        Assert.Single(vista.Resultados!);

        vista.Guardar("ecuador", "Ecuador");
        Assert.Equal("importar:ecuador|Ecuador", servicio.Llamadas.Last());
        Assert.Equal("País importado correctamente (registrado)", vista.Mensajes.Last());
        Assert.Equal("Ecuador", vista.Guardados.Single());

        vista.ImportarTodos();
        Assert.Equal("Importación finalizada: 250 recibidos, 240 registrados, 10 actualizados", vista.Mensajes.Last());
    }

    [Fact]
    public void ErrorDeLaApiSeMuestraEnLaPantalla()
    {
        servicio.Falla = new ServicioException("La API REST Countries no está disponible, intente más tarde");
        var vista = new ConsultaApiVistaFalsa();
        _ = new ConsultaApiControlador(vista, servicio, sesion, SesionNoValida);

        vista.Consultar("ecuador");

        Assert.Equal("La API REST Countries no está disponible, intente más tarde", vista.Errores.Single());
        Assert.Equal(0, sesionesNoValidas);
    }

    // ------------------------------------------------------------------ ECUD5

    [Fact]
    public void BuscarEnviaLosCriteriosYTodasEsSinRegion()
    {
        var vista = new BusquedaVistaFalsa();
        _ = new BusquedaControlador(vista, servicio, sesion, SesionNoValida);

        vista.Buscar(new Vistas.Criterios("ecu", "", Formatos.Todas));
        Assert.Equal("buscar:ecu||", servicio.Llamadas.Last());
        Assert.Equal("Búsqueda realizada correctamente", vista.Mensaje);

        vista.Buscar(new Vistas.Criterios(new string('x', 101), "", "Asia"));
        Assert.Equal(Formatos.NombreMuyLargo, vista.Errores.Single());
    }

    // ------------------------------------------------------------------ ECUD7

    [Fact]
    public void InformesSinPaisesAgreganLaSugerencia()
    {
        var vista = new InformesVistaFalsa();
        _ = new InformesControlador(vista, servicio, sesion, SesionNoValida);

        vista.Generar();
        Assert.Equal("Informes generados correctamente", vista.Mensaje);
        Assert.NotNull(vista.Informe);

        servicio.Falla = new ServicioException(InformesControlador.SinPaises);
        vista.Generar();
        Assert.Equal(InformesControlador.SinPaises + InformesControlador.Sugerencia, vista.Errores.Single());
    }

    [Fact]
    public void EstadoMuestraSiElServidorResponde()
    {
        var vista = new EstadoVistaFalsa();
        bool disponible = false;
        _ = new EstadoControlador(vista, () => disponible);

        vista.Revisar();
        Assert.False(vista.Disponible);
        disponible = true;
        vista.Revisar();
        Assert.True(vista.Disponible);
    }
}
