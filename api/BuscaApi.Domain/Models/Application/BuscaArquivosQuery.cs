namespace BuscaApi.Domain.Models.Application;

public class BuscaArquivosQuery
{
    public Guid ExternalKeyConta { get; set; }
    public Guid? ExternalKeyPasta { get; set; }
    public string Termo { get; set; } = string.Empty;
    public bool IncluirPastas { get; set; } = true;
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 50;
}
