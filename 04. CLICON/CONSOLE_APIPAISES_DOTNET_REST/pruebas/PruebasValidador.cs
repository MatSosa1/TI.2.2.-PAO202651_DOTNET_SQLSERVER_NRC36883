using CONSOLE_APIPAISES_DOTNET_REST.Controllers;
using CONSOLE_APIPAISES_DOTNET_REST.Models.Data;
using CONSOLE_APIPAISES_DOTNET_REST.Views.Utils;

namespace CONSOLE_APIPAISES_DOTNET_REST.Pruebas;

internal static class PruebasValidador
{
    public static Ejecutor Crear()
    {
        var e = new Ejecutor();

        e.Caso("Nombre vacío o de solo espacios", () =>
        {
            Afirmar.Igual(ValidadorDatos.IngreseNombrePais, ValidadorDatos.ErrorNombre(null));
            Afirmar.Igual(ValidadorDatos.IngreseNombrePais, ValidadorDatos.ErrorNombre("   "));
        });
        e.Caso("Nombre de 100 caracteres es válido y de 101 no", () =>
        {
            Afirmar.Nulo(ValidadorDatos.ErrorNombre(new string('x', 100)));
            Afirmar.Igual(ValidadorDatos.NombreMuyLargo, ValidadorDatos.ErrorNombre(new string('x', 101)));
        });
        e.Caso("Las seis regiones en el orden del catálogo", () =>
            Afirmar.Igual("América,Europa,Asia,África,Oceanía,Antártida", string.Join(",", ValidadorDatos.Regiones)));
        e.Caso("Población con separador de miles", () =>
        {
            Afirmar.Igual("18.001.000", Formato.Poblacion(18001000));
            Afirmar.Igual("0", Formato.Poblacion(0));
            Afirmar.Igual("1.408.280.000", Formato.Poblacion(1408280000));
        });
        e.Caso("Total en singular y plural", () =>
        {
            Afirmar.Igual("1 país encontrado", Formato.Total(1));
            Afirmar.Igual("3 países encontrados", Formato.Total(3));
        });
        e.Caso("Sí/No y texto vacío como guion", () =>
        {
            Afirmar.Igual("Sí", Formato.SiNo(true));
            Afirmar.Igual("No", Formato.SiNo(false));
            Afirmar.Igual("—", Formato.Texto(" "));
        });
        e.Caso("Parámetros de consulta: omite vacíos y codifica", () =>
        {
            Afirmar.Igual("", Endpoints.Consulta(Endpoints.Parametro("nombre", " "), Endpoints.Parametro("region", null)));
            Afirmar.Igual("?region=%C3%81frica", Endpoints.Consulta(Endpoints.Parametro("nombre", ""), Endpoints.Parametro("region", "África")));
        });

        return e;
    }
}
