using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Service;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Service;

public class TransferDomainServiceTests
{
    private readonly TransferDomainService _sut = new(new TransactionDomainService());

    [Fact]
    public void CreateTransfer_ContasAtivas_CriaDebitECreditComTransferGroup()
    {
        var source = Account.Create("Conta Origem", AccountType.Corrente, 500m, false, "user-1");
        var destination = Account.Create("Conta Destino", AccountType.Corrente, 100m, false, "user-1");

        var result = _sut.CreateTransfer(
            source,
            destination,
            Guid.NewGuid(),
            120m,
            "Reserva",
            new DateTime(2026, 2, 10),
            "user-1");

        result.debit.Type.Should().Be(TransactionType.Debit);
        result.credit.Type.Should().Be(TransactionType.Credit);
        result.debit.TransferGroupId.Should().NotBeNull();
        result.debit.TransferGroupId.Should().Be(result.credit.TransferGroupId);
    }

    [Fact]
    public void CreateTransfer_ContaOrigem_SaldoReduzido()
    {
        var source = Account.Create("Conta Origem", AccountType.Corrente, 500m, false, "user-1");
        var destination = Account.Create("Conta Destino", AccountType.Corrente, 100m, false, "user-1");

        _sut.CreateTransfer(
            source,
            destination,
            Guid.NewGuid(),
            120m,
            "Reserva",
            new DateTime(2026, 2, 10),
            "user-1");

        source.Balance.Should().Be(380m);
    }

    [Fact]
    public void CreateTransfer_ContaDestino_SaldoAumentado()
    {
        var source = Account.Create("Conta Origem", AccountType.Corrente, 500m, false, "user-1");
        var destination = Account.Create("Conta Destino", AccountType.Corrente, 100m, false, "user-1");

        _sut.CreateTransfer(
            source,
            destination,
            Guid.NewGuid(),
            120m,
            "Reserva",
            new DateTime(2026, 2, 10),
            "user-1");

        destination.Balance.Should().Be(220m);
    }

    [Fact]
    public void CancelTransfer_TransferenciaExistente_ReverteSaldosEmAmbasContas()
    {
        var source = Account.Create("Conta Origem", AccountType.Corrente, 500m, false, "user-1");
        var destination = Account.Create("Conta Destino", AccountType.Corrente, 100m, false, "user-1");
        var transfer = _sut.CreateTransfer(
            source,
            destination,
            Guid.NewGuid(),
            120m,
            "Reserva",
            new DateTime(2026, 2, 10),
            "user-1");

        _sut.CancelTransfer(source, destination, transfer.debit, transfer.credit, "user-2", "Erro de lancamento");

        source.Balance.Should().Be(500m);
        destination.Balance.Should().Be(100m);
        transfer.debit.Status.Should().Be(TransactionStatus.Cancelled);
        transfer.credit.Status.Should().Be(TransactionStatus.Cancelled);
    }

    [Fact]
    public void CreateInvoicePayment_WithValidAccounts_ShouldReturnTwoTransactions()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1500m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        result.Should().HaveCount(2);
    }

    [Fact]
    public void CreateInvoicePayment_ShouldCreateDebitOnDebitAccount()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1500m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        var debitTransaction = result.First(t => t.Type == TransactionType.Debit);
        debitTransaction.AccountId.Should().Be(debitAccount.Id);
        debitTransaction.Amount.Should().Be(1500m);
        debitAccount.Balance.Should().Be(500m);
    }

    [Fact]
    public void CreateInvoicePayment_ShouldCreateCreditOnCardAccount()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1500m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        var creditTransaction = result.First(t => t.Type == TransactionType.Credit);
        creditTransaction.AccountId.Should().Be(creditCardAccount.Id);
        creditTransaction.Amount.Should().Be(1500m);
        creditCardAccount.Balance.Should().Be(0m);
    }

    [Fact]
    public void CreateInvoicePayment_ShouldSetTransferGroupId()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1500m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        var debitTransaction = result.First(t => t.Type == TransactionType.Debit);
        var creditTransaction = result.First(t => t.Type == TransactionType.Credit);

        debitTransaction.TransferGroupId.Should().NotBeNull();
        creditTransaction.TransferGroupId.Should().NotBeNull();
        debitTransaction.TransferGroupId.Should().Be(creditTransaction.TransferGroupId);
    }

    [Fact]
    public void CreateInvoicePayment_ShouldUseInvoicePaymentDescription()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1500m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        var debitTransaction = result.First(t => t.Type == TransactionType.Debit);
        var creditTransaction = result.First(t => t.Type == TransactionType.Credit);

        var expectedDescription = $"Pgto. Fatura — {creditCardAccount.Name}";
        debitTransaction.Description.Should().Be(expectedDescription);
        creditTransaction.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public void CreateInvoicePayment_DebitAccountWithInsufficientBalance_ShouldThrow()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 500m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var action = () => _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1500m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        action.Should().Throw<System.Exception>();
        debitAccount.Balance.Should().Be(500m);
    }

    [Fact]
    public void CreateInvoicePayment_PartialPayment_ShouldSucceed()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 2000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            1000m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        result.Should().HaveCount(2);
        debitAccount.Balance.Should().Be(1000m);
        creditCardAccount.Balance.Should().Be(-500m);
    }

    [Fact]
    public void CreateInvoicePayment_OverPayment_ShouldSucceed()
    {
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 3000m, false, "user-1");
        var creditCardAccount = Account.CreateCreditCard("Cartão Nubank", 5000m, 5, 10, debitAccount.Id, true, "user-1");
        creditCardAccount.ApplyDebit(1500m, "user-1");

        var result = _sut.CreateInvoicePayment(
            debitAccount,
            creditCardAccount,
            2000m,
            new DateTime(2026, 2, 10),
            Guid.NewGuid(),
            "user-1",
            "op-123");

        result.Should().HaveCount(2);
        debitAccount.Balance.Should().Be(1000m);
        creditCardAccount.Balance.Should().Be(500m);
    }
}
