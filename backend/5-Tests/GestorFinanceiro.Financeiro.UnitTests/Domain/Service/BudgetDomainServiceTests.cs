using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using NSubstitute;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Service;

public class BudgetDomainServiceTests
{
    private readonly BudgetDomainService _sut = new();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();

    [Fact]
    public async Task ValidatePercentageCap_WhenWithinLimit_ShouldNotThrow()
    {
        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 3, null, Arg.Any<CancellationToken>())
            .Returns(40m);

        var action = async () => await _sut.ValidatePercentageCapAsync(
            _budgetRepository,
            2026,
            3,
            50m,
            null,
            CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidatePercentageCap_WhenExactly100_ShouldNotThrow()
    {
        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 3, null, Arg.Any<CancellationToken>())
            .Returns(80m);

        var action = async () => await _sut.ValidatePercentageCapAsync(
            _budgetRepository,
            2026,
            3,
            20m,
            null,
            CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidatePercentageCap_WhenExceeds100_ShouldThrowBudgetPercentageExceededException()
    {
        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 3, null, Arg.Any<CancellationToken>())
            .Returns(95m);

        var action = async () => await _sut.ValidatePercentageCapAsync(
            _budgetRepository,
            2026,
            3,
            10m,
            null,
            CancellationToken.None);

        await action.Should().ThrowAsync<BudgetPercentageExceededException>();
    }

    [Fact]
    public async Task ValidatePercentageCap_WithExcludeBudgetId_ShouldExcludeFromSum()
    {
        var excludeBudgetId = Guid.NewGuid();
        _budgetRepository
            .GetTotalPercentageForMonthAsync(2026, 3, excludeBudgetId, Arg.Any<CancellationToken>())
            .Returns(60m);

        var action = async () => await _sut.ValidatePercentageCapAsync(
            _budgetRepository,
            2026,
            3,
            20m,
            excludeBudgetId,
            CancellationToken.None);

        await action.Should().NotThrowAsync();
        await _budgetRepository.Received(1)
            .GetTotalPercentageForMonthAsync(2026, 3, excludeBudgetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateCategoryUniqueness_WhenNoDuplicate_ShouldNotThrow()
    {
        var categories = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _budgetRepository
            .IsCategoryUsedInMonthAsync(Arg.Any<Guid>(), 2026, 3, null, Arg.Any<CancellationToken>())
            .Returns(false);

        var action = async () => await _sut.ValidateCategoryUniquenessAsync(
            _budgetRepository,
            categories,
            2026,
            3,
            null,
            CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateCategoryUniqueness_WhenDuplicate_ShouldThrowCategoryAlreadyBudgetedException()
    {
        var categoryId = Guid.NewGuid();
        var categories = new[] { categoryId };
        _budgetRepository
            .IsCategoryUsedInMonthAsync(categoryId, 2026, 3, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var action = async () => await _sut.ValidateCategoryUniquenessAsync(
            _budgetRepository,
            categories,
            2026,
            3,
            null,
            CancellationToken.None);

        await action.Should().ThrowAsync<CategoryAlreadyBudgetedException>();
    }

    [Fact]
    public void ValidateReferenceMonth_WithCurrentMonth_ShouldNotThrow()
    {
        var currentDate = DateTime.UtcNow;

        var action = () => _sut.ValidateReferenceMonth(currentDate.Year, currentDate.Month);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateReferenceMonth_WithFutureMonth_ShouldNotThrow()
    {
        var futureDate = DateTime.UtcNow.AddMonths(1);

        var action = () => _sut.ValidateReferenceMonth(futureDate.Year, futureDate.Month);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateReferenceMonth_WithPastMonth_ShouldThrowBudgetPeriodLockedException()
    {
        var pastDate = DateTime.UtcNow.AddMonths(-1);

        var action = () => _sut.ValidateReferenceMonth(pastDate.Year, pastDate.Month);

        action.Should().Throw<BudgetPeriodLockedException>();
    }
}
