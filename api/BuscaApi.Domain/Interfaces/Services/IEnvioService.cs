using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Models.Application;

namespace BuscaApi.Domain.Interfaces.Services;

public interface IEnvioService
{
    Task<IReadOnlyList<ItemPasta>> ListarItensAsync(Guid externalKeyConta, Guid? externalKeyPasta);

    Task<Paginacao<ArquivoBuscaResultado>> BuscarArquivosAsync(BuscaArquivosQuery query);
}
