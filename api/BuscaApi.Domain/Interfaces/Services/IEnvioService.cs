using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Models.Application;

namespace BuscaApi.Domain.Interfaces.Services;

public interface IEnvioService
{
    Task<Paginacao<ArquivoBuscaResultado>> BuscarArquivosAsync(BuscaArquivosQuery query);
}
