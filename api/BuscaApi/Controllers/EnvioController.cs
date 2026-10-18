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
    /// Lista o conteudo de uma pasta: filhos diretos e envios daquele nivel, paginado.
    /// </summary>
    [HttpGet("conta/{externalKeyConta:guid}/itens")]
    public async Task<ActionResult<Paginacao<ItemPasta>>> ListarItens(
        Guid externalKeyConta,
        [FromQuery] Guid? externalKeyPasta = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 50)
    {
        return Ok(await envioService.ListarItensAsync(
            externalKeyConta, externalKeyPasta, pagina, tamanhoPagina));
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

    /// <summary>
    /// Devolve totais da conta (quantidade de envios/arquivos no banco).
    /// </summary>
    [HttpGet("conta/{externalKeyConta:guid}/totais")]
    public async Task<ActionResult<TotaisConta>> ObterTotais(Guid externalKeyConta)
    {
        return Ok(await envioService.ObterTotaisAsync(externalKeyConta));
    }
}
