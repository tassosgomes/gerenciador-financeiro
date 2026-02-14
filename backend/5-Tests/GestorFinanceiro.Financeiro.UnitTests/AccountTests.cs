using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class AccountTests
{
    [Fact]
    public void Create_DadosValidos_CriaContaComAuditoria()
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
    public void Activate_ContaInativa_TornaContaAtiva()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 0m, false, "user-1");
        account.Deactivate("user-1");

        account.Activate("user-2");

        account.IsActive.Should().BeTrue();
        account.UpdatedBy.Should().Be("user-2");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ContaAtiva_TornaContaInativa()
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
    public void ApplyCredit_ValorValido_AumentaSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");

        account.ApplyCredit(25m, "user-2");

        account.Balance.Should().Be(125m);
    }

    [Fact]
    public void RevertDebit_DebitoAplicadoAnteriormente_ReverteSaldoCorretamente()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        account.ApplyDebit(30m, "user-2");

        account.RevertDebit(30m, "user-3");

        account.Balance.Should().Be(100m);
        account.UpdatedBy.Should().Be("user-3");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RevertCredit_CreditoAplicadoAnteriormente_ReverteSaldoCorretamente()
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
    public void ApplyDebit_SaldoInsuficienteComPermissao_AceitaSaldoNegativo()
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
}
