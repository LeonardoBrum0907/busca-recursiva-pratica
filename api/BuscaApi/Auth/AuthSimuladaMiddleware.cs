using BuscaApi.Domain.Interfaces.Repository;

namespace BuscaApi.Auth;

/// <summary>
/// Le o header "X-Auth-Key" (GUID do usuario) e popula o <see cref="AuthenticatedUser"/>
/// do escopo atual da requisicao. Simula o contexto de usuario que, no projeto original,
/// vem do Keycloak (claim "Contexto").
/// </summary>
public class AuthSimuladaMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUsuarioRepository usuarioRepository, AuthenticatedUser authenticatedUser)
    {
        var header = context.Request.Headers["X-Auth-Key"].FirstOrDefault();

        if (Guid.TryParse(header, out var authKey))
        {
            var usuario = await usuarioRepository.GetByAuthKeyAsync(authKey);
            authenticatedUser.DefinirUsuario(usuario);
        }

        await next(context);
    }
}
