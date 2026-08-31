using BuscaApi.Infrastructure.Busca;
using Xunit;

namespace BuscaApi.Testes;

public class FiltroNivelAtualTests
{
    private static readonly PastaCarregada Clientes = new()
    {
        IdPasta = 1, IdPastaPai = null, ExternalKey = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        NomePasta = "Clientes"
    };
    private static readonly PastaCarregada Ano2026 = new()
    {
        IdPasta = 2, IdPastaPai = 1, ExternalKey = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        NomePasta = "2026"
    };
    private static readonly PastaCarregada Acme = new()
    {
        IdPasta = 3, IdPastaPai = 2, ExternalKey = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        NomePasta = "ACME"
    };
    private static readonly PastaCarregada Financeiro = new()
    {
        IdPasta = 7, IdPastaPai = null, ExternalKey = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        NomePasta = "Financeiro"
    };
    private static readonly PastaCarregada Contratos = new()
    {
        IdPasta = 8, IdPastaPai = 7, ExternalKey = Guid.Parse("88888888-8888-8888-8888-888888888888"),
        NomePasta = "Contratos"
    };

    private static readonly IReadOnlyList<PastaCarregada> Arvore =
    [
        Clientes, Ano2026, Acme, Financeiro, Contratos
    ];

    private static readonly IReadOnlyList<EnvioCarregado> Envios =
    [
        new()
        {
            IdPasta = 3,
            ExternalKey = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Assunto = "Contrato ACME 2026",
            NomeDestinatarios = "Joao"
        },
        new()
        {
            IdPasta = 8,
            ExternalKey = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Assunto = "Contrato de Locacao",
            NomeDestinatarios = "Maria"
        }
    ];

    [Fact(DisplayName = "Busca na raiz nao encontra envios em subpastas")]
    public void FiltrarNivel_BuscaNaRaiz_NaoDesceAArvore()
    {
        var permitidas = FiltroNivelAtual.ResolverPastasPermitidas(Arvore, idsVinculados: null);

        var (itens, total) = FiltroNivelAtual.FiltrarNivel(
            Arvore, Envios, permitidas, idPastaAtual: null, termo: "contrato",
            incluirPastas: true, pagina: 1, tamanhoPagina: 50);

        Assert.Equal(0, total);
        Assert.Empty(itens);
    }

    [Fact(DisplayName = "Busca na pasta atual encontra so os envios daquele nivel")]
    public void FiltrarNivel_BuscaNaPastaAcme_RetornaEnvioLocal()
    {
        var permitidas = FiltroNivelAtual.ResolverPastasPermitidas(Arvore, idsVinculados: null);

        var (itens, total) = FiltroNivelAtual.FiltrarNivel(
            Arvore, Envios, permitidas, idPastaAtual: 3, termo: "contrato",
            incluirPastas: true, pagina: 1, tamanhoPagina: 50);

        Assert.Equal(1, total);
        Assert.Equal("Contrato ACME 2026", itens[0].Nome);
        Assert.Equal("Clientes > 2026 > ACME", itens[0].Caminho);
    }

    [Fact(DisplayName = "Navegacao na raiz lista apenas pastas-raiz")]
    public void FiltrarNivel_NavegacaoRaiz_ListaFilhosDiretos()
    {
        var permitidas = FiltroNivelAtual.ResolverPastasPermitidas(Arvore, idsVinculados: null);

        var (itens, total) = FiltroNivelAtual.FiltrarNivel(
            Arvore, Envios, permitidas, idPastaAtual: null, termo: "",
            incluirPastas: true, pagina: 1, tamanhoPagina: 50);

        Assert.Equal(2, total);
        Assert.Equal(["Clientes", "Financeiro"], itens.Select(i => i.Nome).ToArray());
    }

    [Fact(DisplayName = "Usuario restrito so enxerga o ramo vinculado e descendentes")]
    public void ResolverPastasPermitidas_NaoAdmin_ExpandeAPartirDoVinculo()
    {
        var permitidas = FiltroNivelAtual.ResolverPastasPermitidas(Arvore, idsVinculados: [7]);

        Assert.Contains(7, permitidas);
        Assert.Contains(8, permitidas);
        Assert.DoesNotContain(1, permitidas);
        Assert.DoesNotContain(3, permitidas);
    }
}
