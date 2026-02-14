using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class AccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateAccountCommandHandler>> _logger = new();

    private readonly CreateAccountCommandHandler _sut;

    public AccountCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _accountRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account account, CancellationToken _) => account);

        _sut = new CreateAccountCommandHandler(
            _accountRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaContaComSucesso()
    {
        var command = new CreateAccountCommand("Conta Principal", AccountType.Corrente, 100m, false, "user-1");

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Conta Principal", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Conta Principal");
        response.Balance.Should().Be(100m);
        _accountRepository.Verify(mock => mock.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ComandoInvalido_LancaValidationException()
    {
        var command = new CreateAccountCommand(string.Empty, AccountType.Corrente, -1m, false, string.Empty);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
