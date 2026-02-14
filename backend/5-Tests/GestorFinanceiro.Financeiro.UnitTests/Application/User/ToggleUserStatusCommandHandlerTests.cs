using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.User;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.User;

public class ToggleUserStatusCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ToggleUserStatusCommandHandler>> _logger = new();

    private readonly ToggleUserStatusCommandHandler _sut;

    public ToggleUserStatusCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new ToggleUserStatusCommandHandler(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_DesativarUsuario_DesativaERevogaTokens()
    {
        var userId = Guid.NewGuid();
        var user = GestorFinanceiro.Financeiro.Domain.Entity.User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");

        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _refreshTokenRepository.Setup(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new ToggleUserStatusCommand(userId, false, "admin-id");
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.IsActive.Should().BeFalse();
        _refreshTokenRepository.Verify(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AtivarUsuario_AtivaUsuario()
    {
        var userId = Guid.NewGuid();
        var user = GestorFinanceiro.Financeiro.Domain.Entity.User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        user.Deactivate("system");

        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new ToggleUserStatusCommand(userId, true, "admin-id");
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.IsActive.Should().BeTrue();
        _refreshTokenRepository.Verify(mock => mock.RevokeByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNaoEncontrado_LancaUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((GestorFinanceiro.Financeiro.Domain.Entity.User?)null);

        var command = new ToggleUserStatusCommand(userId, false, "admin-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<UserNotFoundException>();
    }
}
