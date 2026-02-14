using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.User;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.User;

public class GetAllUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<GetAllUsersQueryHandler>> _logger = new();

    private readonly GetAllUsersQueryHandler _sut;

    public GetAllUsersQueryHandlerTests()
    {
        _sut = new GetAllUsersQueryHandler(
            _userRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ExistemUsuarios_RetornaListaDeUserResponse()
    {
        var user1 = GestorFinanceiro.Financeiro.Domain.Entity.User.Create("User 1", "user1@test.com", "hash1", UserRole.Admin, "system");
        var user2 = GestorFinanceiro.Financeiro.Domain.Entity.User.Create("User 2", "user2@test.com", "hash2", UserRole.Member, "system");

        _userRepository.Setup(mock => mock.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GestorFinanceiro.Financeiro.Domain.Entity.User> { user1, user2 });

        var query = new GetAllUsersQuery();
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Name.Should().Be("User 1");
        resultList[1].Name.Should().Be("User 2");
    }

    [Fact]
    public async Task HandleAsync_NenhumUsuario_RetornaListaVazia()
    {
        _userRepository.Setup(mock => mock.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GestorFinanceiro.Financeiro.Domain.Entity.User>());

        var query = new GetAllUsersQuery();
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
