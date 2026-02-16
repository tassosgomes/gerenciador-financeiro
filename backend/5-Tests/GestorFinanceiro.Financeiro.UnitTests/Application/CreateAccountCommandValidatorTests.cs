using AwesomeAssertions;
using FluentValidation.TestHelper;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class CreateAccountCommandValidatorTests
{
    private readonly CreateAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_TypeCarto_MissingCreditLimit_ShouldFail()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            null,
            15,
            25,
            Guid.NewGuid(),
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CreditLimit);
    }

    [Fact]
    public void Validate_TypeCarto_MissingClosingDay_ShouldFail()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            null,
            25,
            Guid.NewGuid(),
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ClosingDay);
    }

    [Fact]
    public void Validate_TypeCarto_MissingDueDay_ShouldFail()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            null,
            Guid.NewGuid(),
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDay);
    }

    [Fact]
    public void Validate_TypeCarto_MissingDebitAccountId_ShouldFail()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            null,
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DebitAccountId);
    }

    [Fact]
    public void Validate_TypeCarto_InvalidClosingDay_ShouldFail()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            30,
            25,
            Guid.NewGuid(),
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ClosingDay);
    }

    [Fact]
    public void Validate_TypeCarto_ValidFields_ShouldPass()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            Guid.NewGuid(),
            true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TypeCorrente_WithoutCreditFields_ShouldPass()
    {
        var command = new CreateAccountCommand(
            "Conta Corrente",
            AccountType.Corrente,
            100m,
            false,
            "user-1");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TypeCorrente_NegativeInitialBalance_ShouldFail()
    {
        var command = new CreateAccountCommand(
            "Conta Corrente",
            AccountType.Corrente,
            -100m,
            false,
            "user-1");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.InitialBalance);
    }
}
