using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Models.Application;

namespace BuscaApi.Domain.Interfaces.Services;

public interface IEnvioService
{
    Task<Paginacao<ItemPasta>> ListarItensAsync(
        Guid externalKeyConta, Guid? externalKeyPasta, int pagina, int tamanhoPagina);

    Task<Paginacao<ArquivoBuscaResultado>> BuscarArquivosAsync(BuscaArquivosQuery query);

    Task<TotaisConta> ObterTotaisAsync(Guid externalKeyConta);
}
