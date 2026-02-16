using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Service;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Service;

public class CreditCardDomainServiceTests
{
    private readonly CreditCardDomainService _sut = new();

    [Fact]
    public void CalculateInvoicePeriod_January_ShouldCrossYear()
    {
        // Arrange
        const int closingDay = 3;
        const int month = 1;
        const int year = 2026;

        // Act
        var (start, end) = _sut.CalculateInvoicePeriod(closingDay, month, year);

        // Assert
        end.Should().Be(new DateTime(2026, 1, 3));
        start.Should().Be(new DateTime(2025, 12, 4));
    }

    [Fact]
    public void CalculateInvoicePeriod_RegularMonth_ShouldReturnCorrectPeriod()
    {
        // Arrange
        const int closingDay = 10;
        const int month = 3;
        const int year = 2026;

        // Act
        var (start, end) = _sut.CalculateInvoicePeriod(closingDay, month, year);

        // Assert
        end.Should().Be(new DateTime(2026, 3, 10));
        start.Should().Be(new DateTime(2026, 2, 11));
    }

    [Fact]
    public void CalculateInvoicePeriod_ClosingDay28_ShouldHandleFebruary()
    {
        // Arrange
        const int closingDay = 28;
        const int month = 3;
        const int year = 2026;

        // Act
        var (start, end) = _sut.CalculateInvoicePeriod(closingDay, month, year);

        // Assert
        end.Should().Be(new DateTime(2026, 3, 28));
        start.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void CalculateInvoicePeriod_December_ShouldHandleYearEnd()
    {
        // Arrange
        const int closingDay = 5;
        const int month = 12;
        const int year = 2025;

        // Act
        var (start, end) = _sut.CalculateInvoicePeriod(closingDay, month, year);

        // Assert
        end.Should().Be(new DateTime(2025, 12, 5));
        start.Should().Be(new DateTime(2025, 11, 6));
    }

    [Fact]
    public void CalculateInvoiceTotal_WithDebitsOnly_ShouldReturnPositiveSum()
    {
        // Arrange
        var transactions = new[]
        {
            CreateTransaction(TransactionType.Debit, 100m),
            CreateTransaction(TransactionType.Debit, 50m),
            CreateTransaction(TransactionType.Debit, 25m)
        };

        // Act
        var total = _sut.CalculateInvoiceTotal(transactions);

        // Assert
        total.Should().Be(175m);
    }

    [Fact]
    public void CalculateInvoiceTotal_WithDebitsAndCredits_ShouldReturnNetAmount()
    {
        // Arrange
        var transactions = new[]
        {
            CreateTransaction(TransactionType.Debit, 200m),
            CreateTransaction(TransactionType.Debit, 100m),
            CreateTransaction(TransactionType.Credit, 50m),
            CreateTransaction(TransactionType.Credit, 30m)
        };

        // Act
        var total = _sut.CalculateInvoiceTotal(transactions);

        // Assert
        total.Should().Be(220m);
    }

    [Fact]
    public void CalculateInvoiceTotal_WithCreditsOnly_ShouldReturnNegativeAmount()
    {
        // Arrange
        var transactions = new[]
        {
            CreateTransaction(TransactionType.Credit, 100m),
            CreateTransaction(TransactionType.Credit, 50m)
        };

        // Act
        var total = _sut.CalculateInvoiceTotal(transactions);

        // Assert
        total.Should().Be(-150m);
    }

    [Fact]
    public void CalculateInvoiceTotal_WithNoTransactions_ShouldReturnZero()
    {
        // Arrange
        var transactions = Array.Empty<Transaction>();

        // Act
        var total = _sut.CalculateInvoiceTotal(transactions);

        // Assert
        total.Should().Be(0m);
    }

    private static Transaction CreateTransaction(TransactionType type, decimal amount)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            amount,
            "Test transaction",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-test");
    }
}
