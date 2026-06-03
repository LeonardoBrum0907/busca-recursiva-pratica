namespace BuscaApi.Domain.Models.Data;

public class Conta
{
    public int IdConta { get; set; }
    public Guid ExternalKey { get; set; }
    public string Nome { get; set; } = string.Empty;
}
