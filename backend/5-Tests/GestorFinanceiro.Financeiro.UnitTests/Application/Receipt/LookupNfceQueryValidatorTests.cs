using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Receipt;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Receipt;

public class LookupNfceQueryValidatorTests
{
    private readonly LookupNfceQueryValidator _validator = new();

    [Fact]
    public void Validate_EmptyInput_ShouldBeInvalid()
    {
        var result = _validator.Validate(new LookupNfceQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Input");
    }

    [Fact]
    public void Validate_ValidAccessKey_ShouldBeValid()
    {
        var result = _validator.Validate(new LookupNfceQuery("12345678901234567890123456789012345678901234"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789012345678901234567890123456789012345")]
    [InlineData("1234567890123456789012345678901234567890123A")]
    public void Validate_InvalidAccessKey_ShouldBeInvalid(string input)
    {
        var result = _validator.Validate(new LookupNfceQuery(input));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidUrl_ShouldBeValid()
    {
        var result = _validator.Validate(new LookupNfceQuery("https://www.sefaz.pb.gov.br/nfce"));

        result.IsValid.Should().BeTrue();
    }
}
