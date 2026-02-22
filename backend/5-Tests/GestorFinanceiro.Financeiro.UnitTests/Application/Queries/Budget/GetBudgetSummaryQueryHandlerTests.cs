using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Budget;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using BudgetEntity = GestorFinanceiro.Financeiro.Domain.Entity.Budget;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Queries.Budget;

public class GetBudgetSummaryQueryHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private readonly GetBudgetSummaryQueryHandler _sut;

    public GetBudgetSummaryQueryHandlerTests()
    {
        _sut = new GetBudgetSummaryQueryHandler(
            _budgetRepository,
            _categoryRepository,
            NullLogger<GetBudgetSummaryQueryHandler>.Instance);

        _categoryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task Handle_ShouldReturnConsolidatedSummary()
    {
        var year = 2026;
        var month = 2;
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();

        var budgets = new List<BudgetEntity>
        {
            BuildBudget("Moradia", 30m, year, month, [categoryA]),
            BuildBudget("Lazer", 20m, year, month, [categoryB])
        };

        _budgetRepository.GetByMonthAsync(year, month, Arg.Any<CancellationToken>()).Returns(budgets);
        _budgetRepository.GetMonthlyIncomeAsync(year, month, Arg.Any<CancellationToken>()).Returns(5000m);
        _budgetRepository.GetUnbudgetedExpensesAsync(year, month, Arg.Any<CancellationToken>()).Returns(250m);
        _budgetRepository
            .GetConsumedAmountAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { categoryA })), year, month, Arg.Any<CancellationToken>())
            .Returns(800m);
        _budgetRepository
            .GetConsumedAmountAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { categoryB })), year, month, Arg.Any<CancellationToken>())
            .Returns(500m);

        _categoryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Category.Restore(categoryA, "Aluguel", CategoryType.Despesa, true, false, "sys", DateTime.UtcNow, null, null),
            Category.Restore(categoryB, "Cinema", CategoryType.Despesa, true, false, "sys", DateTime.UtcNow, null, null)
        ]);

        var result = await _sut.HandleAsync(new GetBudgetSummaryQuery(year, month), CancellationToken.None);

        result.ReferenceYear.Should().Be(year);
        result.ReferenceMonth.Should().Be(month);
        result.MonthlyIncome.Should().Be(5000m);
        result.Budgets.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalsCorrectly()
    {
        var year = 2026;
        var month = 2;
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();

        _budgetRepository.GetByMonthAsync(year, month, Arg.Any<CancellationToken>()).Returns(
        [
            BuildBudget("AA", 40m, year, month, [categoryA]),
            BuildBudget("BB", 20m, year, month, [categoryB])
        ]);
        _budgetRepository.GetMonthlyIncomeAsync(year, month, Arg.Any<CancellationToken>()).Returns(5000m);
        _budgetRepository.GetUnbudgetedExpensesAsync(year, month, Arg.Any<CancellationToken>()).Returns(100m);
        _budgetRepository
            .GetConsumedAmountAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { categoryA })), year, month, Arg.Any<CancellationToken>())
            .Returns(700m);
        _budgetRepository
            .GetConsumedAmountAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { categoryB })), year, month, Arg.Any<CancellationToken>())
            .Returns(200m);

        var result = await _sut.HandleAsync(new GetBudgetSummaryQuery(year, month), CancellationToken.None);

        result.TotalBudgetedPercentage.Should().Be(60m);
        result.TotalBudgetedAmount.Should().Be(3000m);
        result.TotalConsumedAmount.Should().Be(900m);
        result.TotalRemainingAmount.Should().Be(2100m);
        result.UnbudgetedPercentage.Should().Be(40m);
        result.UnbudgetedAmount.Should().Be(2000m);
    }

    [Fact]
    public async Task Handle_ShouldIncludeUnbudgetedExpenses()
    {
        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([]);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(1000m);
        _budgetRepository.GetUnbudgetedExpensesAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(175m);

        var result = await _sut.HandleAsync(new GetBudgetSummaryQuery(2026, 2), CancellationToken.None);

        result.UnbudgetedExpenses.Should().Be(175m);
    }

    [Fact]
    public async Task Handle_WithNoBudgets_ShouldReturnEmptySummary()
    {
        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([]);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(2500m);
        _budgetRepository.GetUnbudgetedExpensesAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(300m);

        var result = await _sut.HandleAsync(new GetBudgetSummaryQuery(2026, 2), CancellationToken.None);

        result.Budgets.Should().BeEmpty();
        result.TotalBudgetedPercentage.Should().Be(0m);
        result.TotalBudgetedAmount.Should().Be(0m);
        result.UnbudgetedPercentage.Should().Be(100m);
        result.UnbudgetedAmount.Should().Be(2500m);
    }

    [Fact]
    public async Task Handle_WithZeroIncome_ShouldReturnZeroAmounts()
    {
        var categoryId = Guid.NewGuid();
        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([BuildBudget("AA", 60m, 2026, 2, [categoryId])]);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(0m);
        _budgetRepository.GetUnbudgetedExpensesAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(0m);
        _budgetRepository.GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), 2026, 2, Arg.Any<CancellationToken>()).Returns(0m);

        var result = await _sut.HandleAsync(new GetBudgetSummaryQuery(2026, 2), CancellationToken.None);

        result.MonthlyIncome.Should().Be(0m);
        result.TotalBudgetedAmount.Should().Be(0m);
        result.TotalRemainingAmount.Should().Be(0m);
        result.UnbudgetedAmount.Should().Be(0m);
    }

    private static BudgetEntity BuildBudget(string name, decimal percentage, int year, int month, IReadOnlyList<Guid> categoryIds)
    {
        return BudgetEntity.Create(name, percentage, year, month, categoryIds, false, "user-test");
    }
}
