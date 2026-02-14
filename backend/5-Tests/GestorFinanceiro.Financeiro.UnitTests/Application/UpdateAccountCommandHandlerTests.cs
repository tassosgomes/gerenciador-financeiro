using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class UpdateAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UpdateAccountCommandHandler>> _logger = new();

    private readonly UpdateAccountCommandHandler _sut;

    public UpdateAccountCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new UpdateAccountCommandHandler(
            _accountRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_AtualizaContaComSucesso()
    {
        var account = Account.Create("Conta Antiga", AccountType.Corrente, 150m, false, "user-1");
        var command = new UpdateAccountCommand(account.Id, "Conta Nova", true, "user-2");

        _accountRepository
            .Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Conta Nova");
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ContaInexistente_LancaAccountNotFoundException()
    {
        var command = new UpdateAccountCommand(Guid.NewGuid(), "Conta Nova", true, "user-2");

        _accountRepository
            .Setup(mock => mock.GetByIdWithLockAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<AccountNotFoundException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ComandoInvalido_LancaValidationException()
    {
        var command = new UpdateAccountCommand(Guid.Empty, string.Empty, true, string.Empty);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
