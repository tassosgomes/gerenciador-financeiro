using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class BudgetTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnInstance()
    {
        var categoryIds = new[] { Guid.NewGuid() };

        var budget = Budget.Create(
            "Lazer",
            20m,
            2026,
            3,
            categoryIds,
            true,
            "user-1");

        budget.Should().NotBeNull();
        budget.Name.Should().Be("Lazer");
        budget.Percentage.Should().Be(20m);
        budget.ReferenceYear.Should().Be(2026);
        budget.ReferenceMonth.Should().Be(3);
        budget.IsRecurrent.Should().BeTrue();
        budget.CategoryIds.Should().BeEquivalentTo(categoryIds);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var action = () => Budget.Create(
            string.Empty,
            10m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameTooShort_ShouldThrow()
    {
        var action = () => Budget.Create(
            "A",
            10m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldThrow()
    {
        var longName = new string('A', 151);

        var action = () => Budget.Create(
            longName,
            10m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroPercentage_ShouldThrow()
    {
        var action = () => Budget.Create(
            "Moradia",
            0m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativePercentage_ShouldThrow()
    {
        var action = () => Budget.Create(
            "Moradia",
            -1m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithPercentageOver100_ShouldThrow()
    {
        var action = () => Budget.Create(
            "Moradia",
            101m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithInvalidMonth_ShouldThrow()
    {
        var action = () => Budget.Create(
            "Moradia",
            10m,
            2026,
            13,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithEmptyCategoryIds_ShouldThrow()
    {
        var action = () => Budget.Create(
            "Moradia",
            10m,
            2026,
            3,
            [],
            false,
            "user-1");

        action.Should().Throw<BudgetMustHaveCategoriesException>();
    }

    [Fact]
    public void Create_ShouldSetCreatedByAndCreatedAt()
    {
        var budget = Budget.Create(
            "Moradia",
            10m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        budget.CreatedBy.Should().Be("user-1");
        budget.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateAllFields()
    {
        var budget = Budget.Create(
            "Moradia",
            30m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        var newCategoryIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        budget.Update("Lazer", 15m, newCategoryIds, true, "user-2");

        budget.Name.Should().Be("Lazer");
        budget.Percentage.Should().Be(15m);
        budget.IsRecurrent.Should().BeTrue();
        budget.CategoryIds.Should().BeEquivalentTo(newCategoryIds);
    }

    [Fact]
    public void Update_ShouldSetUpdatedByAndUpdatedAt()
    {
        var budget = Budget.Create(
            "Moradia",
            30m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        budget.Update("Lazer", 15m, new[] { Guid.NewGuid() }, false, "user-2");

        budget.UpdatedBy.Should().Be("user-2");
        budget.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void CalculateLimit_ShouldReturnCorrectValue()
    {
        var budget = Budget.Create(
            "Moradia",
            25m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        var limit = budget.CalculateLimit(4000m);

        limit.Should().Be(1000m);
    }

    [Fact]
    public void CalculateLimit_WithZeroIncome_ShouldReturnZero()
    {
        var budget = Budget.Create(
            "Moradia",
            25m,
            2026,
            3,
            new[] { Guid.NewGuid() },
            false,
            "user-1");

        var limit = budget.CalculateLimit(0m);

        limit.Should().Be(0m);
    }
}
