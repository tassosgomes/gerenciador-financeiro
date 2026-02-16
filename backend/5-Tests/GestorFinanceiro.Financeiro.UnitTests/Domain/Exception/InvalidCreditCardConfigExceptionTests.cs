using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Exception;

public class InvalidCreditCardConfigExceptionTests
{
    [Fact]
    public void Constructor_ShouldContainCustomMessage()
    {
        // Arrange
        var customMessage = "Mensagem de erro personalizada";

        // Act
        var exception = new InvalidCreditCardConfigException(customMessage);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(customMessage);
    }
}
