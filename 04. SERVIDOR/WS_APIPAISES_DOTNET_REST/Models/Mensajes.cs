namespace WS_APIPAISES_DOTNET_REST.Models;

/// <summary>
/// Mensajes del sistema, copiados literalmente de las convenciones del ECUD.
/// Los clientes los muestran tal cual, así que no se deben reescribir.
/// </summary>
public static class Mensajes
{
    public const string LoginExitoso = "Ingreso de sesión exitoso";
    public const string CredencialesIncorrectas = "Credenciales incorrectas";
    public const string SesionCerrada = "Sesión cerrada";
    public const string TokenInvalido = "Token inválido o sesión no activa";

    public const string ConsultaApiExitosa = "Consulta en la API exitosa";
    public const string SinCoincidenciasApi = "No se encontraron países con ese nombre en la API";
    public const string PaisImportado = "País importado correctamente";
    public const string ImportacionFinalizada = "Importación finalizada";

    public const string PaisesListados = "Países obtenidos correctamente";
    public const string PaisRegistrado = "País registrado correctamente";
    public const string PaisActualizado = "País actualizado correctamente";
    public const string PaisEliminado = "País eliminado correctamente";
    public const string PaisDuplicado = "Ya existe un país con ese nombre";
    public const string PaisNoExiste = "El país no existe";

    public const string RegionNoValida = "Región no válida";
    public const string BusquedaRealizada = "Búsqueda realizada correctamente";
    public const string FiltroAplicado = "Filtro aplicado correctamente";

    public const string InformesGenerados = "Informes generados correctamente";
    public const string SinPaisesInformes = "No hay países registrados para generar informes";

    public const string ApiNoDisponible = "La API REST Countries no está disponible, intente más tarde";
    public const string ApiRespuestaInvalida = "La API REST Countries devolvió una respuesta no válida";
    public const string ServicioNoDisponible = "Servicio no disponible, intente más tarde";
    public const string ErrorInesperado = "Ocurrió un error inesperado, intente nuevamente";

    // Validaciones de entrada: las mismas que hacen los clientes antes de enviar.
    public const string IngreseUsuario = "Ingrese el usuario";
    public const string IngreseClave = "Ingrese la clave";
    public const string IngreseNombrePais = "Ingrese el nombre del país";
    public const string NombreMuyLargo = "El nombre no puede superar 100 caracteres";

    // Errores del protocolo HTTP (ruta, método o cuerpo), no previstos en el ECUD.
    public const string DatosNoValidos = "Los datos enviados no son válidos";
    public const string RecursoNoEncontrado = "El recurso solicitado no existe";
    public const string MetodoNoPermitido = "Operación no permitida en este recurso";
}
