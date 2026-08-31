using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Infrastructure.Busca;
using BuscaApi.Infrastructure.Data;
using BuscaApi.Infrastructure.Repositorios.Queries;
using Dapper;

namespace BuscaApi.Infrastructure.Repositorios;

public class EnvioRepository(DapperContext dapperContext) : IEnvioRepository
{
    public async Task<(IReadOnlyList<ArquivoBuscaResultado> Itens, int Total)> BuscarArquivosAsync(
        int idConta, int? idPastaRaiz, string termo, bool incluirPastas,
        int pagina, int tamanhoPagina, int? idUsuarioFiltro = null)
    {
        using var connection = dapperContext.CreateConnection();

        // Carrega a arvore inteira da conta em memoria (propositalmente caro).
        var pastas = (await connection.QueryAsync<PastaCarregada>(
            QueriesEnvio.QueryPastasDaConta, new { idConta })).ToList();

        var envios = (await connection.QueryAsync<EnvioCarregado>(
            QueriesEnvio.QueryEnviosDaConta, new { idConta })).ToList();

        IReadOnlyList<int>? vinculos = null;
        if (idUsuarioFiltro is int idUsuario)
        {
            vinculos = (await connection.QueryAsync<int>(
                QueriesEnvio.QueryPastasVinculadas, new { idUsuario })).ToList();
        }

        var permitidas = FiltroNivelAtual.ResolverPastasPermitidas(pastas, vinculos);

        return FiltroNivelAtual.FiltrarNivel(
            pastas,
            envios,
            permitidas,
            idPastaRaiz,
            termo,
            incluirPastas,
            pagina,
            tamanhoPagina);
    }
}
