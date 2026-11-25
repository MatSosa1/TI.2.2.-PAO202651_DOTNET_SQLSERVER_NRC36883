using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// Tabla de países (el «DataGrid» del ERS): Nombre, Capital, Región, Población y Moneda, y en
/// la consulta a la API la columna «Guardado» (ECUD3, paso 7). La población lleva separador de
/// miles y va alineada a la derecha. Equivale a ModeloTablaPaises de TI.2.1.
/// </summary>
internal sealed class TablaPaises
{
    // Mismas proporciones que los anchos preferidos de TI.2.1: 170, 140, 100, 110, 220, 95
    private static readonly (string Titulo, string Campo, double Ancho, string Estilo)[] Columnas =
    [
        ("Nombre", nameof(FilaPais.Nombre), 170, "CeldaIzquierda"),
        ("Capital", nameof(FilaPais.Capital), 140, "CeldaIzquierda"),
        ("Región", nameof(FilaPais.Region), 100, "CeldaIzquierda"),
        ("Población", nameof(FilaPais.Poblacion), 110, "CeldaDerecha"),
        ("Moneda", nameof(FilaPais.Moneda), 220, "CeldaIzquierda"),
        ("Guardado", nameof(FilaPais.Guardado), 95, "CeldaCentro"),
    ];

    private readonly DataGrid tabla;
    private readonly ObservableCollection<FilaPais> filas = [];

    public TablaPaises(DataGrid tabla, bool conGuardado)
    {
        this.tabla = tabla;
        tabla.Style = (Style)Application.Current.Resources["Tabla"];
        int cantidad = conGuardado ? Columnas.Length : Columnas.Length - 1;
        foreach (var (titulo, campo, ancho, estilo) in Columnas.Take(cantidad))
        {
            tabla.Columns.Add(new DataGridTextColumn
            {
                Header = titulo,
                Binding = new Binding(campo),
                Width = new DataGridLength(ancho, DataGridLengthUnitType.Star),
                ElementStyle = (Style)Application.Current.Resources[estilo],
            });
        }
        tabla.ItemsSource = filas;
    }

    public int Cantidad => filas.Count;

    public void SetPaises(IEnumerable<Pais>? paises)
    {
        filas.Clear();
        foreach (Pais pais in paises ?? [])
            filas.Add(new FilaPais(pais));
    }

    /// <summary>País de la fila elegida, o null si no hay ninguna.</summary>
    public Pais? Seleccionado => (tabla.SelectedItem as FilaPais)?.Pais;

    public void SeleccionarPrimero()
    {
        if (filas.Count > 0)
            tabla.SelectedIndex = 0;
    }

    /// <summary>ECUD2 paso 9: el país recién guardado se marca en la tabla.</summary>
    public void MarcarGuardado(string nombre)
    {
        for (int i = 0; i < filas.Count; i++)
        {
            if (filas[i].Pais.Nombre == nombre)
            {
                bool elegido = tabla.SelectedIndex == i;
                filas[i] = new FilaPais(filas[i].Pais.ComoGuardado());
                if (elegido)
                    tabla.SelectedIndex = i;
            }
        }
    }
}
