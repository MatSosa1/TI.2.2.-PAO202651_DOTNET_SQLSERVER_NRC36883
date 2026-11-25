using MOV_APIPAISES_DOTNET_REST.Config;
using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;

namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>ECUD1 Iniciar Sesión: usuario y clave (oculta), con indicador de progreso mientras se valida.</summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginControlador controlador;

    public LoginPage(LoginControlador controlador)
    {
        InitializeComponent();
        this.controlador = controlador;
        LblServidor.Text = string.Format(Textos.LoginServidor, RestConfig.UrlServidor);
        BtnIngresar.Pulsado += async (_, _) => await IngresarAsync();
        CampoUsuario.Completado += (_, _) => CampoClave.Enfocar();
        CampoClave.Completado += async (_, _) => await IngresarAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (controlador.HaySesion)
            await Shell.Current.GoToAsync(NavegacionSesion.RutaInicio);
    }

    private async Task IngresarAsync()
    {
        if (BtnIngresar.Cargando)
            return;
        string usuario = CampoUsuario.Texto;
        string clave = CampoClave.Texto;
        CampoUsuario.Error = null;
        CampoClave.Error = null;
        LblError.Texto = null;

        // ECUD1 alterno 2: con un campo vacío no se envía la petición
        if (controlador.CampoInvalido(usuario, clave) is LoginControlador.Campo campo)
        {
            (campo == LoginControlador.Campo.Usuario ? CampoUsuario : CampoClave).MostrarError(LoginControlador.Mensaje(campo));
            return;
        }

        Cargando(true);
        Resultado<Sesion> resultado;
        try
        {
            resultado = await controlador.IniciarSesionAsync(usuario, clave);
        }
        catch (Exception)
        {
            resultado = Resultado<Sesion>.Error(Textos.ErrorInesperado);
        }
        Cargando(false);
        if (resultado.Exito)
        {
            CampoClave.Texto = "";
            await Shell.Current.GoToAsync(NavegacionSesion.RutaInicio);
            return;
        }
        // ECUD1 alternos 1 y 3: mensaje, se borra la clave y se mantiene el formulario
        LblError.Texto = resultado.Mensaje;
        CampoClave.Texto = "";
    }

    private void Cargando(bool activo)
    {
        BtnIngresar.Cargando = activo;
        CampoUsuario.IsEnabled = !activo;
        CampoClave.IsEnabled = !activo;
    }
}
