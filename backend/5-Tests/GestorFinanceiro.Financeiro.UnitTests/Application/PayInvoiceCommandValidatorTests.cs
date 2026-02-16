using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Invoice;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class PayInvoiceCommandValidatorTests
{
    private readonly PayInvoiceCommandValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyAccountId_ShouldFail()
    {
        var command = new PayInvoiceCommand(
            Guid.Empty,
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "CreditCardAccountId");
    }

    [Fact]
    public void Validate_WithZeroAmount_ShouldFail()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            0m,
            DateTime.UtcNow.AddDays(-1),
            "user-1");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_WithNegativeAmount_ShouldFail()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            -100m,
            DateTime.UtcNow.AddDays(-1),
            "user-1");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_WithFutureCompetenceDate_ShouldFail()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            1500m,
            DateTime.UtcNow.AddDays(1),
            "user-1");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "CompetenceDate");
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldFail()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_WithValidParameters_ShouldPass()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            1500m,
            DateTime.UtcNow.AddDays(-1),
            "user-1",
            "op-123");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCurrentDate_ShouldPass()
    {
        var command = new PayInvoiceCommand(
            Guid.NewGuid(),
            1500m,
            DateTime.UtcNow.AddMinutes(-1),
            "user-1");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
