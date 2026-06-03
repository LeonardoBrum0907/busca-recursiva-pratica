namespace BuscaApi.Domain.Models.Application;

public class Paginacao<T>
{
    public int TamanhoPagina { get; set; }
    public int PaginaSelecionada { get; set; }
    public int Total { get; set; }
    public List<T> Dados { get; set; } = [];
}
