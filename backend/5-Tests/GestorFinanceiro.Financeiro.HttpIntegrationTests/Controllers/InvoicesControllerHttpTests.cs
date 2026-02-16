using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class InvoicesControllerHttpTests : IntegrationTestBase
{
    public InvoicesControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task GET_Invoice_WithValidCard_ShouldReturn200WithInvoice()
    {
        var client = await AuthenticateAsAdminAsync();

        // Criar conta corrente para débito
        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Débito Invoice",
            type = "Corrente",
            initialBalance = 10000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Criar cartão
        var cardResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Teste Invoice",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 5000m,
            closingDay = 5,
            dueDay = 15,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = true
        });
        var card = await cardResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Obter fatura (pode estar vazia, mas deve retornar 200)
        var now = DateTime.UtcNow;
        var invoiceResponse = await client.GetAsync($"/api/v1/accounts/{card!.Id}/invoices?month={now.Month}&year={now.Year}");

        invoiceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>(JsonSerializerOptions);
        invoice.Should().NotBeNull();
        invoice!.AccountId.Should().Be(card.Id);
        invoice.Month.Should().Be(now.Month);
        invoice.Year.Should().Be(now.Year);
    }

    [DockerAvailableFact]
    public async Task GET_Invoice_WithNonCardAccount_ShouldReturn400()
    {
        var client = await AuthenticateAsAdminAsync();

        // Criar conta corrente (não é cartão)
        var accountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Não Cartão",
            type = "Corrente",
            initialBalance = 1000m,
            allowNegativeBalance = false
        });
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var now = DateTime.UtcNow;
        var invoiceResponse = await client.GetAsync($"/api/v1/accounts/{account!.Id}/invoices?month={now.Month}&year={now.Year}");

        await AssertProblemDetailsAsync(invoiceResponse, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task POST_PayInvoice_WithValidPayment_ShouldReturn200WithTransactions()
    {
        var client = await AuthenticateAsAdminAsync();

        // Criar conta corrente para débito
        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Débito Payment",
            type = "Corrente",
            initialBalance = 10000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Criar cartão
        var cardResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Teste Payment",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 5000m,
            closingDay = 5,
            dueDay = 15,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = true
        });
        var card = await cardResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Pagar fatura
        var paymentResponse = await client.PostAsJsonAsync($"/api/v1/accounts/{card!.Id}/invoices/pay", new
        {
            amount = 100m,
            competenceDate = DateTime.UtcNow
        });

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await paymentResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>(JsonSerializerOptions);
        transactions.Should().NotBeNull();
        transactions!.Count.Should().Be(2); // Débito + Crédito
        transactions.Should().Contain(t => t.AccountId == debitAccount.Id && t.Type == GestorFinanceiro.Financeiro.Domain.Enum.TransactionType.Debit);
        transactions.Should().Contain(t => t.AccountId == card.Id && t.Type == GestorFinanceiro.Financeiro.Domain.Enum.TransactionType.Credit);
    }

    [DockerAvailableFact]
    public async Task POST_PayInvoice_WithInactiveDebitAccount_ShouldReturn400()
    {
        var client = await AuthenticateAsAdminAsync();

        // Criar conta corrente para débito
        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Inativa",
            type = "Corrente",
            initialBalance = 10000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Criar cartão
        var cardResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Teste Inactive Debit",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 5000m,
            closingDay = 5,
            dueDay = 15,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = true
        });
        var card = await cardResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        // Desativar conta de débito
        await client.PatchAsJsonAsync($"/api/v1/accounts/{debitAccount.Id}/status", new { isActive = false });

        // Tentar pagar fatura
        var paymentResponse = await client.PostAsJsonAsync($"/api/v1/accounts/{card!.Id}/invoices/pay", new
        {
            amount = 100m,
            competenceDate = DateTime.UtcNow
        });

        await AssertProblemDetailsAsync(paymentResponse, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task POST_PayInvoice_WithFutureCompetenceDate_ShouldReturn400()
    {
        var client = await AuthenticateAsAdminAsync();

        var debitAccountResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Conta Corrente Future Date",
            type = "Corrente",
            initialBalance = 10000m,
            allowNegativeBalance = false
        });
        var debitAccount = await debitAccountResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var cardResponse = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Cartão Future Date",
            type = "Cartao",
            initialBalance = 0m,
            allowNegativeBalance = true,
            creditLimit = 5000m,
            closingDay = 5,
            dueDay = 15,
            debitAccountId = debitAccount!.Id,
            enforceCreditLimit = true
        });
        var card = await cardResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonSerializerOptions);

        var paymentResponse = await client.PostAsJsonAsync($"/api/v1/accounts/{card!.Id}/invoices/pay", new
        {
            amount = 100m,
            competenceDate = DateTime.UtcNow.AddDays(1)
        });

        await AssertProblemDetailsAsync(paymentResponse, HttpStatusCode.BadRequest);
    }
}
