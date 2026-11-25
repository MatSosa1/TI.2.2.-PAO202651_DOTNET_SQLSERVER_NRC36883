using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// ECUD5 Buscar Países: campos Nombre y Capital y la lista de regiones. Al cambiar la región se
/// filtra de inmediato, sin presionar «Buscar», con los textos ya escritos (ECUD6).
/// </summary>
public partial class BusquedaPanel : UserControl, Vistas.BusquedaVista
{
    private readonly TablaPaises tabla;
    private Action<Vistas.Criterios> accionBuscar = _ => { };
    private bool iniciando = true;

    public BusquedaPanel()
    {
        InitializeComponent();
        tabla = new TablaPaises(Tabla, false);
        ComboRegion.Items.Add(Formatos.Todas);
        foreach (string region in Formatos.Regiones)
            ComboRegion.Items.Add(region);
        ComboRegion.SelectedIndex = 0;
        iniciando = false;
    }

    private void Campo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Buscar();
    }

    private void Region_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!iniciando)
            Buscar();
    }

    private void Buscar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void Buscar()
    {
        var criterios = new Vistas.Criterios(CampoNombre.Text, CampoCapital.Text, ComboRegion.SelectedItem as string);
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => accionBuscar(criterios), MostrarError, BotonBuscar, BotonLimpiar);
    }

    private void Limpiar_Click(object sender, RoutedEventArgs e)
    {
        CampoNombre.Text = "";
        CampoCapital.Text = "";
        if (ComboRegion.SelectedIndex == 0)
            Buscar(); // sin criterios: todos los países
        else
            ComboRegion.SelectedIndex = 0; // dispara la búsqueda sin criterios
    }

    // ------------------------------------------------------------------ BusquedaVista

    public void AlBuscar(Action<Vistas.Criterios> accion) => accionBuscar = accion;

    public void MostrarResultado(List<Pais> paises, string texto)
    {
        Estilos.EnUi(this, () =>
        {
            tabla.SetPaises(paises);
            // ECUD5 alterno 4: sin resultados se muestra la tabla vacía y un mensaje
            Total.Text = paises.Count == 0 ? "No se encontraron países con esos criterios" : Formatos.Total(paises.Count);
        });
        Estilos.Mostrar(Mensaje, texto, false);
    }

    public void MostrarError(string texto) => Estilos.Mostrar(Mensaje, texto, true);
}
