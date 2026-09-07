using BuscaApi.Domain.Dtos;

namespace BuscaApi.Domain.Interfaces.Services;

public interface IEnvioService
{
    Task<IReadOnlyList<ItemPasta>> ListarItensAsync(Guid externalKeyConta, Guid? externalKeyPasta);

    Task<TotaisConta> ObterTotaisAsync(Guid externalKeyConta);
}
