namespace BuscaApi.Domain.Models.Data;

public class Envio
{
    public int IdEnvio { get; set; }
    public int IdPasta { get; set; }
    public int IdConta { get; set; }
    public Guid ExternalKey { get; set; }
    public string Assunto { get; set; } = string.Empty;
    public string? StatusEnvio { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public int StatusRegistro { get; set; }
}
