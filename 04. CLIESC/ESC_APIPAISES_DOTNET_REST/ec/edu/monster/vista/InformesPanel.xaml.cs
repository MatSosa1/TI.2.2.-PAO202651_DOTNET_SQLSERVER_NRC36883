using System.Windows;
using System.Windows.Controls;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// ECUD7 Generar Informes: tabla de países por región, país más poblado, tabla de promedio
/// poblacional por región y gráfico de barras con la cantidad de países por región.
/// </summary>
public partial class InformesPanel : UserControl, Vistas.InformesVista
{
    private Action accionGenerar = () => { };

    public InformesPanel()
    {
        InitializeComponent();
        // Los informes se generan al abrir la pantalla
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue)
                Generar();
        };
    }

    private void Generar_Click(object sender, RoutedEventArgs e) => Generar();

    private void Generar()
    {
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, accionGenerar, MostrarError, BotonGenerar);
    }

    // ------------------------------------------------------------------ InformesVista

    public void AlGenerar(Action accion) => accionGenerar = accion;

    public void MostrarInforme(Informe informe, string texto)
    {
        Estilos.EnUi(this, () =>
        {
            TablaConteo.ItemsSource = (informe.PaisesPorRegion ?? []).Select(c => new { c.Region, c.Cantidad }).ToList();
            TablaPromedio.ItemsSource = (informe.PromedioPorRegion ?? [])
                .Select(p => new { p.Region, Promedio = Formatos.Poblacion(p.PromedioPoblacion) }).ToList();
            Pais? mayor = informe.PaisMasPoblado;
            MasPobladoNombre.Text = mayor == null ? "—" : mayor.Nombre;
            MasPobladoDetalle.Text = mayor == null ? "" : Formatos.Poblacion(mayor.Poblacion) + " habitantes · " + mayor.Region;
            Grafico.SetDatos(informe.PaisesPorRegion);
            Contenido.Visibility = Visibility.Visible;
        });
        Estilos.Mostrar(Mensaje, texto, false);
    }

    public void MostrarError(string texto)
    {
        Estilos.EnUi(this, () => Contenido.Visibility = Visibility.Collapsed);
        Estilos.Mostrar(Mensaje, texto, true);
    }
}
