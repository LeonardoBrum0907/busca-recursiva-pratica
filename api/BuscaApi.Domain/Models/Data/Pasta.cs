namespace BuscaApi.Domain.Models.Data;

public class Pasta
{
    public int IdPasta { get; set; }
    public int? IdPastaPai { get; set; }
    public Guid ExternalKey { get; set; }
    public string NomePasta { get; set; } = string.Empty;
    public int IdConta { get; set; }
    public int StatusRegistro { get; set; }
}
