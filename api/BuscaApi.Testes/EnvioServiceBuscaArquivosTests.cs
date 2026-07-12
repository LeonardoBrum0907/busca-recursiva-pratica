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
        // Arrange
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = Guid.NewGuid(),
            Termo = "contrato"
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Conta?)null);

        // Act + Assert
        await Assert.ThrowsAsync<DataNotFoundException>(() => _envioService.BuscarArquivosAsync(query));
    }

    [Fact(DisplayName = "Deve listar conteúdo da pasta (consultando o repositório com termo vazio) quando não há termo")]
    public async Task BuscarArquivosAsync_TermoVazio_ListaConteudoDaPasta()
    {
        // Arrange
        var conta = new Conta { IdConta = 1, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            Termo = "   "
        };

        var itensRepo = new List<ArquivoBuscaResultado>
        {
            new() { ExternalKey = Guid.NewGuid(), Tipo = "Pasta", Nome = "Financeiro",
                    Caminho = "Financeiro" }
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _authUserMock
            .Setup(x => x.IsAdmin(1))
            .Returns(true);
        _envioRepoMock
            .Setup(x => x.BuscarArquivosAsync(1, null, "", true, 1, 50, null))
            .ReturnsAsync(((IReadOnlyList<ArquivoBuscaResultado>)itensRepo, 1));

        // Act
        var resultado = await _envioService.BuscarArquivosAsync(query);

        // Assert
        Assert.Equal(1, resultado.Total);
        Assert.Single(resultado.Dados);
        Assert.Equal("Financeiro", resultado.Dados[0].Nome);
        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(1, null, "", true, 1, 50, null), Times.Once);
    }

    [Fact(DisplayName = "Deve repassar parâmetros ao repositório e devolver paginação populada (admin)")]
    public async Task BuscarArquivosAsync_TermoValido_RetornaResultadosDoRepositorio()
    {
        // Arrange
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
            new() { ExternalKey = Guid.NewGuid(), Tipo = "Envio", Nome = "Contrato ACME",
                    Caminho = "Clientes > 2026 > ACME" }
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

        // Act
        var resultado = await _envioService.BuscarArquivosAsync(query);

        // Assert
        Assert.Equal(137, resultado.Total);
        Assert.Single(resultado.Dados);
        Assert.Equal("Contrato ACME", resultado.Dados[0].Nome);
        Assert.Equal(2, resultado.PaginaSelecionada);
        Assert.Equal(25, resultado.TamanhoPagina);
        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(42, 7, "contrato", true, 2, 25, null), Times.Once);
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException quando a ExternalKeyPasta informada não existe")]
    public async Task BuscarArquivosAsync_PastaInexistente_LancaDataNotFoundException()
    {
        // Arrange
        var conta = new Conta { IdConta = 1, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            ExternalKeyPasta = Guid.NewGuid(),
            Termo = "x"
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _authUserMock
            .Setup(x => x.IsAdmin(1))
            .Returns(true);
        _pastaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Pasta?)null);

        // Act + Assert
        await Assert.ThrowsAsync<DataNotFoundException>(() => _envioService.BuscarArquivosAsync(query));
    }

    [Fact(DisplayName = "Deve repassar idUsuario ao repositório quando o usuário não é admin")]
    public async Task BuscarArquivosAsync_UsuarioNaoAdmin_RepassaIdUsuario()
    {
        // Arrange
        var conta = new Conta { IdConta = 10, ExternalKey = Guid.NewGuid() };
        var usuario = new Usuario { IdUsuario = 99, AuthKey = Guid.NewGuid(), IdConta = 10 };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            Termo = "x"
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
            .Setup(x => x.BuscarArquivosAsync(10, null, "x", true, 1, 50, 99))
            .ReturnsAsync(((IReadOnlyList<ArquivoBuscaResultado>)[], 0));

        // Act
        await _envioService.BuscarArquivosAsync(query);

        // Assert
        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(10, null, "x", true, 1, 50, 99), Times.Once);
    }

    [Fact(DisplayName = "Deve passar null como idUsuarioFiltro quando o usuário é admin")]
    public async Task BuscarArquivosAsync_UsuarioAdmin_NaoFiltraPorUsuario()
    {
        // Arrange
        var conta = new Conta { IdConta = 10, ExternalKey = Guid.NewGuid() };
        var query = new BuscaArquivosQuery
        {
            ExternalKeyConta = conta.ExternalKey,
            Termo = "x"
        };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _authUserMock
            .Setup(x => x.IsAdmin(10))
            .Returns(true);
        _envioRepoMock
            .Setup(x => x.BuscarArquivosAsync(10, null, "x", true, 1, 50, null))
            .ReturnsAsync(((IReadOnlyList<ArquivoBuscaResultado>)[], 0));

        // Act
        await _envioService.BuscarArquivosAsync(query);

        // Assert
        _envioRepoMock.Verify(x => x.BuscarArquivosAsync(10, null, "x", true, 1, 50, null), Times.Once);
        _usuarioRepoMock.Verify(x => x.GetByAuthKeyAsync(It.IsAny<Guid>()), Times.Never);
    }
}
