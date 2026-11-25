using MOV_APIPAISES_DOTNET_REST.Controlador;
using MOV_APIPAISES_DOTNET_REST.Modelos.Data;
using MOV_APIPAISES_DOTNET_REST.Vista.Componentes;

namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>
/// ECUD3 Consultar País en la API: resultados con «Guardado» y «Guardar» (ECUD2, con
/// confirmación) e «Importar todos».
/// </summary>
public class ApiPage : PestanaPage
{
    private readonly CampoTexto txtNombre = new() { Titulo = Textos.NombrePais, TipoRetorno = ReturnType.Search, LargoMaximo = 100 };
    private readonly AvisoError lblError = new() { Margin = new Thickness(16, 12, 16, 0) };
    private readonly Label lblAyuda;
    private readonly BotonCarga btnConsultar = new() { Texto = Textos.Consultar };
    private readonly Button btnImportarTodos;
    private readonly VerticalStackLayout lista = new() { Spacing = 1, BackgroundColor = Colores.BordeTarjeta };
    private List<Pais> paises = [];
    private string consulta = "";

    public ApiPage(LoginControlador login, PaisesControlador controlador) : base(login, controlador, Textos.TituloApi)
    {
        lblAyuda = Secundario(Textos.ApiAyuda, new Thickness(20, 12, 20, 0));
        btnConsultar.Pulsado += async (_, _) => await ConsultarAsync();
        txtNombre.Completado += async (_, _) => await ConsultarAsync();

        btnImportarTodos = new Button
        {
            Text = Textos.ImportarTodos,
            BackgroundColor = Colors.White,
            TextColor = Colores.Primario,
            BorderColor = Colores.Primario,
            BorderWidth = 1,
            CornerRadius = 20,
            FontFamily = "OpenSansSemibold",
            HeightRequest = 44,
            Padding = new Thickness(16, 0),
        };
        btnImportarTodos.Clicked += async (_, _) => await ConfirmarImportarTodosAsync();

        var busqueda = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 12),
            ColumnSpacing = 8,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(130))],
        };
        busqueda.Add(txtNombre, 0);
        busqueda.Add(btnConsultar, 1);
        btnConsultar.VerticalOptions = LayoutOptions.Center;

        var pie = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = 12,
            ColumnSpacing = 12,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
        };
        pie.Add(Secundario(Textos.ImportarTodosAyuda, new Thickness(4, 0, 0, 0), 13).Also(l => l.VerticalOptions = LayoutOptions.Center), 0);
        pie.Add(btnImportarTodos, 1);

        var pagina = new Grid
        {
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)],
        };
        pagina.Add(busqueda, 0, 0);
        pagina.Add(lblError, 0, 1);
        pagina.Add(lblAyuda, 0, 2);
        pagina.Add(new ScrollView { Content = new VerticalStackLayout { Padding = new Thickness(0, 4, 0, 0), Children = { lista } } }, 0, 3);
        pagina.Add(pie, 0, 4);
        Cuerpo = pagina;
    }

    protected override void AlMostrar()
    {
        // La consulta se hace a pedido del usuario
    }

    private async Task ConsultarAsync()
    {
        if (btnConsultar.Cargando)
            return;
        consulta = txtNombre.Texto;
        txtNombre.QuitarFoco();
        Cargando(true);
        await EjecutarAsync(() => Controlador.ConsultarApiAsync(consulta), resultado =>
        {
            Cargando(false);
            if (resultado.Exito)
            {
                MostrarError(lblError, null);
                lblAyuda.IsVisible = false;
                paises = resultado.Datos ?? [];
                Pintar();
            }
            else
            {
                // ECUD3 alterno 2: se conserva el texto para corregirlo
                MostrarError(lblError, resultado.Mensaje);
            }
        });
    }

    private void Pintar()
    {
        lista.Clear();
        foreach (Pais pais in paises)
            lista.Add(new FilaPais(pais, true, p => _ = ConfirmarGuardarAsync(p)));
    }

    /// <summary>ECUD2 pasos 2 y 3.</summary>
    private async Task ConfirmarGuardarAsync(Pais pais)
    {
        if (!await DisplayAlertAsync(Textos.Guardar, string.Format(Textos.GuardarPregunta, pais.Nombre), Textos.Guardar, Textos.Cancelar))
            return;
        Cargando(true);
        await EjecutarAsync(() => Controlador.ImportarAsync(consulta, pais.Nombre ?? ""), resultado =>
        {
            Cargando(false);
            if (resultado.Exito)
            {
                MostrarError(lblError, null);
                pais.Guardado = true;
                Pintar();
                Avisar(resultado.Mensaje);
            }
            else
            {
                MostrarError(lblError, resultado.Mensaje);
            }
        });
    }

    private async Task ConfirmarImportarTodosAsync()
    {
        if (!await DisplayAlertAsync(Textos.ImportarTodos, Textos.ImportarTodosPregunta, Textos.ImportarTodos, Textos.Cancelar))
            return;
        Cargando(true);
        await EjecutarAsync(() => Controlador.ImportarTodosAsync(), resultado =>
        {
            Cargando(false);
            if (resultado.Exito)
            {
                MostrarError(lblError, null);
                Avisar(resultado.Mensaje);
            }
            else
            {
                MostrarError(lblError, resultado.Mensaje);
            }
        });
    }

    private void Cargando(bool activo)
    {
        btnConsultar.Cargando = activo;
        btnImportarTodos.IsEnabled = !activo;
    }
}
