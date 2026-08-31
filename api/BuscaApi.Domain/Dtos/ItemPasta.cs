namespace BuscaApi.Domain.Dtos;

/// <summary>
/// Item exibido na listagem do diretorio: uma pasta filha ou um envio da pasta aberta.
/// </summary>
public class ItemPasta
{
    public Guid ExternalKey { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Pasta" ou "Envio"
    public string Nome { get; set; } = string.Empty;
    public string? StatusEnvio { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public string? NomeDestinatarios { get; set; }
}
