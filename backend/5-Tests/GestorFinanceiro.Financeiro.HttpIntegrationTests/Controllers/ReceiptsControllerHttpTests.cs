using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Dto;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Seed;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class ReceiptsControllerHttpTests : IntegrationTestBase
{
    private const string AccessKeyOne = "12345678901234567890123456789012345678901234";
    private const string AccessKeyTwo = "99999999999999999999999999999999999999999999";

    public ReceiptsControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task Lookup_WithValidInput_ReturnsReceiptPreview()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyOne)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);

        var response = await client.PostAsJsonAsync("/api/v1/receipts/lookup", new { input = AccessKeyOne });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonObjectAsync(response);
        payload["accessKey"]!.GetValue<string>().Should().Be(AccessKeyOne);
        payload["establishmentName"]!.GetValue<string>().Should().Be("SUPERMERCADO TESTE LTDA");
        payload["items"]!.AsArray().Count.Should().Be(2);
        payload["alreadyImported"]!.GetValue<bool>().Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task Lookup_WithEmptyInput_ReturnsBadRequest()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyOne)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);

        var response = await client.PostAsJsonAsync("/api/v1/receipts/lookup", new { input = string.Empty });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task Lookup_WhenSefazUnavailable_ReturnsBadGateway()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => throw new SefazUnavailableException());
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);

        var response = await client.PostAsJsonAsync("/api/v1/receipts/lookup", new { input = AccessKeyOne });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadGateway);
    }

    [DockerAvailableFact]
    public async Task Lookup_WhenNfceNotFound_ReturnsNotFound()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => throw new NfceNotFoundException(AccessKeyOne));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);

        var response = await client.PostAsJsonAsync("/api/v1/receipts/lookup", new { input = AccessKeyOne });

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task Import_WithValidPayload_ReturnsCreatedAndPersistsReceiptData()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyOne)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyOne,
            accountId,
            categoryId,
            description = "Importacao mercado",
            competenceDate = DateTime.UtcNow.Date
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await ReadJsonObjectAsync(response);
        var transactionId = payload["transaction"]!["id"]!.GetValue<Guid>();

        await using var scope = applicationFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();

        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(value => value.Id == transactionId);
        var establishment = await dbContext.Establishments.FirstOrDefaultAsync(value => value.TransactionId == transactionId);
        var receiptItemsCount = await dbContext.ReceiptItems.CountAsync(value => value.TransactionId == transactionId);

        transaction.Should().NotBeNull();
        establishment.Should().NotBeNull();
        establishment!.AccessKey.Should().Be(AccessKeyOne);
        receiptItemsCount.Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task Import_WhenAlreadyImported_ReturnsConflict()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyOne)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var firstResponse = await client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyOne,
            accountId,
            categoryId,
            description = "Primeira importacao",
            competenceDate = DateTime.UtcNow.Date
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyOne,
            accountId,
            categoryId,
            description = "Importacao duplicada",
            competenceDate = DateTime.UtcNow.Date
        });

        await AssertProblemDetailsAsync(secondResponse, HttpStatusCode.Conflict);
    }

    [DockerAvailableFact]
    public async Task Import_WithInvalidPayload_ReturnsBadRequest()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyTwo)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);
        var (_, categoryId) = await GetSeedReferencesAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyTwo,
            accountId = Guid.Empty,
            categoryId,
            description = "Payload invalido",
            competenceDate = DateTime.UtcNow.Date
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task GetReceipt_ByTransactionIdWithReceipt_ReturnsOk()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyTwo)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var importResponse = await client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyTwo,
            accountId,
            categoryId,
            description = "Compra com cupom",
            competenceDate = DateTime.UtcNow.Date
        });

        importResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var importPayload = await ReadJsonObjectAsync(importResponse);
        var transactionId = importPayload["transaction"]!["id"]!.GetValue<Guid>();

        var response = await client.GetAsync($"/api/v1/transactions/{transactionId}/receipt");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonObjectAsync(response);
        payload["establishment"]!["accessKey"]!.GetValue<string>().Should().Be(AccessKeyTwo);
        payload["items"]!.AsArray().Count.Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task GetReceipt_ForTransactionWithoutReceipt_ReturnsNotFound()
    {
        var client = await AuthenticateAsAdminAsync();
        var transactionId = await GetFirstTransactionIdAsync(client);

        var response = await client.GetAsync($"/api/v1/transactions/{transactionId}/receipt");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task GetReceipt_ForInexistentTransaction_ReturnsNotFound()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}/receipt");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task Endpoints_WithoutToken_ReturnUnauthorized()
    {
        var lookupResponse = await Client.PostAsJsonAsync("/api/v1/receipts/lookup", new { input = AccessKeyOne });
        lookupResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var importResponse = await Client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyOne,
            accountId = Guid.NewGuid(),
            categoryId = Guid.NewGuid(),
            description = "Sem token",
            competenceDate = DateTime.UtcNow.Date
        });
        importResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var receiptResponse = await Client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}/receipt");
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task ImportThenCancelTransaction_RemovesReceiptItemsAndEstablishment()
    {
        using var applicationFactory = CreateFactoryWithSefaz((_, _) => Task.FromResult(BuildNfceData(AccessKeyOne)));
        var client = await AuthenticateAsAdminForFactoryAsync(applicationFactory);
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var importResponse = await client.PostAsJsonAsync("/api/v1/receipts/import", new
        {
            accessKey = AccessKeyOne,
            accountId,
            categoryId,
            description = "Compra para cancelamento",
            competenceDate = DateTime.UtcNow.Date
        });
        importResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var importPayload = await ReadJsonObjectAsync(importResponse);
        var transactionId = importPayload["transaction"]!["id"]!.GetValue<Guid>();

        var cancelResponse = await client.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancel", new
        {
            reason = "Cancelamento de teste"
        });

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = applicationFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();

        var establishment = await dbContext.Establishments.FirstOrDefaultAsync(value => value.TransactionId == transactionId);
        var receiptItemsCount = await dbContext.ReceiptItems.CountAsync(value => value.TransactionId == transactionId);

        establishment.Should().BeNull();
        receiptItemsCount.Should().Be(0);
    }

    private WebApplicationFactory<Program> CreateFactoryWithSefaz(Func<string, CancellationToken, Task<NfceData>> lookupAsync)
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ISefazNfceService));
                services.AddScoped<ISefazNfceService>(_ => new StubSefazNfceService(lookupAsync));
            });
        });
    }

    private static async Task<HttpClient> AuthenticateAsAdminForFactoryAsync(WebApplicationFactory<Program> applicationFactory)
    {
        var client = applicationFactory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestDataSeeder.AdminEmail,
            password = TestDataSeeder.AdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        return client;
    }

    private static async Task<(Guid AccountId, Guid CategoryId)> GetSeedReferencesAsync(HttpClient client)
    {
        var accountsResponse = await client.GetAsync("/api/v1/accounts");
        var categoriesResponse = await client.GetAsync("/api/v1/categories?type=Despesa");

        var accountsJson = await accountsResponse.Content.ReadFromJsonAsync<JsonArray>(JsonSerializerOptions);
        var categoriesJson = await categoriesResponse.Content.ReadFromJsonAsync<JsonArray>(JsonSerializerOptions);

        accountsJson.Should().NotBeNull();
        categoriesJson.Should().NotBeNull();

        var accountId = accountsJson![0]!["id"]!.GetValue<Guid>();
        var categoryId = categoriesJson![0]!["id"]!.GetValue<Guid>();

        return (accountId, categoryId);
    }

    private static async Task<Guid> GetFirstTransactionIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/transactions?_page=1&_size=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await ReadJsonObjectAsync(response);
        var first = payload["data"]!.AsArray().FirstOrDefault();
        first.Should().NotBeNull();

        return first!["id"]!.GetValue<Guid>();
    }

    private static NfceData BuildNfceData(string accessKey)
    {
        return new NfceData(
            accessKey,
            "SUPERMERCADO TESTE LTDA",
            "12345678000190",
            DateTime.UtcNow.AddDays(-1),
            150m,
            10m,
            140m,
            [
                new NfceItemData("ARROZ TIPO 1", "7891111111111", 2m, "UN", 25m, 50m),
                new NfceItemData("FEIJAO CARIOCA", "7892222222222", 3m, "UN", 30m, 90m)
            ]);
    }

    private sealed class StubSefazNfceService : ISefazNfceService
    {
        private readonly Func<string, CancellationToken, Task<NfceData>> _lookupAsync;

        public StubSefazNfceService(Func<string, CancellationToken, Task<NfceData>> lookupAsync)
        {
            _lookupAsync = lookupAsync;
        }

        public Task<NfceData> LookupAsync(string accessKey, CancellationToken cancellationToken)
        {
            return _lookupAsync(accessKey, cancellationToken);
        }
    }
}
