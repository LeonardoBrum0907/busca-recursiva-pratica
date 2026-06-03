using BuscaApi.Domain.Models.Data;

namespace BuscaApi.Domain.Interfaces.Repository;

public interface IContaRepository
{
    Task<Conta?> GetByExternalKeyAsync(Guid externalKey);
}
