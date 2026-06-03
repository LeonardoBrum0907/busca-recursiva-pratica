using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Models.Data;
using BuscaApi.Infrastructure.Data;
using Dapper;

namespace BuscaApi.Infrastructure.Repositorios;

public class PastaRepository(DapperContext dapperContext) : IPastaRepository
{
    public async Task<Pasta?> GetByExternalKeyAsync(Guid externalKey)
    {
        using var connection = dapperContext.CreateConnection();

        const string sql = @"
SELECT idpasta        AS IdPasta,
       idpastapai     AS IdPastaPai,
       externalkey    AS ExternalKey,
       nomepasta      AS NomePasta,
       idconta        AS IdConta,
       statusregistro AS StatusRegistro
FROM pasta
WHERE externalkey = @externalKey";

        return await connection.QueryFirstOrDefaultAsync<Pasta>(sql, new { externalKey });
    }
}
