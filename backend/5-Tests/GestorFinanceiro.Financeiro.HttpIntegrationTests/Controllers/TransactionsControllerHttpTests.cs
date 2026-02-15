using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class TransactionsControllerHttpTests : IntegrationTestBase
{
    public TransactionsControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task CreateSimpleTransaction_ReturnsCreated()
    {
        var client = await AuthenticateAsAdminAsync();
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            accountId,
            categoryId,
            type = "Debit",
            amount = 120.50m,
            description = "Compra mercado",
            competenceDate = DateTime.UtcNow.Date,
            dueDate = DateTime.UtcNow.Date
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [DockerAvailableFact]
    public async Task CreateInstallmentsAndTransfer_ReturnCreated()
    {
        var client = await AuthenticateAsAdminAsync();
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);
        var secondAccountId = await GetSecondAccountIdAsync(client, accountId);

        var installmentsResponse = await client.PostAsJsonAsync("/api/v1/transactions/installments", new
        {
            accountId,
            categoryId,
            type = "Debit",
            amount = 600m,
            numberOfInstallments = 3,
            description = "Notebook",
            competenceDate = DateTime.UtcNow.Date,
            dueDate = DateTime.UtcNow.Date
        });

        installmentsResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var transferResponse = await client.PostAsJsonAsync("/api/v1/transactions/transfers", new
        {
            sourceAccountId = accountId,
            destinationAccountId = secondAccountId,
            categoryId,
            amount = 100m,
            description = "Transferencia entre contas",
            competenceDate = DateTime.UtcNow.Date
        });

        transferResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [DockerAvailableFact]
    public async Task CreateRecurrence_ReturnsCreated()
    {
        var client = await AuthenticateAsAdminAsync();
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/transactions/recurrences", new
        {
            accountId,
            categoryId,
            type = "Debit",
            amount = 120m,
            description = "Recorrencia teste",
            startDate = DateTime.UtcNow.Date,
            dayOfMonth = 5,
            defaultStatus = "Pending"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [DockerAvailableFact]
    public async Task ListWithFiltersAndPagination_ReturnsMetadata()
    {
        var client = await AuthenticateAsAdminAsync();
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            accountId,
            categoryId,
            type = "Debit",
            amount = 80m,
            description = "Despesa paginada",
            competenceDate = DateTime.UtcNow.Date,
            dueDate = DateTime.UtcNow.Date
        });

        var response = await client.GetAsync($"/api/v1/transactions?accountId={accountId}&_page=1&_size=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonObjectAsync(response);
        json["data"]!.AsArray().Count.Should().BeGreaterThanOrEqualTo(0);
        json["pagination"]!["page"]!.GetValue<int>().Should().Be(1);
        json["pagination"]!["size"]!.GetValue<int>().Should().Be(5);
    }

    [DockerAvailableFact]
    public async Task AdjustCancelAndHistory_ReturnsExpectedStates()
    {
        var client = await AuthenticateAsAdminAsync();
        var transactionId = await GetPaidTransactionIdAsync(client);

        var adjustResponse = await client.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/adjustments", new
        {
            newAmount = 110m
        });
        adjustResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var cancelResponse = await client.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/cancel", new
        {
            reason = "Teste de cancelamento"
        });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await client.GetAsync($"/api/v1/transactions/{transactionId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await ReadJsonObjectAsync(historyResponse);
        var entries = history["entries"]!.AsArray();
        entries.Count.Should().Be(2);
        history.ToJsonString().Should().Contain("Cancelled");
    }

    [DockerAvailableFact]
    public async Task CreateTransaction_WithNegativeAmount_ReturnsBadRequestProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            accountId,
            categoryId,
            type = "Debit",
            amount = -1m,
            description = "Valor invalido",
            competenceDate = DateTime.UtcNow.Date,
            dueDate = DateTime.UtcNow.Date
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task History_WithOriginalAndAdjustment_ReturnsTwoEntries()
    {
        var client = await AuthenticateAsAdminAsync();
        var transactionId = await GetPaidTransactionIdAsync(client);

        var adjustResponse = await client.PostAsJsonAsync($"/api/v1/transactions/{transactionId}/adjustments", new
        {
            newAmount = 90m
        });
        adjustResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var historyResponse = await client.GetAsync($"/api/v1/transactions/{transactionId}/history");
        var history = await ReadJsonObjectAsync(historyResponse);
        history["entries"]!.AsArray().Count.Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task CancelInstallmentGroup_ReturnsOk()
    {
        var client = await AuthenticateAsAdminAsync();
        var (accountId, categoryId) = await GetSeedReferencesAsync(client);

        var installmentsResponse = await client.PostAsJsonAsync("/api/v1/transactions/installments", new
        {
            accountId,
            categoryId,
            type = "Debit",
            amount = 450m,
            numberOfInstallments = 3,
            description = "Parcelamento cancelamento grupo",
            competenceDate = DateTime.UtcNow.Date,
            dueDate = DateTime.UtcNow.Date
        });

        installmentsResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var installments = await installmentsResponse.Content.ReadFromJsonAsync<JsonArray>(JsonSerializerOptions);
        installments.Should().NotBeNull();
        var groupId = installments![0]!["installmentGroupId"]!.GetValue<Guid>();

        var cancelResponse = await client.PostAsJsonAsync($"/api/v1/transactions/installment-groups/{groupId}/cancel", new
        {
            reason = "Cancelamento em lote"
        });

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DockerAvailableFact]
    public async Task CreateTransaction_WithInexistentAccount_ReturnsProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();
        var (_, categoryId) = await GetSeedReferencesAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            accountId = Guid.NewGuid(),
            categoryId,
            type = "Debit",
            amount = 100m,
            description = "Conta inexistente",
            competenceDate = DateTime.UtcNow.Date,
            dueDate = DateTime.UtcNow.Date
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
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

    private static async Task<Guid> GetSecondAccountIdAsync(HttpClient client, Guid firstAccountId)
    {
        var response = await client.GetAsync("/api/v1/accounts");
        var accountsJson = await response.Content.ReadFromJsonAsync<JsonArray>(JsonSerializerOptions);

        accountsJson.Should().NotBeNull();

        var secondAccount = accountsJson!
            .Select(node => node?.AsObject())
            .FirstOrDefault(account => account is not null && account["id"]!.GetValue<Guid>() != firstAccountId);

        secondAccount.Should().NotBeNull();
        return secondAccount!["id"]!.GetValue<Guid>();
    }

    private static async Task<Guid> GetPaidTransactionIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/transactions?status=Paid&_page=1&_size=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await ReadJsonObjectAsync(response);
        var first = payload["data"]!.AsArray().FirstOrDefault();
        first.Should().NotBeNull();

        return first!["id"]!.GetValue<Guid>();
    }
}
