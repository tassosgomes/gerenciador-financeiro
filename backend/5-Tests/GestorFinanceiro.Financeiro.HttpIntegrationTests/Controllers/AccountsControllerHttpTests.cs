using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class AccountsControllerHttpTests : IntegrationTestBase
{
    public AccountsControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task CORS_RequestWithAllowedOrigin_ReturnsAllowOriginHeader()
    {
        var client = await AuthenticateAsAdminAsync();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:5173");

        var response = await client.GetAsync("/api/v1/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins).Should().BeTrue();
        allowedOrigins.Should().ContainSingle().Which.Should().Be("http://localhost:5173");
    }

    [DockerAvailableFact]
    public async Task CreateAccount_ReturnsCreatedWithLocation()
    {
        var client = await AuthenticateAsAdminAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Teste HTTP",
            type = "Corrente",
            initialBalance = 1000m,
            allowNegativeBalance = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();

        var account = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);
        account.Should().NotBeNull();
        account!.Name.Should().Be("Conta Teste HTTP");
        account.Type.Should().Be(GestorFinanceiro.Financeiro.Domain.Enum.AccountType.Corrente);
        account.AllowNegativeBalance.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task CreateAccount_WithDuplicateName_ReturnsBadRequestProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Seed",
            type = "Corrente",
            initialBalance = 0m,
            allowNegativeBalance = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task CreateAccount_WithInvalidPayload_ReturnsBadRequestProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "",
            initialBalance = 100m,
            allowNegativeBalance = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task CreateAccount_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Sem Token",
            type = "Corrente",
            initialBalance = 100m,
            allowNegativeBalance = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task ListAccounts_WithAndWithoutFilter_ReturnsExpectedData()
    {
        var client = await AuthenticateAsAdminAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Para Filtro",
            type = "Carteira",
            initialBalance = 25m,
            allowNegativeBalance = true
        });
        var created = await createdResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var deactivateResponse = await client.PatchAsJsonAsync($"/api/v1/accounts/{created!.Id}/status", new { isActive = false });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAllResponse = await client.GetAsync("/api/v1/accounts");
        listAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var allAccounts = await listAllResponse.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonSerializerOptions);
        allAccounts.Should().NotBeNull();
        allAccounts!.Should().Contain(account => account.Id == created.Id);

        var activeOnlyResponse = await client.GetAsync("/api/v1/accounts?isActive=true");
        activeOnlyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeOnly = await activeOnlyResponse.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonSerializerOptions);
        activeOnly.Should().NotBeNull();
        activeOnly!.Should().OnlyContain(account => account.IsActive);

        var byTypeResponse = await client.GetAsync("/api/v1/accounts?type=Carteira");
        byTypeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var byType = await byTypeResponse.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonSerializerOptions);
        byType.Should().NotBeNull();
        byType!.Should().Contain(account => account.Id == created.Id);
        byType.Should().OnlyContain(account => account.Type == GestorFinanceiro.Financeiro.Domain.Enum.AccountType.Carteira);
    }

    [DockerAvailableFact]
    public async Task GetUpdateAndToggleStatusAccount_ReturnsExpectedData()
    {
        var client = await AuthenticateAsAdminAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Editavel",
            type = "Corrente",
            initialBalance = 100m,
            allowNegativeBalance = false
        });
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var detailResponse = await client.GetAsync($"/api/v1/accounts/{created!.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/accounts/{created.Id}", new
        {
            name = "Conta Editada",
            allowNegativeBalance = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Conta Editada");

        var deactivateResponse = await client.PatchAsJsonAsync($"/api/v1/accounts/{created.Id}/status", new { isActive = false });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activateResponse = await client.PatchAsJsonAsync($"/api/v1/accounts/{created.Id}/status", new { isActive = true });
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [DockerAvailableFact]
    public async Task AccountNotFound_ReturnsNotFoundProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        var detailResponse = await client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");
        await AssertProblemDetailsAsync(detailResponse, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task POST_CreateCreditCardAccount_ShouldReturn201WithCreditCardDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        // Criar conta corrente para ser a conta de débito
        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Para Débito",
            type = "Corrente",
            initialBalance = 5000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var createResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Teste HTTP",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 3000m,
            closingDay = 5,
            dueDay = 15,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = true
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();

        var account = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);
        account.Should().NotBeNull();
        account!.Name.Should().Be("Cartão Teste HTTP");
        account.Type.Should().Be(GestorFinanceiro.Financeiro.Domain.Enum.AccountType.Cartao);
        account.Balance.Should().Be(0m);
        account.CreditCard.Should().NotBeNull();
        account.CreditCard!.CreditLimit.Should().Be(3000m);
        account.CreditCard.ClosingDay.Should().Be(5);
        account.CreditCard.DueDay.Should().Be(15);
        account.CreditCard.DebitAccountId.Should().Be(debitAccount.Id);
        account.CreditCard.EnforceCreditLimit.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task POST_CreateCreditCardAccount_WithInvalidClosingDay_ShouldReturn400()
    {
        var client = await AuthenticateAsAdminAsync();

        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Para Débito 2",
            type = "Corrente",
            initialBalance = 5000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var createResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Inválido",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 3000m,
            closingDay = 35, // Inválido
            dueDay = 15,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = true
        });

        await AssertProblemDetailsAsync(createResponse, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task POST_CreateRegularAccount_ShouldReturn201WithoutCreditCardDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Regular",
            type = "Corrente",
            initialBalance = 1000m,
            allowNegativeBalance = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var account = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);
        account.Should().NotBeNull();
        account!.CreditCard.Should().BeNull();
    }

    [DockerAvailableFact]
    public async Task PUT_UpdateCreditCardAccount_ShouldReturn200WithUpdatedDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        // Criar conta de débito
        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Para Débito 3",
            type = "Corrente",
            initialBalance = 5000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Criar cartão
        var createResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Para Edição",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 2000m,
            closingDay = 10,
            dueDay = 20,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = false
        });
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Atualizar cartão
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/accounts/{created!.Id}", new
        {
            name = "Cartão Editado",
            allowNegativeBalance = true,
            creditLimit = 5000m,
            closingDay = 15,
            dueDay = 25,
            debitAccountId = debitAccount.Id,
            enforceCreditLimit = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Cartão Editado");
        updated.CreditCard.Should().NotBeNull();
        updated.CreditCard!.CreditLimit.Should().Be(5000m);
        updated.CreditCard.ClosingDay.Should().Be(15);
        updated.CreditCard.DueDay.Should().Be(25);
        updated.CreditCard.EnforceCreditLimit.Should().BeTrue();
    }
}
