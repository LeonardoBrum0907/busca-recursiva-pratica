using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Exceptions;
using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Interfaces.Services;
using BuscaApi.Domain.Models.Application;

namespace BuscaApi.Application.Services;

public class EnvioService(
    IContaRepository contaRepository,
    IPastaRepository pastaRepository,
    IUsuarioRepository usuarioRepository,
    IEnvioRepository envioRepository,
    IAuthenticatedUser authenticatedUser) : IEnvioService
{
    public async Task<Paginacao<ArquivoBuscaResultado>> BuscarArquivosAsync(BuscaArquivosQuery query)
    {
        var conta = await contaRepository.GetByExternalKeyAsync(query.ExternalKeyConta)
                    ?? throw new DataNotFoundException("Conta não encontrada");

        // Termo vazio = navegacao: filhos diretos da pasta atual + envios nela.
        // Termo preenchido = busca textual apenas no nivel atual (nao desce a arvore).

        int? idPastaRaiz = null;
        if (query.ExternalKeyPasta.HasValue)
        {
            var pasta = await pastaRepository.GetByExternalKeyAsync(query.ExternalKeyPasta.Value)
                        ?? throw new DataNotFoundException("Pasta não encontrada");
            idPastaRaiz = pasta.IdPasta;
        }

        int? idUsuarioFiltro = null;
        if (!authenticatedUser.IsAdmin(conta.IdConta))
        {
            var authKey = authenticatedUser.AuthKey
                          ?? throw new DataNotFoundException("Usuário não autenticado");

            var usuario = await usuarioRepository.GetByAuthKeyAsync(authKey);
            if (usuario is null || usuario.IdConta != conta.IdConta)
                throw new DataNotFoundException("Usuário não encontrado para a conta");

            idUsuarioFiltro = usuario.IdUsuario;
        }

        var (itens, total) = await envioRepository.BuscarArquivosAsync(
            conta.IdConta,
            idPastaRaiz,
            query.Termo.Trim(),
            query.IncluirPastas,
            Math.Max(1, query.Pagina),
            Math.Clamp(query.TamanhoPagina, 1, 200),
            idUsuarioFiltro);

        return new Paginacao<ArquivoBuscaResultado>
        {
            PaginaSelecionada = query.Pagina,
            TamanhoPagina = query.TamanhoPagina,
            Total = total,
            Dados = itens.ToList()
        };
    }
}
