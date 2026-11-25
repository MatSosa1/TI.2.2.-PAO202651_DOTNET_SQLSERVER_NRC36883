using System.Windows;
using System.Windows.Controls;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// ECUD4 Gestionar Países: tabla con los países guardados, botones Nuevo, Actualizar y Eliminar,
/// y la lista de filtros por región en la parte inferior (ECUD6, ERS 9.1.1), que filtra la
/// tabla de inmediato al elegir una región.
/// </summary>
public partial class PaisesPanel : UserControl, Vistas.PaisesVista
{
    private readonly TablaPaises tabla;
    private readonly List<RadioButton> filtros = [];
    private NuevoPaisDialog? dialogo;

    private Action<string> accionCargar = _ => { };
    private Action<string> accionActualizar = _ => { };
    private Action<string> accionEliminar = _ => { };
    private Action<string> accionConsultarApi = _ => { };
    private Action<string, string> accionRegistrar = (_, _) => { };
    private string region = "";

    public PaisesPanel()
    {
        InitializeComponent();
        tabla = new TablaPaises(Tabla, false);

        var opciones = new List<string> { Formatos.Todas };
        opciones.AddRange(Formatos.Regiones);
        foreach (string opcion in opciones)
        {
            var boton = new RadioButton
            {
                Content = opcion, GroupName = "RegionPaises", IsChecked = opcion == Formatos.Todas,
                Style = (Style)FindResource("Filtro"),
            };
            boton.Checked += (_, _) => Cargar(Formatos.Region(opcion));
            filtros.Add(boton);
            Filtros.Children.Add(boton);
        }

        // La tabla se recarga cada vez que se abre la pantalla
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue)
                Cargar(region);
        };
    }

    // ------------------------------------------------------------------ Acciones del usuario

    private void Cargar(string regionElegida)
    {
        region = regionElegida;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => accionCargar(regionElegida), MostrarError, Controles());
    }

    private void Actualizar_Click(object sender, RoutedEventArgs e)
    {
        Pais? pais = Seleccionado();
        if (pais == null)
            return;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => accionActualizar(pais.Nombre ?? ""), MostrarError, Controles());
    }

    /// <summary>ECUD4 Eliminar pasos 2 y 3: se pide confirmar antes de enviar.</summary>
    private void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        Pais? pais = Seleccionado();
        if (pais == null || !Dialogo.Confirmar(this, "¿Eliminar el país " + pais.Nombre + "?"))
            return;
        Estilos.Limpiar(Mensaje);
        Estilos.ConProgreso(Progreso, () => accionEliminar(pais.Nombre ?? ""), MostrarError, Controles());
    }

    private void Nuevo_Click(object sender, RoutedEventArgs e)
    {
        dialogo = new NuevoPaisDialog(Window.GetWindow(this), accionConsultarApi, accionRegistrar);
        dialogo.ShowDialog();
        dialogo = null;
    }

    private Pais? Seleccionado()
    {
        Pais? pais = tabla.Seleccionado;
        if (pais == null)
            Estilos.Mostrar(Mensaje, "Seleccione un país de la tabla", true);
        return pais;
    }

    private UIElement[] Controles()
    {
        var lista = new List<UIElement> { BotonNuevo, BotonActualizar, BotonEliminar };
        lista.AddRange(filtros);
        return lista.ToArray();
    }

    // ------------------------------------------------------------------ PaisesVista

    public void AlCargar(Action<string> accion) => accionCargar = accion;
    public void AlActualizar(Action<string> accion) => accionActualizar = accion;
    public void AlEliminar(Action<string> accion) => accionEliminar = accion;
    public void AlConsultarApi(Action<string> accion) => accionConsultarApi = accion;
    public void AlRegistrar(Action<string, string> accion) => accionRegistrar = accion;

    public void MostrarPaises(List<Pais> paises, string regionActual)
    {
        Estilos.EnUi(this, () =>
        {
            tabla.SetPaises(paises);
            // ECUD4 alterno 7 y ECUD6 alterno 3
            Total.Text = paises.Count == 0
                ? regionActual.Length == 0
                    ? "No hay países registrados. Use «Consultar en la API» para guardarlos"
                    : "No se encontraron países en esa región"
                : Formatos.Total(paises.Count);
        });
    }

    public void MostrarResultadosApi(List<Pais> paises) => dialogo?.MostrarResultados(paises);

    public void RegistroCompletado(string texto)
    {
        // ECUD4 Registrar paso 9: se muestra el mensaje, se cierra el formulario y se recarga la tabla
        Estilos.EnUi(this, () =>
        {
            dialogo?.Close();
            dialogo = null;
        });
        MostrarMensaje(texto);
    }

    public void MostrarMensaje(string texto) => Estilos.Mostrar(Mensaje, texto, false);

    public void MostrarError(string texto) => Estilos.Mostrar(Mensaje, texto, true);

    public void MostrarErrorRegistro(string texto)
    {
        if (dialogo != null)
            dialogo.MostrarError(texto);
        else
            MostrarError(texto);
    }
}
