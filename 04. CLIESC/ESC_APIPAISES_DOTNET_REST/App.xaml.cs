using System.Windows;
using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.vista;

namespace ec.edu.monster;

/// <summary>
/// Raíz de composición: crea el modelo una sola vez y conecta cada ventana con sus
/// controladores. Maneja la navegación entre el inicio de sesión y la ventana principal.
/// </summary>
public partial class App : Application
{
    private readonly Sesion sesion = new();
    private ServiciosRest servicios = null!;
    private EstadoServidor estado = null!;
    private PrincipalWindow? principal;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Configuracion configuracion;
        try
        {
            configuracion = Configuracion.Cargar();
        }
        catch (InvalidOperationException ex)
        {
            Dialogo.Error(null, ex.Message);
            Shutdown();
            return;
        }
        var cliente = new ClienteRest(configuracion.UrlBase, new TransporteHttp());
        servicios = new ServiciosRest(cliente);
        estado = new EstadoServidor(cliente.UrlBase);
        MostrarLogin(null);
    }

    /// <param name="aviso">mensaje a mostrar al abrir (sesión no válida), o null</param>
    private void MostrarLogin(string? aviso)
    {
        var login = new LoginWindow(MostrarPrincipal);
        _ = new LoginControlador(login, servicios, sesion);
        _ = new EstadoControlador(login.IndicadorEstado, estado);
        login.Closed += (_, _) => SalirSiNoHayVentanas();
        login.Show();
        if (aviso != null)
            login.MostrarError(aviso);
    }

    private void MostrarPrincipal()
    {
        principal = new PrincipalWindow(sesion.Nombre ?? "", CerrarSesion);
        _ = new PaisesControlador(principal.PaisesPanel, servicios, sesion, SesionNoValida);
        _ = new ConsultaApiControlador(principal.ConsultaApiPanel, servicios, sesion, SesionNoValida);
        _ = new BusquedaControlador(principal.BusquedaPanel, servicios, sesion, SesionNoValida);
        _ = new InformesControlador(principal.InformesPanel, servicios, sesion, SesionNoValida);
        _ = new EstadoControlador(principal.IndicadorEstado, estado);
        principal.Closed += (_, _) => SalirSiNoHayVentanas();
        principal.Show();
    }

    /// <summary>ECUD1 alterno 4: se borra la sesión y se vuelve al inicio aunque el servidor no responda.</summary>
    private void CerrarSesion()
    {
        string? token = sesion.Token;
        sesion.Cerrar();
        _ = Task.Run(() =>
        {
            try
            {
                servicios.CerrarSesion(token);
            }
            catch (ServicioException)
            {
                // El token se descarta igual en el cliente
            }
        });
        principal = null;
        MostrarLogin(null);
    }

    /// <summary>ECUD8 alterno 2: el token ya no vale; se cierra la ventana principal y se vuelve al login.</summary>
    private void SesionNoValida()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (principal == null)
                return;
            PrincipalWindow anterior = principal;
            principal = null;
            MostrarLogin(ServicioException.SesionNoValidaMensaje);
            anterior.Cerrar();
        });
    }

    /// <summary>Equivale a EXIT_ON_CLOSE: al cerrar la última ventana termina la aplicación.</summary>
    private void SalirSiNoHayVentanas()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (Windows.Count == 0)
                Shutdown();
        });
    }
}
