using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Invoice;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Queries;

public class GetInvoiceQueryHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly CreditCardDomainService _creditCardDomainService;
    private readonly IValidator<GetInvoiceQuery> _validator;
    private readonly Mock<ILogger<GetInvoiceQueryHandler>> _loggerMock;
    private readonly GetInvoiceQueryHandler _sut;

    public GetInvoiceQueryHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _creditCardDomainService = new CreditCardDomainService(); // Use real instance
        _validator = new GetInvoiceQueryValidator();
        _loggerMock = new Mock<ILogger<GetInvoiceQueryHandler>>();

        // Default mock setup to avoid null reference exceptions
        _transactionRepositoryMock
            .Setup(r => r.GetByAccountAndPeriodAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        _sut = new GetInvoiceQueryHandler(
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _creditCardDomainService, // Use real instance
            _validator,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCardAndTransactions_ShouldReturnInvoiceResponse()
    {
        // Arrange
        var debitAccountId = Guid.NewGuid();
        var account = Account.CreateCreditCard("Test Card", 5000m, 10, 15, debitAccountId, true, "user-test");
        var query = new GetInvoiceQuery(account.Id, 3, 2026);

        // Real service will calculate period automatically
        var transactions = new List<Transaction>
        {
            CreateTransaction(account.Id, 100m, TransactionType.Debit),
            CreateTransaction(account.Id, 50m, TransactionType.Debit)
        };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(r => r.GetByAccountAndPeriodAsync(
                account.Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert  
        result.Should().NotBeNull();
        result.AccountId.Should().Be(account.Id);
        result.AccountName.Should().Be("Test Card");
        result.Month.Should().Be(3);
        result.Year.Should().Be(2026);
        result.TotalAmount.Should().Be(150m); // Real calculation: 100 + 50
        result.AmountDue.Should().Be(150m);
        result.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoTransactions_ShouldReturnZeroTotalAmount()
    {
        // Arrange
        var debitAccountId = Guid.NewGuid();
        var account = Account.CreateCreditCard("Test Card", 5000m, 10, 15, debitAccountId, true, "user-test");
        var query = new GetInvoiceQuery(account.Id, 3, 2026);

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(0m);
        result.AmountDue.Should().Be(0m);
        result.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldThrowAccountNotFoundException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetInvoiceQuery(accountId, 3, 2026);

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        var action = () => _sut.HandleAsync(query, CancellationToken.None);
        await action.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task Handle_AccountIsNotCard_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var userId = "user-test";
        var account = Account.Create("Regular Account", AccountType.Corrente, 1000m, false, userId);
        var query = new GetInvoiceQuery(account.Id, 3, 2026);

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act & Assert
        var action = () => _sut.HandleAsync(query, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidCreditCardConfigException>()
            .WithMessage("Conta não é um cartão de crédito.");
    }

    [Fact]
    public async Task Handle_WithParceledTransactions_ShouldIncludeInstallmentInfo()
    {
        // Arrange
        var debitAccountId = Guid.NewGuid();
        var account = Account.CreateCreditCard("Test Card", 5000m, 10, 15, debitAccountId, true, "user-test");
        var query = new GetInvoiceQuery(account.Id, 3, 2026);
        var installmentGroupId = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            CreateTransactionWithInstallment(account.Id, 100m, installmentGroupId, 3, 12)
        };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(r => r.GetByAccountAndPeriodAsync(
                account.Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].InstallmentNumber.Should().Be(3);
        result.Transactions[0].TotalInstallments.Should().Be(12);
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectPeriod()
    {
        // Arrange
        var debitAccountId = Guid.NewGuid();
        var account = Account.CreateCreditCard("Test Card", 5000m, 5, 20, debitAccountId, true, "user-test");
        var query = new GetInvoiceQuery(account.Id, 12, 2025);
        var expectedStart = new DateTime(2025, 11, 6);
        var expectedEnd = new DateTime(2025, 12, 5);

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.PeriodStart.Should().Be(expectedStart);
        result.PeriodEnd.Should().Be(expectedEnd);

        _transactionRepositoryMock.Verify(r => r.GetByAccountAndPeriodAsync(
            account.Id,
            expectedStart,
            expectedEnd,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPreviousPositiveBalance_ShouldAbateFromTotal()
    {
        // Arrange
        var debitAccountId = Guid.NewGuid();
        var account = Account.CreateCreditCard("Test Card", 5000m, 10, 15, debitAccountId, true, "user-test");
        account.ApplyCredit(300m, "user-test"); // Simula crédito a favor

        var query = new GetInvoiceQuery(account.Id, 3, 2026);

        var transactions = new List<Transaction>
        {
            CreateTransaction(account.Id, 500m, TransactionType.Debit)
        };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(r => r.GetByAccountAndPeriodAsync(
                account.Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(500m); // Real calculation
        result.PreviousBalance.Should().Be(300m);
        result.AmountDue.Should().Be(200m); // 500 - 300 = 200
    }

    [Fact]
    public async Task Handle_WithCreditTransactions_ShouldSubtractFromTotal()
    {
        // Arrange
        var debitAccountId = Guid.NewGuid();
        var account = Account.CreateCreditCard("Test Card", 5000m, 10, 15, debitAccountId, true, "user-test");
        var query = new GetInvoiceQuery(account.Id, 3, 2026);

        var transactions = new List<Transaction>
        {
            CreateTransaction(account.Id, 200m, TransactionType.Debit),
            CreateTransaction(account.Id, 50m, TransactionType.Credit)
        };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(r => r.GetByAccountAndPeriodAsync(
                account.Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(150m); // Real calculation: 200 - 50
        result.Transactions.Should().HaveCount(2);
    }

    private static Transaction CreateTransaction(Guid accountId, decimal amount, TransactionType type)
    {
        return Transaction.Create(
            accountId: accountId,
            categoryId: Guid.NewGuid(),
            type: type,
            amount: amount,
            description: "Test Transaction",
            competenceDate: DateTime.UtcNow,
            dueDate: null,
            status: TransactionStatus.Paid,
            userId: "user-test");
    }

    private static Transaction CreateTransactionWithInstallment(
        Guid accountId,
        decimal amount,
        Guid installmentGroupId,
        int installmentNumber,
        int totalInstallments)
    {
        var transaction = Transaction.Create(
            accountId: accountId,
            categoryId: Guid.NewGuid(),
            type: TransactionType.Debit,
            amount: amount,
            description: "Installment Transaction",
            competenceDate: DateTime.UtcNow,
            dueDate: null,
            status: TransactionStatus.Paid,
            userId: "user-test");

        transaction.SetInstallmentInfo(installmentGroupId, installmentNumber, totalInstallments);
        return transaction;
    }
}
