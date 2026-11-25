using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// Gráfico de barras con la cantidad de países por región (ECUD7, paso 8), dibujado con
/// DrawingContext para no depender de librerías externas. Usa los colores de Estilos.
/// </summary>
public class GraficoBarras : FrameworkElement
{
    private const double Margen = 16;
    private const double AltoEtiqueta = 22;

    private List<ConteoRegion> datos = [];

    public GraficoBarras()
    {
        MinWidth = 360;
        MinHeight = 150;
    }

    public void SetDatos(List<ConteoRegion>? nuevos)
    {
        datos = [.. nuevos ?? []];
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext g)
    {
        base.OnRender(g);
        if (datos.Count == 0)
            return;

        double ancho = ActualWidth - 2 * Margen;
        double baseY = ActualHeight - AltoEtiqueta - 4;
        double altoMaximo = baseY - Margen - 18;
        int maximo = Math.Max(1, datos.Max(d => d.Cantidad));
        double columna = ancho / datos.Count;
        double barra = Math.Max(8, columna * 0.6);
        Brush texto = Estilos.Pincel("Texto");
        Brush primario = Estilos.Pincel("Primario");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var fuente = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        // Línea base
        g.DrawLine(new Pen(Estilos.Pincel("Borde"), 1), new Point(Margen, baseY), new Point(Margen + ancho, baseY));

        for (int i = 0; i < datos.Count; i++)
        {
            ConteoRegion dato = datos[i];
            double alto = Math.Round((double)dato.Cantidad / maximo * altoMaximo);
            double x = Margen + i * columna + (columna - barra) / 2;
            if (alto > 0)
                g.DrawRoundedRectangle(primario, null, new Rect(x, baseY - alto, barra, alto), 2, 2);

            var cantidad = new FormattedText(dato.Cantidad.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, fuente, 12, texto, dpi);
            g.DrawText(cantidad, new Point(x + (barra - cantidad.Width) / 2, baseY - alto - 4 - cantidad.Height));

            var region = new FormattedText(dato.Region ?? "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                fuente, 12, texto, dpi)
            {
                MaxTextWidth = Math.Max(1, columna - 4),
                MaxLineCount = 1,
                Trimming = TextTrimming.CharacterEllipsis,
            };
            double anchoRegion = Math.Min(region.WidthIncludingTrailingWhitespace, columna - 4);
            g.DrawText(region, new Point(Margen + i * columna + (columna - anchoRegion) / 2, baseY + 4));
        }
    }
}
