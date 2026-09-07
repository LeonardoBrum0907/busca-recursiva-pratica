using BuscaApi.Application.Services;
using BuscaApi.Domain.Dtos;
using BuscaApi.Domain.Exceptions;
using BuscaApi.Domain.Interfaces;
using BuscaApi.Domain.Interfaces.Repository;
using BuscaApi.Domain.Models.Data;
using Moq;
using Xunit;

namespace BuscaApi.Testes;

public class EnvioServiceListarItensTests
{
    private readonly Mock<IContaRepository> _contaRepoMock = new();
    private readonly Mock<IPastaRepository> _pastaRepoMock = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IEnvioRepository> _envioRepoMock = new();
    private readonly Mock<IAuthenticatedUser> _authUserMock = new();
    private readonly EnvioService _envioService;

    public EnvioServiceListarItensTests()
    {
        _envioService = new EnvioService(
            _contaRepoMock.Object,
            _pastaRepoMock.Object,
            _usuarioRepoMock.Object,
            _envioRepoMock.Object,
            _authUserMock.Object);
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException quando a conta não existe")]
    public async Task ListarItensAsync_ContaInexistente_LancaDataNotFoundException()
    {
        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Conta?)null);

        await Assert.ThrowsAsync<DataNotFoundException>(
            () => _envioService.ListarItensAsync(Guid.NewGuid(), null));
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException quando a ExternalKeyPasta informada não existe")]
    public async Task ListarItensAsync_PastaInexistente_LancaDataNotFoundException()
    {
        var conta = new Conta { IdConta = 1, ExternalKey = Guid.NewGuid() };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _authUserMock
            .Setup(x => x.IsAdmin(1))
            .Returns(true);
        _pastaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Pasta?)null);

        await Assert.ThrowsAsync<DataNotFoundException>(
            () => _envioService.ListarItensAsync(conta.ExternalKey, Guid.NewGuid()));
    }

    [Fact(DisplayName = "Deve devolver o conteúdo da pasta informada quando o usuário é admin")]
    public async Task ListarItensAsync_Admin_RetornaConteudoDaPasta()
    {
        var conta = new Conta { IdConta = 42, ExternalKey = Guid.NewGuid() };
        var pasta = new Pasta { IdPasta = 7, ExternalKey = Guid.NewGuid() };

        var itensRepo = new List<ItemPasta>
        {
            new() { ExternalKey = Guid.NewGuid(), Tipo = "Envio", Nome = "Contrato ACME 2026" }
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
            .Setup(x => x.ListarItensAsync(42, 7, null))
            .ReturnsAsync(itensRepo);

        var itens = await _envioService.ListarItensAsync(conta.ExternalKey, pasta.ExternalKey);

        Assert.Single(itens);
        Assert.Equal("Contrato ACME 2026", itens[0].Nome);
        _envioRepoMock.Verify(x => x.ListarItensAsync(42, 7, null), Times.Once);
        _usuarioRepoMock.Verify(x => x.GetByAuthKeyAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact(DisplayName = "Deve repassar idUsuario ao repositório quando o usuário não é admin")]
    public async Task ListarItensAsync_UsuarioNaoAdmin_RepassaIdUsuario()
    {
        var conta = new Conta { IdConta = 10, ExternalKey = Guid.NewGuid() };
        var usuario = new Usuario { IdUsuario = 99, AuthKey = Guid.NewGuid(), IdConta = 10 };

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
            .Setup(x => x.ListarItensAsync(10, null, 99))
            .ReturnsAsync([]);

        await _envioService.ListarItensAsync(conta.ExternalKey, null);

        _envioRepoMock.Verify(x => x.ListarItensAsync(10, null, 99), Times.Once);
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException quando o usuário não pertence à conta")]
    public async Task ListarItensAsync_UsuarioDeOutraConta_LancaDataNotFoundException()
    {
        var conta = new Conta { IdConta = 10, ExternalKey = Guid.NewGuid() };
        var usuario = new Usuario { IdUsuario = 99, AuthKey = Guid.NewGuid(), IdConta = 77 };

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

        await Assert.ThrowsAsync<DataNotFoundException>(
            () => _envioService.ListarItensAsync(conta.ExternalKey, null));
    }

    [Fact(DisplayName = "Deve lançar DataNotFoundException ao obter totais de conta inexistente")]
    public async Task ObterTotaisAsync_ContaInexistente_LancaDataNotFoundException()
    {
        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Conta?)null);

        await Assert.ThrowsAsync<DataNotFoundException>(
            () => _envioService.ObterTotaisAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Deve devolver o total de arquivos da conta")]
    public async Task ObterTotaisAsync_ContaExistente_RetornaTotalArquivos()
    {
        var conta = new Conta { IdConta = 42, ExternalKey = Guid.NewGuid() };

        _contaRepoMock
            .Setup(x => x.GetByExternalKeyAsync(conta.ExternalKey))
            .ReturnsAsync(conta);
        _envioRepoMock
            .Setup(x => x.ContarArquivosAsync(42))
            .ReturnsAsync(30728);

        var totais = await _envioService.ObterTotaisAsync(conta.ExternalKey);

        Assert.Equal(30728, totais.TotalArquivos);
        _envioRepoMock.Verify(x => x.ContarArquivosAsync(42), Times.Once);
    }
}
