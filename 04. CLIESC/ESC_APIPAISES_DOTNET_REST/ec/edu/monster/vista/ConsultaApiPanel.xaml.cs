using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// ECUD3 Consultar País en la API y ECUD2 Importar: campo de búsqueda, tabla de resultados que
/// indica si cada país ya está guardado, botón «Guardar» y «Importar todos».
/// </summary>
public partial class ConsultaApiPanel : UserControl, Vistas.ConsultaApiVista
{
    private readonly TablaPaises tabla;
    private Action<string> accionConsultar = _ => { };
    private Action<string, string> accionGuardar = (_, _) => { };
    private Action accionImportarTodos = () => { };
    private volatile string ultimaConsulta = "";

    public ConsultaApiPanel()
    {
        InitializeComponent();
        tabla = new TablaPaises(Tabla, true);
    }

    private void CampoNombre_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Consultar();
    }

    private void Consultar_Click(object sender, RoutedEventArgs e) => Consultar();

    private void Consultar()
    {
        string nombre = CampoNombre.Text;
        ultimaConsulta = nombre;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => accionConsultar(nombre), MostrarError, Controles());
    }

    /// <summary>ECUD2 pasos 1 a 3: país elegido y confirmación.</summary>
    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        Pais? pais = tabla.Seleccionado;
        if (pais == null)
        {
            MostrarError("Seleccione un país de los resultados");
            return;
        }
        if (!Dialogo.Confirmar(this, "¿Guardar el país " + pais.Nombre + " en la base de datos local?"))
            return;
        string consulta = ultimaConsulta;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => accionGuardar(consulta, pais.Nombre ?? ""), MostrarError, Controles());
    }

    private void ImportarTodos_Click(object sender, RoutedEventArgs e)
    {
        if (!Dialogo.Confirmar(this, "¿Importar todos los países desde la API REST Countries?"))
            return;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, accionImportarTodos, MostrarError, Controles());
    }

    private UIElement[] Controles() => [BotonConsultar, BotonGuardar, BotonImportarTodos, CampoNombre];

    /// <summary>«Guardar» solo se habilita con resultados (se decide después de que ConProgreso reactiva los botones).</summary>
    private void AjustarGuardar()
    {
        Dispatcher.BeginInvoke(() => BotonGuardar.IsEnabled = tabla.Cantidad > 0, DispatcherPriority.Background);
    }

    // ------------------------------------------------------------------ ConsultaApiVista

    public void AlConsultar(Action<string> accion) => accionConsultar = accion;
    public void AlGuardar(Action<string, string> accion) => accionGuardar = accion;
    public void AlImportarTodos(Action accion) => accionImportarTodos = accion;

    public void MostrarResultados(List<Pais> paises)
    {
        Estilos.EnUi(this, () =>
        {
            tabla.SetPaises(paises);
            tabla.SeleccionarPrimero();
            AjustarGuardar();
        });
    }

    public void MarcarGuardado(string nombre) => Estilos.EnUi(this, () => tabla.MarcarGuardado(nombre));

    public void MostrarMensaje(string texto)
    {
        Estilos.Mostrar(Mensaje, texto, false);
        Estilos.EnUi(this, AjustarGuardar);
    }

    public void MostrarError(string texto)
    {
        Estilos.EnUi(this, () =>
        {
            // ECUD3 alterno 2: se conserva el texto para corregirlo
            Estilos.Mostrar(Mensaje, texto, true);
            AjustarGuardar();
        });
    }
}
