using BuscaApi.Domain.Dtos;

namespace BuscaApi.Domain.Interfaces.Repository;

public interface IEnvioRepository
{
    Task<(IReadOnlyList<ArquivoBuscaResultado> Itens, int Total)> BuscarArquivosAsync(
        int idConta,
        int? idPastaRaiz,
        string termo,
        bool incluirPastas,
        int pagina,
        int tamanhoPagina,
        int? idUsuarioFiltro = null);
}
