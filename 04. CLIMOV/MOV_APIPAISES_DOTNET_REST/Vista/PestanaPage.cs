using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;
using MOV_APIPAISES_DOTNET_REST.Vista.Componentes;

namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>
/// Pestaña de la pantalla principal (PestanaFragment del cliente Android): barra con el título y el
/// nombre del usuario, «Cerrar sesión» con confirmación, revisión de la sesión cada vez que se
/// muestra, la hoja inferior y el aviso breve sobre las pestañas.
/// </summary>
public abstract class PestanaPage : ContentPage
{
    private readonly LoginControlador login;
    private readonly Label subtitulo = new() { FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Grid raiz = new();

    protected PaisesControlador Controlador { get; }
    protected Hoja HojaInferior { get; } = new();
    protected Aviso AvisoBreve { get; } = new();

    protected PestanaPage(LoginControlador login, PaisesControlador controlador, string titulo)
    {
        this.login = login;
        Controlador = controlador;
        Title = titulo;
        BackgroundColor = Colores.FondoApp;

        subtitulo.TextColor = Application.Current?.Resources.TryGetValue("TextoSobrePrimario", out object? color) == true
            ? (Color)color
            : Colors.White;
        Shell.SetTitleView(this, new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 0,
            Children =
            {
                new Label { Text = titulo, TextColor = Colors.White, FontSize = 20, FontFamily = "OpenSansSemibold" },
                subtitulo,
            },
        });
        ToolbarItems.Add(new ToolbarItem
        {
            Text = Textos.CerrarSesion,
            IconImageSource = "ic_salir.png",
            Order = ToolbarItemOrder.Primary,
            Command = new Command(async () => await ConfirmarCierreSesionAsync()),
        });
        Content = raiz;
    }

    /// <summary>Contenido de la pestaña, con la hoja y el aviso por encima.</summary>
    protected View Cuerpo
    {
        set
        {
            raiz.Clear();
            raiz.Add(value);
            raiz.Add(HojaInferior);
            raiz.Add(AvisoBreve);
        }
    }

    /// <summary>Se llama cada vez que la pestaña se vuelve visible con una sesión vigente.</summary>
    protected abstract void AlMostrar();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!Controlador.HaySesion)
        {
            _ = NavegacionSesion.VolverAlLoginAsync(this, ControladorConSesion.MensajeSesionNoValida);
            return;
        }
        subtitulo.Text = Controlador.Sesion?.Nombre;
        AlMostrar();
    }

    protected override bool OnBackButtonPressed()
    {
        if (HojaInferior.Abierta)
        {
            _ = HojaInferior.CerrarAsync();
            return true;
        }
        return base.OnBackButtonPressed();
    }

    /// <summary>
    /// Ejecuta la operación y entrega el resultado; con la sesión vencida vuelve al inicio de sesión
    /// (ECUD8) y con una falla no prevista entrega «Ocurrió un error inesperado…».
    /// </summary>
    protected async Task EjecutarAsync<T>(Func<Task<Resultado<T>>> operacion, Action<Resultado<T>> alTerminar)
    {
        Resultado<T> resultado;
        try
        {
            resultado = await operacion();
        }
        catch (Exception)
        {
            resultado = Resultado<T>.Error(Textos.ErrorInesperado);
        }
        if (resultado.SesionExpirada)
        {
            await NavegacionSesion.VolverAlLoginAsync(this, resultado.Mensaje);
            return;
        }
        alTerminar(resultado);
    }

    /// <summary>Aviso de error en la pantalla (ERS: mensajes de error destacados).</summary>
    protected static void MostrarError(AvisoError aviso, string? mensaje) => aviso.Texto = mensaje;

    /// <summary>Mensaje de confirmación breve (por ejemplo «País eliminado correctamente»).</summary>
    protected void Avisar(string? mensaje)
    {
        if (!string.IsNullOrEmpty(mensaje))
            _ = AvisoBreve.MostrarAsync(mensaje);
    }

    private async Task ConfirmarCierreSesionAsync()
    {
        if (!await DisplayAlertAsync(Textos.CerrarSesion, Textos.CerrarSesionPregunta, Textos.Si, Textos.No))
            return;
        // ECUD1 alterno 4: la sesión local se borra de inmediato y el logout se avisa en segundo plano.
        string? token = login.CerrarSesion();
        _ = login.NotificarCierreAsync(token);
        await NavegacionSesion.VolverAlLoginAsync(this, null);
    }

    /// <summary>Tarjeta blanca con borde (style Tarjeta).</summary>
    protected static Border Tarjeta(View contenido, Thickness? margen = null)
    {
        var tarjeta = new Border { Content = contenido, Margin = margen ?? Thickness.Zero };
        if (Application.Current?.Resources.TryGetValue("Tarjeta", out object? estilo) == true)
            tarjeta.Style = (Style)estilo;
        return tarjeta;
    }

    protected static Label Seccion(string texto, Thickness margen)
    {
        var etiqueta = new Label { Text = texto, Margin = margen };
        if (Application.Current?.Resources.TryGetValue("TituloSeccion", out object? estilo) == true)
            etiqueta.Style = (Style)estilo;
        return etiqueta;
    }

    protected static Label Secundario(string texto, Thickness margen, double tamano = 14)
        => new() { Text = texto, Margin = margen, FontSize = tamano, TextColor = Colores.TextoSecundario };
}
