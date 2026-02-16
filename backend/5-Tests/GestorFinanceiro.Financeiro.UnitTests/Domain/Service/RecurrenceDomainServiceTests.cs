using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Service;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Service;

public class RecurrenceDomainServiceTests
{
    private readonly RecurrenceDomainService _sut = new(new TransactionDomainService());

    [Fact]
    public void GenerateNextOccurrence_MesNaoGerado_CriaTransacaoRecorrente()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var template = CreateTemplate(dayOfMonth: 10, status: TransactionStatus.Pending);

        var transaction = _sut.GenerateNextOccurrence(template, account, new DateTime(2026, 2, 1), "user-1");

        transaction.Should().NotBeNull();
        transaction!.IsRecurrent.Should().BeTrue();
        transaction.RecurrenceTemplateId.Should().Be(template.Id);
        template.LastGeneratedDate.Should().Be(new DateTime(2026, 2, 10));
    }

    [Fact]
    public void GenerateNextOccurrence_MesJaGerado_RetornaNull()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var template = CreateTemplate(dayOfMonth: 10, status: TransactionStatus.Pending);
        template.MarkGenerated(new DateTime(2026, 2, 10), "user-1");

        var transaction = _sut.GenerateNextOccurrence(template, account, new DateTime(2026, 2, 15), "user-1");

        transaction.Should().BeNull();
    }

    [Fact]
    public void GenerateNextOccurrence_TemplateInativo_RetornaNull()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var template = CreateTemplate(dayOfMonth: 10, status: TransactionStatus.Pending);
        template.Deactivate("user-2");

        var transaction = _sut.GenerateNextOccurrence(template, account, new DateTime(2026, 2, 1), "user-1");

        transaction.Should().BeNull();
    }

    [Fact]
    public void GenerateNextOccurrence_Dia31EmMesCom28Dias_NormalizaDia()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var template = CreateTemplate(dayOfMonth: 31, status: TransactionStatus.Pending);

        var transaction = _sut.GenerateNextOccurrence(template, account, new DateTime(2026, 2, 1), "user-1");

        transaction.Should().NotBeNull();
        transaction!.CompetenceDate.Should().Be(new DateTime(2026, 2, 28));
        transaction.DueDate.Should().Be(new DateTime(2026, 2, 28));
    }

    private static RecurrenceTemplate CreateTemplate(int dayOfMonth, TransactionStatus status)
    {
        return RecurrenceTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            80m,
            "Internet",
            dayOfMonth,
            status,
            "user-1");
    }
}
