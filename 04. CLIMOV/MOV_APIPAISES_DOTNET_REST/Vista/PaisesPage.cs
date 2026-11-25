using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;
using MOV_APIPAISES_DOTNET_REST.Utils;
using MOV_APIPAISES_DOTNET_REST.Vista.Componentes;

namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>
/// ECUD4 Gestionar Países con ECUD5 Buscar y ECUD6 Filtrar: búsqueda por nombre y capital, lista,
/// filtros por región en la parte inferior (filtran al elegir) y «Nuevo». Al tocar un país se abre
/// su detalle con Actualizar y Eliminar.
/// </summary>
public class PaisesPage : PestanaPage
{
    private readonly CampoTexto txtNombre = new() { Titulo = Textos.BuscarNombre, TipoRetorno = ReturnType.Search, LargoMaximo = 100 };
    private readonly CampoTexto txtCapital = new() { Titulo = Textos.BuscarCapital, TipoRetorno = ReturnType.Search, LargoMaximo = 100 };
    private readonly Label lblTotal = new() { FontSize = 14, Margin = new Thickness(20, 12, 20, 4) };
    private readonly AvisoError lblError = new() { Margin = new Thickness(16, 12, 16, 0) };
    private readonly VerticalStackLayout lista = new() { Spacing = 1, BackgroundColor = Colores.BordeTarjeta };
    private readonly RefreshView refrescar = new();
    private readonly ChipsRegion chips = new(ValidadorDatos.Regiones);

    // Hoja abierta (detalle o nuevo) y sus controles
    private AvisoError? errorHoja;
    private ActivityIndicator? cargandoHoja;
    private readonly List<Button> botonesHoja = [];

    public PaisesPage(LoginControlador login, PaisesControlador controlador) : base(login, controlador, Textos.PestanaPaises)
    {
        lblTotal.TextColor = Colores.TextoSecundario;
        refrescar.RefreshColor = Colores.Primario;
        refrescar.Command = new Command(async () => await BuscarAsync());
        txtNombre.Completado += async (_, _) => await BuscarAsync();
        txtCapital.Completado += async (_, _) => await BuscarAsync();
        chips.Cambiado += async (_, _) => await BuscarAsync();

        var busqueda = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 12),
            ColumnSpacing = 8,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
        };
        busqueda.Add(txtNombre, 0);
        busqueda.Add(txtCapital, 1);

        refrescar.Content = new ScrollView { Content = new VerticalStackLayout { Padding = new Thickness(0, 0, 0, 88), Children = { lista } } };

        var filtros = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(0, 8, 0, 4),
            Children =
            {
                new Label { Text = Textos.FiltrarRegion, Style = Estilo("Etiqueta"), Margin = new Thickness(16, 0, 16, 0) },
                chips,
            },
        };

        var pagina = new Grid
        {
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)],
        };
        pagina.Add(busqueda, 0, 0);
        pagina.Add(lblError, 0, 1);
        pagina.Add(lblTotal, 0, 2);
        pagina.Add(refrescar, 0, 3);
        pagina.Add(filtros, 0, 4);

        // Botón flotante «Nuevo» (ExtendedFloatingActionButton)
        var nuevo = new Button
        {
            Text = Textos.Nuevo,
            ImageSource = new FontImageSource { Glyph = "+", Color = Colors.White, Size = 20 },
            BackgroundColor = Colores.Primario,
            TextColor = Colors.White,
            FontFamily = "OpenSansSemibold",
            CornerRadius = 16,
            HeightRequest = 56,
            Padding = new Thickness(20, 0),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 16, 104),
            Shadow = new Shadow { Opacity = 0.25f, Radius = 6, Offset = new Point(0, 3) },
        };
        nuevo.Clicked += (_, _) => AbrirNuevo();
        pagina.Add(nuevo, 0, 3);
        Grid.SetRowSpan(nuevo, 2);

        Cuerpo = pagina;
    }

    protected override void AlMostrar() => _ = BuscarAsync();

    /// <summary>Listar, Buscar o Filtrar según los criterios escritos y la región elegida.</summary>
    private async Task BuscarAsync()
    {
        string nombre = txtNombre.Texto.Trim();
        string capital = txtCapital.Texto.Trim();
        string region = chips.Region;
        bool conCriterios = nombre.Length > 0 || capital.Length > 0;
        refrescar.IsRefreshing = true;
        await EjecutarAsync(() => Controlador.BuscarAsync(nombre, capital, region), resultado =>
        {
            refrescar.IsRefreshing = false;
            if (!resultado.Exito)
            {
                MostrarError(lblError, resultado.Mensaje);
                return;
            }
            MostrarError(lblError, null);
            List<Pais> paises = resultado.Datos ?? [];
            lista.Clear();
            foreach (Pais pais in paises)
                lista.Add(new FilaPais(pais, false, AbrirDetalle));
            lblTotal.Text = paises.Count > 0
                ? Formato.Total(paises.Count)
                : conCriterios ? Textos.SinResultados : region.Length == 0 ? Textos.SinPaises : Textos.SinPaisesRegion;
        });
    }

    // ------------------------------------------------------------------ Detalle (HojaPais)

    private void AbrirDetalle(Pais pais)
    {
        var contenido = new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = pais.Nombre, FontSize = 22, TextColor = Colores.TextoOscuro, FontFamily = "OpenSansSemibold", Margin = new Thickness(0, 0, 0, 8) },
                new FilaDato(Textos.DatoCapital, Formato.Texto(pais.Capital)),
                new FilaDato(Textos.DatoRegion, Formato.Texto(pais.Region)),
                new FilaDato(Textos.DatoPoblacion, Formato.Poblacion(pais.Poblacion)),
                new FilaDato(Textos.DatoMoneda, Formato.Texto(pais.Moneda)),
            },
        };
        PrepararHoja(contenido);
        Button actualizar = BotonHoja(Textos.Actualizar, true, async () =>
        {
            CargandoHoja(true);
            await OperarAsync(() => Controlador.ActualizarAsync(pais.Nombre ?? ""));
        });
        Button eliminar = BotonHoja(Textos.Eliminar, false, async () => await ConfirmarEliminarAsync(pais));
        contenido.Add(FilaBotones(eliminar, actualizar));
        _ = HojaInferior.AbrirAsync(contenido);
    }

    /// <summary>ECUD4 Eliminar pasos 2 y 3: confirmación antes de enviar.</summary>
    private async Task ConfirmarEliminarAsync(Pais pais)
    {
        if (!await DisplayAlertAsync(Textos.Eliminar, string.Format(Textos.EliminarPregunta, pais.Nombre), Textos.Eliminar, Textos.Cancelar))
            return;
        CargandoHoja(true);
        await OperarAsync(() => Controlador.EliminarAsync(pais.Nombre ?? ""));
    }

    private async Task OperarAsync<T>(Func<Task<Resultado<T>>> operacion)
    {
        await EjecutarAsync(operacion, async resultado =>
        {
            if (resultado.Exito)
            {
                await HojaInferior.CerrarAsync();
                Avisar(resultado.Mensaje);
                await BuscarAsync();
            }
            else
            {
                CargandoHoja(false);
                errorHoja!.Texto = resultado.Mensaje;
            }
        });
    }

    // ------------------------------------------------------------------ Nuevo (HojaNuevoPais)

    /// <summary>ECUD4 Registrar: hoja con la consulta a la API y «Registrar»; «Cancelar» cierra sin enviar nada.</summary>
    private void AbrirNuevo()
    {
        var txtConsulta = new CampoTexto { Titulo = Textos.NombrePais, TipoRetorno = ReturnType.Search, LargoMaximo = 100 };
        var lblElija = new Label { Text = Textos.ElijaPais, FontSize = 14, TextColor = Colores.TextoSecundario, IsVisible = false, Margin = new Thickness(0, 16, 0, 4) };
        var resultados = new VerticalStackLayout();
        string consulta = "";
        string? elegido = null;

        var contenido = new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = Textos.NuevoTitulo, FontSize = 22, TextColor = Colores.TextoOscuro, FontFamily = "OpenSansSemibold", Margin = new Thickness(0, 0, 0, 16) },
                txtConsulta,
            },
        };
        PrepararHoja(contenido);
        Button registrar = BotonHoja(Textos.Registrar, true, async () =>
        {
            if (elegido == null)
                return;
            CargandoHoja(true);
            await EjecutarAsync(() => Controlador.RegistrarAsync(consulta, elegido), async resultado =>
            {
                CargandoHoja(false);
                if (resultado.Exito)
                {
                    // ECUD4 Registrar paso 9: mensaje, se cierra el formulario y se recarga
                    await HojaInferior.CerrarAsync();
                    Avisar(resultado.Mensaje);
                    await BuscarAsync();
                }
                else
                {
                    errorHoja!.Texto = resultado.Mensaje;
                }
            });
        });
        registrar.IsEnabled = false;

        async Task ConsultarAsync()
        {
            consulta = txtConsulta.Texto;
            CargandoHoja(true);
            await EjecutarAsync(() => Controlador.ConsultarApiAsync(consulta), resultado =>
            {
                CargandoHoja(false);
                if (!resultado.Exito)
                {
                    errorHoja!.Texto = resultado.Mensaje;
                    return;
                }
                // ECUD4 Registrar paso 4: resultados para elegir; se habilita «Registrar»
                List<Pais> paises = resultado.Datos ?? [];
                resultados.Clear();
                elegido = null;
                string grupo = Guid.NewGuid().ToString();
                for (int i = 0; i < paises.Count; i++)
                {
                    Pais pais = paises[i];
                    var opcion = new RadioButton
                    {
                        GroupName = grupo,
                        IsChecked = i == 0,
                        Content = pais.Nombre + "\n" + Formato.Texto(pais.Capital) + " · " + Formato.Texto(pais.Region) + " · "
                                  + Formato.Poblacion(pais.Poblacion) + (pais.Guardado ? " · " + Textos.Guardado : ""),
                        TextColor = Colores.TextoOscuro,
                        Margin = new Thickness(0, 4),
                    };
                    opcion.CheckedChanged += (_, e) =>
                    {
                        if (e.Value)
                            elegido = pais.Nombre;
                    };
                    if (i == 0)
                        elegido = pais.Nombre;
                    resultados.Add(opcion);
                }
                lblElija.IsVisible = paises.Count > 0;
                registrar.IsEnabled = paises.Count > 0;
            });
        }

        Button consultar = BotonHoja(Textos.Consultar, false, ConsultarAsync);
        txtConsulta.Completado += async (_, _) => await ConsultarAsync();
        contenido.Add(new HorizontalStackLayout { HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 12, 0, 0), Children = { consultar } });
        contenido.Add(lblElija);
        // Muchos resultados: se desplazan dentro de la hoja
        contenido.Add(new ScrollView { Content = resultados, MaximumHeightRequest = 300 });
        Button cancelar = BotonHoja(Textos.Cancelar, false, () => HojaInferior.CerrarAsync());
        contenido.Add(FilaBotones(cancelar, registrar));
        _ = HojaInferior.AbrirAsync(contenido);
    }

    // ------------------------------------------------------------------ Apoyo de las hojas

    private void PrepararHoja(VerticalStackLayout contenido)
    {
        botonesHoja.Clear();
        errorHoja = new AvisoError { Margin = new Thickness(0, 12, 0, 0) };
        cargandoHoja = new ActivityIndicator { Color = Colores.Primario, IsRunning = false, IsVisible = false, HeightRequest = 32, Margin = new Thickness(0, 12, 0, 0) };
        contenido.Add(errorHoja);
        contenido.Add(cargandoHoja);
    }

    private void CargandoHoja(bool activo)
    {
        if (cargandoHoja != null)
        {
            cargandoHoja.IsRunning = activo;
            cargandoHoja.IsVisible = activo;
        }
        foreach (Button boton in botonesHoja)
            boton.IsEnabled = !activo;
        if (activo && errorHoja != null)
            errorHoja.Texto = null;
    }

    private Button BotonHoja(string texto, bool primario, Func<Task> accion)
    {
        var boton = new Button
        {
            Text = texto,
            FontFamily = "OpenSansSemibold",
            FontSize = 15,
            HeightRequest = 48,
            CornerRadius = 12,
            Padding = new Thickness(20, 0),
            BackgroundColor = primario ? Colores.Primario : Colors.White,
            TextColor = primario ? Colors.White : Colores.Primario,
            BorderColor = Colores.Primario,
            BorderWidth = primario ? 0 : 1,
        };
        boton.Clicked += async (_, _) => await accion();
        botonesHoja.Add(boton);
        return boton;
    }

    private static Grid FilaBotones(Button izquierda, Button derecha)
    {
        var fila = new Grid
        {
            ColumnSpacing = 12,
            Margin = new Thickness(0, 20, 0, 0),
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
        };
        fila.Add(izquierda, 0);
        fila.Add(derecha, 1);
        return fila;
    }

    private static Style? Estilo(string clave)
        => Application.Current?.Resources.TryGetValue(clave, out object? estilo) == true ? (Style)estilo : null;
}
