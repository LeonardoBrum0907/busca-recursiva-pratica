namespace BuscaApi.Domain.Dtos;

/// <summary>
/// Resultado da busca recursiva. Diferente do <see cref="ItemPasta"/>, carrega o
/// caminho completo do item na arvore e a pasta-pai, porque o item pode estar em
/// qualquer profundidade.
/// </summary>
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
