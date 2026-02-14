using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using DomainUser = GestorFinanceiro.Financeiro.Domain.Entity.User;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _logger = new();

    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new ChangePasswordCommandHandler(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _auditService.Object,
            _passwordHasher.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_SenhaAtualCorreta_AlteraSenhaERevogaTokens()
    {
        var userId = Guid.NewGuid();
        var user = DomainUser.Create("Test User", "test@test.com", "old-hash", UserRole.Admin, "system");
        
        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(mock => mock.Verify("current-password", "old-hash")).Returns(true);
        _passwordHasher.Setup(mock => mock.Hash("NewPassword1!")).Returns("new-hash");
        _refreshTokenRepository.Setup(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new ChangePasswordCommand(userId, "current-password", "NewPassword1!");
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.PasswordHash.Should().Be("new-hash");
        user.MustChangePassword.Should().BeFalse();
        _refreshTokenRepository.Verify(mock => mock.RevokeByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNaoEncontrado_LancaUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((DomainUser?)null);

        var command = new ChangePasswordCommand(userId, "current-password", "NewPassword1!");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<UserNotFoundException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SenhaAtualIncorreta_LancaInvalidCredentialsException()
    {
        var userId = Guid.NewGuid();
        var user = DomainUser.Create("Test User", "test@test.com", "old-hash", UserRole.Admin, "system");
        
        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(mock => mock.Verify("senha-errada", "old-hash")).Returns(false);

        var command = new ChangePasswordCommand(userId, "senha-errada", "NewPassword1!");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidCredentialsException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SenhaAtualVazia_LancaInvalidOperationException()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "", "NewPassword1!");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_NovaSenhaVazia_LancaInvalidOperationException()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "current-password", "");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_NovaSenhaCurta_LancaInvalidOperationException()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "current-password", "short");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
