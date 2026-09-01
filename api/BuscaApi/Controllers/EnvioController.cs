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
    /// Lista o conteudo de uma pasta: filhos diretos e envios daquele nivel.
    /// </summary>
    [HttpGet("conta/{externalKeyConta:guid}/itens")]
    public async Task<ActionResult<IReadOnlyList<ItemPasta>>> ListarItens(
        Guid externalKeyConta,
        [FromQuery] Guid? externalKeyPasta = null)
    {
        var itens = await envioService.ListarItensAsync(externalKeyConta, externalKeyPasta);
        return Ok(itens);
    }

    /// <summary>
    /// Busca recursiva de pastas e envios cujo nome contem o termo, varrendo a
    /// subarvore inteira a partir de uma pasta (ou da raiz da conta).
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

        return Ok(await envioService.BuscarArquivosAsync(query));
    }
}
