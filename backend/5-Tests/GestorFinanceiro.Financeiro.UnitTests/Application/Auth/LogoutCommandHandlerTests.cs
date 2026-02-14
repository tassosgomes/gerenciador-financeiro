using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<LogoutCommandHandler>> _logger = new();

    private readonly LogoutCommandHandler _sut;

    public LogoutCommandHandlerTests()
    {
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new LogoutCommandHandler(
            _refreshTokenRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_UsuarioValido_RevogaTodosTokensESalva()
    {
        var userId = Guid.NewGuid();
        _refreshTokenRepository.Setup(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new LogoutCommand(userId);
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _refreshTokenRepository.Verify(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UsuarioSemTokens_ExecutaNormalmente()
    {
        var userId = Guid.NewGuid();
        _refreshTokenRepository.Setup(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new LogoutCommand(userId);
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
    }
}
