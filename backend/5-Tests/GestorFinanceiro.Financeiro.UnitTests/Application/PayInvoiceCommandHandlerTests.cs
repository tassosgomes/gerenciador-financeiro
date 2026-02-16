using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Invoice;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class PayInvoiceCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<PayInvoiceCommandHandler>> _logger = new();

    private readonly PayInvoiceCommandHandler _sut;

    public PayInvoiceCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) => transaction);

        _sut = new PayInvoiceCommandHandler(
            _accountRepository.Object,
            _transactionRepository.Object,
            _categoryRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            new TransferDomainService(new TransactionDomainService()),
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidPayment_ShouldReturnTransactionResponses()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");
        var invoicePaymentCategory = Category.Restore(
            Guid.NewGuid(),
            "Pagamento de Fatura",
            CategoryType.Despesa,
            true,
            true,
            "system",
            DateTime.UtcNow,
            null,
            null);

        var command = new PayInvoiceCommand(
            creditCardAccount.Id,
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1",
            "op-123");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(creditCardAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creditCardAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(debitAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);
        _categoryRepository.Setup(mock => mock.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { invoicePaymentCategory });
        _operationLogRepository.Setup(mock => mock.ExistsByOperationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Should().HaveCount(2);
        debitAccount.Balance.Should().Be(500m);
        creditCardAccount.Balance.Should().Be(0m);
        _transactionRepository.Verify(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldThrowNotFoundException()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1",
            "op-123");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(command.CreditCardAccountId, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<AccountNotFoundException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountIsNotCard_ShouldThrowDomainException()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");

        var command = new PayInvoiceCommand(
            debitAccount.Id,
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1",
            "op-123");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(debitAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<AccountIsNotCreditCardException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DebitAccountInactive_ShouldThrowDomainException()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        debitAccount.Deactivate("user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");

        var command = new PayInvoiceCommand(
            creditCardAccount.Id,
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1",
            "op-123");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(creditCardAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creditCardAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(debitAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InactiveAccountException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOperationId_ShouldBeIdempotent()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1",
            "op-123");

        _operationLogRepository.Setup(mock => mock.ExistsByOperationIdAsync("op-123", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateOperationException>();
    }

    [Fact]
    public async Task Handle_ShouldAuditLog()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");
        var invoicePaymentCategory = Category.Restore(
            Guid.NewGuid(),
            "Pagamento de Fatura",
            CategoryType.Despesa,
            true,
            true,
            "system",
            DateTime.UtcNow,
            null,
            null);

        var command = new PayInvoiceCommand(
            creditCardAccount.Id,
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(creditCardAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creditCardAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(debitAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);
        _categoryRepository.Setup(mock => mock.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { invoicePaymentCategory });
        _operationLogRepository.Setup(mock => mock.ExistsByOperationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.HandleAsync(command, CancellationToken.None);

        _auditService.Verify(mock => mock.LogAsync(
            "Transaction",
            It.IsAny<Guid>(),
            "Created",
            "user-1",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_PartialPayment_ShouldSucceed()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");
        var invoicePaymentCategory = Category.Restore(
            Guid.NewGuid(),
            "Pagamento de Fatura",
            CategoryType.Despesa,
            true,
            true,
            "system",
            DateTime.UtcNow,
            null,
            null);

        var command = new PayInvoiceCommand(
            creditCardAccount.Id,
            1000m,
            DateTime.UtcNow.AddDays(-1),
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(creditCardAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creditCardAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(debitAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);
        _categoryRepository.Setup(mock => mock.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { invoicePaymentCategory });
        _operationLogRepository.Setup(mock => mock.ExistsByOperationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Should().HaveCount(2);
        debitAccount.Balance.Should().Be(1000m);
        creditCardAccount.Balance.Should().Be(-500m);
    }

    [Fact]
    public async Task Handle_OverPayment_ShouldSucceed()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 3000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");
        var invoicePaymentCategory = Category.Restore(
            Guid.NewGuid(),
            "Pagamento de Fatura",
            CategoryType.Despesa,
            true,
            true,
            "system",
            DateTime.UtcNow,
            null,
            null);

        var command = new PayInvoiceCommand(
            creditCardAccount.Id,
            2000m,
            DateTime.UtcNow.AddDays(-1),
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(creditCardAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creditCardAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(debitAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);
        _categoryRepository.Setup(mock => mock.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { invoicePaymentCategory });
        _operationLogRepository.Setup(mock => mock.ExistsByOperationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Should().HaveCount(2);
        debitAccount.Balance.Should().Be(1000m);
        creditCardAccount.Balance.Should().Be(500m);
    }
}
