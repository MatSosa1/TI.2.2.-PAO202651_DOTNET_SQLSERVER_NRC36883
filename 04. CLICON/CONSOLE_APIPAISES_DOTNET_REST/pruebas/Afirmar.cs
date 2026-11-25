namespace CONSOLE_APIPAISES_DOTNET_REST.Pruebas;

public class FalloPrueba(string mensaje) : Exception(mensaje);

public static class Afirmar
{
    public static void Igual<T>(T esperado, T actual, string contexto = "")
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, actual))
            throw new FalloPrueba($"{contexto} esperado «{esperado}», obtenido «{actual}»".Trim());
    }

    public static void Verdadero(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new FalloPrueba(mensaje);
    }

    public static void Falso(bool condicion, string mensaje) => Verdadero(!condicion, mensaje);

    public static void Nulo(object? valor, string contexto = "")
        => Verdadero(valor is null, $"{contexto} esperado null, obtenido «{valor}»".Trim());
}

/// <summary>Ejecuta los casos de una suite y muestra OK o FALLA por cada uno.</summary>
public class Ejecutor
{
    private readonly List<(string Nombre, Func<Task> Prueba)> casos = [];

    public void Caso(string nombre, Action prueba) => casos.Add((nombre, () => { prueba(); return Task.CompletedTask; }));

    public void Caso(string nombre, Func<Task> prueba) => casos.Add((nombre, prueba));

    public async Task<(int Total, int Fallos)> EjecutarAsync(string titulo)
    {
        Console.WriteLine($"\n== {titulo} ({casos.Count} casos)");
        int fallos = 0;
        foreach (var (nombre, prueba) in casos)
        {
            try
            {
                await prueba();
                Console.WriteLine($"  OK    {nombre}");
            }
            catch (Exception ex)
            {
                fallos++;
                string detalle = ex is FalloPrueba ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";
                Console.WriteLine($"  FALLA {nombre}\n        {detalle}");
            }
        }
        return (casos.Count, fallos);
    }
}
