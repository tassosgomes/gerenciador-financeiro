using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class BudgetsControllerTests : IntegrationTestBase
{
    public BudgetsControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithValidData_ShouldReturn201WithBudgetResponse()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryId = await GetExpenseCategoryIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento API {Guid.NewGuid()}",
            percentage = 35m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { categoryId },
            isRecurrent = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<BudgetResponse>(JsonSerializerOptions);
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);
        created.ReferenceYear.Should().Be(reference.Year);
        created.ReferenceMonth.Should().Be(reference.Month);
        created.Categories.Should().ContainSingle(category => category.Id == categoryId);
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithInvalidData_ShouldReturn400WithValidationErrors()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);

        var response = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = "",
            percentage = 0m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = Array.Empty<Guid>(),
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithDuplicateName_ShouldReturn409()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync($"Despesa A {Guid.NewGuid()}");
        var categoryB = await CreateExpenseCategoryAsync($"Despesa B {Guid.NewGuid()}");
        var budgetName = $"Orcamento Duplicado {Guid.NewGuid()}";

        var firstResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = budgetName,
            percentage = 20m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { categoryA.Id },
            isRecurrent = false
        });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = budgetName,
            percentage = 25m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { categoryB.Id },
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(duplicateResponse, HttpStatusCode.Conflict);
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithPercentageExceeding100_ShouldReturn422()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync($"Despesa A {Guid.NewGuid()}");
        var categoryB = await CreateExpenseCategoryAsync($"Despesa B {Guid.NewGuid()}");

        var firstResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento 80 {Guid.NewGuid()}",
            percentage = 80m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { categoryA.Id },
            isRecurrent = false
        });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento 30 {Guid.NewGuid()}",
            percentage = 30m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { categoryB.Id },
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithCategoryAlreadyBudgeted_ShouldReturn409()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync($"Despesa Unica {Guid.NewGuid()}");

        var firstResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento A {Guid.NewGuid()}",
            percentage = 30m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { category.Id },
            isRecurrent = false
        });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateCategoryResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento B {Guid.NewGuid()}",
            percentage = 20m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { category.Id },
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(duplicateCategoryResponse, HttpStatusCode.Conflict);
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithPastMonth_ShouldReturn422()
    {
        var client = await AuthenticateAsAdminAsync();
        var pastReference = DateTime.UtcNow.AddMonths(-1);
        var category = await GetExpenseCategoryIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento Passado {Guid.NewGuid()}",
            percentage = 20m,
            referenceYear = pastReference.Year,
            referenceMonth = pastReference.Month,
            categoryIds = new[] { category },
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [DockerAvailableFact]
    public async Task PostBudget_WithoutAuth_ShouldReturn401()
    {
        var reference = DateTime.UtcNow.AddMonths(1);

        var response = await Client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento Sem Auth {Guid.NewGuid()}",
            percentage = 15m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = Array.Empty<Guid>(),
            isRecurrent = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task PutBudget_WithValidData_ShouldReturn200()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync($"Despesa A {Guid.NewGuid()}");
        var categoryB = await CreateExpenseCategoryAsync($"Despesa B {Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento Editavel {Guid.NewGuid()}",
            percentage = 22m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { categoryA.Id },
            isRecurrent = false
        });

        var created = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>(JsonSerializerOptions);
        created.Should().NotBeNull();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/budgets/{created!.Id}", new
        {
            name = $"Orcamento Editado {Guid.NewGuid()}",
            percentage = 30m,
            categoryIds = new[] { categoryB.Id },
            isRecurrent = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<BudgetResponse>(JsonSerializerOptions);
        updated.Should().NotBeNull();
        updated!.Percentage.Should().Be(30m);
        updated.IsRecurrent.Should().BeTrue();
        updated.Categories.Should().ContainSingle(category => category.Id == categoryB.Id);
    }

    [DockerAvailableFact]
    public async Task PutBudget_WithNonExistingId_ShouldReturn404()
    {
        var client = await AuthenticateAsAdminAsync();
        var category = await GetExpenseCategoryIdAsync(client);

        var response = await client.PutAsJsonAsync($"/api/v1/budgets/{Guid.NewGuid()}", new
        {
            name = $"Orcamento Nao Existe {Guid.NewGuid()}",
            percentage = 25m,
            categoryIds = new[] { category },
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task PutBudget_WithPastMonth_ShouldReturn422()
    {
        var client = await AuthenticateAsAdminAsync();
        var pastReference = DateTime.UtcNow.AddMonths(-1);
        var category = await CreateExpenseCategoryAsync($"Despesa Passado {Guid.NewGuid()}");
        var budget = await CreateBudgetDirectAsync(
            $"Orcamento Passado {Guid.NewGuid()}",
            20m,
            pastReference.Year,
            pastReference.Month,
            [category.Id]);

        var response = await client.PutAsJsonAsync($"/api/v1/budgets/{budget.Id}", new
        {
            name = $"Orcamento Passado Editado {Guid.NewGuid()}",
            percentage = 25m,
            categoryIds = new[] { category.Id },
            isRecurrent = false
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [DockerAvailableFact]
    public async Task DeleteBudget_WithValidId_ShouldReturn204()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync($"Despesa Delete {Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento Delete {Guid.NewGuid()}",
            percentage = 15m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { category.Id },
            isRecurrent = false
        });

        var created = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>(JsonSerializerOptions);
        created.Should().NotBeNull();

        var deleteResponse = await client.DeleteAsync($"/api/v1/budgets/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [DockerAvailableFact]
    public async Task DeleteBudget_WithNonExistingId_ShouldReturn404()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.DeleteAsync($"/api/v1/budgets/{Guid.NewGuid()}");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task DeleteBudget_WithPastMonth_ShouldReturn422()
    {
        var client = await AuthenticateAsAdminAsync();
        var pastReference = DateTime.UtcNow.AddMonths(-1);
        var category = await CreateExpenseCategoryAsync($"Despesa Passado Delete {Guid.NewGuid()}");
        var budget = await CreateBudgetDirectAsync(
            $"Orcamento Passado Delete {Guid.NewGuid()}",
            20m,
            pastReference.Year,
            pastReference.Month,
            [category.Id]);

        var response = await client.DeleteAsync($"/api/v1/budgets/{budget.Id}");

        await AssertProblemDetailsAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [DockerAvailableFact]
    public async Task GetBudgetById_WithValidId_ShouldReturn200WithResponse()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync($"Despesa Detail {Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/api/v1/budgets", new
        {
            name = $"Orcamento Detail {Guid.NewGuid()}",
            percentage = 28m,
            referenceYear = reference.Year,
            referenceMonth = reference.Month,
            categoryIds = new[] { category.Id },
            isRecurrent = false
        });

        var created = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>(JsonSerializerOptions);
        created.Should().NotBeNull();

        var response = await client.GetAsync($"/api/v1/budgets/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var budget = await response.Content.ReadFromJsonAsync<BudgetResponse>(JsonSerializerOptions);
        budget.Should().NotBeNull();
        budget!.Id.Should().Be(created.Id);
    }

    [DockerAvailableFact]
    public async Task GetBudgetById_WithInvalidId_ShouldReturn404()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.GetAsync($"/api/v1/budgets/{Guid.NewGuid()}");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task GetBudgets_WithMonthFilter_ShouldReturnFilteredList()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var nextReference = reference.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync($"Despesa Filtro A {Guid.NewGuid()}");
        var categoryB = await CreateExpenseCategoryAsync($"Despesa Filtro B {Guid.NewGuid()}");

        var target = await CreateBudgetDirectAsync(
            $"Orcamento Filtro A {Guid.NewGuid()}",
            15m,
            reference.Year,
            reference.Month,
            [categoryA.Id]);

        await CreateBudgetDirectAsync(
            $"Orcamento Filtro B {Guid.NewGuid()}",
            20m,
            nextReference.Year,
            nextReference.Month,
            [categoryB.Id]);

        var response = await client.GetAsync($"/api/v1/budgets?month={reference.Month}&year={reference.Year}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetResponse>>(JsonSerializerOptions);
        budgets.Should().NotBeNull();
        budgets!.Should().ContainSingle(budget => budget.Id == target.Id);
    }

    [DockerAvailableFact]
    public async Task GetBudgets_WithNoResults_ShouldReturn200EmptyList()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(6);

        var response = await client.GetAsync($"/api/v1/budgets?month={reference.Month}&year={reference.Year}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetResponse>>(JsonSerializerOptions);
        budgets.Should().NotBeNull();
        budgets!.Should().BeEmpty();
    }

    [DockerAvailableFact]
    public async Task GetBudgetSummary_ShouldReturn200WithConsolidatedData()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var accountId = await GetAccountIdAsync(client);
        var incomeCategory = await CreateIncomeCategoryAsync($"Receita Summary {Guid.NewGuid()}");
        var budgetedCategory = await CreateExpenseCategoryAsync($"Despesa Summary Orcada {Guid.NewGuid()}");
        var unbudgetedCategory = await CreateExpenseCategoryAsync($"Despesa Summary Fora {Guid.NewGuid()}");

        await CreateBudgetDirectAsync(
            $"Orcamento Summary {Guid.NewGuid()}",
            40m,
            reference.Year,
            reference.Month,
            [budgetedCategory.Id]);

        await CreateTransactionAsync(accountId, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Paid, 1000m, new DateTime(reference.Year, reference.Month, 5));
        await CreateTransactionAsync(accountId, budgetedCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 200m, new DateTime(reference.Year, reference.Month, 10));
        await CreateTransactionAsync(accountId, unbudgetedCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 50m, new DateTime(reference.Year, reference.Month, 11));

        var response = await client.GetAsync($"/api/v1/budgets/summary?month={reference.Month}&year={reference.Year}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<BudgetSummaryResponse>(JsonSerializerOptions);
        summary.Should().NotBeNull();
        summary!.ReferenceMonth.Should().Be(reference.Month);
        summary.ReferenceYear.Should().Be(reference.Year);
        summary.Budgets.Should().HaveCount(1);
        summary.MonthlyIncome.Should().Be(1000m);
        summary.TotalConsumedAmount.Should().Be(200m);
        summary.UnbudgetedExpenses.Should().Be(50m);
    }

    [DockerAvailableFact]
    public async Task GetBudgetSummary_ShouldIncludeCalculatedFields()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var accountId = await GetAccountIdAsync(client);
        var incomeCategory = await CreateIncomeCategoryAsync($"Receita Calc {Guid.NewGuid()}");
        var budgetedCategory = await CreateExpenseCategoryAsync($"Despesa Calc {Guid.NewGuid()}");

        await CreateBudgetDirectAsync(
            $"Orcamento Calc {Guid.NewGuid()}",
            25m,
            reference.Year,
            reference.Month,
            [budgetedCategory.Id]);

        await CreateTransactionAsync(accountId, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Paid, 2000m, new DateTime(reference.Year, reference.Month, 3));
        await CreateTransactionAsync(accountId, budgetedCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 250m, new DateTime(reference.Year, reference.Month, 8));

        var response = await client.GetAsync($"/api/v1/budgets/summary?month={reference.Month}&year={reference.Year}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<BudgetSummaryResponse>(JsonSerializerOptions);
        summary.Should().NotBeNull();
        summary!.TotalBudgetedPercentage.Should().Be(25m);
        summary.TotalBudgetedAmount.Should().Be(500m);
        summary.TotalRemainingAmount.Should().Be(250m);
        summary.UnbudgetedAmount.Should().Be(1500m);

        var budget = summary.Budgets.Single();
        budget.LimitAmount.Should().Be(500m);
        budget.ConsumedAmount.Should().Be(250m);
        budget.RemainingAmount.Should().Be(250m);
        budget.ConsumedPercentage.Should().Be(50m);
    }

    [DockerAvailableFact]
    public async Task GetAvailablePercentage_ShouldReturn200WithCorrectPercentage()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync($"Despesa Disponivel {Guid.NewGuid()}");

        await CreateBudgetDirectAsync(
            $"Orcamento Disponivel {Guid.NewGuid()}",
            35m,
            reference.Year,
            reference.Month,
            [category.Id]);

        var response = await client.GetAsync($"/api/v1/budgets/available-percentage?month={reference.Month}&year={reference.Year}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var available = await response.Content.ReadFromJsonAsync<AvailablePercentageResponse>(JsonSerializerOptions);
        available.Should().NotBeNull();
        available!.UsedPercentage.Should().Be(35m);
        available.AvailablePercentage.Should().Be(65m);
        available.UsedCategoryIds.Should().Contain(category.Id);
    }

    [DockerAvailableFact]
    public async Task GetAvailablePercentage_WithExcludeBudgetId_ShouldExclude()
    {
        var client = await AuthenticateAsAdminAsync();
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync($"Despesa Excluir A {Guid.NewGuid()}");
        var categoryB = await CreateExpenseCategoryAsync($"Despesa Excluir B {Guid.NewGuid()}");

        var budgetA = await CreateBudgetDirectAsync(
            $"Orcamento Excluir A {Guid.NewGuid()}",
            30m,
            reference.Year,
            reference.Month,
            [categoryA.Id]);

        await CreateBudgetDirectAsync(
            $"Orcamento Excluir B {Guid.NewGuid()}",
            20m,
            reference.Year,
            reference.Month,
            [categoryB.Id]);

        var response = await client.GetAsync($"/api/v1/budgets/available-percentage?month={reference.Month}&year={reference.Year}&excludeBudgetId={budgetA.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var available = await response.Content.ReadFromJsonAsync<AvailablePercentageResponse>(JsonSerializerOptions);
        available.Should().NotBeNull();
        available!.UsedPercentage.Should().Be(20m);
        available.AvailablePercentage.Should().Be(80m);
        available.UsedCategoryIds.Should().Contain(categoryB.Id);
        available.UsedCategoryIds.Should().NotContain(categoryA.Id);
    }

    private async Task<Guid> GetExpenseCategoryIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/categories?type=Despesa");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(JsonSerializerOptions);
        categories.Should().NotBeNull();
        categories!.Should().NotBeEmpty();

        return categories[0].Id;
    }

    private async Task<Guid> GetAccountIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonSerializerOptions);
        accounts.Should().NotBeNull();
        accounts!.Should().NotBeEmpty();

        return accounts[0].Id;
    }

    private async Task<Category> CreateExpenseCategoryAsync(string name)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var category = Category.Create(name, CategoryType.Despesa, "http-test-seed");

        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        return category;
    }

    private async Task<Category> CreateIncomeCategoryAsync(string name)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var category = Category.Create(name, CategoryType.Receita, "http-test-seed");

        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();

        return category;
    }

    private async Task<Budget> CreateBudgetDirectAsync(
        string name,
        decimal percentage,
        int year,
        int month,
        IReadOnlyList<Guid> categoryIds)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var repository = new BudgetRepository(dbContext);

        var budget = Budget.Create(name, percentage, year, month, categoryIds, false, "http-test-seed");
        await repository.AddAsync(budget, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        return budget;
    }

    private async Task CreateTransactionAsync(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        TransactionStatus status,
        decimal amount,
        DateTime competenceDate)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();

        var transaction = Transaction.Create(
            accountId,
            categoryId,
            type,
            amount,
            $"Transacao Seed {Guid.NewGuid()}",
            DateTime.SpecifyKind(competenceDate, DateTimeKind.Utc),
            DateTime.SpecifyKind(competenceDate, DateTimeKind.Utc),
            status,
            "http-test-seed");

        await dbContext.Transactions.AddAsync(transaction);
        await dbContext.SaveChangesAsync();
    }
}