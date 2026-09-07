using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Infrastructure.Data;
using BuscaApi.Infrastructure.Repositorios.Queries;
using Dapper;

namespace BuscaApi.Infrastructure.Repositorios;

public class EnvioRepository(DapperContext dapperContext) : IEnvioRepository
{
    public async Task<IReadOnlyList<ItemPasta>> ListarItensAsync(
        int idConta, int? idPasta, int? idUsuarioFiltro = null)
    {
        using var connection = dapperContext.CreateConnection();

        var itens = await connection.QueryAsync<ItemPasta>(QueriesEnvio.QueryItensDoNivel, new
        {
            idConta,
            idPasta,
            idUsuarioFiltro
        });

        return itens.ToList();
    }

    public async Task<(IReadOnlyList<ArquivoBuscaResultado> Itens, int Total)> BuscarArquivosAsync(
        int idConta, int? idPastaRaiz, string termo, bool incluirPastas,
        int pagina, int tamanhoPagina, int? idUsuarioFiltro = null)
    {
        using var connection = dapperContext.CreateConnection();

        var rows = (await connection.QueryAsync<dynamic>(QueriesEnvio.QueryBuscaArquivos, new
        {
            idConta,
            idPastaRaiz,
            termo,
            incluirPastas,
            idUsuarioFiltro,
            tamanhoPagina,
            offset = (pagina - 1) * tamanhoPagina
        })).ToList();

        // COUNT(*) OVER () volta como bigint; so existe se houver ao menos uma linha.
        var total = rows.Count == 0 ? 0 : Convert.ToInt32(rows[0].total);

        var itens = rows.Select(r => new ArquivoBuscaResultado
        {
            ExternalKey = (Guid)r.externalkey,
            Tipo = (string)r.tipo,
            Nome = (string)r.nome,
            Caminho = (string)r.caminho,
            ExternalKeyPastaPai = r.externalkeypastapai is null ? null : (Guid?)r.externalkeypastapai,
            StatusEnvio = (string?)r.statusenvio,
            DataCriacao = (DateTime?)r.datacriacao,
            DataExpiracao = (DateTime?)r.dataexpiracao,
            NomeDestinatarios = (string?)r.nomedestinatarios
        }).ToList();

        return (itens, total);
    }

    public async Task<int> ContarArquivosAsync(int idConta)
    {
        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(QueriesEnvio.QueryContarArquivos, new { idConta });
    }
}
