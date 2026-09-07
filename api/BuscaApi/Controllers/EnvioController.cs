using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuscaApi.Controllers;

[ApiController]
[Route("api/envio")]
public class EnvioController(IEnvioService envioService) : ControllerBase
{
    /// <summary>
    /// Lista o conteudo de uma pasta: filhos diretos e envios daquele nivel.
    /// Nao ha parametro de termo — a filtragem textual acontece no frontend.
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
    /// Devolve totais da conta (quantidade de envios/arquivos no banco).
    /// </summary>
    [HttpGet("conta/{externalKeyConta:guid}/totais")]
    public async Task<ActionResult<TotaisConta>> ObterTotais(Guid externalKeyConta)
    {
        return Ok(await envioService.ObterTotaisAsync(externalKeyConta));
    }
}
