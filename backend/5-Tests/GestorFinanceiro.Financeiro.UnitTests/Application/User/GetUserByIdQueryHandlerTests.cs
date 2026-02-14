using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.User;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.User;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<GetUserByIdQueryHandler>> _logger = new();

    private readonly GetUserByIdQueryHandler _sut;

    public GetUserByIdQueryHandlerTests()
    {
        _sut = new GetUserByIdQueryHandler(
            _userRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_UsuarioExiste_RetornaUserResponse()
    {
        var userId = Guid.NewGuid();
        var user = GestorFinanceiro.Financeiro.Domain.Entity.User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");

        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var query = new GetUserByIdQuery(userId);
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@test.com");
        result.Role.Should().Be("Admin");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_UsuarioNaoExiste_LancaUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((GestorFinanceiro.Financeiro.Domain.Entity.User?)null);

        var query = new GetUserByIdQuery(userId);
        var action = () => _sut.HandleAsync(query, CancellationToken.None);

        await action.Should().ThrowAsync<UserNotFoundException>();
    }
}
