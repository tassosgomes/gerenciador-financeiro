using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class RecurrenceTemplateTests
{
    [Fact]
    public void Create_DadosValidos_CriaTemplateRecorrencia()
    {
        var template = RecurrenceTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            150m,
            "Internet",
            10,
            TransactionStatus.Pending,
            "user-1");

        template.Type.Should().Be(TransactionType.Debit);
        template.Amount.Should().Be(150m);
        template.Description.Should().Be("Internet");
        template.DayOfMonth.Should().Be(10);
        template.DefaultStatus.Should().Be(TransactionStatus.Pending);
        template.IsActive.Should().BeTrue();
        template.LastGeneratedDate.Should().BeNull();
    }

    [Fact]
    public void ShouldGenerateForMonth_SemGeracaoAnterior_RetornaTrue()
    {
        var template = RecurrenceTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            150m,
            "Internet",
            10,
            TransactionStatus.Pending,
            "user-1");

        var shouldGenerate = template.ShouldGenerateForMonth(new DateTime(2026, 2, 1));

        shouldGenerate.Should().BeTrue();
    }

    [Fact]
    public void ShouldGenerateForMonth_MesJaGerado_RetornaFalse()
    {
        var template = RecurrenceTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            150m,
            "Internet",
            10,
            TransactionStatus.Pending,
            "user-1");
        template.MarkGenerated(new DateTime(2026, 2, 10), "user-1");

        var shouldGenerate = template.ShouldGenerateForMonth(new DateTime(2026, 2, 20));

        shouldGenerate.Should().BeFalse();
    }

    [Fact]
    public void ShouldGenerateForMonth_MesPosteriorAoUltimoGerado_RetornaTrue()
    {
        var template = RecurrenceTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            150m,
            "Internet",
            10,
            TransactionStatus.Pending,
            "user-1");
        template.MarkGenerated(new DateTime(2026, 2, 10), "user-1");

        var shouldGenerate = template.ShouldGenerateForMonth(new DateTime(2026, 3, 1));

        shouldGenerate.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_TemplateAtivo_DesativaTemplate()
    {
        var template = RecurrenceTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            150m,
            "Internet",
            10,
            TransactionStatus.Pending,
            "user-1");

        template.Deactivate("user-2");

        template.IsActive.Should().BeFalse();
        template.UpdatedBy.Should().Be("user-2");
        template.UpdatedAt.Should().NotBeNull();
    }
}
