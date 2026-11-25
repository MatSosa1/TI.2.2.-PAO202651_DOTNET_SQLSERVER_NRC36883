using System.Windows;
using System.Windows.Controls;

namespace ec.edu.monster.vista;

/// <summary>Pantalla de inicio: saludo y una tarjeta por opción del menú, como el menú de EurekaBank.</summary>
public partial class InicioPanel : UserControl
{
    public InicioPanel()
    {
        InitializeComponent();
    }

    public void Configurar(string nombre, Action<string> alSeleccionar)
    {
        Saludo.Text = "Bienvenido, " + nombre;
        Tarjetas.Children.Clear();
        Tarjetas.Children.Add(Tarjeta(PrincipalWindow.PaisesNombre,
            "Liste, registre, actualice y elimine los países guardados", alSeleccionar, new Thickness(0, 0, 8, 8)));
        Tarjetas.Children.Add(Tarjeta(PrincipalWindow.ConsultaNombre,
            "Consulte un país en REST Countries y guárdelo en la base local", alSeleccionar, new Thickness(8, 0, 0, 8)));
        Tarjetas.Children.Add(Tarjeta(PrincipalWindow.BusquedaNombre,
            "Busque por nombre, capital o región", alSeleccionar, new Thickness(0, 8, 8, 0)));
        Tarjetas.Children.Add(Tarjeta(PrincipalWindow.InformesNombre,
            "Países por región, país más poblado y promedio poblacional", alSeleccionar, new Thickness(8, 8, 0, 0)));
    }

    private Button Tarjeta(string titulo, string descripcion, Action<string> alSeleccionar, Thickness margen)
    {
        var texto = new StackPanel { Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        texto.Children.Add(new TextBlock { Text = titulo, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 18) });
        texto.Children.Add(new TextBlock { Text = descripcion, TextWrapping = TextWrapping.Wrap });
        var boton = new Button { Content = texto, Style = (Style)FindResource("BotonTarjeta"), Margin = margen };
        boton.Click += (_, _) => alSeleccionar(titulo);
        return boton;
    }
}
