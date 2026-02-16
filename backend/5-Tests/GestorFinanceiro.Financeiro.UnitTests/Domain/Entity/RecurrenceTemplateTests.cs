using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class RecurrenceTemplateTests
{
    [Fact]
    public void Create_DadosValidos_CriaTemplateAtivo()
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
    public void ShouldGenerateForMonth_SemLastGenerated_RetornaTrue()
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
    public void ShouldGenerateForMonth_MesIgualUltimoGerado_RetornaFalse()
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
    public void ShouldGenerateForMonth_MesMaiorQueUltimoGerado_RetornaTrue()
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
    public void ShouldGenerateForMonth_TemplateInativo_RetornaFalse()
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

        var shouldGenerate = template.ShouldGenerateForMonth(new DateTime(2026, 3, 1));

        shouldGenerate.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_TemplateAtivo_DesativaComSucesso()
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

    [Fact]
    public void MarkGenerated_Data_AtualizaLastGeneratedDate()
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
        var generatedDate = new DateTime(2026, 4, 10);

        template.MarkGenerated(generatedDate, "user-2");

        template.LastGeneratedDate.Should().Be(generatedDate);
        template.UpdatedBy.Should().Be("user-2");
        template.UpdatedAt.Should().NotBeNull();
    }
}
