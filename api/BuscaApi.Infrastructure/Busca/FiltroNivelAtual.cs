using BuscaApi.Domain.Dtos;

namespace BuscaApi.Infrastructure.Busca;

public sealed class PastaCarregada
{
    public int IdPasta { get; init; }
    public int? IdPastaPai { get; init; }
    public Guid ExternalKey { get; init; }
    public string NomePasta { get; init; } = string.Empty;
}

public sealed class EnvioCarregado
{
    public int IdPasta { get; init; }
    public Guid ExternalKey { get; init; }
    public string Assunto { get; init; } = string.Empty;
    public string? StatusEnvio { get; init; }
    public DateTime? DataCriacao { get; init; }
    public DateTime? DataExpiracao { get; init; }
    public string NomeDestinatarios { get; init; } = string.Empty;
}

/// <summary>
/// Filtra em memoria apenas o nivel atual da arvore ja carregada.
/// Nao desce para subpastas — esse e o comportamento propositalmente ruim.
/// </summary>
public static class FiltroNivelAtual
{
    public static HashSet<int> ResolverPastasPermitidas(
        IReadOnlyList<PastaCarregada> pastas,
        IReadOnlyList<int>? idsVinculados)
    {
        if (idsVinculados is null)
            return pastas.Select(p => p.IdPasta).ToHashSet();

        var filhosPorPai = pastas.ToLookup(p => p.IdPastaPai);
        var permitidas = new HashSet<int>();
        var fila = new Queue<int>(idsVinculados);

        while (fila.Count > 0)
        {
            var id = fila.Dequeue();
            if (!permitidas.Add(id))
                continue;

            foreach (var filho in filhosPorPai[id])
                fila.Enqueue(filho.IdPasta);
        }

        return permitidas;
    }

    public static (IReadOnlyList<ArquivoBuscaResultado> Itens, int Total) FiltrarNivel(
        IReadOnlyList<PastaCarregada> pastas,
        IReadOnlyList<EnvioCarregado> envios,
        HashSet<int> permitidas,
        int? idPastaAtual,
        string termo,
        bool incluirPastas,
        int pagina,
        int tamanhoPagina)
    {
        var porId = pastas.ToDictionary(p => p.IdPasta);
        var termoLimpo = termo.Trim();
        var buscando = termoLimpo.Length > 0;

        var resultado = new List<ArquivoBuscaResultado>();

        if (incluirPastas)
        {
            foreach (var pasta in pastas)
            {
                if (!permitidas.Contains(pasta.IdPasta))
                    continue;
                if (pasta.IdPastaPai != idPastaAtual)
                    continue;
                if (buscando && !ContemTermo(pasta.NomePasta, termoLimpo))
                    continue;

                Guid? keyPai = null;
                if (pasta.IdPastaPai is int idPai && porId.TryGetValue(idPai, out var pai))
                    keyPai = pai.ExternalKey;

                resultado.Add(new ArquivoBuscaResultado
                {
                    ExternalKey = pasta.ExternalKey,
                    Tipo = "Pasta",
                    Nome = pasta.NomePasta,
                    Caminho = MontarCaminho(pasta.IdPasta, porId),
                    ExternalKeyPastaPai = keyPai
                });
            }
        }

        if (idPastaAtual is int idPastaEnvios)
        {
            foreach (var envio in envios)
            {
                if (envio.IdPasta != idPastaEnvios)
                    continue;
                if (!permitidas.Contains(envio.IdPasta))
                    continue;
                if (buscando && !ContemTermo(envio.Assunto, termoLimpo))
                    continue;

                var pastaDoEnvio = porId.GetValueOrDefault(envio.IdPasta);
                resultado.Add(new ArquivoBuscaResultado
                {
                    ExternalKey = envio.ExternalKey,
                    Tipo = "Envio",
                    Nome = envio.Assunto,
                    Caminho = pastaDoEnvio is null ? string.Empty : MontarCaminho(envio.IdPasta, porId),
                    ExternalKeyPastaPai = pastaDoEnvio?.ExternalKey,
                    StatusEnvio = envio.StatusEnvio,
                    DataCriacao = envio.DataCriacao,
                    DataExpiracao = envio.DataExpiracao,
                    NomeDestinatarios = envio.NomeDestinatarios
                });
            }
        }

        var ordenados = resultado
            .OrderBy(item => Relevancia(item, termoLimpo, buscando))
            .ThenBy(item => item.Caminho, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Tipo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = ordenados.Count;
        var offset = (pagina - 1) * tamanhoPagina;
        var paginaItens = ordenados.Skip(offset).Take(tamanhoPagina).ToList();
        return (paginaItens, total);
    }

    internal static bool ContemTermo(string texto, string termo) =>
        texto.Contains(termo, StringComparison.OrdinalIgnoreCase);

    internal static string MontarCaminho(int idPasta, IReadOnlyDictionary<int, PastaCarregada> porId)
    {
        var partes = new List<string>();
        int? atual = idPasta;
        var guard = 0;

        while (atual is int id && porId.TryGetValue(id, out var pasta) && guard++ < 64)
        {
            partes.Add(pasta.NomePasta);
            atual = pasta.IdPastaPai;
        }

        partes.Reverse();
        return string.Join(" > ", partes);
    }

    private static int Relevancia(ArquivoBuscaResultado item, string termo, bool buscando)
    {
        if (!buscando)
            return item.Tipo == "Pasta" ? 0 : 1;

        if (string.Equals(item.Nome, termo, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (item.Nome.StartsWith(termo, StringComparison.OrdinalIgnoreCase))
            return 1;
        return 2;
    }
}
