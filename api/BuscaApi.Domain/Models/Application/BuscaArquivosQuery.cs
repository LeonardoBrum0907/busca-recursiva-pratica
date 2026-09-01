namespace BuscaApi.Domain.Models.Application;

/// <summary>
/// Parametros de entrada da busca: termo, escopo (pasta de partida) e paginacao.
/// </summary>
public class BuscaArquivosQuery
{
    public Guid ExternalKeyConta { get; set; }

    /// <summary>Pasta de onde a busca parte. Nulo = raiz da conta.</summary>
    public Guid? ExternalKeyPasta { get; set; }

    public string Termo { get; set; } = string.Empty;
    public bool IncluirPastas { get; set; } = true;
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 50;
}
