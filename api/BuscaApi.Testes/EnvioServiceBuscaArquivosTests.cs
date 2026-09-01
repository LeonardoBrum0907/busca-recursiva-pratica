using BuscaApi.Application.Services;
using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Exceptions;
using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Models.Application;
using BuscaApi.Domain.Models.Data;
using Moq;
using Xunit;

namespace BuscaApi.Testes;

public class EnvioServiceBuscaArquivosTests
{
    private readonly Mock<IContaRepository> _contaRepoMock = new();
    private readonly Mock<IPastaRepository> _pastaRepoMock = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IEnvioRepository> _envioRepoMock = new();
    private readonly Mock<IAuthenticatedUser> _authUserMock = new();
    private readonly EnvioService _envioService;

    public EnvioServiceBuscaArquivosTests()
    {
        _envioService = new EnvioService(
            _contaRepoMock.Object,
            _pastaRepoMock.Object,
            _usuarioRepoMock.Object,
            _envioRepoMock.Object,
            _authUserMock.Object);
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException quando a conta da busca não existe")]
    public async Task BuscarArquivosAsync_ContaInexistente_LancaDataNotFoundException()
    {
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = Guid.NewGuid(),
            Termo = "contrato"
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Conta?)null);

        await Assert.ThrowsAsync<DataNotFoundException>(() => _envioService.BuscarArquivosAsync(query));
    }

    [Fact(DisplayName = "Deve retornar paginação vazia sem consultar o repositório quando o termo é vazio")]
    public async Task BuscarArquivosAsync_TermoVazio_NaoConsultaRepositorio()
    {
        var conta = new Conta { IdConta = 1, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            Termo = "   "
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);

        var resultado = await _envioService.BuscarArquivosAsync(query);

        Assert.Equal(0, resultado.Total);
        Assert.Empty(resultado.Dados);
        _envioRepoMock.Verify(
            x => x.BuscarArquivosAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact(DisplayName = "Deve repassar parâmetros ao repositório e devolver paginação populada (admin)")]
    public async Task BuscarArquivosAsync_TermoValido_RetornaResultadosDoRepositorio()
    {
        var conta = new Conta { IdConta = 42, ExternalKey = Guid.NewGuid() };
        var pasta = new Pasta { IdPasta = 7, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            ExternalKeyPasta = pasta.ExternalKey,
            Termo = "contrato",
            IncluirPastas = true,
            Pagina = 2,
            TamanhoPagina = 25
        };

        var itensRepo = new List<ArquivoBuscaResultado>
        {
            new()
            {
                ExternalKey = Guid.NewGuid(), Tipo = "Envio", Nome = "Contrato ACME",
                Caminho = "Clientes > 2026 > ACME"
            }
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _pastaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(pasta.ExternalKey))
            .ReturnsAsync(pasta);
        _authUserMock
            .Setup(x => x.IsAdmin(42))
            .Returns(true);
        _envioRepoMock
            .Setup(x => x.BuscarArquivosAsync(42, 7, "contrato", true, 2, 25, null))
            .ReturnsAsync(((IReadOnlyList<ArquivoBuscaResultado>)itensRepo, 137));

        var resultado = await _envioService.BuscarArquivosAsync(query);

        Assert.Equal(137, resultado.Total);
        Assert.Single(resultado.Dados);
        Assert.Equal("Contrato ACME", resultado.Dados[0].Nome);
        Assert.Equal("Clientes > 2026 > ACME", resultado.Dados[0].Caminho);
        Assert.Equal(2, resultado.PaginaSelecionada);
        Assert.Equal(25, resultado.TamanhoPagina);
        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(42, 7, "contrato", true, 2, 25, null), Times.Once);
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException quando a ExternalKeyPasta da busca não existe")]
    public async Task BuscarArquivosAsync_PastaInexistente_LancaDataNotFoundException()
    {
        var conta = new Conta { IdConta = 1, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            ExternalKeyPasta = Guid.NewGuid(),
            Termo = "contrato"
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _pastaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Pasta?)null);

        await Assert.ThrowsAsync<DataNotFoundException>(() => _envioService.BuscarArquivosAsync(query));
    }

    [Fact(DisplayName = "Deve repassar idUsuario ao repositório quando o usuário não é admin")]
    public async Task BuscarArquivosAsync_UsuarioNaoAdmin_RepassaIdUsuario()
    {
        var conta = new Conta { IdConta = 10, ExternalKey = Guid.NewGuid() };
        var usuario = new Usuario { IdUsuario = 99, AuthKey = Guid.NewGuid(), IdConta = 10 };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            Termo = "contrato"
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _authUserMock
            .Setup(x => x.IsAdmin(10))
            .Returns(false);
        _authUserMock
            .Setup(x => x.AuthKey)
            .Returns(usuario.AuthKey);
        _usuarioRepoMock
            .Setup(x => x.GetByAuthKeyAsync(usuario.AuthKey))
            .ReturnsAsync(usuario);
        _envioRepoMock
            .Setup(x => x.BuscarArquivosAsync(10, null, "contrato", true, 1, 50, 99))
            .ReturnsAsync(((IReadOnlyList<ArquivoBuscaResultado>)[], 0));

        await _envioService.BuscarArquivosAsync(query);

        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(10, null, "contrato", true, 1, 50, 99), Times.Once);
    }

    [Fact(DisplayName = "Deve limitar o tamanho de página a 200 para não deixar o cliente pedir a árvore inteira")]
    public async Task BuscarArquivosAsync_TamanhoPaginaAcimaDoLimite_EhLimitado()
    {
        var conta = new Conta { IdConta = 5, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            Termo = "contrato",
            TamanhoPagina = 100_000
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _authUserMock
            .Setup(x => x.IsAdmin(5))
            .Returns(true);
        _envioRepoMock
            .Setup(x => x.BuscarArquivosAsync(5, null, "contrato", true, 1, 200, null))
            .ReturnsAsync(((IReadOnlyList<ArquivoBuscaResultado>)[], 0));

        var resultado = await _envioService.BuscarArquivosAsync(query);

        Assert.Equal(200, resultado.TamanhoPagina);
        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(5, null, "contrato", true, 1, 200, null), Times.Once);
    }
}
