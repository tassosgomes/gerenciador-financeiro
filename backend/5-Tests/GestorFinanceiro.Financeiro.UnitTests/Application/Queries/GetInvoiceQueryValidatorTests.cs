using AwesomeAssertions;
using FluentValidation.TestHelper;
using GestorFinanceiro.Financeiro.Application.Queries.Invoice;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Queries;

public class GetInvoiceQueryValidatorTests
{
    private readonly GetInvoiceQueryValidator _sut;

    public GetInvoiceQueryValidatorTests()
    {
        _sut = new GetInvoiceQueryValidator();
    }

    [Fact]
    public void Validate_WithEmptyAccountId_ShouldFail()
    {
        // Arrange
        var query = new GetInvoiceQuery(Guid.Empty, 3, 2026);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccountId)
            .WithErrorMessage("AccountId não pode ser vazio.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(100)]
    public void Validate_WithInvalidMonth_ShouldFail(int month)
    {
        // Arrange
        var query = new GetInvoiceQuery(Guid.NewGuid(), month, 2026);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Month)
            .WithErrorMessage("Month deve estar entre 1 e 12.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2026)]
    public void Validate_WithInvalidYear_ShouldFail(int year)
    {
        // Arrange
        var query = new GetInvoiceQuery(Guid.NewGuid(), 3, year);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year)
            .WithErrorMessage("Year deve ser maior que 0.");
    }

    [Fact]
    public void Validate_WithValidParameters_ShouldPass()
    {
        // Arrange
        var query = new GetInvoiceQuery(Guid.NewGuid(), 3, 2026);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
