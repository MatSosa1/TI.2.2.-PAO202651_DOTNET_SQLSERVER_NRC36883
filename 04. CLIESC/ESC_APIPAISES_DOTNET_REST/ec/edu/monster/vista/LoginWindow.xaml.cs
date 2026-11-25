using System.Windows;

namespace ec.edu.monster.vista;

/// <summary>
/// Ventana de inicio de sesión (ECUD1), con el mismo diseño que EurekaBank sin el selector de
/// perfil: hay un único tipo de usuario. La navegación al menú la decide la aplicación con
/// <c>alAbrirMenu</c>.
/// </summary>
public partial class LoginWindow : Window, Vistas.LoginVista
{
    private readonly Action alAbrirMenu;
    private Action accionIngresar = () => { };

    // Copias tomadas en el hilo de la interfaz para que el controlador las lea desde el hilo de trabajo
    private volatile string? usuarioActual;
    private volatile string? claveActual;

    public IndicadorEstado IndicadorEstado { get; } = new("");

    public LoginWindow(Action alAbrirMenu)
    {
        InitializeComponent();
        this.alAbrirMenu = alAbrirMenu;
        ContenedorEstado.Content = IndicadorEstado;
        BarraMenu.Items.Add(Estilos.MenuAyuda(this));
        Loaded += (_, _) => CampoUsuario.Focus();
    }

    private void Ingresar_Click(object sender, RoutedEventArgs e)
    {
        usuarioActual = CampoUsuario.Text;
        claveActual = CampoClave.Password;
        Estilos.ConProgreso(Progreso, accionIngresar, MostrarError, BotonIngresar);
    }

    private void Salir_Click(object sender, RoutedEventArgs e) => Close();

    public string? Usuario => usuarioActual;

    public string? Clave => claveActual;

    public void MostrarError(string mensaje) => Estilos.EnUi(this, () => Dialogo.Error(this, mensaje));

    public void LimpiarClave()
    {
        Estilos.EnUi(this, () =>
        {
            CampoClave.Clear();
            CampoClave.Focus();
        });
    }

    public void AbrirMenu()
    {
        Estilos.EnUi(this, () =>
        {
            claveActual = null;
            alAbrirMenu();
            IndicadorEstado.Detener();
            Close();
        });
    }

    public void AlIngresar(Action accion) => accionIngresar = accion;
}
