using System.Net;
using System.Text;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Infra.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Infra.Services;

public class SefazPbNfceServiceTests
{
    private const string ValidAccessKey = "25260212345678000190650010000000012345678901";

    [Fact]
    public async Task LookupAsync_ComHtmlValido_DeveRetornarDadosDaNfce()
    {
        var html = LoadFixture("nfce-valid.html");
        var service = CreateService(CreateSuccessHandler(html));

        var result = await service.LookupAsync(ValidAccessKey, CancellationToken.None);

        result.AccessKey.Should().Be(ValidAccessKey);
        result.EstablishmentName.Should().Be("SUPERMERCADO TESTE LTDA");
        result.EstablishmentCnpj.Should().Be("12345678000190");
        result.Items.Should().HaveCount(2);
        result.TotalAmount.Should().Be(87.50m);
        result.DiscountAmount.Should().Be(0m);
        result.PaidAmount.Should().Be(87.50m);
    }

    [Fact]
    public async Task LookupAsync_ComHtmlValido_DeveExtrairTodosOsCamposDoItem()
    {
        var html = LoadFixture("nfce-valid.html");
        var service = CreateService(CreateSuccessHandler(html));

        var result = await service.LookupAsync(ValidAccessKey, CancellationToken.None);
        var firstItem = result.Items.First();

        firstItem.Description.Should().Be("ARROZ TIPO 1 5KG");
        firstItem.ProductCode.Should().Be("7891234567890");
        firstItem.Quantity.Should().Be(2.000m);
        firstItem.UnitOfMeasure.Should().Be("UN");
        firstItem.UnitPrice.Should().Be(25.90m);
        firstItem.TotalPrice.Should().Be(51.80m);
    }

    [Fact]
    public async Task LookupAsync_ComDesconto_DeveExtrairTotaisCorretamente()
    {
        var html = LoadFixture("nfce-with-discount.html");
        var service = CreateService(CreateSuccessHandler(html));

        var result = await service.LookupAsync(ValidAccessKey, CancellationToken.None);

        result.TotalAmount.Should().Be(70.00m);
        result.DiscountAmount.Should().Be(5.50m);
        result.PaidAmount.Should().Be(64.50m);
    }

    [Fact]
    public async Task LookupAsync_ComItemSemCodigoProduto_DeveRetornarProductCodeNulo()
    {
        var html = LoadFixture("nfce-item-without-product-code.html");
        var service = CreateService(CreateSuccessHandler(html));

        var result = await service.LookupAsync(ValidAccessKey, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().ProductCode.Should().BeNull();
    }

    [Fact]
    public async Task LookupAsync_ComPaginaDeNaoEncontrada_DeveLancarNfceNotFoundException()
    {
        var html = LoadFixture("nfce-not-found.html");
        var service = CreateService(CreateSuccessHandler(html));

        var action = () => service.LookupAsync(ValidAccessKey, CancellationToken.None);

        await action.Should().ThrowAsync<NfceNotFoundException>();
    }

    [Fact]
    public async Task LookupAsync_ComHtmlMalformado_DeveLancarSefazParsingException()
    {
        var html = LoadFixture("nfce-malformed.html");
        var service = CreateService(CreateSuccessHandler(html));

        var action = () => service.LookupAsync(ValidAccessKey, CancellationToken.None);

        await action.Should().ThrowAsync<SefazParsingException>();
    }

    [Fact]
    public async Task LookupAsync_ComChaveInvalida_DeveLancarInvalidAccessKeyException()
    {
        var html = LoadFixture("nfce-valid.html");
        var service = CreateService(CreateSuccessHandler(html));

        var action = () => service.LookupAsync("12345", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidAccessKeyException>();
    }

    [Fact]
    public async Task LookupAsync_ComTimeout_DeveLancarSefazUnavailableException()
    {
        var service = CreateService(CreateExceptionHandler(new TaskCanceledException("timeout")));

        var action = () => service.LookupAsync(ValidAccessKey, CancellationToken.None);

        await action.Should().ThrowAsync<SefazUnavailableException>();
    }

    [Fact]
    public async Task LookupAsync_ComUrlValida_DeveExtrairChaveEConsultarComSucesso()
    {
        var html = LoadFixture("nfce-valid.html");
        var capturedRequests = new List<HttpRequestMessage>();

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            capturedRequests.Add(request);
            return Task.FromResult(CreateHtmlResponse(html));
        });

        var service = CreateService(handler);
        var urlInput = $"https://www4.sefaz.pb.gov.br/atf/seg/SEGf_AcessarFuncao.jsp?cdFuncao=FIS_1410&p={ValidAccessKey}|2|1|1|ABC123";

        var result = await service.LookupAsync(urlInput, CancellationToken.None);

        result.AccessKey.Should().Be(ValidAccessKey);
        capturedRequests.Should().NotBeEmpty();
        capturedRequests.Should().Contain(request => request.RequestUri!.ToString().Contains(ValidAccessKey, StringComparison.Ordinal));
        capturedRequests.Should().Contain(request => request.RequestUri!.ToString().Contains("FISf_ExibirNFCE.do", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LookupAsync_ComUrlSemChaveValida_DeveLancarInvalidAccessKeyException()
    {
        var html = LoadFixture("nfce-valid.html");
        var service = CreateService(CreateSuccessHandler(html));

        var action = () => service.LookupAsync("https://www.sefaz.pb.gov.br/nfce/consulta?p=sem-chave", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidAccessKeyException>();
    }

    private static string LoadFixture(string fileName)
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SefazPb", fileName);
        return File.ReadAllText(fixturePath, Encoding.UTF8);
    }

    private static SefazPbNfceService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www4.sefaz.pb.gov.br/atf/")
        };

        var logger = new Mock<ILogger<SefazPbNfceService>>();
        return new SefazPbNfceService(httpClient, logger.Object);
    }

    private static HttpMessageHandler CreateSuccessHandler(string html)
    {
        return new TestHttpMessageHandler((_, _) => Task.FromResult(CreateHtmlResponse(html)));
    }

    private static HttpMessageHandler CreateExceptionHandler(Exception exception)
    {
        return new TestHttpMessageHandler((_, _) => Task.FromException<HttpResponseMessage>(exception));
    }

    private static HttpResponseMessage CreateHtmlResponse(string html)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        };
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
