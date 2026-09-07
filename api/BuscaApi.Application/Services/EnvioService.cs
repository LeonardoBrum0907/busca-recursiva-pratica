using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Exceptions;
using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Interfaces.Services;

namespace BuscaApi.Application.Services;

public class EnvioService(
    IContaRepository contaRepository,
    IPastaRepository pastaRepository,
    IUsuarioRepository usuarioRepository,
    IEnvioRepository envioRepository,
    IAuthenticatedUser authenticatedUser) : IEnvioService
{
    public async Task<IReadOnlyList<ItemPasta>> ListarItensAsync(Guid externalKeyConta, Guid? externalKeyPasta)
    {
        var conta = await contaRepository.GetByExternalKeyAsync(externalKeyConta)
                    ?? throw new DataNotFoundException("Conta não encontrada");

        int? idPasta = null;
        if (externalKeyPasta.HasValue)
        {
            var pasta = await pastaRepository.GetByExternalKeyAsync(externalKeyPasta.Value)
                        ?? throw new DataNotFoundException("Pasta não encontrada");
            idPasta = pasta.IdPasta;
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

        return await envioRepository.ListarItensAsync(conta.IdConta, idPasta, idUsuarioFiltro);
    }

    public async Task<TotaisConta> ObterTotaisAsync(Guid externalKeyConta)
    {
        var conta = await contaRepository.GetByExternalKeyAsync(externalKeyConta)
                    ?? throw new DataNotFoundException("Conta não encontrada");

        var totalArquivos = await envioRepository.ContarArquivosAsync(conta.IdConta);
        return new TotaisConta { TotalArquivos = totalArquivos };
    }
}
