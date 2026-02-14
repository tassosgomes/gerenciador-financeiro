using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.User;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.User;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateUserCommandHandler>> _logger = new();

    private readonly CreateUserCommandHandler _sut;

    public CreateUserCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _userRepository
            .Setup(mock => mock.AddAsync(It.IsAny<GestorFinanceiro.Financeiro.Domain.Entity.User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GestorFinanceiro.Financeiro.Domain.Entity.User user, CancellationToken _) => user);

        _sut = new CreateUserCommandHandler(
            _userRepository.Object,
            _auditService.Object,
            _passwordHasher.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_DadosValidos_CriaUsuarioComSucesso()
    {
        _userRepository.Setup(mock => mock.ExistsByEmailAsync("novo@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(mock => mock.Hash("Password1!")).Returns("hashed-password");

        var command = new CreateUserCommand("Novo Usuario", "novo@test.com", "Password1!", "Admin", "creator-id");
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Novo Usuario");
        result.Email.Should().Be("novo@test.com");
        result.Role.Should().Be("Admin");
        result.IsActive.Should().BeTrue();
        result.MustChangePassword.Should().BeTrue();
        _userRepository.Verify(mock => mock.AddAsync(It.IsAny<GestorFinanceiro.Financeiro.Domain.Entity.User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmailDuplicado_LancaUserEmailAlreadyExistsException()
    {
        _userRepository.Setup(mock => mock.ExistsByEmailAsync("existente@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateUserCommand("Outro Usuario", "existente@test.com", "Password1!", "Admin", "creator-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<UserEmailAlreadyExistsException>();
        _userRepository.Verify(mock => mock.AddAsync(It.IsAny<GestorFinanceiro.Financeiro.Domain.Entity.User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NomeVazio_LancaInvalidOperationException()
    {
        var command = new CreateUserCommand("", "test@test.com", "Password1!", "Admin", "creator-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_EmailInvalido_LancaInvalidOperationException()
    {
        var command = new CreateUserCommand("Test User", "email-invalido", "Password1!", "Admin", "creator-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_SenhaVazia_LancaInvalidOperationException()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "", "Admin", "creator-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_RoleInvalida_LancaInvalidOperationException()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "Password1!", "InvalidRole", "creator-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_NomeCurto_LancaInvalidOperationException()
    {
        var command = new CreateUserCommand("AB", "test@test.com", "Password1!", "Admin", "creator-id");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
