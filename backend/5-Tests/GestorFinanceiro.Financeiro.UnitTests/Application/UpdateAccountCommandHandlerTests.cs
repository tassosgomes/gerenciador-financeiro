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

    [Fact]
    public async Task HandleAsync_UpdateCreditCard_ShouldUpdateAllFields()
    {
        var debitAccountId = Guid.NewGuid();
        var newDebitAccountId = Guid.NewGuid();
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 1000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);

        var newDebitAccount = Account.Create("Nova Conta", AccountType.Corrente, 2000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(newDebitAccount, newDebitAccountId);

        var creditCard = Account.CreateCreditCard("Cartão Antigo", 5000m, 10, 20, debitAccountId, true, "user-1");

        var command = new UpdateAccountCommand(
            creditCard.Id,
            "Cartão Novo",
            false,
            "user-2",
            null,
            8000m,
            15,
            25,
            newDebitAccountId,
            false);

        _accountRepository
            .Setup(mock => mock.GetByIdWithLockAsync(creditCard.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditCard);
        _accountRepository
            .Setup(mock => mock.GetByIdAsync(newDebitAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newDebitAccount);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Cartão Novo");
        response.CreditCard.Should().NotBeNull();
        response.CreditCard!.CreditLimit.Should().Be(8000m);
        response.CreditCard.ClosingDay.Should().Be(15);
        response.CreditCard.DueDay.Should().Be(25);
        response.CreditCard.DebitAccountId.Should().Be(newDebitAccountId);
        response.CreditCard.EnforceCreditLimit.Should().BeFalse();
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UpdateCreditCard_InvalidDebitAccount_ShouldThrow()
    {
        var debitAccountId = Guid.NewGuid();
        var newDebitAccountId = Guid.NewGuid();
        var creditCard = Account.CreateCreditCard("Cartão", 5000m, 10, 20, debitAccountId, true, "user-1");

        var command = new UpdateAccountCommand(
            creditCard.Id,
            "Cartão",
            false,
            "user-2",
            null,
            5000m,
            10,
            20,
            newDebitAccountId,
            true);

        _accountRepository
            .Setup(mock => mock.GetByIdWithLockAsync(creditCard.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditCard);
        _accountRepository
            .Setup(mock => mock.GetByIdAsync(newDebitAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<AccountNotFoundException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UpdateRegularAccount_ShouldMaintainExistingBehavior()
    {
        var account = Account.Create("Conta Antiga", AccountType.Corrente, 150m, false, "user-1");
        var command = new UpdateAccountCommand(account.Id, "Conta Nova", true, "user-2");

        _accountRepository
            .Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Conta Nova");
        response.AllowNegativeBalance.Should().BeTrue();
        response.CreditCard.Should().BeNull();
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UpdateCreditCard_ShouldAuditLog()
    {
        var debitAccountId = Guid.NewGuid();
        var creditCard = Account.CreateCreditCard("Cartão", 5000m, 10, 20, debitAccountId, true, "user-1");

        var command = new UpdateAccountCommand(
            creditCard.Id,
            "Cartão Atualizado",
            false,
            "user-2",
            null,
            6000m,
            15,
            25,
            debitAccountId,
            true);

        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 1000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);

        _accountRepository
            .Setup(mock => mock.GetByIdWithLockAsync(creditCard.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditCard);
        _accountRepository
            .Setup(mock => mock.GetByIdAsync(debitAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(debitAccount);

        await _sut.HandleAsync(command, CancellationToken.None);

        _auditService.Verify(
            mock => mock.LogAsync("Account", creditCard.Id, "Updated", "user-2", It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
