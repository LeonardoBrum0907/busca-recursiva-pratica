using BuscaApi.Domain.Dtos;

namespace BuscaApi.Domain.Interfaces.Repository;

public interface IEnvioRepository
{
    /// <summary>
    /// Devolve os filhos diretos da pasta informada (ou as pastas-raiz da conta) mais
    /// os envios que estao nela. Nao recebe termo de busca.
    /// </summary>
    Task<IReadOnlyList<ItemPasta>> ListarItensAsync(
        int idConta,
        int? idPasta,
        int? idUsuarioFiltro = null);
}
