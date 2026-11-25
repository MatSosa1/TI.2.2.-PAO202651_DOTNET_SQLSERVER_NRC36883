using System.Windows;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// ECUD4 Registrar País, pasos 2 a 6: el usuario consulta un nombre en la API, elige un país de
/// los resultados y presiona «Registrar». «Cancelar» cierra sin enviar nada (alterno 6).
/// </summary>
public partial class NuevoPaisDialog : Window
{
    private readonly TablaPaises tabla;
    private readonly Action<string> alConsultar;
    private readonly Action<string, string> alRegistrar;
    private volatile string consulta = "";

    public NuevoPaisDialog(Window? padre, Action<string> alConsultar, Action<string, string> alRegistrar)
    {
        InitializeComponent();
        if (padre != null)
            Owner = padre;
        this.alConsultar = alConsultar;
        this.alRegistrar = alRegistrar;
        tabla = new TablaPaises(Tabla, true);
        Loaded += (_, _) => CampoConsulta.Focus();
    }

    private void Consultar_Click(object sender, RoutedEventArgs e)
    {
        consulta = CampoConsulta.Text;
        string texto = consulta;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => alConsultar(texto), MostrarError, BotonConsultar, BotonRegistrar);
    }

    private void Registrar_Click(object sender, RoutedEventArgs e)
    {
        Pais? pais = tabla.Seleccionado;
        if (pais == null)
        {
            MostrarError("Seleccione un país de los resultados");
            return;
        }
        string texto = consulta;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => alRegistrar(texto, pais.Nombre ?? ""), MostrarError, BotonConsultar, BotonRegistrar);
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>ECUD4 Registrar paso 4: se muestran los resultados y se habilita «Registrar».</summary>
    public void MostrarResultados(List<Pais> paises)
    {
        Estilos.EnUi(this, () =>
        {
            tabla.SetPaises(paises);
            tabla.SeleccionarPrimero();
            // ConProgreso vuelve a habilitar los botones al terminar; se decide después
            Dispatcher.BeginInvoke(() =>
            {
                BotonRegistrar.IsEnabled = paises.Count > 0;
                BotonRegistrar.IsDefault = paises.Count > 0;
                BotonConsultar.IsDefault = paises.Count == 0;
            }, System.Windows.Threading.DispatcherPriority.Background);
        });
    }

    public void MostrarError(string texto)
    {
        Estilos.Mostrar(Mensaje, texto, true);
        Estilos.EnUi(this, () => Dispatcher.BeginInvoke(() => BotonRegistrar.IsEnabled = tabla.Cantidad > 0,
            System.Windows.Threading.DispatcherPriority.Background));
    }
}
