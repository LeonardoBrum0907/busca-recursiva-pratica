using BuscaApi.Domain.Dtos;

namespace BuscaApi.Domain.Interfaces.Repository;

public interface IEnvioRepository
{
    /// <summary>
    /// Devolve os filhos diretos da pasta informada (ou as pastas-raiz da conta) mais
    /// os envios que estao nela, uma pagina por vez. Usado pela navegacao.
    /// </summary>
    Task<(IReadOnlyList<ItemPasta> Itens, int Total)> ListarItensAsync(
        int idConta,
        int? idPasta,
        int pagina,
        int tamanhoPagina,
        int? idUsuarioFiltro = null);

    /// <summary>
    /// Busca pastas e envios cujo nome contem o termo, varrendo a subarvore inteira a
    /// partir de <paramref name="idPastaRaiz"/> (ou da raiz da conta).
    /// </summary>
    Task<(IReadOnlyList<ArquivoBuscaResultado> Itens, int Total)> BuscarArquivosAsync(
        int idConta,
        int? idPastaRaiz,
        string termo,
        bool incluirPastas,
        int pagina,
        int tamanhoPagina,
        int? idUsuarioFiltro = null);

    /// <summary>
    /// Conta todos os envios ativos da conta (independente de permissao de pasta).
    /// </summary>
    Task<int> ContarArquivosAsync(int idConta);
}
