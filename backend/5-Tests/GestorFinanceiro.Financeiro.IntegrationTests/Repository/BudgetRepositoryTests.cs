using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Model;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class BudgetRepositoryTests : IntegrationTestBase
{
    public BudgetRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task GetByMonthAsync_ShouldReturnOnlyBudgetsOfSpecifiedMonth()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var otherReference = reference.AddMonths(1);

        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        await CreateBudgetAsync("Orcamento Mes", 30m, reference.Year, reference.Month, [categoryA.Id]);
        await CreateBudgetAsync("Orcamento Outro Mes", 25m, otherReference.Year, otherReference.Month, [categoryB.Id]);

        var repository = CreateRepository();
        var budgets = await repository.GetByMonthAsync(reference.Year, reference.Month, CancellationToken.None);

        budgets.Should().HaveCount(1);
        budgets[0].Name.Should().Be("Orcamento Mes");
    }

    [DockerAvailableFact]
    public async Task GetByMonthAsync_WithNoBudgets_ShouldReturnEmptyList()
    {
        var repository = CreateRepository();
        var reference = DateTime.UtcNow.AddMonths(1);

        var budgets = await repository.GetByMonthAsync(reference.Year, reference.Month, CancellationToken.None);

        budgets.Should().BeEmpty();
    }

    [DockerAvailableFact]
    public async Task GetByIdWithCategoriesAsync_ShouldReturnBudgetWithCategories()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        var budget = await CreateBudgetAsync(
            "Orcamento Com Categorias",
            45m,
            reference.Year,
            reference.Month,
            [categoryA.Id, categoryB.Id]);

        var repository = CreateRepository();
        var found = await repository.GetByIdWithCategoriesAsync(budget.Id, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(budget.Id);
        found.CategoryIds.Should().BeEquivalentTo([categoryA.Id, categoryB.Id]);
    }

    [DockerAvailableFact]
    public async Task GetByIdWithCategoriesAsync_WithInvalidId_ShouldReturnNull()
    {
        var repository = CreateRepository();

        var found = await repository.GetByIdWithCategoriesAsync(Guid.NewGuid(), CancellationToken.None);

        found.Should().BeNull();
    }

    [DockerAvailableFact]
    public async Task GetTotalPercentageForMonthAsync_ShouldReturnCorrectSum()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        await CreateBudgetAsync("Orcamento A", 35m, reference.Year, reference.Month, [categoryA.Id]);
        await CreateBudgetAsync("Orcamento B", 20m, reference.Year, reference.Month, [categoryB.Id]);

        var repository = CreateRepository();
        var total = await repository.GetTotalPercentageForMonthAsync(reference.Year, reference.Month, null, CancellationToken.None);

        total.Should().Be(55m);
    }

    [DockerAvailableFact]
    public async Task GetTotalPercentageForMonthAsync_WithExcludeBudgetId_ShouldExclude()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        var budgetA = await CreateBudgetAsync("Orcamento A", 40m, reference.Year, reference.Month, [categoryA.Id]);
        await CreateBudgetAsync("Orcamento B", 25m, reference.Year, reference.Month, [categoryB.Id]);

        var repository = CreateRepository();
        var total = await repository.GetTotalPercentageForMonthAsync(reference.Year, reference.Month, budgetA.Id, CancellationToken.None);

        total.Should().Be(25m);
    }

    [DockerAvailableFact]
    public async Task GetTotalPercentageForMonthAsync_WithNoBudgets_ShouldReturnZero()
    {
        var repository = CreateRepository();
        var reference = DateTime.UtcNow.AddMonths(2);

        var total = await repository.GetTotalPercentageForMonthAsync(reference.Year, reference.Month, null, CancellationToken.None);

        total.Should().Be(0m);
    }

    [DockerAvailableFact]
    public async Task IsCategoryUsedInMonthAsync_WhenUsed_ShouldReturnTrue()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");
        await CreateBudgetAsync("Orcamento", 30m, reference.Year, reference.Month, [category.Id]);

        var repository = CreateRepository();
        var isUsed = await repository.IsCategoryUsedInMonthAsync(category.Id, reference.Year, reference.Month, null, CancellationToken.None);

        isUsed.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task IsCategoryUsedInMonthAsync_WhenNotUsed_ShouldReturnFalse()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");

        var repository = CreateRepository();
        var isUsed = await repository.IsCategoryUsedInMonthAsync(category.Id, reference.Year, reference.Month, null, CancellationToken.None);

        isUsed.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task IsCategoryUsedInMonthAsync_WithExcludeBudgetId_ShouldExclude()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");
        var budget = await CreateBudgetAsync("Orcamento", 30m, reference.Year, reference.Month, [category.Id]);

        var repository = CreateRepository();
        var isUsed = await repository.IsCategoryUsedInMonthAsync(category.Id, reference.Year, reference.Month, budget.Id, CancellationToken.None);

        isUsed.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task GetUsedCategoryIdsForMonthAsync_ShouldReturnAllUsedIds()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        await CreateBudgetAsync("Orcamento A", 20m, reference.Year, reference.Month, [categoryA.Id]);
        await CreateBudgetAsync("Orcamento B", 30m, reference.Year, reference.Month, [categoryB.Id]);

        var repository = CreateRepository();
        var usedIds = await repository.GetUsedCategoryIdsForMonthAsync(reference.Year, reference.Month, null, CancellationToken.None);

        usedIds.Should().BeEquivalentTo([categoryA.Id, categoryB.Id]);
    }

    [DockerAvailableFact]
    public async Task GetMonthlyIncomeAsync_ShouldSumOnlyCreditPaidTransactions()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var incomeCategory = await CreateIncomeCategoryAsync("Receita A");
        var expenseCategory = await CreateExpenseCategoryAsync("Despesa A");

        await CreateTransactionAsync(account.Id, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Paid, 1000m, new DateTime(reference.Year, reference.Month, 5));
        await CreateTransactionAsync(account.Id, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Pending, 200m, new DateTime(reference.Year, reference.Month, 6));
        await CreateTransactionAsync(account.Id, expenseCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 300m, new DateTime(reference.Year, reference.Month, 7));

        var repository = CreateRepository();
        var monthlyIncome = await repository.GetMonthlyIncomeAsync(reference.Year, reference.Month, CancellationToken.None);

        monthlyIncome.Should().Be(1000m);
    }

    [DockerAvailableFact]
    public async Task GetMonthlyIncomeAsync_ShouldExcludeCancelledTransactions()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var incomeCategory = await CreateIncomeCategoryAsync("Receita A");

        await CreateTransactionAsync(account.Id, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Paid, 1000m, new DateTime(reference.Year, reference.Month, 5));
        await CreateTransactionAsync(account.Id, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Cancelled, 500m, new DateTime(reference.Year, reference.Month, 6));

        var repository = CreateRepository();
        var monthlyIncome = await repository.GetMonthlyIncomeAsync(reference.Year, reference.Month, CancellationToken.None);

        monthlyIncome.Should().Be(1000m);
    }

    [DockerAvailableFact]
    public async Task GetMonthlyIncomeAsync_WithNoTransactions_ShouldReturnZero()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var repository = CreateRepository();

        var monthlyIncome = await repository.GetMonthlyIncomeAsync(reference.Year, reference.Month, CancellationToken.None);

        monthlyIncome.Should().Be(0m);
    }

    [DockerAvailableFact]
    public async Task GetConsumedAmountAsync_ShouldSumOnlyDebitPaidOfSpecifiedCategories()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var budgetCategory = await CreateExpenseCategoryAsync("Despesa A");
        var otherExpenseCategory = await CreateExpenseCategoryAsync("Despesa B");
        var incomeCategory = await CreateIncomeCategoryAsync("Receita A");

        await CreateTransactionAsync(account.Id, budgetCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 120m, new DateTime(reference.Year, reference.Month, 8));
        await CreateTransactionAsync(account.Id, budgetCategory.Id, TransactionType.Debit, TransactionStatus.Pending, 50m, new DateTime(reference.Year, reference.Month, 9));
        await CreateTransactionAsync(account.Id, otherExpenseCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 80m, new DateTime(reference.Year, reference.Month, 10));
        await CreateTransactionAsync(account.Id, incomeCategory.Id, TransactionType.Credit, TransactionStatus.Paid, 300m, new DateTime(reference.Year, reference.Month, 11));

        var repository = CreateRepository();
        var consumed = await repository.GetConsumedAmountAsync([budgetCategory.Id], reference.Year, reference.Month, CancellationToken.None);

        consumed.Should().Be(120m);
    }

    [DockerAvailableFact]
    public async Task GetConsumedAmountAsync_ShouldExcludeCancelledTransactions()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var category = await CreateExpenseCategoryAsync("Despesa A");

        await CreateTransactionAsync(account.Id, category.Id, TransactionType.Debit, TransactionStatus.Paid, 200m, new DateTime(reference.Year, reference.Month, 5));
        await CreateTransactionAsync(account.Id, category.Id, TransactionType.Debit, TransactionStatus.Cancelled, 90m, new DateTime(reference.Year, reference.Month, 6));

        var repository = CreateRepository();
        var consumed = await repository.GetConsumedAmountAsync([category.Id], reference.Year, reference.Month, CancellationToken.None);

        consumed.Should().Be(200m);
    }

    [DockerAvailableFact]
    public async Task GetConsumedAmountAsync_ShouldFilterByCompetenceMonth()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var previous = reference.AddMonths(-1);
        var next = reference.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var category = await CreateExpenseCategoryAsync("Despesa A");

        await CreateTransactionAsync(account.Id, category.Id, TransactionType.Debit, TransactionStatus.Paid, 70m, new DateTime(previous.Year, previous.Month, 15));
        await CreateTransactionAsync(account.Id, category.Id, TransactionType.Debit, TransactionStatus.Paid, 150m, new DateTime(reference.Year, reference.Month, 15));
        await CreateTransactionAsync(account.Id, category.Id, TransactionType.Debit, TransactionStatus.Paid, 40m, new DateTime(next.Year, next.Month, 15));

        var repository = CreateRepository();
        var consumed = await repository.GetConsumedAmountAsync([category.Id], reference.Year, reference.Month, CancellationToken.None);

        consumed.Should().Be(150m);
    }

    [DockerAvailableFact]
    public async Task GetConsumedAmountAsync_WithNoTransactions_ShouldReturnZero()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");
        var repository = CreateRepository();

        var consumed = await repository.GetConsumedAmountAsync([category.Id], reference.Year, reference.Month, CancellationToken.None);

        consumed.Should().Be(0m);
    }

    [DockerAvailableFact]
    public async Task GetUnbudgetedExpensesAsync_ShouldSumDebitPaidNotInAnyBudget()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var budgetedCategory = await CreateExpenseCategoryAsync("Despesa Orcada");
        var unbudgetedCategory = await CreateExpenseCategoryAsync("Despesa Fora");

        await CreateBudgetAsync("Orcamento", 20m, reference.Year, reference.Month, [budgetedCategory.Id]);

        await CreateTransactionAsync(account.Id, budgetedCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 90m, new DateTime(reference.Year, reference.Month, 10));
        await CreateTransactionAsync(account.Id, unbudgetedCategory.Id, TransactionType.Debit, TransactionStatus.Paid, 140m, new DateTime(reference.Year, reference.Month, 11));
        await CreateTransactionAsync(account.Id, unbudgetedCategory.Id, TransactionType.Debit, TransactionStatus.Cancelled, 20m, new DateTime(reference.Year, reference.Month, 12));

        var repository = CreateRepository();
        var unbudgeted = await repository.GetUnbudgetedExpensesAsync(reference.Year, reference.Month, CancellationToken.None);

        unbudgeted.Should().Be(140m);
    }

    [DockerAvailableFact]
    public async Task GetUnbudgetedExpensesAsync_WithAllCategoriesBudgeted_ShouldReturnZero()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, CancellationToken.None);
        var category = await CreateExpenseCategoryAsync("Despesa Orcada");

        await CreateBudgetAsync("Orcamento", 20m, reference.Year, reference.Month, [category.Id]);
        await CreateTransactionAsync(account.Id, category.Id, TransactionType.Debit, TransactionStatus.Paid, 80m, new DateTime(reference.Year, reference.Month, 11));

        var repository = CreateRepository();
        var unbudgeted = await repository.GetUnbudgetedExpensesAsync(reference.Year, reference.Month, CancellationToken.None);

        unbudgeted.Should().Be(0m);
    }

    [DockerAvailableFact]
    public async Task GetRecurrentBudgetsForMonthAsync_ShouldReturnOnlyRecurrent()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        await CreateBudgetAsync("Orcamento Recorrente", 25m, reference.Year, reference.Month, [categoryA.Id], true);
        await CreateBudgetAsync("Orcamento Nao Recorrente", 30m, reference.Year, reference.Month, [categoryB.Id], false);

        var repository = CreateRepository();
        var recurrent = await repository.GetRecurrentBudgetsForMonthAsync(reference.Year, reference.Month, CancellationToken.None);

        recurrent.Should().HaveCount(1);
        recurrent[0].IsRecurrent.Should().BeTrue();
        recurrent[0].Name.Should().Be("Orcamento Recorrente");
    }

    [DockerAvailableFact]
    public async Task ExistsByNameAsync_WhenExists_ShouldReturnTrue()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");
        await CreateBudgetAsync("Orcamento Nome", 35m, reference.Year, reference.Month, [category.Id]);

        var repository = CreateRepository();
        var exists = await repository.ExistsByNameAsync("Orcamento Nome", null, CancellationToken.None);

        exists.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task ExistsByNameAsync_WhenNotExists_ShouldReturnFalse()
    {
        var repository = CreateRepository();

        var exists = await repository.ExistsByNameAsync("Nome Inexistente", null, CancellationToken.None);

        exists.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task ExistsByNameAsync_WithExcludeBudgetId_ShouldExclude()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");
        var budget = await CreateBudgetAsync("Orcamento Nome", 35m, reference.Year, reference.Month, [category.Id]);

        var repository = CreateRepository();
        var exists = await repository.ExistsByNameAsync("Orcamento Nome", budget.Id, CancellationToken.None);

        exists.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task RemoveCategoryFromBudgetsAsync_ShouldRemoveFromAllBudgets()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var nextReference = reference.AddMonths(1);
        var sharedCategory = await CreateExpenseCategoryAsync("Despesa Compartilhada");

        await CreateBudgetAsync("Orcamento A", 20m, reference.Year, reference.Month, [sharedCategory.Id]);
        await CreateBudgetAsync("Orcamento B", 30m, nextReference.Year, nextReference.Month, [sharedCategory.Id]);

        var repository = CreateRepository();
        await repository.RemoveCategoryFromBudgetsAsync(sharedCategory.Id, CancellationToken.None);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var links = await DbContext.Set<BudgetCategoryLink>()
            .Where(link => link.CategoryId == sharedCategory.Id)
            .ToListAsync(CancellationToken.None);

        links.Should().BeEmpty();
    }

    [DockerAvailableFact]
    public async Task Insert_DuplicateCategoryInSameMonth_ShouldThrowException()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");

        await CreateBudgetAsync("Orcamento A", 20m, reference.Year, reference.Month, [category.Id]);

        var repository = CreateRepository();
        var duplicateBudget = Budget.Create(
            "Orcamento B",
            30m,
            reference.Year,
            reference.Month,
            [category.Id],
            false,
            "integration-user");

        await repository.AddAsync(duplicateBudget, CancellationToken.None);

        var act = async () => await DbContext.SaveChangesAsync(CancellationToken.None);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(act);

        var postgresException = exception.InnerException as PostgresException;
        postgresException.Should().NotBeNull();
        postgresException!.ConstraintName.Should().Be("ux_budget_categories_category_reference");
    }

    [DockerAvailableFact]
    public async Task Insert_SameCategoryDifferentMonth_ShouldSucceed()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var nextReference = reference.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");

        await CreateBudgetAsync("Orcamento A", 20m, reference.Year, reference.Month, [category.Id]);

        var repository = CreateRepository();
        var budget = Budget.Create(
            "Orcamento B",
            25m,
            nextReference.Year,
            nextReference.Month,
            [category.Id],
            false,
            "integration-user");

        await repository.AddAsync(budget, CancellationToken.None);
        var saveChanges = await DbContext.SaveChangesAsync(CancellationToken.None);

        saveChanges.Should().BeGreaterThan(0);
    }

    [DockerAvailableFact]
    public async Task Insert_DuplicateName_ShouldThrowException()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var nextReference = reference.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        await CreateBudgetAsync("Orcamento Unico", 20m, reference.Year, reference.Month, [categoryA.Id]);

        var repository = CreateRepository();
        var duplicateNameBudget = Budget.Create(
            "Orcamento Unico",
            25m,
            nextReference.Year,
            nextReference.Month,
            [categoryB.Id],
            false,
            "integration-user");

        await repository.AddAsync(duplicateNameBudget, CancellationToken.None);

        var act = async () => await DbContext.SaveChangesAsync(CancellationToken.None);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(act);

        var postgresException = exception.InnerException as PostgresException;
        postgresException.Should().NotBeNull();
        postgresException!.ConstraintName.Should().Be("ux_budgets_name");
    }

    [DockerAvailableFact]
    public async Task DeleteBudget_ShouldCascadeDeleteBudgetCategories()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var categoryA = await CreateExpenseCategoryAsync("Despesa A");
        var categoryB = await CreateExpenseCategoryAsync("Despesa B");

        var budget = await CreateBudgetAsync(
            "Orcamento Cascade",
            20m,
            reference.Year,
            reference.Month,
            [categoryA.Id, categoryB.Id]);

        var repository = CreateRepository();
        repository.Remove(budget);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var links = await DbContext.Set<BudgetCategoryLink>()
            .Where(link => link.BudgetId == budget.Id)
            .ToListAsync(CancellationToken.None);

        links.Should().BeEmpty();
    }

    [DockerAvailableFact]
    public async Task DeleteCategory_ShouldCascadeDeleteFromBudgetCategories()
    {
        var reference = DateTime.UtcNow.AddMonths(1);
        var category = await CreateExpenseCategoryAsync("Despesa A");
        await CreateBudgetAsync("Orcamento Cascade Categoria", 20m, reference.Year, reference.Month, [category.Id]);

        DbContext.Categories.Remove(category);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var links = await DbContext.Set<BudgetCategoryLink>()
            .Where(link => link.CategoryId == category.Id)
            .ToListAsync(CancellationToken.None);

        links.Should().BeEmpty();
    }

    private BudgetRepository CreateRepository()
    {
        return new BudgetRepository(DbContext);
    }

    private async Task<Category> CreateExpenseCategoryAsync(string name)
    {
        return await CreateCategoryAsync($"{name}-{Guid.NewGuid()}", CategoryType.Despesa, CancellationToken.None);
    }

    private async Task<Category> CreateIncomeCategoryAsync(string name)
    {
        return await CreateCategoryAsync($"{name}-{Guid.NewGuid()}", CategoryType.Receita, CancellationToken.None);
    }

    private async Task<Budget> CreateBudgetAsync(
        string name,
        decimal percentage,
        int year,
        int month,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent = false)
    {
        var repository = CreateRepository();
        var budget = Budget.Create(
            name,
            percentage,
            year,
            month,
            categoryIds,
            isRecurrent,
            "integration-user");

        await repository.AddAsync(budget, CancellationToken.None);
        await DbContext.SaveChangesAsync(CancellationToken.None);
        return budget;
    }

    private async Task<Transaction> CreateTransactionAsync(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        TransactionStatus status,
        decimal amount,
        DateTime competenceDate)
    {
        var transaction = Transaction.Create(
            accountId,
            categoryId,
            type,
            amount,
            $"Mov-{Guid.NewGuid()}",
            DateTime.SpecifyKind(competenceDate, DateTimeKind.Utc),
            DateTime.SpecifyKind(competenceDate, DateTimeKind.Utc),
            status,
            "integration-user");

        await DbContext.Transactions.AddAsync(transaction, CancellationToken.None);
        await DbContext.SaveChangesAsync(CancellationToken.None);
        return transaction;
    }
}