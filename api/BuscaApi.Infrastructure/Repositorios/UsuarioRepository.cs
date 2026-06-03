using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Models.Data;
using BuscaApi.Infrastructure.Data;
using Dapper;

namespace BuscaApi.Infrastructure.Repositorios;

public class UsuarioRepository(DapperContext dapperContext) : IUsuarioRepository
{
    public async Task<Usuario?> GetByAuthKeyAsync(Guid authKey)
    {
        using var connection = dapperContext.CreateConnection();

        const string sql = @"
SELECT idusuario AS IdUsuario,
       authkey   AS AuthKey,
       nome      AS Nome,
       idconta   AS IdConta,
       admin     AS Admin
FROM usuario
WHERE authkey = @authKey";

        return await connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { authKey });
    }
}
