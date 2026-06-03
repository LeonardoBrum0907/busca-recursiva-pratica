namespace BuscaApi.Domain.Models.Data;

public class Destinatario
{
    public int IdDestinatario { get; set; }
    public int IdEnvio { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
}
