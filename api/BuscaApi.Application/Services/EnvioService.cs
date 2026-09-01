using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Exceptions;
using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Interfaces.Services;
using BuscaApi.Domain.Models.Application;
using BuscaApi.Domain.Models.Data;

namespace BuscaApi.Application.Services;

public class EnvioService(
    IContaRepository contaRepository,
    IPastaRepository pastaRepository,
    IUsuarioRepository usuarioRepository,
    IEnvioRepository envioRepository,
    IAuthenticatedUser authenticatedUser) : IEnvioService
{
    private const int TamanhoPaginaMaximo = 200;

    public async Task<IReadOnlyList<ItemPasta>> ListarItensAsync(Guid externalKeyConta, Guid? externalKeyPasta)
    {
        var conta = await ResolverContaAsync(externalKeyConta);
        var idPasta = await ResolverIdPastaAsync(externalKeyPasta);
        var idUsuarioFiltro = await ResolverIdUsuarioFiltroAsync(conta);

        return await envioRepository.ListarItensAsync(conta.IdConta, idPasta, idUsuarioFiltro);
    }

    public async Task<Paginacao<ArquivoBuscaResultado>> BuscarArquivosAsync(BuscaArquivosQuery query)
    {
        var conta = await ResolverContaAsync(query.ExternalKeyConta);

        var termo = query.Termo.Trim();
        if (termo.Length == 0)
            return PaginacaoVazia(query);

        var idPastaRaiz = await ResolverIdPastaAsync(query.ExternalKeyPasta);
        var idUsuarioFiltro = await ResolverIdUsuarioFiltroAsync(conta);

        var pagina = Math.Max(1, query.Pagina);
        var tamanhoPagina = Math.Clamp(query.TamanhoPagina, 1, TamanhoPaginaMaximo);

        var (itens, total) = await envioRepository.BuscarArquivosAsync(
            conta.IdConta,
            idPastaRaiz,
            termo,
            query.IncluirPastas,
            pagina,
            tamanhoPagina,
            idUsuarioFiltro);

        return new Paginacao<ArquivoBuscaResultado>
        {
            PaginaSelecionada = pagina,
            TamanhoPagina = tamanhoPagina,
            Total = total,
            Dados = itens.ToList()
        };
    }

    private async Task<Conta> ResolverContaAsync(Guid externalKeyConta) =>
        await contaRepository.GetByExternalKeyAsync(externalKeyConta)
        ?? throw new DataNotFoundException("Conta não encontrada");

    private async Task<int?> ResolverIdPastaAsync(Guid? externalKeyPasta)
    {
        if (!externalKeyPasta.HasValue)
            return null;

        var pasta = await pastaRepository.GetByExternalKeyAsync(externalKeyPasta.Value)
                    ?? throw new DataNotFoundException("Pasta não encontrada");

        return pasta.IdPasta;
    }

    /// <summary>
    /// Admin enxerga a conta inteira (filtro nulo). Para os demais, devolve o id do
    /// usuario para a query restringir a subarvore as pastas vinculadas.
    /// </summary>
    private async Task<int?> ResolverIdUsuarioFiltroAsync(Conta conta)
    {
        if (authenticatedUser.IsAdmin(conta.IdConta))
            return null;

        var authKey = authenticatedUser.AuthKey
                      ?? throw new DataNotFoundException("Usuário não autenticado");

        var usuario = await usuarioRepository.GetByAuthKeyAsync(authKey);
        if (usuario is null || usuario.IdConta != conta.IdConta)
            throw new DataNotFoundException("Usuário não encontrado para a conta");

        return usuario.IdUsuario;
    }

    private static Paginacao<ArquivoBuscaResultado> PaginacaoVazia(BuscaArquivosQuery query) => new()
    {
        PaginaSelecionada = Math.Max(1, query.Pagina),
        TamanhoPagina = Math.Clamp(query.TamanhoPagina, 1, TamanhoPaginaMaximo),
        Total = 0,
        Dados = []
    };
}
