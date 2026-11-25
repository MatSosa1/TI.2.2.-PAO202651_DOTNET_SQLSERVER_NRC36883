using Microsoft.AspNetCore.Mvc;
using WS_APIPAISES_DOTNET_REST.DAO;
using WS_APIPAISES_DOTNET_REST.Models;
using WS_APIPAISES_DOTNET_REST.Models.DTO;
using WS_APIPAISES_DOTNET_REST.Vista;

namespace WS_APIPAISES_DOTNET_REST.Controllers;

/// <summary>Informes estadísticos (RQF-007, ECUD7). No dependen de la API pública.</summary>
[Route("api/Reportes")]
public class ReportesController : ControladorBase
{
    private readonly PaisDAO paisDAO;

    public ReportesController(PaisDAO paisDAO, ILogger<ReportesController> log) : base(log)
    {
        this.paisDAO = paisDAO;
    }

    /// <summary>
    /// GET /api/Reportes: países por región, país más poblado y promedio poblacional por región.
    /// Sin países guardados responde estado ERROR con HTTP 200, como indica el diagrama de
    /// secuencia Generar_Informes.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> Generar()
    {
        return ResponderAsync("generarInformes", async () =>
        {
            if (await paisDAO.ContarAsync() == 0)
            {
                return JsonResponseView.Error(StatusCodes.Status200OK, Mensajes.SinPaisesInformes);
            }
            var informe = new InformeEstadistico(await paisDAO.ContarPorRegionAsync(),
                await paisDAO.PaisMasPobladoAsync(), await paisDAO.PromedioPorRegionAsync());
            return JsonResponseView.Ok(Mensajes.InformesGenerados, informe);
        });
    }
}
