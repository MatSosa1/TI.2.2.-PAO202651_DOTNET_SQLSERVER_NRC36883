using ec.edu.monster.modelo;
using ec.edu.monster.vista;

namespace ec.edu.monster.dobles;

/// <summary>Transporte simulado: guarda la última petición y devuelve la respuesta preparada.</summary>
public sealed class TransporteFalso : Transporte
{
    public string Metodo = "";
    public string Url = "";
    public string? Cuerpo;
    public IDictionary<string, string> Cabeceras = new Dictionary<string, string>();
    public TimeSpan Espera;
    public int Codigo = 200;
    public string Respuesta = """{"estado":"OK","mensaje":"ok","datos":null}""";
    public Exception? Falla;

    public Transporte.Respuesta Enviar(string metodo, string url, string? cuerpo, IDictionary<string, string> cabeceras,
        TimeSpan espera)
    {
        Metodo = metodo;
        Url = url;
        Cuerpo = cuerpo;
        Cabeceras = cabeceras;
        Espera = espera;
        if (Falla != null)
            throw Falla;
        return new Transporte.Respuesta(Codigo, Respuesta);
    }
}

/// <summary>Servicios simulados: devuelven lo preparado o lanzan la excepción indicada.</summary>
public sealed class ServicioFalso : LoginServicio, PaisServicio, ReporteServicio
{
    public ServicioException? Falla;
    public List<string> Llamadas = [];
    public string? UltimoToken;
    public List<Pais> Paises = [];
    public Informe Informe = new([new ConteoRegion("América", 1)], null, [new PromedioRegion("América", 10)]);

    private void Registrar(string llamada, string? token = null)
    {
        Llamadas.Add(llamada);
        UltimoToken = token;
        if (Falla != null)
            throw Falla;
    }

    public DatosSesion IniciarSesion(string usuario, string clave)
    {
        Registrar("login:" + usuario);
        return new DatosSesion(new string('a', 64), "Administrador");
    }

    public void CerrarSesion(string? token) => Registrar("logout", token);

    public Resultado<List<Pais>> Listar(string? token)
    {
        Registrar("listar", token);
        return new Resultado<List<Pais>>("Países obtenidos correctamente", Paises);
    }

    public Resultado<ResultadoBusqueda> Buscar(string? token, string? nombre, string? capital, string? region)
    {
        Registrar($"buscar:{nombre}|{capital}|{region}", token);
        return new Resultado<ResultadoBusqueda>(
            nombre == null && capital == null ? "Filtro aplicado correctamente" : "Búsqueda realizada correctamente",
            new ResultadoBusqueda(Paises, Paises.Count));
    }

    public Resultado<List<Pais>> ConsultarApi(string? token, string nombre)
    {
        Registrar("consultarApi:" + nombre, token);
        return new Resultado<List<Pais>>("Consulta en la API exitosa", Paises);
    }

    public Resultado<ResultadoImportacion> Importar(string? token, string? consulta, string nombre)
    {
        Registrar($"importar:{consulta}|{nombre}", token);
        return new Resultado<ResultadoImportacion>("País importado correctamente", new ResultadoImportacion(null, "registrado"));
    }

    public Resultado<ResumenImportacion> ImportarTodos(string? token)
    {
        Registrar("importarTodos", token);
        return new Resultado<ResumenImportacion>("Importación finalizada", new ResumenImportacion(250, 240, 10));
    }

    public Resultado<Pais> Registrar(string? token, string? consulta, string nombre)
    {
        Registrar($"registrar:{consulta}|{nombre}", token);
        return new Resultado<Pais>("País registrado correctamente", null);
    }

    public Resultado<Pais> Actualizar(string? token, string nombre)
    {
        Registrar("actualizar:" + nombre, token);
        return new Resultado<Pais>("País actualizado correctamente", null);
    }

    public Resultado<object> Eliminar(string? token, string nombre)
    {
        Registrar("eliminar:" + nombre, token);
        return new Resultado<object>("País eliminado correctamente", null);
    }

    public Resultado<Informe> Generar(string? token)
    {
        Registrar("generar", token);
        return new Resultado<Informe>("Informes generados correctamente", Informe);
    }
}

/// <summary>Vistas falsas: guardan las acciones registradas y lo que el controlador mostró.</summary>
public sealed class LoginVistaFalsa : Vistas.LoginVista
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public Action Ingresar = () => { };
    public List<string> Errores = [];
    public bool ClaveLimpia;
    public bool MenuAbierto;

    public void MostrarError(string mensaje) => Errores.Add(mensaje);
    public void LimpiarClave() => ClaveLimpia = true;
    public void AbrirMenu() => MenuAbierto = true;
    public void AlIngresar(Action accion) => Ingresar = accion;
}

public sealed class PaisesVistaFalsa : Vistas.PaisesVista
{
    public Action<string> Cargar = _ => { };
    public Action<string> Actualizar = _ => { };
    public Action<string> Eliminar = _ => { };
    public Action<string> ConsultarApi = _ => { };
    public Action<string, string> Registrar = (_, _) => { };
    public List<Pais>? Paises;
    public string? Region;
    public List<Pais>? ResultadosApi;
    public string? Registro;
    public List<string> Mensajes = [];
    public List<string> Errores = [];
    public List<string> ErroresRegistro = [];

    public void AlCargar(Action<string> accion) => Cargar = accion;
    public void AlActualizar(Action<string> accion) => Actualizar = accion;
    public void AlEliminar(Action<string> accion) => Eliminar = accion;
    public void AlConsultarApi(Action<string> accion) => ConsultarApi = accion;
    public void AlRegistrar(Action<string, string> accion) => Registrar = accion;
    public void MostrarPaises(List<Pais> paises, string region) { Paises = paises; Region = region; }
    public void MostrarResultadosApi(List<Pais> paises) => ResultadosApi = paises;
    public void RegistroCompletado(string mensaje) => Registro = mensaje;
    public void MostrarMensaje(string mensaje) => Mensajes.Add(mensaje);
    public void MostrarError(string mensaje) => Errores.Add(mensaje);
    public void MostrarErrorRegistro(string mensaje) => ErroresRegistro.Add(mensaje);
}

public sealed class ConsultaApiVistaFalsa : Vistas.ConsultaApiVista
{
    public Action<string> Consultar = _ => { };
    public Action<string, string> Guardar = (_, _) => { };
    public Action ImportarTodos = () => { };
    public List<Pais>? Resultados;
    public List<string> Guardados = [];
    public List<string> Mensajes = [];
    public List<string> Errores = [];

    public void AlConsultar(Action<string> accion) => Consultar = accion;
    public void AlGuardar(Action<string, string> accion) => Guardar = accion;
    public void AlImportarTodos(Action accion) => ImportarTodos = accion;
    public void MostrarResultados(List<Pais> paises) => Resultados = paises;
    public void MarcarGuardado(string nombre) => Guardados.Add(nombre);
    public void MostrarMensaje(string mensaje) => Mensajes.Add(mensaje);
    public void MostrarError(string mensaje) => Errores.Add(mensaje);
}

public sealed class BusquedaVistaFalsa : Vistas.BusquedaVista
{
    public Action<Vistas.Criterios> Buscar = _ => { };
    public List<Pais>? Resultado;
    public string? Mensaje;
    public List<string> Errores = [];

    public void AlBuscar(Action<Vistas.Criterios> accion) => Buscar = accion;
    public void MostrarResultado(List<Pais> paises, string mensaje) { Resultado = paises; Mensaje = mensaje; }
    public void MostrarError(string mensaje) => Errores.Add(mensaje);
}

public sealed class InformesVistaFalsa : Vistas.InformesVista
{
    public Action Generar = () => { };
    public Informe? Informe;
    public string? Mensaje;
    public List<string> Errores = [];

    public void AlGenerar(Action accion) => Generar = accion;
    public void MostrarInforme(Informe informe, string mensaje) { Informe = informe; Mensaje = mensaje; }
    public void MostrarError(string mensaje) => Errores.Add(mensaje);
}

public sealed class EstadoVistaFalsa : Vistas.EstadoVista
{
    public Action Revisar = () => { };
    public bool? Disponible;

    public void MostrarEstado(bool disponible) => Disponible = disponible;
    public void AlRevisarEstado(Action accion) => Revisar = accion;
}
