using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Interfaces.Services;
using BuscaApi.Domain.Models.Application;
using Microsoft.AspNetCore.Mvc;

namespace BuscaApi.Controllers;

[ApiController]
[Route("api/envio")]
public class EnvioController(IEnvioService envioService) : ControllerBase
{
    /// <summary>
    /// Lista ou filtra pastas e envios do nivel atual. A arvore da conta e carregada
    /// em memoria; a busca textual nao percorre subpastas.
    /// </summary>
    [HttpGet("conta/{externalKeyConta:guid}/buscar")]
    public async Task<ActionResult<Paginacao<ArquivoBuscaResultado>>> BuscarArquivos(
        Guid externalKeyConta,
        [FromQuery] string? termo,
        [FromQuery] Guid? externalKeyPasta = null,
        [FromQuery] bool incluirPastas = true,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 50)
    {
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = externalKeyConta,
            ExternalKeyPasta = externalKeyPasta,
            Termo = termo ?? string.Empty,
            IncluirPastas = incluirPastas,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };

        var resultado = await envioService.BuscarArquivosAsync(query);
        return Ok(resultado);
    }
}
