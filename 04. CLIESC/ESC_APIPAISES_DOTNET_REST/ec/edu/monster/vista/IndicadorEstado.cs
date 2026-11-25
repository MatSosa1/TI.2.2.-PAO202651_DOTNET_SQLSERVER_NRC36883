using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ec.edu.monster.vista;

/// <summary>
/// Línea «punto + texto» que indica si el servidor responde. Mientras está en pantalla ejecuta
/// la revisión registrada de inmediato y luego cada 5 segundos, fuera del hilo de la interfaz,
/// para que un servidor lento o apagado nunca congele la ventana.
/// </summary>
public class IndicadorEstado : StackPanel, Vistas.EstadoVista
{
    private const string DisponibleTexto = "Servicio disponible";
    private const string NoDisponibleTexto = "Servicio no disponible";

    private readonly string prefijo;
    private readonly TextBlock etiqueta = new() { FontSize = 13, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
    private readonly ContentControl punto = new() { Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly DispatcherTimer temporizador;
    private int revisando;
    private volatile Action accionRevisar = () => { };

    /// <summary><paramref name="prefijo"/> va antes del estado, p. ej. «Sesión activa · ».</summary>
    public IndicadorEstado(string prefijo) : this(prefijo, TimeSpan.FromSeconds(5))
    {
    }

    public IndicadorEstado(string prefijo, TimeSpan intervalo)
    {
        this.prefijo = prefijo;
        Orientation = Orientation.Horizontal;
        Children.Add(punto);
        Children.Add(etiqueta);
        Pintar(true);
        temporizador = new DispatcherTimer { Interval = intervalo };
        temporizador.Tick += (_, _) => Revisar();
        Loaded += (_, _) => { temporizador.Start(); Revisar(); };
        Unloaded += (_, _) => temporizador.Stop();
    }

    public void Detener() => temporizador.Stop();

    public void MostrarEstado(bool disponible) => Estilos.EnUi(this, () => Pintar(disponible));

    public void AlRevisarEstado(Action accion) => accionRevisar = accion;

    // Se salta un ciclo mientras la revisión anterior sigue esperando al servidor
    private void Revisar()
    {
        if (Interlocked.CompareExchange(ref revisando, 1, 0) != 0)
            return;
        Task.Run(accionRevisar).ContinueWith(_ => Interlocked.Exchange(ref revisando, 0));
    }

    private void Pintar(bool disponible)
    {
        var color = Estilos.Pincel(disponible ? "Exito" : "Error");
        etiqueta.Text = prefijo + (disponible ? DisponibleTexto : NoDisponibleTexto);
        etiqueta.Foreground = color;
        punto.Content = Estilos.Punto(color);
    }
}
