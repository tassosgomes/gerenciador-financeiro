using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Exception;

public class CreditLimitExceededExceptionTests
{
    [Fact]
    public void Constructor_ShouldContainAccountIdAndAmountsInMessage()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var availableLimit = 1000m;
        var requestedAmount = 1500m;

        // Act
        var exception = new CreditLimitExceededException(accountId, availableLimit, requestedAmount);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Contain(accountId.ToString());
        exception.Message.Should().Contain("Disponível");
        exception.Message.Should().Contain("Solicitado");
    }
}
