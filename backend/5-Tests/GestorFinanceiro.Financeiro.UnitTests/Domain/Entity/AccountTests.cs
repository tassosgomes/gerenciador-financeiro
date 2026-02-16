using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class AccountTests
{
    [Fact]
    public void Create_DadosValidos_CriaContaComSaldoInicial()
    {
        var before = DateTime.UtcNow;

        var account = Account.Create("Conta Principal", AccountType.Corrente, 100m, false, "user-1");

        var after = DateTime.UtcNow;
        account.Name.Should().Be("Conta Principal");
        account.Type.Should().Be(AccountType.Corrente);
        account.Balance.Should().Be(100m);
        account.AllowNegativeBalance.Should().BeFalse();
        account.IsActive.Should().BeTrue();
        account.CreatedBy.Should().Be("user-1");
        account.CreatedAt.Should().BeOnOrAfter(before);
        account.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Activate_ContaInativa_AtivaENaoLancaExcecao()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 0m, false, "user-1");
        account.Deactivate("user-1");

        account.Activate("user-2");

        account.IsActive.Should().BeTrue();
        account.UpdatedBy.Should().Be("user-2");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ContaAtiva_DesativaComSucesso()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 0m, false, "user-1");

        account.Deactivate("user-2");

        account.IsActive.Should().BeFalse();
        account.UpdatedBy.Should().Be("user-2");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyDebit_SaldoSuficiente_DiminuiSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");

        account.ApplyDebit(40m, "user-2");

        account.Balance.Should().Be(60m);
    }

    [Fact]
    public void ApplyCredit_ValorPositivo_AumentaSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");

        account.ApplyCredit(25m, "user-2");

        account.Balance.Should().Be(125m);
    }

    [Fact]
    public void RevertDebit_ValorDebito_AumentaSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        account.ApplyDebit(30m, "user-2");

        account.RevertDebit(30m, "user-3");

        account.Balance.Should().Be(100m);
        account.UpdatedBy.Should().Be("user-3");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RevertCredit_ValorCredito_DiminuiSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        account.ApplyCredit(45m, "user-2");

        account.RevertCredit(45m, "user-3");

        account.Balance.Should().Be(100m);
        account.UpdatedBy.Should().Be("user-3");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyDebit_SaldoInsuficienteSemPermissao_LancaInsufficientBalanceException()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");

        var action = () => account.ApplyDebit(101m, "user-2");

        action.Should().Throw<InsufficientBalanceException>();
    }

    [Fact]
    public void ApplyDebit_SaldoInsuficienteComPermissao_PermiteDebito()
    {
        var account = Account.Create("Conta", AccountType.Cartao, 100m, true, "user-1");

        account.ApplyDebit(150m, "user-2");

        account.Balance.Should().Be(-50m);
    }

    [Fact]
    public void ValidateCanReceiveTransaction_ContaInativa_LancaInactiveAccountException()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        account.Deactivate("user-1");

        var action = () => account.ValidateCanReceiveTransaction();

        action.Should().Throw<InactiveAccountException>();
    }

    [Fact]
    public void ValidateCanReceiveTransaction_ContaAtiva_NaoLancaExcecao()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");

        var action = () => account.ValidateCanReceiveTransaction();

        action.Should().NotThrow();
    }

    [Fact]
    public void CreateCreditCard_WithValidParameters_ShouldSetBalanceToZero()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void CreateCreditCard_WithValidParameters_ShouldSetAllowNegativeBalanceToTrue()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.AllowNegativeBalance.Should().BeTrue();
    }

    [Fact]
    public void CreateCreditCard_WithValidParameters_ShouldSetTypeToCartao()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.Type.Should().Be(AccountType.Cartao);
    }

    [Fact]
    public void CreateCreditCard_WithValidParameters_ShouldHaveCreditCardDetailsPopulated()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.CreditCard.Should().NotBeNull();
        account.CreditCard!.CreditLimit.Should().Be(5000m);
        account.CreditCard.ClosingDay.Should().Be(10);
        account.CreditCard.DueDay.Should().Be(15);
        account.CreditCard.DebitAccountId.Should().Be(debitAccountId);
        account.CreditCard.EnforceCreditLimit.Should().BeTrue();
    }

    [Fact]
    public void CreateCreditCard_WithInvalidCreditLimit_ShouldThrowException()
    {
        var debitAccountId = Guid.NewGuid();

        var action = () => Account.CreateCreditCard(
            "Nubank",
            -100m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        action.Should().Throw<InvalidCreditCardConfigException>();
    }

    [Fact]
    public void UpdateCreditCard_WithValidParameters_ShouldUpdateNameAndDetails()
    {
        var debitAccountId = Guid.NewGuid();
        var newDebitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.UpdateCreditCard(
            "Nubank Gold",
            8000m,
            12,
            17,
            newDebitAccountId,
            false,
            "user-2");

        account.Name.Should().Be("Nubank Gold");
        account.CreditCard!.CreditLimit.Should().Be(8000m);
        account.CreditCard.ClosingDay.Should().Be(12);
        account.CreditCard.DueDay.Should().Be(17);
        account.CreditCard.DebitAccountId.Should().Be(newDebitAccountId);
        account.CreditCard.EnforceCreditLimit.Should().BeFalse();
    }

    [Fact]
    public void UpdateCreditCard_WhenNotCreditCard_ShouldThrowInvalidCreditCardConfigException()
    {
        var account = Account.Create("Conta Corrente", AccountType.Corrente, 100m, false, "user-1");
        var debitAccountId = Guid.NewGuid();

        var action = () => account.UpdateCreditCard(
            "Nome",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-2");

        action.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Conta n\u00e3o \u00e9 um cart\u00e3o de cr\u00e9dito.");
    }

    [Fact]
    public void UpdateCreditCard_ShouldUpdateAudit()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.UpdateCreditCard(
            "Nubank Gold",
            8000m,
            10,
            15,
            debitAccountId,
            true,
            "user-2");

        account.UpdatedBy.Should().Be("user-2");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ValidateCreditLimit_WhenCreditCardIsNull_ShouldNotThrow()
    {
        var account = Account.Create("Conta Corrente", AccountType.Corrente, 100m, false, "user-1");

        var action = () => account.ValidateCreditLimit(200m);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateCreditLimit_WhenEnforceIsFalse_ShouldNotThrow()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            false,
            "user-1");

        account.ApplyDebit(4500m, "user-1");

        var action = () => account.ValidateCreditLimit(1000m);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateCreditLimit_WhenAmountExceedsLimit_ShouldThrowCreditLimitExceededException()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.ApplyDebit(4500m, "user-1");

        var action = () => account.ValidateCreditLimit(1000m);

        action.Should().Throw<CreditLimitExceededException>();
    }

    [Fact]
    public void ValidateCreditLimit_WhenAmountWithinLimit_ShouldNotThrow()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.ApplyDebit(3000m, "user-1");

        var action = () => account.ValidateCreditLimit(1500m);

        action.Should().NotThrow();
    }

    [Fact]
    public void GetAvailableLimit_WhenCreditCardIsNull_ShouldReturnZero()
    {
        var account = Account.Create("Conta Corrente", AccountType.Corrente, 100m, false, "user-1");

        var availableLimit = account.GetAvailableLimit();

        availableLimit.Should().Be(0m);
    }

    [Fact]
    public void GetAvailableLimit_WithNegativeBalance_ShouldReturnLimitMinusAbsBalance()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.ApplyDebit(2000m, "user-1");

        var availableLimit = account.GetAvailableLimit();

        availableLimit.Should().Be(3000m);
    }

    [Fact]
    public void GetAvailableLimit_WithPositiveBalance_ShouldReturnLimitMinusAbsBalance()
    {
        var debitAccountId = Guid.NewGuid();

        var account = Account.CreateCreditCard(
            "Nubank",
            5000m,
            10,
            15,
            debitAccountId,
            true,
            "user-1");

        account.ApplyCredit(500m, "user-1");

        var availableLimit = account.GetAvailableLimit();

        availableLimit.Should().Be(4500m);
    }
}
