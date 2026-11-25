namespace MOV_APIPAISES_DOTNET_REST.Vista;

/// <summary>Textos de la interfaz (strings.xml del cliente Android de TI.2.1).</summary>
public static class Textos
{
    public const string AppNombre = "APIPaises";
    public const string AppLema = "Países del mundo · Servicio REST / .NET";

    // ECUD1
    public const string LoginTitulo = "Iniciar sesión";
    public const string LoginSubtitulo = "Ingrese sus credenciales para continuar";
    public const string LoginUsuario = "Usuario";
    public const string LoginClave = "Clave";
    public const string LoginIngresar = "Ingresar";
    public const string LoginServidor = "Servidor: {0}";
    public const string CerrarSesion = "Cerrar sesión";
    public const string CerrarSesionPregunta = "¿Desea cerrar la sesión?";
    public const string Sesion = "Sesión";

    // Pestañas
    public const string PestanaInicio = "Inicio";
    public const string PestanaPaises = "Países";
    public const string PestanaApi = "API";
    public const string PestanaInformes = "Informes";
    public const string TituloApi = "Consultar en la API";

    // Inicio
    public const string InicioSaludo = "Hola,";
    public const string InicioServicio = "Servicio REST / .NET";
    public const string InicioPaisesGuardados = "Países guardados";
    public const string InicioEnBaseLocal = "en la base de datos local";
    public const string InicioAcciones = "Opciones";
    public const string AccionPaises = "Países";
    public const string AccionPaisesDetalle = "Liste, actualice y elimine los países guardados";
    public const string AccionApi = "Consultar en la API";
    public const string AccionApiDetalle = "Consulte un país en REST Countries y guárdelo";
    public const string AccionBuscar = "Buscar países";
    public const string AccionBuscarDetalle = "Busque por nombre, capital o región";
    public const string AccionInformes = "Informes";
    public const string AccionInformesDetalle = "Países por región, país más poblado y promedios";

    // Países (ECUD4, ECUD5, ECUD6)
    public const string BuscarNombre = "Nombre";
    public const string BuscarCapital = "Capital";
    public const string FiltrarRegion = "Filtrar por región";
    public const string Todas = "Todas";
    public const string Nuevo = "Nuevo";
    public const string Actualizar = "Actualizar";
    public const string Eliminar = "Eliminar";
    public const string EliminarPregunta = "¿Eliminar el país {0}?";
    public const string SinPaises = "No hay países registrados. Use «Consultar en la API» para guardarlos";
    public const string SinPaisesRegion = "No se encontraron países en esa región";
    public const string SinResultados = "No se encontraron países con esos criterios";
    public const string NuevoTitulo = "Nuevo país";
    public const string NombrePais = "Nombre del país";
    public const string Consultar = "Consultar";
    public const string Registrar = "Registrar";
    public const string ElijaPais = "Elija el país a registrar";

    // Datos del país
    public const string DatoCapital = "Capital";
    public const string DatoRegion = "Región";
    public const string DatoPoblacion = "Población";
    public const string DatoMoneda = "Moneda";
    public const string Guardado = "Guardado";
    public const string NoGuardado = "No guardado";

    // API (ECUD3, ECUD2)
    public const string Guardar = "Guardar";
    public const string GuardarPregunta = "¿Guardar el país {0} en la base de datos local?";
    public const string ImportarTodos = "Importar todos";
    public const string ImportarTodosPregunta = "¿Importar todos los países desde la API REST Countries? Los nuevos se registran y los que ya existen se actualizan.";
    public const string ImportarTodosAyuda = "Todos los países de REST Countries a la base local";
    public const string ApiAyuda = "Escriba el nombre de un país, por ejemplo «ecuador», y presione Consultar.";

    // Informes (ECUD7)
    public const string InformeMasPoblado = "País más poblado";
    public const string InformeGrafico = "Cantidad de países por región";
    public const string InformePorRegion = "Países por región";
    public const string InformePromedio = "Promedio poblacional por región";
    public const string InformeSugerencia = "Use la pestaña «API» para guardar países.";

    public const string Si = "Sí";
    public const string No = "No";
    public const string Aceptar = "Aceptar";
    public const string Cancelar = "Cancelar";
    public const string ErrorInesperado = "Ocurrió un error inesperado, intente nuevamente";
}
