using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class CreditCardDetailsTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnInstance()
    {
        // Arrange
        var creditLimit = 5000m;
        var closingDay = 3;
        var dueDay = 10;
        var debitAccountId = Guid.NewGuid();
        var enforceCreditLimit = true;

        // Act
        var details = CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit);

        // Assert
        details.Should().NotBeNull();
        details.CreditLimit.Should().Be(creditLimit);
        details.ClosingDay.Should().Be(closingDay);
        details.DueDay.Should().Be(dueDay);
        details.DebitAccountId.Should().Be(debitAccountId);
        details.EnforceCreditLimit.Should().Be(enforceCreditLimit);
    }

    [Fact]
    public void Create_WithZeroCreditLimit_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = 0m;
        var closingDay = 3;
        var dueDay = 10;
        var debitAccountId = Guid.NewGuid();

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Limite de crédito deve ser maior que zero.");
    }

    [Fact]
    public void Create_WithNegativeCreditLimit_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = -100m;
        var closingDay = 3;
        var dueDay = 10;
        var debitAccountId = Guid.NewGuid();

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Limite de crédito deve ser maior que zero.");
    }

    [Fact]
    public void Create_WithClosingDayLessThan1_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = 5000m;
        var closingDay = 0;
        var dueDay = 10;
        var debitAccountId = Guid.NewGuid();

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Dia de fechamento deve estar entre 1 e 28.");
    }

    [Fact]
    public void Create_WithClosingDayGreaterThan28_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = 5000m;
        var closingDay = 29;
        var dueDay = 10;
        var debitAccountId = Guid.NewGuid();

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Dia de fechamento deve estar entre 1 e 28.");
    }

    [Fact]
    public void Create_WithDueDayLessThan1_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = 5000m;
        var closingDay = 3;
        var dueDay = 0;
        var debitAccountId = Guid.NewGuid();

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Dia de vencimento deve estar entre 1 e 28.");
    }

    [Fact]
    public void Create_WithDueDayGreaterThan28_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = 5000m;
        var closingDay = 3;
        var dueDay = 29;
        var debitAccountId = Guid.NewGuid();

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Dia de vencimento deve estar entre 1 e 28.");
    }

    [Fact]
    public void Create_WithEmptyDebitAccountId_ShouldThrowInvalidCreditCardConfigException()
    {
        // Arrange
        var creditLimit = 5000m;
        var closingDay = 3;
        var dueDay = 10;

        // Act
        var act = () => CreditCardDetails.Create(creditLimit, closingDay, dueDay, Guid.Empty, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Conta de débito é obrigatória.");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateProperties()
    {
        // Arrange
        var details = CreditCardDetails.Create(5000m, 3, 10, Guid.NewGuid(), true);
        var newCreditLimit = 10000m;
        var newClosingDay = 5;
        var newDueDay = 15;
        var newDebitAccountId = Guid.NewGuid();
        var newEnforceCreditLimit = false;

        // Act
        details.Update(newCreditLimit, newClosingDay, newDueDay, newDebitAccountId, newEnforceCreditLimit);

        // Assert
        details.CreditLimit.Should().Be(newCreditLimit);
        details.ClosingDay.Should().Be(newClosingDay);
        details.DueDay.Should().Be(newDueDay);
        details.DebitAccountId.Should().Be(newDebitAccountId);
        details.EnforceCreditLimit.Should().Be(newEnforceCreditLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Update_WithInvalidCreditLimit_ShouldThrowException(decimal creditLimit)
    {
        // Arrange
        var details = CreditCardDetails.Create(5000m, 3, 10, Guid.NewGuid(), true);

        // Act
        var act = () => details.Update(creditLimit, 3, 10, Guid.NewGuid(), true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Limite de crédito deve ser maior que zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(29)]
    [InlineData(31)]
    public void Update_WithInvalidClosingDay_ShouldThrowException(int closingDay)
    {
        // Arrange
        var details = CreditCardDetails.Create(5000m, 3, 10, Guid.NewGuid(), true);

        // Act
        var act = () => details.Update(5000m, closingDay, 10, Guid.NewGuid(), true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Dia de fechamento deve estar entre 1 e 28.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(29)]
    [InlineData(31)]
    public void Update_WithInvalidDueDay_ShouldThrowException(int dueDay)
    {
        // Arrange
        var details = CreditCardDetails.Create(5000m, 3, 10, Guid.NewGuid(), true);

        // Act
        var act = () => details.Update(5000m, 3, dueDay, Guid.NewGuid(), true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Dia de vencimento deve estar entre 1 e 28.");
    }

    [Fact]
    public void Update_WithEmptyDebitAccountId_ShouldThrowException()
    {
        // Arrange
        var details = CreditCardDetails.Create(5000m, 3, 10, Guid.NewGuid(), true);

        // Act
        var act = () => details.Update(5000m, 3, 10, Guid.Empty, true);

        // Assert
        act.Should().Throw<InvalidCreditCardConfigException>()
            .WithMessage("Conta de débito é obrigatória.");
    }
}
