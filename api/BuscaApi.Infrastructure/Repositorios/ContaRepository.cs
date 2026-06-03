using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Models.Data;
using BuscaApi.Infrastructure.Data;
using Dapper;

namespace BuscaApi.Infrastructure.Repositorios;

public class ContaRepository(DapperContext dapperContext) : IContaRepository
{
    public async Task<Conta?> GetByExternalKeyAsync(Guid externalKey)
    {
        using var connection = dapperContext.CreateConnection();

        const string sql = @"
SELECT idconta     AS IdConta,
       externalkey AS ExternalKey,
       nome        AS Nome
FROM conta
WHERE externalkey = @externalKey";

        return await connection.QueryFirstOrDefaultAsync<Conta>(sql, new { externalKey });
    }
}
