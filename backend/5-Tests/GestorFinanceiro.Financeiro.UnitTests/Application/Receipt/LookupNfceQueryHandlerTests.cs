using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Receipt;
using GestorFinanceiro.Financeiro.Domain.Dto;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Receipt;

public class LookupNfceQueryHandlerTests
{
    private const string AccessKey = "12345678901234567890123456789012345678901234";

    private readonly Mock<ISefazNfceService> _sefazNfceService = new();
    private readonly Mock<IEstablishmentRepository> _establishmentRepository = new();
    private readonly Mock<ILogger<LookupNfceQueryHandler>> _logger = new();
    private readonly LookupNfceQueryHandler _sut;

    public LookupNfceQueryHandlerTests()
    {
        _sut = new LookupNfceQueryHandler(
            _sefazNfceService.Object,
            _establishmentRepository.Object,
            new LookupNfceQueryValidator(),
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidAccessKey_ReturnsLookupResponse()
    {
        var nfceData = CreateNfceData();
        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(nfceData);
        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(new LookupNfceQuery(AccessKey), CancellationToken.None);

        response.AccessKey.Should().Be(AccessKey);
        response.Items.Should().HaveCount(2);
        response.AlreadyImported.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_AlreadyImported_ReturnsAlreadyImportedTrue()
    {
        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(CreateNfceData());
        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var response = await _sut.HandleAsync(new LookupNfceQuery(AccessKey), CancellationToken.None);

        response.AlreadyImported.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_UrlInput_ExtractsAccessKeyAndQueriesSefaz()
    {
        var input = $"https://www.sefaz.pb.gov.br/nfce?p={AccessKey}|2|1|1";

        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(CreateNfceData());
        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.HandleAsync(new LookupNfceQuery(input), CancellationToken.None);

        _sefazNfceService.Verify(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSefazThrows_PropagatesException()
    {
        _sefazNfceService
            .Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SefazUnavailableException());

        var action = () => _sut.HandleAsync(new LookupNfceQuery(AccessKey), CancellationToken.None);

        await action.Should().ThrowAsync<SefazUnavailableException>();
    }

    private static NfceData CreateNfceData()
    {
        return new NfceData(
            AccessKey,
            "SUPERMERCADO TESTE",
            "12345678000190",
            DateTime.UtcNow,
            120m,
            10m,
            110m,
            [
                new NfceItemData("Arroz", "789", 1m, "UN", 20m, 20m),
                new NfceItemData("Feijao", null, 2m, "UN", 15m, 30m)
            ]);
    }
}
