namespace BuscaApi.Domain.Models.Data;

public class Usuario
{
    public int IdUsuario { get; set; }
    public Guid AuthKey { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int IdConta { get; set; }
    public bool Admin { get; set; }
}
