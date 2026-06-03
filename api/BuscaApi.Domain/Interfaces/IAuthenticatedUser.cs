namespace BuscaApi.Domain.Interfaces;

public interface IAuthenticatedUser
{
    Guid? AuthKey { get; }

    bool IsAdmin(int idConta);
}
