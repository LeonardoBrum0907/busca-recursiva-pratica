using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Models.Data;

namespace BuscaApi.Auth;

/// <summary>
/// Usuario autenticado simulado. Em vez de Keycloak/OIDC, o usuario corrente e
/// resolvido pelo header HTTP "X-Auth-Key" (ver <see cref="AuthSimuladaMiddleware"/>).
/// </summary>
public class AuthenticatedUser : IAuthenticatedUser
{
    private Usuario? _usuario;

    public Guid? AuthKey => _usuario?.AuthKey;

    public void DefinirUsuario(Usuario? usuario) => _usuario = usuario;

    public bool IsAdmin(int idConta) =>
        _usuario is not null && _usuario.Admin && _usuario.IdConta == idConta;
}
