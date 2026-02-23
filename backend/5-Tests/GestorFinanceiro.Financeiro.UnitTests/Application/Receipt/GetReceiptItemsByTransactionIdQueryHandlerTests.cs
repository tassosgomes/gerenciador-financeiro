using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Receipt;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Receipt;

public class GetReceiptItemsByTransactionIdQueryHandlerTests
{
    private readonly Mock<IEstablishmentRepository> _establishmentRepository = new();
    private readonly Mock<IReceiptItemRepository> _receiptItemRepository = new();
    private readonly Mock<ILogger<GetReceiptItemsByTransactionIdQueryHandler>> _logger = new();

    private readonly GetReceiptItemsByTransactionIdQueryHandler _sut;

    public GetReceiptItemsByTransactionIdQueryHandlerTests()
    {
        _sut = new GetReceiptItemsByTransactionIdQueryHandler(
            _establishmentRepository.Object,
            _receiptItemRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenReceiptExists_ReturnsEstablishmentAndItems()
    {
        var transactionId = Guid.NewGuid();
        var establishment = Establishment.Create(transactionId, "Mercado", "12345678000190", "12345678901234567890123456789012345678901234", "user-1");
        var items = new List<ReceiptItem>
        {
            ReceiptItem.Create(transactionId, "Arroz", "1", 1m, "UN", 10m, 10m, 1, "user-1"),
            ReceiptItem.Create(transactionId, "Feijao", "2", 2m, "UN", 8m, 16m, 2, "user-1")
        };

        _establishmentRepository.Setup(mock => mock.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>())).ReturnsAsync(establishment);
        _receiptItemRepository.Setup(mock => mock.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var response = await _sut.HandleAsync(new GetReceiptItemsByTransactionIdQuery(transactionId), CancellationToken.None);

        response.Establishment.Name.Should().Be("Mercado");
        response.Items.Should().HaveCount(2);
        response.Items[0].ItemOrder.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WhenReceiptDoesNotExist_ThrowsNfceNotFoundException()
    {
        var transactionId = Guid.NewGuid();
        _establishmentRepository.Setup(mock => mock.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>())).ReturnsAsync((Establishment?)null);

        var action = () => _sut.HandleAsync(new GetReceiptItemsByTransactionIdQuery(transactionId), CancellationToken.None);

        await action.Should().ThrowAsync<NfceNotFoundException>();
    }
}
