using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Auth;
using GestorFinanceiro.Financeiro.Infra.Context;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Seed;

public static class TestDataSeeder
{
    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Admin123!";
    public const string MemberEmail = "member@test.com";
    public const string MemberPassword = "Member123!";

    public static async Task SeedAsync(FinanceiroDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Users.Any())
        {
            return;
        }

        var passwordHasher = new PasswordHasher();

        var adminUser = User.Create(
            "Admin Test",
            AdminEmail,
            passwordHasher.Hash(AdminPassword),
            UserRole.Admin,
            "seed");

        var memberUser = User.Create(
            "Member Test",
            MemberEmail,
            passwordHasher.Hash(MemberPassword),
            UserRole.Member,
            "seed");

        var checkingAccount = Account.Create(
            "Conta Corrente Seed",
            AccountType.Corrente,
            5000m,
            false,
            "seed");

        var investmentAccount = Account.Create(
            "Conta Investimento Seed",
            AccountType.Investimento,
            3000m,
            false,
            "seed");

        var incomeCategory = Category.Create("Salario", CategoryType.Receita, "seed");
        var expenseCategoryOne = Category.Create("Alimentacao", CategoryType.Despesa, "seed");
        var expenseCategoryTwo = Category.Create("Moradia", CategoryType.Despesa, "seed");

        var salaryTransaction = Transaction.Create(
            checkingAccount.Id,
            incomeCategory.Id,
            TransactionType.Credit,
            3500m,
            "Salario mensal",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            TransactionStatus.Paid,
            "seed");

        var expenseTransaction = Transaction.Create(
            checkingAccount.Id,
            expenseCategoryOne.Id,
            TransactionType.Debit,
            150m,
            "Mercado semanal",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(1),
            TransactionStatus.Pending,
            "seed");

        await context.Users.AddRangeAsync([adminUser, memberUser], cancellationToken);
        await context.Accounts.AddRangeAsync([checkingAccount, investmentAccount], cancellationToken);
        await context.Categories.AddRangeAsync([incomeCategory, expenseCategoryOne, expenseCategoryTwo], cancellationToken);
        await context.Transactions.AddRangeAsync([salaryTransaction, expenseTransaction], cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
