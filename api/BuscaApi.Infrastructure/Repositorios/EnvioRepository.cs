using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Interfaces.Repository;
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

        var offset = (pagina - 1) * tamanhoPagina;

        var rows = await connection.QueryAsync<dynamic>(QueriesEnvio.QueryBuscaArquivos, new
        {
            idConta,
            idPastaRaiz,
            termo,
            incluirPastas,
            idUsuarioFiltro,
            tamanhoPagina,
            offset
        });

        var list = rows.ToList();

        // COUNT(*) OVER () retorna bigint -> long no Dapper dynamic.
        var total = list.Count == 0 ? 0 : Convert.ToInt32(list[0].total);

        var itens = list.Select(r => new ArquivoBuscaResultado
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
}
