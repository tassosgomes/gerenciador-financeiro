using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Budget;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using BudgetEntity = GestorFinanceiro.Financeiro.Domain.Entity.Budget;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Queries.Budget;

public class GetBudgetByIdQueryHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private readonly GetBudgetByIdQueryHandler _sut;

    public GetBudgetByIdQueryHandlerTests()
    {
        _sut = new GetBudgetByIdQueryHandler(
            _budgetRepository,
            _categoryRepository,
            NullLogger<GetBudgetByIdQueryHandler>.Instance);

        _categoryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task Handle_WithExistingBudget_ShouldReturnResponse()
    {
        var categoryId = Guid.NewGuid();
        var budget = BuildBudget("Saúde", 15m, 2026, 2, [categoryId]);

        _budgetRepository.GetByIdWithCategoriesAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _budgetRepository.GetMonthlyIncomeAsync(2026, 2, Arg.Any<CancellationToken>()).Returns(3000m);
        _budgetRepository.GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), 2026, 2, Arg.Any<CancellationToken>()).Returns(200m);
        _categoryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Category.Restore(categoryId, "Farmácia", CategoryType.Despesa, true, false, "sys", DateTime.UtcNow, null, null)
        ]);

        var result = await _sut.HandleAsync(new GetBudgetByIdQuery(budget.Id), CancellationToken.None);

        result.Id.Should().Be(budget.Id);
        result.Name.Should().Be("Saúde");
        result.LimitAmount.Should().Be(450m);
        result.Categories.Should().ContainSingle(item => item.Name == "Farmácia");
    }

    [Fact]
    public async Task Handle_WithNonExistingBudget_ShouldThrowBudgetNotFoundException()
    {
        var budgetId = Guid.NewGuid();
        _budgetRepository.GetByIdWithCategoriesAsync(budgetId, Arg.Any<CancellationToken>()).Returns((BudgetEntity?)null);

        var action = async () => await _sut.HandleAsync(new GetBudgetByIdQuery(budgetId), CancellationToken.None);

        await action.Should().ThrowAsync<BudgetNotFoundException>();
    }

    private static BudgetEntity BuildBudget(string name, decimal percentage, int year, int month, IReadOnlyList<Guid> categoryIds)
    {
        return BudgetEntity.Create(name, percentage, year, month, categoryIds, false, "user-test");
    }
}
