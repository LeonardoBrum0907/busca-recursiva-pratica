namespace BuscaApi.Domain.Dtos;

public class ArquivoBuscaResultado
{
    public Guid ExternalKey { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Pasta" ou "Envio"
    public string Nome { get; set; } = string.Empty;
    public string Caminho { get; set; } = string.Empty;
    public Guid? ExternalKeyPastaPai { get; set; }
    public string? StatusEnvio { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public string? NomeDestinatarios { get; set; }
}
