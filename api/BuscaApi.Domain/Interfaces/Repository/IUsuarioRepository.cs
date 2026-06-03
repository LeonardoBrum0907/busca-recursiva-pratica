using BuscaApi.Domain.Models.Data;

namespace BuscaApi.Domain.Interfaces.Repository;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByAuthKeyAsync(Guid authKey);
}
