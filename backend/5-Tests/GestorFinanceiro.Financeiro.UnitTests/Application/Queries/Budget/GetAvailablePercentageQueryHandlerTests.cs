using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Budget;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Queries.Budget;

public class GetAvailablePercentageQueryHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();

    private readonly GetAvailablePercentageQueryHandler _sut;

    public GetAvailablePercentageQueryHandlerTests()
    {
        _sut = new GetAvailablePercentageQueryHandler(
            _budgetRepository,
            NullLogger<GetAvailablePercentageQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithSomeBudgets_ShouldReturnCorrectAvailable()
    {
        var usedCategoryIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 2, null, Arg.Any<CancellationToken>())
            .Returns(65m);

        _budgetRepository
            .GetUsedCategoryIdsForMonthAsync(2026, 2, null, Arg.Any<CancellationToken>())
            .Returns(usedCategoryIds);

        var result = await _sut.HandleAsync(new GetAvailablePercentageQuery(2026, 2), CancellationToken.None);

        result.UsedPercentage.Should().Be(65m);
        result.AvailablePercentage.Should().Be(35m);
        result.UsedCategoryIds.Should().BeEquivalentTo(usedCategoryIds);
    }

    [Fact]
    public async Task Handle_WithNoBudgets_ShouldReturn100Available()
    {
        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 2, null, Arg.Any<CancellationToken>())
            .Returns(0m);

        _budgetRepository
            .GetUsedCategoryIdsForMonthAsync(2026, 2, null, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.HandleAsync(new GetAvailablePercentageQuery(2026, 2), CancellationToken.None);

        result.UsedPercentage.Should().Be(0m);
        result.AvailablePercentage.Should().Be(100m);
        result.UsedCategoryIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithExcludeBudgetId_ShouldExcludeFromCalculation()
    {
        var excludeBudgetId = Guid.NewGuid();

        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 2, excludeBudgetId, Arg.Any<CancellationToken>())
            .Returns(40m);

        _budgetRepository
            .GetUsedCategoryIdsForMonthAsync(2026, 2, excludeBudgetId, Arg.Any<CancellationToken>())
            .Returns([Guid.NewGuid()]);

        var result = await _sut.HandleAsync(new GetAvailablePercentageQuery(2026, 2, excludeBudgetId), CancellationToken.None);

        result.AvailablePercentage.Should().Be(60m);

        await _budgetRepository.Received(1)
            .GetTotalPercentageForMonthAsync(2026, 2, excludeBudgetId, Arg.Any<CancellationToken>());
        await _budgetRepository.Received(1)
            .GetUsedCategoryIdsForMonthAsync(2026, 2, excludeBudgetId, Arg.Any<CancellationToken>());
    }
}
