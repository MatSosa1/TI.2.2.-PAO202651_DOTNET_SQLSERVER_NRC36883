using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// Contratos entre las pantallas y sus controladores. Las pantallas registran las acciones del
/// usuario («Al…») y los controladores responden con «Mostrar…». Las implementaciones WPF
/// actualizan la interfaz en el hilo de la interfaz; las pruebas usan implementaciones falsas.
/// </summary>
public static class Vistas
{
    /// <summary>ECUD1: formulario de inicio de sesión.</summary>
    public interface LoginVista
    {
        string? Usuario { get; }
        string? Clave { get; }
        void MostrarError(string mensaje);
        /// <summary>ECUD1 alterno 1: se borra la clave y se mantiene el formulario.</summary>
        void LimpiarClave();
        void AbrirMenu();
        void AlIngresar(Action accion);
    }

    /// <summary>Indicador «Servicio disponible / no disponible».</summary>
    public interface EstadoVista
    {
        void MostrarEstado(bool disponible);
        void AlRevisarEstado(Action accion);
    }

    /// <summary>ECUD4 Gestionar Países y ECUD6 Filtrar.</summary>
    public interface PaisesVista
    {
        /// <summary>Cargar la tabla con la región indicada ("" = todas).</summary>
        void AlCargar(Action<string> accion);
        void AlActualizar(Action<string> accion);
        void AlEliminar(Action<string> accion);
        /// <summary>Diálogo «Nuevo»: consultar en la API (ECUD4 Registrar, pasos 2 y 3).</summary>
        void AlConsultarApi(Action<string> accion);
        /// <summary>Diálogo «Nuevo»: registrar el país elegido (consulta, nombre).</summary>
        void AlRegistrar(Action<string, string> accion);
        void MostrarPaises(List<Pais> paises, string region);
        void MostrarResultadosApi(List<Pais> paises);
        void RegistroCompletado(string mensaje);
        void MostrarMensaje(string mensaje);
        void MostrarError(string mensaje);
        void MostrarErrorRegistro(string mensaje);
    }

    /// <summary>ECUD3 Consultar país en la API y ECUD2 Importar.</summary>
    public interface ConsultaApiVista
    {
        void AlConsultar(Action<string> accion);
        /// <summary>Guardar el país elegido (consulta, nombre).</summary>
        void AlGuardar(Action<string, string> accion);
        void AlImportarTodos(Action accion);
        void MostrarResultados(List<Pais> paises);
        void MarcarGuardado(string nombre);
        void MostrarMensaje(string mensaje);
        void MostrarError(string mensaje);
    }

    /// <summary>Criterios de ECUD5 Buscar Países (y ECUD6 al cambiar la región).</summary>
    public record Criterios(string? Nombre, string? Capital, string? Region);

    /// <summary>ECUD5 Buscar Países (y ECUD6 al cambiar la región).</summary>
    public interface BusquedaVista
    {
        void AlBuscar(Action<Criterios> accion);
        void MostrarResultado(List<Pais> paises, string mensaje);
        void MostrarError(string mensaje);
    }

    /// <summary>ECUD7 Generar Informes Estadísticos.</summary>
    public interface InformesVista
    {
        void AlGenerar(Action accion);
        void MostrarInforme(Informe informe, string mensaje);
        void MostrarError(string mensaje);
    }
}
