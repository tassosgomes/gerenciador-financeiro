using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class ListTransactionsQueryValidatorTests
{
    private readonly ListTransactionsQueryValidator _validator = new();

    [Fact]
    public void Validate_PageZero_ShouldBeInvalid()
    {
        var query = new ListTransactionsQuery(null, null, null, null, null, null, null, null, 0, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Page");
    }

    [Fact]
    public void Validate_SizeGreaterThan100_ShouldBeInvalid()
    {
        var query = new ListTransactionsQuery(null, null, null, null, null, null, null, null, 1, 101);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Size");
    }

    [Fact]
    public void Validate_DateFromGreaterThanDateTo_ShouldBeInvalid()
    {
        var query = new ListTransactionsQuery(
            null,
            null,
            null,
            null,
            new DateTime(2026, 2, 10),
            new DateTime(2026, 2, 1),
            null,
            null,
            1,
            20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("CompetenceDateFrom"));
    }
}
