using System.Text.Json;
using BuscaApi.Domain.Exceptions;

namespace BuscaApi.Middleware;

/// <summary>
/// Converte excecoes de dominio em respostas HTTP adequadas.
/// </summary>
public class ExcecaoMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DataNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { erro = ex.Message }));
        }
    }
}
