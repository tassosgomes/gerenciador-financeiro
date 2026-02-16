using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class TransactionTests
{
    [Fact]
    public void Create_ValorPositivo_CriaTransacaoComStatusCorreto()
    {
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var competenceDate = new DateTime(2026, 2, 1);
        var dueDate = new DateTime(2026, 2, 10);

        var transaction = Transaction.Create(
            accountId,
            categoryId,
            TransactionType.Debit,
            10m,
            "Padaria",
            competenceDate,
            dueDate,
            TransactionStatus.Pending,
            "user-1",
            "op-1");

        transaction.AccountId.Should().Be(accountId);
        transaction.CategoryId.Should().Be(categoryId);
        transaction.Type.Should().Be(TransactionType.Debit);
        transaction.Amount.Should().Be(10m);
        transaction.Description.Should().Be("Padaria");
        transaction.CompetenceDate.Should().Be(competenceDate);
        transaction.DueDate.Should().Be(dueDate);
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.OperationId.Should().Be("op-1");
        transaction.IsAdjustment.Should().BeFalse();
        transaction.HasAdjustment.Should().BeFalse();
    }

    [Fact]
    public void Create_ValorZero_LancaInvalidTransactionAmountException()
    {
        var action = () => Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Credit,
            0m,
            "Receita",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        action.Should().Throw<InvalidTransactionAmountException>();
    }

    [Fact]
    public void Create_ValorNegativo_LancaInvalidTransactionAmountException()
    {
        var action = () => Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Credit,
            -10m,
            "Receita",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        action.Should().Throw<InvalidTransactionAmountException>();
    }

    [Fact]
    public void Cancel_TransacaoPending_AlteraStatusParaCancelled()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            50m,
            "Conta de luz",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(3),
            TransactionStatus.Pending,
            "user-1");

        transaction.Cancel("user-2", "Lancamento duplicado");

        transaction.Status.Should().Be(TransactionStatus.Cancelled);
        transaction.CancellationReason.Should().Be("Lancamento duplicado");
        transaction.CancelledBy.Should().Be("user-2");
        transaction.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_TransacaoJaCancelada_LancaTransactionAlreadyCancelledException()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            50m,
            "Conta de luz",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(3),
            TransactionStatus.Pending,
            "user-1");
        transaction.Cancel("user-2");

        var action = () => transaction.Cancel("user-3");

        action.Should().Throw<TransactionAlreadyCancelledException>();
    }

    [Fact]
    public void CreateAdjustment_DiferencaPositiva_CriaAjusteVinculado()
    {
        var originalId = Guid.NewGuid();

        var adjustment = Transaction.CreateAdjustment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Credit,
            20m,
            originalId,
            "Ajuste de valor",
            DateTime.UtcNow,
            "user-1",
            "op-adjust");

        adjustment.IsAdjustment.Should().BeTrue();
        adjustment.OriginalTransactionId.Should().Be(originalId);
        adjustment.Status.Should().Be(TransactionStatus.Paid);
        adjustment.OperationId.Should().Be("op-adjust");
    }

    [Fact]
    public void MarkAsAdjusted_TransacaoNormal_DefineHasAdjustmentTrue()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            "Compra",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        transaction.MarkAsAdjusted("user-2");

        transaction.HasAdjustment.Should().BeTrue();
        transaction.UpdatedBy.Should().Be("user-2");
    }

    [Fact]
    public void IsOverdue_PendingComDueDatePassada_RetornaTrue()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            10m,
            "Despesa",
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1),
            TransactionStatus.Pending,
            "user-1");

        transaction.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_PendingComDueDateFutura_RetornaFalse()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            10m,
            "Despesa",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(3),
            TransactionStatus.Pending,
            "user-1");

        transaction.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_PaidComDueDatePassada_RetornaFalse()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            10m,
            "Despesa",
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1),
            TransactionStatus.Paid,
            "user-1");

        transaction.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_PendingSemDueDate_RetornaFalse()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            10m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Pending,
            "user-1");

        transaction.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void SetInstallmentInfo_DadosValidos_DefineGrupoENumero()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            "Parcela",
            DateTime.UtcNow,
            DateTime.UtcNow,
            TransactionStatus.Pending,
            "user-1");
        var groupId = Guid.NewGuid();

        transaction.SetInstallmentInfo(groupId, 2, 10);

        transaction.InstallmentGroupId.Should().Be(groupId);
        transaction.InstallmentNumber.Should().Be(2);
        transaction.TotalInstallments.Should().Be(10);
    }

    [Fact]
    public void SetRecurrenceInfo_TemplateId_DefineFlagETemplate()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Credit,
            200m,
            "Receita",
            DateTime.UtcNow,
            DateTime.UtcNow,
            TransactionStatus.Pending,
            "user-1");
        var templateId = Guid.NewGuid();

        transaction.SetRecurrenceInfo(templateId);

        transaction.IsRecurrent.Should().BeTrue();
        transaction.RecurrenceTemplateId.Should().Be(templateId);
    }

    [Fact]
    public void SetTransferGroup_TransferenciaValida_DefineTransferGroupId()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            80m,
            "Transferencia",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");
        var transferGroupId = Guid.NewGuid();

        transaction.SetTransferGroup(transferGroupId);

        transaction.TransferGroupId.Should().Be(transferGroupId);
    }
}
