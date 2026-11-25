using Microsoft.Maui.Controls.Shapes;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;
using MOV_APIPAISES_DOTNET_REST.Utils;

namespace MOV_APIPAISES_DOTNET_REST.Vista.Componentes;

/// <summary>Ícono blanco en un círculo del color primario suave (CirculoIcono del cliente Android).</summary>
public class CirculoIcono : Border
{
    public CirculoIcono(string datos, double lado = 40, double tamano = 22)
    {
        WidthRequest = HeightRequest = lado;
        StrokeThickness = 0;
        StrokeShape = new Ellipse();
        BackgroundColor = Colores.PrimarioSuave;
        VerticalOptions = LayoutOptions.Center;
        Content = new Icono { Datos = datos, Color = Colores.Primario, Tamano = tamano };
    }
}

/// <summary>Fila «etiqueta · valor» (item_dato del cliente Android).</summary>
public class FilaDato : Grid
{
    public FilaDato(string etiqueta, string valor)
    {
        Padding = new Thickness(0, 10);
        ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        this.Add(new Label { Text = etiqueta, FontSize = 14, TextColor = Colores.TextoSecundario, VerticalOptions = LayoutOptions.Center }, 0);
        this.Add(new Label { Text = valor, FontSize = 15, TextColor = Colores.TextoOscuro, FontFamily = "OpenSansSemibold",
            HorizontalTextAlignment = TextAlignment.End, VerticalOptions = LayoutOptions.Center }, 1);
    }
}

/// <summary>Opción del inicio: ícono en círculo, título, detalle y flecha (item_accion).</summary>
public class FilaAccion : Grid
{
    public FilaAccion(string icono, string titulo, string detalle, Action alTocar)
    {
        Padding = new Thickness(16, 12, 12, 12);
        ColumnSpacing = 16;
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        this.Add(new CirculoIcono(icono), 0);
        this.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2,
            Children =
            {
                new Label { Text = titulo, FontSize = 16, TextColor = Colores.TextoOscuro },
                new Label { Text = detalle, FontSize = 13, TextColor = Colores.TextoSecundario },
            },
        }, 1);
        this.Add(new Icono { Datos = Iconos.Chevron, Color = Colores.TextoSecundario, Tamano = 22 }, 2);
        GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(alTocar) });
        SemanticProperties.SetDescription(this, titulo);
    }
}

/// <summary>
/// Fila de un país (item_pais): globo, nombre, capital · región, moneda y población. En la consulta
/// a la API muestra además «Guardado / No guardado» y el botón «Guardar».
/// </summary>
public class FilaPais : Grid
{
    public FilaPais(Pais pais, bool modoApi, Action<Pais> alElegir)
    {
        Padding = new Thickness(16, 12);
        ColumnSpacing = 14;
        BackgroundColor = Colors.White;
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        this.Add(new CirculoIcono(Iconos.Globo, 40, 22), 0);
        this.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2,
            Children =
            {
                new Label { Text = pais.Nombre, FontSize = 15, TextColor = Colores.TextoOscuro, FontFamily = "OpenSansSemibold" },
                new Label { Text = Formato.Texto(pais.Capital) + " · " + Formato.Texto(pais.Region), FontSize = 13, TextColor = Colores.TextoSecundario },
                new Label { Text = Formato.Texto(pais.Moneda), FontSize = 12, TextColor = Colores.TextoSecundario },
            },
        }, 1);

        var derecha = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End, Spacing = 4 };
        derecha.Add(new Label { Text = Formato.Poblacion(pais.Poblacion), FontSize = 14, TextColor = Colores.TextoOscuro,
            HorizontalOptions = LayoutOptions.End });
        if (modoApi)
        {
            derecha.Add(Pastilla(pais.Guardado ? Textos.Guardado : Textos.NoGuardado,
                pais.Guardado ? Colores.Ingreso : Colores.TextoSecundario,
                pais.Guardado ? Colores.IngresoFondo : Colores.FondoApp));
            var guardar = new Button
            {
                Text = Textos.Guardar, FontSize = 13, HeightRequest = 36, Padding = new Thickness(14, 0), CornerRadius = 18,
                BackgroundColor = Colores.PrimarioSuave, TextColor = Colores.Primario, FontFamily = "OpenSansSemibold",
                HorizontalOptions = LayoutOptions.End,
            };
            guardar.Clicked += (_, _) => alElegir(pais);
            derecha.Add(guardar);
        }
        else
        {
            GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => alElegir(pais)) });
        }
        this.Add(derecha, 2);
    }

    public static Border Pastilla(string texto, Color color, Color fondo)
    {
        return new Border
        {
            BackgroundColor = fondo,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 3),
            HorizontalOptions = LayoutOptions.End,
            Content = new Label { Text = texto, TextColor = color, FontSize = 11, FontFamily = "OpenSansSemibold" },
        };
    }
}

/// <summary>Chips de región (ChipGroup de selección única): «Todas» y las seis del catálogo.</summary>
public class ChipsRegion : ScrollView
{
    private readonly List<(string Region, Border Chip, Label Texto)> chips = [];

    public event EventHandler? Cambiado;

    public string Region { get; private set; } = "";

    public ChipsRegion(IEnumerable<string> regiones)
    {
        Orientation = ScrollOrientation.Horizontal;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never;
        var fila = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(12, 4) };
        foreach (string region in new[] { "" }.Concat(regiones))
        {
            var texto = new Label { Text = region.Length == 0 ? Textos.Todas : region, FontSize = 14, VerticalOptions = LayoutOptions.Center };
            var chip = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 1,
                Padding = new Thickness(14, 6),
                Content = texto,
            };
            string elegida = region;
            chip.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Elegir(elegida, true)) });
            chips.Add((region, chip, texto));
            fila.Add(chip);
        }
        Content = fila;
        Elegir("", false);
    }

    public void Elegir(string region, bool avisar)
    {
        Region = region;
        foreach (var (valor, chip, texto) in chips)
        {
            bool activo = valor == region;
            chip.BackgroundColor = activo ? Colores.Primario : Colors.White;
            chip.Stroke = activo ? Colores.Primario : Colores.BordeCampo;
            texto.TextColor = activo ? Colors.White : Colores.TextoOscuro;
        }
        if (avisar)
            Cambiado?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Hoja inferior (BottomSheetDialog del cliente Android): fondo oscurecido y una tarjeta con las
/// esquinas superiores redondeadas que sube desde abajo. Tocar fuera la cierra.
/// </summary>
public class Hoja : Grid
{
    private readonly Border tarjeta;
    private readonly VerticalStackLayout cuerpo = new() { Spacing = 0 };

    public event EventHandler? Cerrada;

    public Hoja()
    {
        IsVisible = false;
        // Zona libre sobre la hoja: tocarla la cierra. No se oscurece: en Android una capa translúcida a
        // pantalla completa hace que MAUI deje de dibujar el contenido de abajo (se ve negro).
        var fondo = new ContentView();
        fondo.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _ = CerrarAsync()) });
        Add(fondo);
        var asa = new Border
        {
            WidthRequest = 36, HeightRequest = 4, BackgroundColor = Colores.BordeCampo, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 2 }, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 16),
        };
        tarjeta = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24, 24, 0, 0) },
            Padding = new Thickness(24, 12, 24, 24),
            VerticalOptions = LayoutOptions.End,
            // Sin ScrollView: en Android se estira a todo el alto; la hoja mide lo que su contenido
            Content = new VerticalStackLayout { Children = { asa, cuerpo } },
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.3f, Radius = 16, Offset = new Point(0, -2) },
        };
        Add(tarjeta);
    }

    public bool Abierta => IsVisible;

    public async Task AbrirAsync(View contenido)
    {
        cuerpo.Clear();
        cuerpo.Add(contenido);
        IsVisible = true;
        tarjeta.TranslationY = 600;
        await tarjeta.TranslateToAsync(0, 0, 200, Easing.CubicOut);
    }

    public async Task CerrarAsync()
    {
        if (!IsVisible)
            return;
        await tarjeta.TranslateToAsync(0, 600, 160, Easing.CubicIn);
        IsVisible = false;
        cuerpo.Clear();
        Cerrada?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Mensaje breve sobre las pestañas (Snackbar del cliente Android).</summary>
public class Aviso : Border
{
    private readonly Label texto = new() { TextColor = Colors.White, FontSize = 14 };
    private int version;

    public Aviso()
    {
        IsVisible = false;
        BackgroundColor = Color.FromArgb("#323232");
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 6 };
        Padding = new Thickness(16, 14);
        Margin = new Thickness(12, 0, 12, 12);
        VerticalOptions = LayoutOptions.End;
        Content = texto;
    }

    public async Task MostrarAsync(string mensaje)
    {
        int actual = ++version;
        texto.Text = mensaje;
        Opacity = 0;
        IsVisible = true;
        SemanticScreenReader.Announce(mensaje);
        await this.FadeToAsync(1, 150);
        await Task.Delay(3500);
        if (actual != version)
            return;
        await this.FadeToAsync(0, 200);
        IsVisible = false;
    }
}

/// <summary>
/// Gráfico de barras con la cantidad de países por región (ECUD7, paso 8), dibujado en un
/// GraphicsView para no depender de librerías externas. Usa los colores de la paleta.
/// </summary>
public class GraficoBarras : GraphicsView, IDrawable
{
    private List<ConteoRegion> datos = [];

    public GraficoBarras()
    {
        Drawable = this;
        HeightRequest = 180;
        SemanticProperties.SetDescription(this, Textos.InformeGrafico);
    }

    public void SetDatos(List<ConteoRegion>? nuevos)
    {
        datos = [.. nuevos ?? []];
        Invalidate();
    }

    public void Draw(ICanvas lienzo, RectF area)
    {
        if (datos.Count == 0)
            return;
        float ancho = area.Width;
        float baseY = area.Height - 24;
        float altoMaximo = baseY - 24;
        int maximo = Math.Max(1, datos.Max(d => d.Cantidad));
        float columna = ancho / datos.Count;
        float anchoBarra = columna * 0.55f;

        lienzo.StrokeColor = Colores.BordeTarjeta;
        lienzo.StrokeSize = 1;
        lienzo.DrawLine(0, baseY, ancho, baseY);
        for (int i = 0; i < datos.Count; i++)
        {
            ConteoRegion dato = datos[i];
            float centro = i * columna + columna / 2;
            float alto = dato.Cantidad / (float)maximo * altoMaximo;
            lienzo.FillColor = Colores.Primario;
            if (alto > 0)
                lienzo.FillRoundedRectangle(centro - anchoBarra / 2, baseY - alto, anchoBarra, alto, 4);

            lienzo.FontColor = Colores.TextoOscuro;
            lienzo.FontSize = 13;
            lienzo.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            lienzo.DrawString(dato.Cantidad.ToString(), centro - columna / 2, baseY - alto - 22, columna, 18,
                HorizontalAlignment.Center, VerticalAlignment.Bottom);

            lienzo.FontColor = Colores.TextoSecundario;
            lienzo.FontSize = 11;
            lienzo.Font = Microsoft.Maui.Graphics.Font.Default;
            lienzo.DrawString(dato.Region ?? "", centro - columna / 2 + 2, baseY + 4, columna - 4, 18,
                HorizontalAlignment.Center, VerticalAlignment.Top, TextFlow.ClipBounds);
        }
    }
}
