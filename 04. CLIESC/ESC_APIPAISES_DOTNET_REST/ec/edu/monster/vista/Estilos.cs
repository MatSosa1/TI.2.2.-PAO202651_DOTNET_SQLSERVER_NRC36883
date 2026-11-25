using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Ayudas comunes a las pantallas (equivalen a las de vista/Estilos.java de TI.2.1): colores,
/// ejecución en segundo plano con barra de progreso, línea de mensaje y menú «Ayuda».
/// </summary>
internal static class Estilos
{
    public const string TituloVentana = "APIPaises";
    public const string ErrorInesperado = "Ocurrió un error inesperado";

    public static Brush Pincel(string clave) => (Brush)Application.Current.Resources[clave];

    /// <summary>Ejecuta la actualización en el hilo de la interfaz, de inmediato si ya está en él.</summary>
    public static void EnUi(DependencyObject control, Action accion)
    {
        if (control.Dispatcher.CheckAccess())
            accion();
        else
            control.Dispatcher.Invoke(accion);
    }

    /// <summary>
    /// Ejecuta una acción del controlador fuera del hilo de la interfaz mientras los controles
    /// quedan deshabilitados y la barra de progreso visible. Las fallas no previstas se informan
    /// con <paramref name="alFallar"/>.
    /// </summary>
    public static async void ConProgreso(ProgressBar? progreso, Action accion, Action<string> alFallar,
        params UIElement[] controles)
    {
        foreach (UIElement control in controles)
            control.IsEnabled = false;
        if (progreso != null)
            progreso.Visibility = Visibility.Visible;
        try
        {
            await Task.Run(accion);
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.TraceError("Acción en segundo plano fallida: " + e);
            alFallar(ErrorInesperado);
        }
        finally
        {
            if (progreso != null)
                progreso.Visibility = Visibility.Collapsed;
            foreach (UIElement control in controles)
                control.IsEnabled = true;
        }
    }

    /// <summary>Línea de mensaje bajo un formulario: verde si salió bien, roja si hubo un error.</summary>
    public static void Mostrar(TextBlock linea, string? mensaje, bool error)
    {
        EnUi(linea, () =>
        {
            linea.Foreground = Pincel(error ? "Error" : "Exito");
            linea.Text = string.IsNullOrWhiteSpace(mensaje) ? (error ? ErrorInesperado : "") : mensaje;
        });
    }

    public static void Limpiar(TextBlock linea) => EnUi(linea, () => linea.Text = "");

    /// <summary>Menú «Ayuda» con «Acerca de».</summary>
    public static MenuItem MenuAyuda(Window padre)
    {
        var ayuda = new MenuItem { Header = "Ayuda" };
        var acerca = new MenuItem { Header = "Acerca de" };
        acerca.Click += (_, _) => Dialogo.Informacion(padre, TituloVentana, "Acerca de");
        ayuda.Items.Add(acerca);
        return ayuda;
    }

    /// <summary>Punto de color de la línea de estado del servidor.</summary>
    public static Ellipse Punto(Brush color) => new() { Width = 10, Height = 10, Fill = color };
}

/// <summary>
/// Diálogos modales con los textos de los JOptionPane de TI.2.1: «Aceptar» en los mensajes y
/// «Sí / No» en las confirmaciones (con «No» como opción por defecto).
/// </summary>
public static class Dialogo
{
    private enum Tipo { Error, Pregunta, Informacion }

    public static void Error(Window? padre, string mensaje) => Mostrar(padre, mensaje, Estilos.TituloVentana, Tipo.Error);

    public static void Informacion(Window? padre, string mensaje, string titulo) => Mostrar(padre, mensaje, titulo, Tipo.Informacion);

    /// <summary>Confirmación «Sí / No» antes de guardar, importar o eliminar.</summary>
    public static bool Confirmar(DependencyObject origen, string pregunta)
        => Mostrar(Window.GetWindow(origen), pregunta, Estilos.TituloVentana, Tipo.Pregunta);

    private static bool Mostrar(Window? padre, string mensaje, string titulo, Tipo tipo)
    {
        var ventana = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = padre != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ShowInTaskbar = padre == null,
            FontFamily = new FontFamily("Arial"),
            FontSize = 15,
            Background = Brushes.White,
        };
        if (padre != null && padre.IsVisible)
            ventana.Owner = padre;

        Color color = tipo switch
        {
            Tipo.Error => Color.FromRgb(0xB9, 0x1C, 0x1C),
            Tipo.Pregunta => Color.FromRgb(0x1F, 0x4E, 0x79),
            _ => Color.FromRgb(0x1F, 0x4E, 0x79),
        };
        var icono = new Grid { Width = 36, Height = 36, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top };
        icono.Children.Add(new Ellipse { Fill = new SolidColorBrush(color) });
        icono.Children.Add(new TextBlock
        {
            Text = tipo == Tipo.Pregunta ? "?" : tipo == Tipo.Error ? "!" : "i",
            Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        });

        var texto = new TextBlock { Text = mensaje, TextWrapping = TextWrapping.Wrap, MaxWidth = 460, VerticalAlignment = VerticalAlignment.Center };
        var fila = new DockPanel { Margin = new Thickness(20, 20, 20, 12) };
        DockPanel.SetDock(icono, Dock.Left);
        fila.Children.Add(icono);
        fila.Children.Add(texto);

        var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(20, 0, 20, 16) };
        bool resultado = false;
        Button Crear(string etiqueta, bool valor, bool primario)
        {
            var boton = new Button
            {
                Content = etiqueta, MinWidth = 90, Margin = new Thickness(8, 0, 0, 0),
                Style = (Style)Application.Current.Resources[primario ? "BotonPrimario" : "BotonSecundario"],
            };
            boton.Click += (_, _) => { resultado = valor; ventana.Close(); };
            botones.Children.Add(boton);
            return boton;
        }
        if (tipo == Tipo.Pregunta)
        {
            Crear("Sí", true, true);
            Button no = Crear("No", false, false);
            no.IsDefault = true;
            no.IsCancel = true;
            ventana.Loaded += (_, _) => no.Focus();
        }
        else
        {
            Button aceptar = Crear("Aceptar", true, true);
            aceptar.IsDefault = true;
            aceptar.IsCancel = true;
            ventana.Loaded += (_, _) => aceptar.Focus();
        }

        var raiz = new StackPanel();
        raiz.Children.Add(fila);
        raiz.Children.Add(botones);
        ventana.Content = raiz;
        ventana.ShowDialog();
        return resultado;
    }
}

/// <summary>Fila de la tabla de países (el «DataGrid» del ERS): textos ya formateados.</summary>
public sealed class FilaPais
{
    public modelo.Pais Pais { get; }

    public FilaPais(modelo.Pais pais)
    {
        Pais = pais;
    }

    public string Nombre => Pais.Nombre ?? "";
    public string Capital => Pais.Capital ?? "";
    public string Region => Pais.Region ?? "";
    public string Poblacion => modelo.Formatos.Poblacion(Pais.Poblacion);
    public string Moneda => Pais.Moneda ?? "";
    public string Guardado => Pais.Guardado ? "Sí" : "No";
}
