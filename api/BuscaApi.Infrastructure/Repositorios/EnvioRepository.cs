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
}
