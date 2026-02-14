using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Service;

namespace GestorFinanceiro.Financeiro.UnitTests;

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
}
