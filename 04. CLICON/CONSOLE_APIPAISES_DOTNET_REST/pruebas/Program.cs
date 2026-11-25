using CONSOLE_APIPAISES_DOTNET_REST.Pruebas;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var suites = new List<(string Titulo, Ejecutor Ejecutor)>
{
    ("Validador de datos y formato", PruebasValidador.Crear()),
    ("Controladores con servidor simulado", PruebasControladores.Crear()),
};

int total = 0, fallos = 0;
foreach (var (titulo, ejecutor) in suites)
{
    var (t, f) = await ejecutor.EjecutarAsync(titulo);
    total += t;
    fallos += f;
}

Console.WriteLine($"\nResultado: {total - fallos}/{total} pruebas correctas, {fallos} fallidas.");
return fallos == 0 ? 0 : 1;
