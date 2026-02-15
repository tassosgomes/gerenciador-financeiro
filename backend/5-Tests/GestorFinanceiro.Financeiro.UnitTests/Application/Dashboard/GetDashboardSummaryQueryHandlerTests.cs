using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Dashboard;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnSummaryWithAllData()
    {
        // Arrange
        var mockRepository = new Mock<IDashboardRepository>();
        var mockLogger = new Mock<ILogger<GetDashboardSummaryQueryHandler>>();

        mockRepository.Setup(r => r.GetTotalBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(15000.50m);
        mockRepository.Setup(r => r.GetMonthlyIncomeAsync(2, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5000.00m);
        mockRepository.Setup(r => r.GetMonthlyExpensesAsync(2, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3500.75m);
        mockRepository.Setup(r => r.GetCreditCardDebtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1200.30m);

        var handler = new GetDashboardSummaryQueryHandler(mockRepository.Object, mockLogger.Object);
        var query = new GetDashboardSummaryQuery(2, 2026);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.TotalBalance.Should().Be(15000.50m);
        result.MonthlyIncome.Should().Be(5000.00m);
        result.MonthlyExpenses.Should().Be(3500.75m);
        result.CreditCardDebt.Should().Be(1200.30m);

        mockRepository.Verify(r => r.GetTotalBalanceAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.GetMonthlyIncomeAsync(2, 2026, It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.GetMonthlyExpensesAsync(2, 2026, It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.GetCreditCardDebtAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithZeroValues_ShouldReturnZeros()
    {
        // Arrange
        var mockRepository = new Mock<IDashboardRepository>();
        var mockLogger = new Mock<ILogger<GetDashboardSummaryQueryHandler>>();

        mockRepository.Setup(r => r.GetTotalBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        mockRepository.Setup(r => r.GetMonthlyIncomeAsync(1, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        mockRepository.Setup(r => r.GetMonthlyExpensesAsync(1, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        mockRepository.Setup(r => r.GetCreditCardDebtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var handler = new GetDashboardSummaryQueryHandler(mockRepository.Object, mockLogger.Object);
        var query = new GetDashboardSummaryQuery(1, 2026);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.TotalBalance.Should().Be(0m);
        result.MonthlyIncome.Should().Be(0m);
        result.MonthlyExpenses.Should().Be(0m);
        result.CreditCardDebt.Should().Be(0m);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentMonthYear_ShouldPassCorrectParameters()
    {
        // Arrange
        var mockRepository = new Mock<IDashboardRepository>();
        var mockLogger = new Mock<ILogger<GetDashboardSummaryQueryHandler>>();

        mockRepository.Setup(r => r.GetTotalBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10000m);
        mockRepository.Setup(r => r.GetMonthlyIncomeAsync(12, 2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4000m);
        mockRepository.Setup(r => r.GetMonthlyExpensesAsync(12, 2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2000m);
        mockRepository.Setup(r => r.GetCreditCardDebtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(500m);

        var handler = new GetDashboardSummaryQueryHandler(mockRepository.Object, mockLogger.Object);
        var query = new GetDashboardSummaryQuery(12, 2025);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        mockRepository.Verify(r => r.GetMonthlyIncomeAsync(12, 2025, It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.GetMonthlyExpensesAsync(12, 2025, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }
}
