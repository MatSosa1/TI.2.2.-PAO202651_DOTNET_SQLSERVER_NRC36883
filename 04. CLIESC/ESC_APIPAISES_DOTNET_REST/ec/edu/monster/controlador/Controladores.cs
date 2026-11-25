using ec.edu.monster.modelo;
using static ec.edu.monster.vista.Vistas;

namespace ec.edu.monster.controlador;

/// <summary>
/// Base de los controladores que trabajan con la sesión abierta. Si el servidor rechaza el
/// token (ECUD8), se borra la sesión y se avisa a la aplicación para volver al inicio de sesión.
/// </summary>
public abstract class ControladorConSesion
{
    protected readonly Sesion sesion;
    private readonly Action alSesionNoValida;

    protected ControladorConSesion(Sesion sesion, Action alSesionNoValida)
    {
        this.sesion = sesion;
        this.alSesionNoValida = alSesionNoValida;
    }

    protected void Ejecutar(Action operacion, Action<string> mostrarError)
    {
        try
        {
            operacion();
        }
        catch (ServicioException e)
        {
            if (e.SesionNoValida)
            {
                sesion.Cerrar();
                alSesionNoValida();
            }
            else
            {
                mostrarError(e.Message);
            }
        }
    }
}

/// <summary>ECUD1 Iniciar Sesión.</summary>
public class LoginControlador
{
    private readonly LoginVista vista;
    private readonly LoginServicio servicio;
    private readonly Sesion sesion;

    public LoginControlador(LoginVista vista, LoginServicio servicio, Sesion sesion)
    {
        this.vista = vista;
        this.servicio = servicio;
        this.sesion = sesion;
        vista.AlIngresar(Ingresar);
    }

    private void Ingresar()
    {
        string? usuario = vista.Usuario;
        string? clave = vista.Clave;
        // ECUD1 alterno 2: con un campo vacío no se envía la petición
        if (string.IsNullOrWhiteSpace(usuario))
        {
            vista.MostrarError(Formatos.IngreseUsuario);
            return;
        }
        if (string.IsNullOrEmpty(clave))
        {
            vista.MostrarError(Formatos.IngreseClave);
            return;
        }
        try
        {
            DatosSesion datos = servicio.IniciarSesion(usuario.Trim(), clave);
            sesion.Iniciar(usuario.Trim(), datos);
            vista.AbrirMenu();
        }
        catch (ServicioException e)
        {
            // ECUD1 alternos 1 y 3: se muestra el mensaje y se borra la clave
            vista.LimpiarClave();
            vista.MostrarError(e.Message);
        }
    }
}

/// <summary>Mantiene actualizado el indicador «Servicio disponible / no disponible».</summary>
public class EstadoControlador
{
    private readonly EstadoVista vista;
    private readonly Func<bool> disponible;

    public EstadoControlador(EstadoVista vista, EstadoServidor estado) : this(vista, estado.Disponible)
    {
    }

    public EstadoControlador(EstadoVista vista, Func<bool> disponible)
    {
        this.vista = vista;
        this.disponible = disponible;
        vista.AlRevisarEstado(Revisar);
    }

    private void Revisar() => vista.MostrarEstado(disponible());
}

/// <summary>
/// ECUD4 Gestionar Países (Listar, Registrar, Actualizar, Eliminar) y ECUD6 Filtrar. La región
/// elegida queda activa: después de registrar, actualizar o eliminar se recarga con ese filtro.
/// </summary>
public class PaisesControlador : ControladorConSesion
{
    private readonly PaisesVista vista;
    private readonly PaisServicio servicio;
    private volatile string region = "";

    public PaisesControlador(PaisesVista vista, PaisServicio servicio, Sesion sesion, Action alSesionNoValida)
        : base(sesion, alSesionNoValida)
    {
        this.vista = vista;
        this.servicio = servicio;
        vista.AlCargar(Cargar);
        vista.AlActualizar(Actualizar);
        vista.AlEliminar(Eliminar);
        vista.AlConsultarApi(ConsultarApi);
        vista.AlRegistrar(Registrar);
    }

    /// <summary>Listar (sin región) o Filtrar (con región).</summary>
    private void Cargar(string regionElegida)
    {
        region = Formatos.Region(regionElegida);
        Ejecutar(Recargar, vista.MostrarError);
    }

    private void Recargar()
    {
        string actual = region;
        List<Pais>? paises = actual.Length == 0
            ? servicio.Listar(sesion.Token).Datos
            : servicio.Buscar(sesion.Token, null, null, actual).Datos?.Paises;
        vista.MostrarPaises(paises ?? [], actual);
    }

    /// <summary>ECUD4 Actualizar: el servidor vuelve a consultar la API.</summary>
    private void Actualizar(string nombre)
    {
        Ejecutar(() =>
        {
            Resultado<Pais> resultado = servicio.Actualizar(sesion.Token, nombre);
            vista.MostrarMensaje(resultado.Mensaje);
            Recargar();
        }, vista.MostrarError);
    }

    /// <summary>ECUD4 Eliminar (la vista ya pidió confirmación).</summary>
    private void Eliminar(string nombre)
    {
        Ejecutar(() =>
        {
            Resultado<object> resultado = servicio.Eliminar(sesion.Token, nombre);
            vista.MostrarMensaje(resultado.Mensaje);
            Recargar();
        }, vista.MostrarError);
    }

    /// <summary>ECUD4 Registrar, pasos 3 y 4: resultados de la API para elegir el país.</summary>
    private void ConsultarApi(string consulta)
    {
        string? error = Formatos.ErrorNombre(consulta);
        if (error != null)
        {
            vista.MostrarErrorRegistro(error);
            return;
        }
        Ejecutar(() => vista.MostrarResultadosApi(servicio.ConsultarApi(sesion.Token, consulta.Trim()).Datos ?? []),
            vista.MostrarErrorRegistro);
    }

    /// <summary>ECUD4 Registrar, pasos 6 a 9 (409 si ya existe).</summary>
    private void Registrar(string consulta, string nombre)
    {
        Ejecutar(() =>
        {
            Resultado<Pais> resultado = servicio.Registrar(sesion.Token, consulta, nombre);
            vista.RegistroCompletado(resultado.Mensaje);
            Recargar();
        }, vista.MostrarErrorRegistro);
    }
}

/// <summary>ECUD3 Consultar País en la API, ECUD2 Importar País e «Importar todos».</summary>
public class ConsultaApiControlador : ControladorConSesion
{
    private readonly ConsultaApiVista vista;
    private readonly PaisServicio servicio;

    public ConsultaApiControlador(ConsultaApiVista vista, PaisServicio servicio, Sesion sesion, Action alSesionNoValida)
        : base(sesion, alSesionNoValida)
    {
        this.vista = vista;
        this.servicio = servicio;
        vista.AlConsultar(Consultar);
        vista.AlGuardar(Guardar);
        vista.AlImportarTodos(ImportarTodos);
    }

    private void Consultar(string nombre)
    {
        // ECUD3 alterno 3: nombre vacío o de más de 100 caracteres
        string? error = Formatos.ErrorNombre(nombre);
        if (error != null)
        {
            vista.MostrarError(error);
            return;
        }
        Ejecutar(() => vista.MostrarResultados(servicio.ConsultarApi(sesion.Token, nombre.Trim()).Datos ?? []),
            vista.MostrarError);
    }

    /// <summary>ECUD2 pasos 4 a 9 (la vista ya pidió confirmación).</summary>
    private void Guardar(string consulta, string nombre)
    {
        Ejecutar(() =>
        {
            Resultado<ResultadoImportacion> resultado = servicio.Importar(sesion.Token, consulta, nombre);
            string accion = resultado.Datos == null ? "" : " (" + resultado.Datos.Accion + ")";
            vista.MostrarMensaje(resultado.Mensaje + accion);
            vista.MarcarGuardado(nombre);
        }, vista.MostrarError);
    }

    private void ImportarTodos()
    {
        Ejecutar(() =>
        {
            Resultado<ResumenImportacion> resultado = servicio.ImportarTodos(sesion.Token);
            ResumenImportacion? resumen = resultado.Datos;
            vista.MostrarMensaje(resumen == null
                ? resultado.Mensaje
                : resultado.Mensaje + ": " + resumen.Recibidos + " recibidos, " + resumen.Registrados
                  + " registrados, " + resumen.Actualizados + " actualizados");
        }, vista.MostrarError);
    }
}

/// <summary>ECUD5 Buscar Países y ECUD6 Filtrar (los criterios se envían juntos en la misma petición).</summary>
public class BusquedaControlador : ControladorConSesion
{
    private readonly BusquedaVista vista;
    private readonly PaisServicio servicio;

    public BusquedaControlador(BusquedaVista vista, PaisServicio servicio, Sesion sesion, Action alSesionNoValida)
        : base(sesion, alSesionNoValida)
    {
        this.vista = vista;
        this.servicio = servicio;
        vista.AlBuscar(Buscar);
    }

    private void Buscar(Criterios criterios)
    {
        if (Largo(criterios.Nombre) || Largo(criterios.Capital))
        {
            vista.MostrarError(Formatos.NombreMuyLargo);
            return;
        }
        Ejecutar(() =>
        {
            Resultado<ResultadoBusqueda> resultado = servicio.Buscar(sesion.Token, criterios.Nombre, criterios.Capital,
                Formatos.Region(criterios.Region));
            vista.MostrarResultado(resultado.Datos?.Paises ?? [], resultado.Mensaje);
        }, vista.MostrarError);
    }

    private static bool Largo(string? texto) => texto != null && texto.Trim().Length > Formatos.MaxNombre;
}

/// <summary>ECUD7 Generar Informes Estadísticos.</summary>
public class InformesControlador : ControladorConSesion
{
    /// <summary>Mensaje del servidor sin países guardados (ECUD7, alterno 2).</summary>
    public const string SinPaises = "No hay países registrados para generar informes";
    public const string Sugerencia = ". Use «Consultar en la API» para guardar países.";

    private readonly InformesVista vista;
    private readonly ReporteServicio servicio;

    public InformesControlador(InformesVista vista, ReporteServicio servicio, Sesion sesion, Action alSesionNoValida)
        : base(sesion, alSesionNoValida)
    {
        this.vista = vista;
        this.servicio = servicio;
        vista.AlGenerar(Generar);
    }

    private void Generar()
    {
        Ejecutar(() =>
        {
            Resultado<Informe> resultado = servicio.Generar(sesion.Token);
            vista.MostrarInforme(resultado.Datos ?? new Informe([], null, []), resultado.Mensaje);
        }, mensaje => vista.MostrarError(mensaje == SinPaises ? mensaje + Sugerencia : mensaje));
    }
}
