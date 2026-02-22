using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Exception;

public class BudgetExceptionsTests
{
    [Fact]
    public void BudgetNotFoundException_ShouldInheritDomainException_AndContainBudgetId()
    {
        var budgetId = Guid.NewGuid();
        var exception = new BudgetNotFoundException(budgetId);

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain(budgetId.ToString());
    }

    [Fact]
    public void BudgetPercentageExceededException_ShouldInheritDomainException_AndContainData()
    {
        var exception = new BudgetPercentageExceededException(30m, 20m, 3, 2026);

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain("30");
        exception.Message.Should().Contain("20");
        exception.Message.Should().Contain("03/2026");
    }

    [Fact]
    public void CategoryAlreadyBudgetedException_ShouldInheritDomainException_AndContainData()
    {
        var categoryId = Guid.NewGuid();
        var exception = new CategoryAlreadyBudgetedException(categoryId, "Moradia", 3, 2026);

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain(categoryId.ToString());
        exception.Message.Should().Contain("Moradia");
        exception.Message.Should().Contain("03/2026");
    }

    [Fact]
    public void BudgetPeriodLockedException_ShouldInheritDomainException_AndContainPeriod()
    {
        var budgetId = Guid.NewGuid();
        var exception = new BudgetPeriodLockedException(budgetId, 2, 2025);

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain(budgetId.ToString());
        exception.Message.Should().Contain("02/2025");
    }

    [Fact]
    public void BudgetMustHaveCategoriesException_ShouldInheritDomainException_AndContainData()
    {
        var budgetId = Guid.NewGuid();
        var exception = new BudgetMustHaveCategoriesException(budgetId);

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain(budgetId.ToString());
    }

    [Fact]
    public void BudgetNameAlreadyExistsException_ShouldInheritDomainException_AndContainName()
    {
        var exception = new BudgetNameAlreadyExistsException("Moradia");

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain("Moradia");
    }

    [Fact]
    public void InvalidBudgetCategoryTypeException_ShouldInheritDomainException_AndContainCategoryId()
    {
        var categoryId = Guid.NewGuid();
        var exception = new InvalidBudgetCategoryTypeException(categoryId);

        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Contain(categoryId.ToString());
    }
}
