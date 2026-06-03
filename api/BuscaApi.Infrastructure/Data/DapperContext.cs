using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BuscaApi.Infrastructure.Data;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionString 'Postgres' não configurada.");

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
