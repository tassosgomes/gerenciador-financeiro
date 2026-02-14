using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Service;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class InstallmentDomainServiceTests
{
    private readonly InstallmentDomainService _sut = new(new TransactionDomainService());

    [Fact]
    public void CreateInstallmentGroup_ValorDivisivel_CriaParcelasIguais()
    {
        var account = CreateActiveAccount();

        var transactions = _sut.CreateInstallmentGroup(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            90m,
            3,
            "Compra parcelada",
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 5),
            "user-1");

        transactions.Should().HaveCount(3);
        transactions.All(transaction => transaction.Amount == 30m).Should().BeTrue();
    }

    [Fact]
    public void CreateInstallmentGroup_ValorComResiduo_AplicaResiduoNaUltimaParcela()
    {
        var account = CreateActiveAccount();

        var transactions = _sut.CreateInstallmentGroup(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            3,
            "Compra parcelada",
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 5),
            "user-1");

        transactions[0].Amount.Should().Be(33.33m);
        transactions[1].Amount.Should().Be(33.33m);
        transactions[2].Amount.Should().Be(33.34m);
    }

    [Fact]
    public void CreateInstallmentGroup_ParcelasTemInfoCorreta_GrupoNumerosTotal()
    {
        var account = CreateActiveAccount();

        var transactions = _sut.CreateInstallmentGroup(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            3,
            "Compra parcelada",
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 5),
            "user-1",
            "op-installment");

        var groupId = transactions[0].InstallmentGroupId;
        transactions.Should().OnlyContain(transaction => transaction.InstallmentGroupId == groupId);
        transactions.Select(transaction => transaction.InstallmentNumber).Should().Equal(1, 2, 3);
        transactions.Should().OnlyContain(transaction => transaction.TotalInstallments == 3);
        transactions[0].OperationId.Should().Be("op-installment");
        transactions[1].OperationId.Should().BeNull();
        transactions[2].OperationId.Should().BeNull();
    }

    [Fact]
    public void AdjustInstallmentGroup_ParcelasPending_RedistribuiDiferenca()
    {
        var account = CreateActiveAccount(initialBalance: 500m);
        var transactions = _sut.CreateInstallmentGroup(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            90m,
            3,
            "Compra parcelada",
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 5),
            "user-1");

        var adjustments = _sut.AdjustInstallmentGroup(account, transactions, 99m, "user-2", "op-adjust");

        adjustments.Should().HaveCount(3);
        adjustments.All(adjustment => adjustment.Type == TransactionType.Debit).Should().BeTrue();
        adjustments.All(adjustment => adjustment.Amount == 3m).Should().BeTrue();
        adjustments[0].OperationId.Should().Be("op-adjust");
        adjustments[1].OperationId.Should().BeNull();
    }

    [Fact]
    public void AdjustInstallmentGroup_SemPending_LancaExcecao()
    {
        var account = CreateActiveAccount();
        var paidTransactions = new[]
        {
            CreateTransaction(TransactionStatus.Paid, amount: 10m, installmentNumber: 1),
            CreateTransaction(TransactionStatus.Paid, amount: 10m, installmentNumber: 2),
        };

        var action = () => _sut.AdjustInstallmentGroup(account, paidTransactions, 40m, "user-1");

        action.Should().Throw<NoPendingInstallmentsToAdjustException>();
    }

    [Fact]
    public void CancelSingleInstallment_ParcelaPending_CancelaComSucesso()
    {
        var account = CreateActiveAccount();
        var installment = CreateTransaction(TransactionStatus.Pending, 50m, installmentNumber: 1);

        _sut.CancelSingleInstallment(account, installment, "user-2", "Cancelamento");

        installment.Status.Should().Be(TransactionStatus.Cancelled);
    }

    [Fact]
    public void CancelSingleInstallment_ParcelaPaid_LancaInstallmentPaidCannotBeCancelledException()
    {
        var account = CreateActiveAccount();
        var installment = CreateTransaction(TransactionStatus.Paid, 50m, installmentNumber: 1);

        var action = () => _sut.CancelSingleInstallment(account, installment, "user-2", "Cancelamento");

        action.Should().Throw<InstallmentPaidCannotBeCancelledException>();
    }

    [Fact]
    public void CancelInstallmentGroup_MixPaidPending_CancelaSomentePending()
    {
        var account = CreateActiveAccount();
        var pendingA = CreateTransaction(TransactionStatus.Pending, 30m, installmentNumber: 1);
        var paid = CreateTransaction(TransactionStatus.Paid, 30m, installmentNumber: 2);
        var pendingB = CreateTransaction(TransactionStatus.Pending, 30m, installmentNumber: 3);
        var group = new[] { pendingA, paid, pendingB };

        _sut.CancelInstallmentGroup(account, group, "user-2", "Cancelamento em lote");

        pendingA.Status.Should().Be(TransactionStatus.Cancelled);
        pendingB.Status.Should().Be(TransactionStatus.Cancelled);
        paid.Status.Should().Be(TransactionStatus.Paid);
    }

    private static Account CreateActiveAccount(decimal initialBalance = 100m)
    {
        return Account.Create("Conta", AccountType.Corrente, initialBalance, true, "user-1");
    }

    private static Transaction CreateTransaction(TransactionStatus status, decimal amount, int installmentNumber)
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            amount,
            "Parcela",
            DateTime.UtcNow,
            DateTime.UtcNow,
            status,
            "user-1");

        transaction.SetInstallmentInfo(Guid.NewGuid(), installmentNumber, 3);
        return transaction;
    }
}
