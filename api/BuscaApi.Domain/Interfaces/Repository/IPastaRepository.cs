using BuscaApi.Domain.Models.Data;

namespace BuscaApi.Domain.Interfaces.Repository;

public interface IPastaRepository
{
    Task<Pasta?> GetByExternalKeyAsync(Guid externalKey);
}
