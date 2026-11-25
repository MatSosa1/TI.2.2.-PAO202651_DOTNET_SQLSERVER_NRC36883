using System.Windows;
using System.Windows.Controls;

namespace ec.edu.monster.vista;

/// <summary>
/// Ventana principal después del inicio de sesión (ECUD1 paso 7): encabezado con la marca, el
/// sello del servicio y el usuario; menú lateral con Inicio, Países, Consultar en la API, Buscar
/// países e Informes; y la línea de estado del servidor. Mismo diseño que TI.2.1.
/// </summary>
public partial class PrincipalWindow : Window
{
    public const string InicioNombre = "Inicio";
    public const string PaisesNombre = "Países";
    public const string ConsultaNombre = "Consultar en la API";
    public const string BusquedaNombre = "Buscar países";
    public const string InformesNombre = "Informes";

    private static readonly string[] PantallasOrden = [InicioNombre, PaisesNombre, ConsultaNombre, BusquedaNombre, InformesNombre];

    private readonly Dictionary<string, Button> navegacion = [];
    private readonly Dictionary<string, UIElement> pantallas;
    private readonly Action alCerrarSesion;
    private bool cerrandoSesion;

    public IndicadorEstado IndicadorEstado { get; } = new("Sesión activa · ");

    public PaisesPanel PaisesPanel => Paises;
    public ConsultaApiPanel ConsultaApiPanel => Consulta;
    public BusquedaPanel BusquedaPanel => Busqueda;
    public InformesPanel InformesPanel => Informes;

    public PrincipalWindow(string nombre, Action alCerrarSesion)
    {
        InitializeComponent();
        this.alCerrarSesion = alCerrarSesion;
        NombreUsuario.Text = nombre;
        ContenedorEstado.Content = IndicadorEstado;
        Inicio.Configurar(nombre, Seleccionar);
        pantallas = new()
        {
            [InicioNombre] = Inicio,
            [PaisesNombre] = Paises,
            [ConsultaNombre] = Consulta,
            [BusquedaNombre] = Busqueda,
            [InformesNombre] = Informes,
        };

        foreach (string pantalla in PantallasOrden)
        {
            var boton = new Button { Content = pantalla, Style = (Style)FindResource("BotonNavegacion") };
            boton.Click += (_, _) => Seleccionar(pantalla);
            navegacion[pantalla] = boton;
            Navegacion.Children.Add(boton);

            var opcion = new MenuItem { Header = pantalla };
            opcion.Click += (_, _) => Seleccionar(pantalla);
            MenuArchivo.Items.Add(opcion);
        }
        MenuArchivo.Items.Add(new Separator());
        var cerrar = new MenuItem { Header = "Cerrar sesión" };
        cerrar.Click += (_, _) => CerrarSesion();
        MenuArchivo.Items.Add(cerrar);
        var salir = new MenuItem { Header = "Salir" };
        salir.Click += (_, _) => Close();
        MenuArchivo.Items.Add(salir);
        BarraMenu.Items.Add(Estilos.MenuAyuda(this));

        Seleccionar(InicioNombre);
    }

    /// <summary>Muestra la pantalla y resalta su opción en el menú lateral.</summary>
    public void Seleccionar(string pantalla)
    {
        foreach (var (nombre, elemento) in pantallas)
            elemento.Visibility = nombre == pantalla ? Visibility.Visible : Visibility.Collapsed;
        foreach (var (nombre, boton) in navegacion)
        {
            bool activa = nombre == pantalla;
            boton.Background = Estilos.Pincel(activa ? "FondoResultado" : "Blanco");
            boton.Foreground = Estilos.Pincel(activa ? "Primario" : "Texto");
            boton.FontWeight = activa ? FontWeights.Bold : FontWeights.Normal;
        }
    }

    private void CerrarSesion_Click(object sender, RoutedEventArgs e) => CerrarSesion();

    private void CerrarSesion()
    {
        cerrandoSesion = true;
        try
        {
            alCerrarSesion();
        }
        finally
        {
            Cerrar();
        }
    }

    /// <summary>Cierra la ventana sin terminar la aplicación (cerrar sesión o sesión no válida).</summary>
    public void Cerrar()
    {
        cerrandoSesion = true;
        IndicadorEstado.Detener();
        Close();
    }

    public bool CerrandoSesion => cerrandoSesion;
}
