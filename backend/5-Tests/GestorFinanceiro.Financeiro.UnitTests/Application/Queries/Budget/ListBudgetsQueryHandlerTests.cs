using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Budget;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using BudgetEntity = GestorFinanceiro.Financeiro.Domain.Entity.Budget;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Queries.Budget;

public class ListBudgetsQueryHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private readonly ListBudgetsQueryHandler _sut;

    public ListBudgetsQueryHandlerTests()
    {
        _sut = new ListBudgetsQueryHandler(
            _budgetRepository,
            _categoryRepository,
            NullLogger<ListBudgetsQueryHandler>.Instance);

        _categoryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task Handle_WithBudgetsInMonth_ShouldReturnListWithCalculatedFields()
    {
        var year = 2026;
        var month = 2;
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var budgets = new List<BudgetEntity>
        {
            BuildBudget("Lazer", 20m, year, month, [categoryA]),
            BuildBudget("Casa", 10m, year, month, [categoryB])
        };

        _budgetRepository.GetByMonthAsync(year, month, Arg.Any<CancellationToken>()).Returns(budgets);
        _budgetRepository.GetMonthlyIncomeAsync(year, month, Arg.Any<CancellationToken>()).Returns(5000m);
        _budgetRepository
            .GetConsumedAmountAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { categoryA })), year, month, Arg.Any<CancellationToken>())
            .Returns(600m);
        _budgetRepository
            .GetConsumedAmountAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { categoryB })), year, month, Arg.Any<CancellationToken>())
            .Returns(300m);

        _categoryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Category.Restore(categoryA, "Cinema", CategoryType.Despesa, true, false, "sys", DateTime.UtcNow, null, null),
            Category.Restore(categoryB, "Aluguel", CategoryType.Despesa, true, false, "sys", DateTime.UtcNow, null, null)
        ]);

        var result = await _sut.HandleAsync(new ListBudgetsQuery(year, month), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(item => item.Name == "Lazer" && item.LimitAmount == 1000m && item.ConsumedAmount == 600m);
        result.Should().Contain(item => item.Name == "Casa" && item.RemainingAmount == 200m && item.Categories.Count == 1);
    }

    [Fact]
    public async Task Handle_WithNoBudgets_ShouldReturnEmptyList()
    {
        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.HandleAsync(new ListBudgetsQuery(2026, 2), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCalculateLimitCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var budget = BuildBudget("Moradia", 25m, 2026, 2, [categoryId]);

        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([budget]);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(4000m);
        _budgetRepository.GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), 2026, 2, Arg.Any<CancellationToken>()).Returns(100m);

        var result = await _sut.HandleAsync(new ListBudgetsQuery(2026, 2), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].LimitAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_ShouldCalculateConsumedPercentageCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var budget = BuildBudget("Mercado", 20m, 2026, 2, [categoryId]);

        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([budget]);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(5000m);
        _budgetRepository.GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), 2026, 2, Arg.Any<CancellationToken>()).Returns(250m);

        var result = await _sut.HandleAsync(new ListBudgetsQuery(2026, 2), CancellationToken.None);

        result[0].ConsumedPercentage.Should().Be(25m);
    }

    [Fact]
    public async Task Handle_WithZeroIncome_ShouldReturnZeroLimits()
    {
        var categoryId = Guid.NewGuid();
        var budget = BuildBudget("Transporte", 10m, 2026, 2, [categoryId]);

        _budgetRepository.GetByMonthAsync(2026, 2, Arg.Any<CancellationToken>()).Returns([budget]);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(0m);
        _budgetRepository.GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), 2026, 2, Arg.Any<CancellationToken>()).Returns(300m);

        var result = await _sut.HandleAsync(new ListBudgetsQuery(2026, 2), CancellationToken.None);

        result[0].LimitAmount.Should().Be(0m);
        result[0].ConsumedPercentage.Should().Be(0m);
    }

    private static BudgetEntity BuildBudget(string name, decimal percentage, int year, int month, IReadOnlyList<Guid> categoryIds)
    {
        return BudgetEntity.Create(name, percentage, year, month, categoryIds, false, "user-test");
    }
}
