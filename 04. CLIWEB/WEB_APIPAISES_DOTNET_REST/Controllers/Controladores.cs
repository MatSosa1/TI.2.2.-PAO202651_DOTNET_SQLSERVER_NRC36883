using Microsoft.AspNetCore.Mvc;
using WEB_APIPAISES_DOTNET_REST.Filtros;
using WEB_APIPAISES_DOTNET_REST.Models.Dto;
using WEB_APIPAISES_DOTNET_REST.Models.Servicios;

namespace WEB_APIPAISES_DOTNET_REST.Controllers;

/// <summary>
/// Apoyo común de los controladores: token de la sesión, vistas, mensajes «flash» que
/// sobreviven a una redirección (Post/Redirect/Get) y la vuelta al inicio de sesión cuando el
/// servidor rechaza el token (ECUD8).
/// </summary>
public abstract class ControladorBase : Controller
{
    protected const string RutaLogin = "/login";

    /// <summary>Token de la sesión, o null si no hay usuario (el AuthFilter ya lo exige).</summary>
    protected string? Token()
    {
        string? token = HttpContext.Session.GetString(SesionWeb.Token);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    protected ViewResult Render(string vista) => View("~/Views/Vista/" + vista + ".cshtml");

    /// <summary>Mensaje para mostrar después de la próxima redirección.</summary>
    protected void Flash(string mensaje, bool error)
        => HttpContext.Session.SetString(error ? SesionWeb.FlashError : SesionWeb.FlashMensaje, mensaje);

    /// <summary>Pasa los mensajes flash a la vista («mensaje» y «error») y los borra.</summary>
    protected void LeerFlash()
    {
        string? mensaje = HttpContext.Session.GetString(SesionWeb.FlashMensaje);
        string? error = HttpContext.Session.GetString(SesionWeb.FlashError);
        if (mensaje != null)
        {
            ViewData["mensaje"] = mensaje;
            HttpContext.Session.Remove(SesionWeb.FlashMensaje);
        }
        if (error != null)
        {
            ViewData["error"] = error;
            HttpContext.Session.Remove(SesionWeb.FlashError);
        }
    }

    /// <summary>
    /// ECUD8 alterno 2: si el servidor rechazó el token, se borra la sesión y se vuelve al login
    /// con el aviso. Devuelve la redirección, o null si el error es otro.
    /// </summary>
    protected IActionResult? SesionVencida(ServicioException e)
    {
        if (!e.SesionNoValida)
            return null;
        HttpContext.Session.Clear();
        return Redirect(RutaLogin + "?sesion=expirada");
    }

    protected static string Codificar(string texto) => Uri.EscapeDataString(texto);
}

/// <summary>
/// Pantalla de inicio de sesión (ECUD1).
/// GET  /login                  → formulario (con el aviso de sesión vencida si ?sesion=expirada)
/// POST /login usuario, clave   → valida contra el servidor REST y abre el menú
/// </summary>
[Route("login")]
public class LoginController : ControladorBase
{
    private readonly LoginService loginService;

    public LoginController(LoginService loginService)
    {
        this.loginService = loginService;
    }

    [HttpGet]
    public IActionResult Mostrar(string? sesion)
    {
        if (sesion == "expirada")
            ViewData["error"] = ServicioException.SesionNoValidaMensaje;
        return Render("login");
    }

    [HttpPost]
    public async Task<IActionResult> Ingresar(string? usuario, string? clave)
    {
        // ECUD1 alterno 2: con un campo vacío no se llama al servidor
        if (string.IsNullOrWhiteSpace(usuario))
            return MostrarError(Formatos.IngreseUsuario, usuario);
        if (string.IsNullOrEmpty(clave))
            return MostrarError(Formatos.IngreseClave, usuario);

        DatosSesion datos;
        try
        {
            datos = await loginService.IniciarSesionAsync(usuario.Trim(), clave);
        }
        catch (ServicioException e)
        {
            // ECUD1 alternos 1 y 3: se mantiene el usuario y se borra la clave
            return MostrarError(e.Message, usuario);
        }
        // Datos de sesión nuevos después del login (se descarta lo anterior, salvo el token CSRF)
        string? csrf = HttpContext.Session.GetString(CsrfFilter.SessionAttribute);
        HttpContext.Session.Clear();
        if (csrf != null)
            HttpContext.Session.SetString(CsrfFilter.SessionAttribute, csrf);
        HttpContext.Session.SetString(SesionWeb.Token, datos.Token!);
        HttpContext.Session.SetString(SesionWeb.Usuario, usuario.Trim());
        HttpContext.Session.SetString(SesionWeb.Nombre, datos.Usuario ?? usuario.Trim());
        // Redirección (no vista): recargar la página no reenvía la clave
        return Redirect("/menu");
    }

    private IActionResult MostrarError(string mensaje, string? usuario)
    {
        ViewData["error"] = mensaje;
        ViewData["usuario"] = usuario;
        return Render("login");
    }
}

/// <summary>
/// Cerrar sesión (ECUD1 alterno 4). Solo POST: un enlace o una precarga (GET) no deben cerrar la
/// sesión. El token se invalida en el servidor y la sesión local se borra aunque el servidor no responda.
/// </summary>
[Route("logout")]
public class LogoutController : ControladorBase
{
    private readonly LoginService loginService;

    public LogoutController(LoginService loginService)
    {
        this.loginService = loginService;
    }

    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        string? token = Token();
        if (token != null)
            await loginService.CerrarSesionAsync(token);
        HttpContext.Session.Clear();
        return Redirect(RutaLogin);
    }
}

/// <summary>Página de bienvenida: la aplicación empieza en el inicio de sesión.</summary>
[Route("")]
public class InicioController : Controller
{
    [HttpGet]
    public IActionResult Index() => Redirect("/login");
}

/// <summary>Menú principal (ECUD1 paso 7). El AuthFilter ya comprobó que hay sesión.</summary>
[Route("menu")]
[AuthFilter]
public class MenuController : ControladorBase
{
    [HttpGet]
    public IActionResult Mostrar() => Render("menu");
}

/// <summary>
/// ECUD4 Gestionar Países y ECUD6 Filtrar.
/// GET  /paises[?region=Europa]                          → tabla (con el filtro de región activo)
/// POST /paises accion=actualizar nombre                  → el servidor vuelve a consultar la API (PRG)
/// POST /paises accion=eliminar paso=revisar nombre       → confirmación «¿Eliminar el país …?»
/// POST /paises accion=eliminar paso=confirmar nombre     → elimina y vuelve a la tabla (PRG)
/// </summary>
[Route("paises")]
[AuthFilter]
public class PaisesController : ControladorBase
{
    private const string Ruta = "/paises";
    private readonly PaisService paisService;

    public PaisesController(PaisService paisService)
    {
        this.paisService = paisService;
    }

    [HttpGet]
    public async Task<IActionResult> Mostrar(string? region)
    {
        LeerFlash();
        string filtro = Formatos.Region(region);
        ViewData["region"] = filtro;
        try
        {
            List<Pais> paises = (filtro.Length == 0
                ? (await paisService.ListarAsync(Token())).Datos
                : (await paisService.BuscarAsync(Token(), null, null, filtro)).Datos?.Paises) ?? [];
            ViewData["paises"] = paises;
            ViewData["total"] = Formatos.Total(paises.Count);
        }
        catch (ServicioException e)
        {
            if (SesionVencida(e) is { } redireccion)
                return redireccion;
            ViewData["error"] = e.Message;
            ViewData["paises"] = new List<Pais>();
        }
        return Render("paises");
    }

    [HttpPost]
    public async Task<IActionResult> Operar(string? accion, string? nombre, string? region, string? paso)
    {
        string limpio = Formatos.Limpiar(nombre);
        string filtro = Formatos.Region(region);
        string volver = Ruta + (filtro.Length == 0 ? "" : "?region=" + Codificar(filtro));
        if (limpio.Length == 0)
            return Redirect(volver);
        if (accion == "eliminar" && paso != "confirmar")
        {
            // ECUD4 Eliminar pasos 2 y 3: se pide confirmar antes de enviar
            ViewData["confirmarEliminar"] = limpio;
            ViewData["region"] = filtro;
            return Render("paises");
        }
        try
        {
            string mensaje = accion == "eliminar"
                ? (await paisService.EliminarAsync(Token(), limpio)).Mensaje
                : (await paisService.ActualizarAsync(Token(), limpio)).Mensaje;
            Flash(mensaje, false);
        }
        catch (ServicioException e)
        {
            if (SesionVencida(e) is { } redireccion)
                return redireccion;
            Flash(e.Message, true);
        }
        // Post/Redirect/Get: recargar la página no repite la operación
        return Redirect(volver);
    }
}

/// <summary>
/// ECUD4 Registrar País.
/// GET  /paises/nuevo                  → campo «Nombre del país»
/// GET  /paises/nuevo?consulta=…       → resultados de la API para elegir (pasos 3 y 4)
/// POST /paises/nuevo consulta, nombre → registra (409 si ya existe) y vuelve a la tabla (PRG)
/// </summary>
[Route("paises/nuevo")]
[AuthFilter]
public class NuevoPaisController : ControladorBase
{
    private readonly PaisService paisService;

    public NuevoPaisController(PaisService paisService)
    {
        this.paisService = paisService;
    }

    [HttpGet]
    public async Task<IActionResult> Mostrar(string? consulta)
    {
        if (consulta != null && await ConsultarAsync(consulta) is { } redireccion)
            return redireccion;
        return Render("nuevo");
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(string? consulta, string? nombre)
    {
        string texto = Formatos.Limpiar(consulta);
        string elegido = Formatos.Limpiar(nombre);
        if (elegido.Length == 0)
        {
            ViewData["error"] = "Seleccione un país de los resultados";
            return await ConsultarAsync(texto) ?? Render("nuevo");
        }
        try
        {
            Resultado<Pais> resultado = await paisService.RegistrarAsync(Token(), texto, elegido);
            // ECUD4 Registrar paso 9: mensaje, se cierra el formulario y se recarga la tabla
            Flash(resultado.Mensaje, false);
            return Redirect("/paises");
        }
        catch (ServicioException e)
        {
            if (SesionVencida(e) is { } redireccion)
                return redireccion;
            // Duplicado, API no disponible...: se muestra el error con los resultados otra vez
            if (await ConsultarAsync(texto) is { } otra)
                return otra;
            ViewData["error"] = e.Message;
            return Render("nuevo");
        }
    }

    /// <summary>
    /// Consulta la API y deja los resultados en la vista. Devuelve una redirección solo si la
    /// sesión venció; los errores quedan en «error».
    /// </summary>
    private async Task<IActionResult?> ConsultarAsync(string consulta)
    {
        ViewData["consulta"] = consulta;
        string? error = Formatos.ErrorNombre(consulta);
        if (error != null)
        {
            ViewData["error"] = error;
            return null;
        }
        try
        {
            ViewData["resultados"] = (await paisService.ConsultarApiAsync(Token(), consulta.Trim())).Datos;
        }
        catch (ServicioException e)
        {
            if (SesionVencida(e) is { } redireccion)
                return redireccion;
            ViewData["error"] = e.Message;
        }
        return null;
    }
}

/// <summary>
/// ECUD3 Consultar País en la API, ECUD2 Importar e «Importar todos».
/// GET  /consulta[?nombre=…]                                   → campo y tabla de resultados
/// POST /consulta accion=guardar paso=revisar consulta, nombre → «¿Guardar el país … en la base de datos local?»
/// POST /consulta accion=guardar paso=confirmar               → importa y vuelve a la consulta (PRG)
/// POST /consulta accion=importarTodos paso=revisar|confirmar → confirmación e importación total
/// </summary>
[Route("consulta")]
[AuthFilter]
public class ConsultaController : ControladorBase
{
    private const string Ruta = "/consulta";
    private readonly PaisService paisService;

    public ConsultaController(PaisService paisService)
    {
        this.paisService = paisService;
    }

    [HttpGet]
    public async Task<IActionResult> Mostrar(string? nombre)
    {
        LeerFlash();
        if (nombre != null)
        {
            ViewData["nombre"] = nombre;
            string? error = Formatos.ErrorNombre(nombre);
            if (error != null)
            {
                ViewData["error"] = error;
            }
            else
            {
                try
                {
                    ViewData["resultados"] = (await paisService.ConsultarApiAsync(Token(), nombre.Trim())).Datos;
                }
                catch (ServicioException e)
                {
                    if (SesionVencida(e) is { } redireccion)
                        return redireccion;
                    // ECUD3 alterno 2: se conserva el texto para corregirlo
                    ViewData["error"] = e.Message;
                }
            }
        }
        return Render("consulta");
    }

    [HttpPost]
    public async Task<IActionResult> Operar(string? accion, string? consulta, string? nombre, string? paso)
    {
        string texto = Formatos.Limpiar(consulta);
        string elegido = Formatos.Limpiar(nombre);
        bool confirmar = paso == "confirmar";
        string volver = Ruta + (texto.Length == 0 ? "" : "?nombre=" + Codificar(texto));

        if (accion == "importarTodos")
        {
            if (!confirmar)
            {
                ViewData["confirmar"] = "importarTodos";
                ViewData["consulta"] = texto;
                return Render("consulta");
            }
            return await EjecutarAsync(volver, async () =>
            {
                Resultado<ResumenImportacion> resultado = await paisService.ImportarTodosAsync(Token());
                ResumenImportacion? resumen = resultado.Datos;
                return resumen == null
                    ? resultado.Mensaje
                    : resultado.Mensaje + ": " + resumen.Recibidos + " recibidos, " + resumen.Registrados
                      + " registrados, " + resumen.Actualizados + " actualizados";
            });
        }
        if (elegido.Length == 0)
            return Redirect(volver);
        if (!confirmar)
        {
            // ECUD2 pasos 2 y 3: confirmación antes de guardar
            ViewData["confirmar"] = "guardar";
            ViewData["consulta"] = texto;
            ViewData["pais"] = elegido;
            return Render("consulta");
        }
        return await EjecutarAsync(volver, async () =>
        {
            Resultado<ResultadoImportacion> resultado = await paisService.ImportarAsync(Token(), texto, elegido);
            string accionRealizada = resultado.Datos == null ? "" : " (" + resultado.Datos.Accion + ")";
            return resultado.Mensaje + accionRealizada;
        });
    }

    /// <summary>Ejecuta la operación, deja el mensaje como flash y redirige (Post/Redirect/Get).</summary>
    private async Task<IActionResult> EjecutarAsync(string volver, Func<Task<string>> operacion)
    {
        try
        {
            Flash(await operacion(), false);
        }
        catch (ServicioException e)
        {
            if (SesionVencida(e) is { } redireccion)
                return redireccion;
            Flash(e.Message, true);
        }
        return Redirect(volver);
    }
}

/// <summary>
/// ECUD5 Buscar Países (y ECUD6: al cambiar la región el formulario se envía solo).
/// GET /busqueda                                   → formulario vacío
/// GET /busqueda?buscar=1&amp;nombre=&amp;capital=&amp;region= → resultados y total
/// </summary>
[Route("busqueda")]
[AuthFilter]
public class BusquedaController : ControladorBase
{
    private readonly PaisService paisService;

    public BusquedaController(PaisService paisService)
    {
        this.paisService = paisService;
    }

    [HttpGet]
    public async Task<IActionResult> Mostrar(string? buscar, string? nombre, string? capital, string? region)
    {
        string textoNombre = Formatos.Limpiar(nombre);
        string textoCapital = Formatos.Limpiar(capital);
        string filtro = Formatos.Region(region);
        ViewData["nombre"] = textoNombre;
        ViewData["capital"] = textoCapital;
        ViewData["region"] = filtro;
        if (buscar != null)
        {
            if (textoNombre.Length > Formatos.MaxNombre || textoCapital.Length > Formatos.MaxNombre)
            {
                ViewData["error"] = Formatos.NombreMuyLargo;
            }
            else
            {
                try
                {
                    Resultado<ResultadoBusqueda> resultado = await paisService.BuscarAsync(Token(), textoNombre, textoCapital, filtro);
                    List<Pais> paises = resultado.Datos?.Paises ?? [];
                    ViewData["mensaje"] = resultado.Mensaje;
                    ViewData["resultados"] = paises;
                    ViewData["total"] = Formatos.Total(paises.Count);
                }
                catch (ServicioException e)
                {
                    if (SesionVencida(e) is { } redireccion)
                        return redireccion;
                    ViewData["error"] = e.Message;
                }
            }
        }
        return Render("busqueda");
    }
}

/// <summary>ECUD7 Generar Informes: GET /informes → tablas, país más poblado y gráfico de barras.</summary>
[Route("informes")]
[AuthFilter]
public class InformesController : ControladorBase
{
    /// <summary>Mensaje del servidor sin países guardados (ECUD7, alterno 2).</summary>
    public const string SinPaises = "No hay países registrados para generar informes";

    private readonly ReporteService reporteService;

    public InformesController(ReporteService reporteService)
    {
        this.reporteService = reporteService;
    }

    [HttpGet]
    public async Task<IActionResult> Mostrar()
    {
        try
        {
            Resultado<Informe> resultado = await reporteService.GenerarAsync(Token());
            ViewData["informe"] = resultado.Datos;
            ViewData["mensaje"] = resultado.Mensaje;
        }
        catch (ServicioException e)
        {
            if (SesionVencida(e) is { } redireccion)
                return redireccion;
            ViewData["error"] = e.Message;
            // ECUD7 alterno 2: se sugiere guardar países
            ViewData["sinPaises"] = e.Message == SinPaises;
        }
        return Render("informes");
    }
}
