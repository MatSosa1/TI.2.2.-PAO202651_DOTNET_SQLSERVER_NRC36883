using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Vista.Componentes;

namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>Inicio: saludo, cantidad de países guardados y accesos a las opciones del menú.</summary>
public class InicioPage : PestanaPage
{
    private readonly Label lblNombre = new() { FontSize = 24, TextColor = Colores.TextoOscuro, FontFamily = "OpenSansSemibold" };
    private readonly Label lblCantidad = new() { Text = "—", FontSize = 40, TextColor = Colors.White, FontFamily = "OpenSansSemibold", Margin = new Thickness(0, 6, 0, 0) };
    private readonly AvisoError lblError = new() { Margin = new Thickness(0, 16, 0, 0) };
    private readonly RefreshView refrescar = new();

    public InicioPage(LoginControlador login, PaisesControlador controlador) : base(login, controlador, Textos.AppNombre)
    {
        refrescar.RefreshColor = Colores.Primario;
        refrescar.Command = new Command(async () => await CargarAsync());

        var texto = Application.Current?.Resources.TryGetValue("TextoSobrePrimario", out object? c) == true ? (Color)c : Colors.White;
        var tarjetaCantidad = new Border
        {
            Margin = new Thickness(0, 20, 0, 0),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = Textos.InicioPaisesGuardados, FontSize = 14, TextColor = texto },
                    lblCantidad,
                    new Label { Text = Textos.InicioEnBaseLocal, FontSize = 14, TextColor = texto },
                },
            },
        };
        if (Application.Current?.Resources.TryGetValue("TarjetaPrimaria", out object? estilo) == true)
            tarjetaCantidad.Style = (Style)estilo;

        var acciones = new VerticalStackLayout
        {
            Children =
            {
                new FilaAccion(Iconos.Lista, Textos.AccionPaises, Textos.AccionPaisesDetalle, () => IrA(NavegacionSesion.RutaPaises)),
                new FilaAccion(Iconos.Nube, Textos.AccionApi, Textos.AccionApiDetalle, () => IrA(NavegacionSesion.RutaApi)),
                new FilaAccion(Iconos.Buscar, Textos.AccionBuscar, Textos.AccionBuscarDetalle, () => IrA(NavegacionSesion.RutaPaises)),
                new FilaAccion(Iconos.Grafico, Textos.AccionInformes, Textos.AccionInformesDetalle, () => IrA(NavegacionSesion.RutaInformes)),
            },
        };

        refrescar.Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Children =
                {
                    Secundario(Textos.InicioSaludo, Thickness.Zero, 16),
                    lblNombre,
                    FilaPais.Pastilla(Textos.InicioServicio, Colores.Primario, Colores.PrimarioSuave).Also(p =>
                    {
                        p.HorizontalOptions = LayoutOptions.Start;
                        p.Margin = new Thickness(0, 8, 0, 0);
                    }),
                    tarjetaCantidad,
                    lblError,
                    Seccion(Textos.InicioAcciones, new Thickness(0, 24, 0, 0)),
                    Tarjeta(acciones, new Thickness(0, 12, 0, 0)),
                },
            },
        };
        Cuerpo = refrescar;
    }

    protected override void AlMostrar()
    {
        lblNombre.Text = Controlador.Sesion?.Nombre;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        refrescar.IsRefreshing = true;
        await EjecutarAsync(() => Controlador.BuscarAsync(null, null, null), resultado =>
        {
            refrescar.IsRefreshing = false;
            if (resultado.Exito)
            {
                lblCantidad.Text = (resultado.Datos?.Count ?? 0).ToString();
                MostrarError(lblError, null);
            }
            else
            {
                lblCantidad.Text = "—";
                MostrarError(lblError, resultado.Mensaje);
            }
        });
    }

    private static void IrA(string ruta) => _ = Shell.Current.GoToAsync(ruta);
}

internal static class Extensiones
{
    /// <summary>Ajusta un control recién creado dentro de un inicializador de colección.</summary>
    public static T Also<T>(this T valor, Action<T> ajuste)
    {
        ajuste(valor);
        return valor;
    }
}
