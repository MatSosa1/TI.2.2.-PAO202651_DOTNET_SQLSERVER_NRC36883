using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;
using MOV_APIPAISES_DOTNET_REST.Utils;
using MOV_APIPAISES_DOTNET_REST.Vista.Componentes;

namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>
/// ECUD7 Informes: país más poblado, gráfico de barras con la cantidad de países por región y las
/// tablas de países por región y promedio poblacional por región.
/// </summary>
public class InformesPage : PestanaPage
{
    /// <summary>Mensaje del servidor sin países guardados (ECUD7, alterno 2).</summary>
    private const string SinPaises = "No hay países registrados para generar informes";

    private readonly RefreshView refrescar = new();
    private readonly AvisoError lblError = new() { Margin = new Thickness(0, 0, 0, 16) };
    private readonly VerticalStackLayout contenido = new() { IsVisible = false };
    private readonly Label lblMasPoblado = new() { FontSize = 26, TextColor = Colors.White, FontFamily = "OpenSansSemibold", Margin = new Thickness(0, 6, 0, 0) };
    private readonly Label lblMasPobladoDetalle = new() { FontSize = 14 };
    private readonly GraficoBarras grafico = new();
    private readonly VerticalStackLayout tablaConteo = new();
    private readonly VerticalStackLayout tablaPromedio = new();

    public InformesPage(LoginControlador login, PaisesControlador controlador) : base(login, controlador, Textos.PestanaInformes)
    {
        refrescar.RefreshColor = Colores.Primario;
        refrescar.Command = new Command(async () => await GenerarAsync());
        var texto = Application.Current?.Resources.TryGetValue("TextoSobrePrimario", out object? c) == true ? (Color)c : Colors.White;
        lblMasPobladoDetalle.TextColor = texto;

        var tarjetaMayor = new Border
        {
            Content = new VerticalStackLayout
            {
                Children = { new Label { Text = Textos.InformeMasPoblado, FontSize = 14, TextColor = texto }, lblMasPoblado, lblMasPobladoDetalle },
            },
        };
        if (Application.Current?.Resources.TryGetValue("TarjetaPrimaria", out object? estilo) == true)
            tarjetaMayor.Style = (Style)estilo;

        contenido.Add(tarjetaMayor);
        contenido.Add(Seccion(Textos.InformeGrafico, new Thickness(0, 24, 0, 0)));
        contenido.Add(Tarjeta(new ContentView { Padding = 16, Content = grafico }, new Thickness(0, 12, 0, 0)));
        contenido.Add(Seccion(Textos.InformePorRegion, new Thickness(0, 24, 0, 0)));
        contenido.Add(Tarjeta(new ContentView { Padding = new Thickness(16, 4), Content = tablaConteo }, new Thickness(0, 12, 0, 0)));
        contenido.Add(Seccion(Textos.InformePromedio, new Thickness(0, 24, 0, 0)));
        contenido.Add(Tarjeta(new ContentView { Padding = new Thickness(16, 4), Content = tablaPromedio }, new Thickness(0, 12, 0, 0)));

        refrescar.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 20, Children = { lblError, contenido } } };
        Cuerpo = refrescar;
    }

    protected override void AlMostrar() => _ = GenerarAsync();

    private async Task GenerarAsync()
    {
        refrescar.IsRefreshing = true;
        await EjecutarAsync(() => Controlador.InformesAsync(), resultado =>
        {
            refrescar.IsRefreshing = false;
            if (!resultado.Exito || resultado.Datos == null)
            {
                contenido.IsVisible = false;
                string mensaje = resultado.Mensaje;
                MostrarError(lblError, mensaje == SinPaises ? mensaje + ". " + Textos.InformeSugerencia : mensaje);
                return;
            }
            MostrarError(lblError, null);
            Mostrar(resultado.Datos);
        });
    }

    private void Mostrar(Informe informe)
    {
        Pais? mayor = informe.PaisMasPoblado;
        lblMasPoblado.Text = mayor == null ? "—" : mayor.Nombre;
        lblMasPobladoDetalle.Text = mayor == null ? "" : Formato.Poblacion(mayor.Poblacion) + " habitantes · " + Formato.Texto(mayor.Region);
        grafico.SetDatos(informe.PaisesPorRegion);
        tablaConteo.Clear();
        foreach (ConteoRegion conteo in informe.PaisesPorRegion)
            tablaConteo.Add(new FilaDato(conteo.Region ?? "", conteo.Cantidad.ToString()));
        tablaPromedio.Clear();
        foreach (PromedioRegion promedio in informe.PromedioPorRegion)
            tablaPromedio.Add(new FilaDato(promedio.Region ?? "", Formato.Poblacion(promedio.PromedioPoblacion)));
        contenido.IsVisible = true;
    }
}
