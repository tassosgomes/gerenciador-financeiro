using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Dashboard;
using GestorFinanceiro.Financeiro.Domain.Dto;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Dashboard;

public class GetDashboardChartsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnChartsDataWithRevenueAndExpenses()
    {
        // Arrange
        var mockRepository = new Mock<IDashboardRepository>();
        var mockLogger = new Mock<ILogger<GetDashboardChartsQueryHandler>>();

        var revenueVsExpense = new List<MonthlyComparisonDto>
        {
            new("2025-09", 5000m, 3000m),
            new("2025-10", 5500m, 3200m),
            new("2025-11", 6000m, 3500m),
            new("2025-12", 6200m, 3700m),
            new("2026-01", 6500m, 4000m),
            new("2026-02", 7000m, 4200m)
        };

        var expenseByCategory = new List<CategoryExpenseDto>
        {
            new(Guid.NewGuid(), "Alimentação", 1500m, 35.71m),
            new(Guid.NewGuid(), "Transporte", 800m, 19.05m),
            new(Guid.NewGuid(), "Lazer", 600m, 14.29m),
            new(Guid.NewGuid(), "Saúde", 500m, 11.90m),
            new(Guid.NewGuid(), "Outros", 800m, 19.05m)
        };

        mockRepository.Setup(r => r.GetRevenueVsExpenseAsync(2, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revenueVsExpense);
        mockRepository.Setup(r => r.GetExpenseByCategoryAsync(2, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenseByCategory);

        var handler = new GetDashboardChartsQueryHandler(mockRepository.Object, mockLogger.Object);
        var query = new GetDashboardChartsQuery(2, 2026);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.RevenueVsExpense.Should().HaveCount(6);
        result.RevenueVsExpense[0].Month.Should().Be("2025-09");
        result.RevenueVsExpense[0].Income.Should().Be(5000m);
        result.RevenueVsExpense[0].Expenses.Should().Be(3000m);
        
        result.ExpenseByCategory.Should().HaveCount(5);
        result.ExpenseByCategory[0].CategoryName.Should().Be("Alimentação");
        result.ExpenseByCategory[0].Total.Should().Be(1500m);
        result.ExpenseByCategory[0].Percentage.Should().Be(35.71m);

        mockRepository.Verify(r => r.GetRevenueVsExpenseAsync(2, 2026, It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.GetExpenseByCategoryAsync(2, 2026, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyData_ShouldReturnEmptyLists()
    {
        // Arrange
        var mockRepository = new Mock<IDashboardRepository>();
        var mockLogger = new Mock<ILogger<GetDashboardChartsQueryHandler>>();

        mockRepository.Setup(r => r.GetRevenueVsExpenseAsync(1, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonthlyComparisonDto>());
        mockRepository.Setup(r => r.GetExpenseByCategoryAsync(1, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryExpenseDto>());

        var handler = new GetDashboardChartsQueryHandler(mockRepository.Object, mockLogger.Object);
        var query = new GetDashboardChartsQuery(1, 2026);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.RevenueVsExpense.Should().BeEmpty();
        result.ExpenseByCategory.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithDifferentMonthYear_ShouldPassCorrectParameters()
    {
        // Arrange
        var mockRepository = new Mock<IDashboardRepository>();
        var mockLogger = new Mock<ILogger<GetDashboardChartsQueryHandler>>();

        mockRepository.Setup(r => r.GetRevenueVsExpenseAsync(12, 2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonthlyComparisonDto> { new("2025-12", 5000m, 3000m) });
        mockRepository.Setup(r => r.GetExpenseByCategoryAsync(12, 2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryExpenseDto> { new(Guid.NewGuid(), "Test", 1000m, 100m) });

        var handler = new GetDashboardChartsQueryHandler(mockRepository.Object, mockLogger.Object);
        var query = new GetDashboardChartsQuery(12, 2025);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        mockRepository.Verify(r => r.GetRevenueVsExpenseAsync(12, 2025, It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.GetExpenseByCategoryAsync(12, 2025, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }
}
