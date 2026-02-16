using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class TransactionRepositoryTests : IntegrationTestBase
{
    public TransactionRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task TransactionRepository_AddAndGetByInstallmentGroup_RetornaParcelas()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var installmentGroupId = Guid.NewGuid();
        var repository = new TransactionRepository(DbContext);

        var firstInstallment = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Parcela 1",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            TransactionStatus.Pending,
            "integration-user");
        firstInstallment.SetInstallmentInfo(installmentGroupId, 1, 2);

        var secondInstallment = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Parcela 2",
            DateTime.UtcNow.Date.AddMonths(1),
            DateTime.UtcNow.Date.AddMonths(1),
            TransactionStatus.Pending,
            "integration-user");
        secondInstallment.SetInstallmentInfo(installmentGroupId, 2, 2);

        await repository.AddAsync(firstInstallment, cancellationToken);
        await repository.AddAsync(secondInstallment, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var installments = (await repository.GetByInstallmentGroupAsync(installmentGroupId, cancellationToken)).ToList();

        installments.Should().HaveCount(2);
        installments[0].InstallmentNumber.Should().Be(1);
        installments[1].InstallmentNumber.Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task TransactionRepository_GetByOperationId_RetornaTransacaoCorreta()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 300m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Receita, cancellationToken);
        var repository = new TransactionRepository(DbContext);
        var operationId = $"op-{Guid.NewGuid()}";

        var transaction = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Credit,
            90m,
            "Credito",
            DateTime.UtcNow.Date,
            null,
            TransactionStatus.Paid,
            "integration-user",
            operationId);

        await repository.AddAsync(transaction, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedTransaction = await repository.GetByOperationIdAsync(operationId, cancellationToken);

        persistedTransaction.Should().NotBeNull();
        persistedTransaction!.Id.Should().Be(transaction.Id);
        persistedTransaction.OperationId.Should().Be(operationId);
    }

    [DockerAvailableFact]
    public async Task GetByAccountAndPeriodAsync_WithTransactionsInPeriod_ShouldReturnFiltered()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var repository = new TransactionRepository(DbContext);

        var startDate = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc); // 5 fev
        var endDate = new DateTime(2026, 3, 4, 23, 59, 59, DateTimeKind.Utc); // 4 mar

        // Transação dentro do período
        var transaction1 = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Compra 1",
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        var transaction2 = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            200m,
            "Compra 2",
            new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        await repository.AddAsync(transaction1, cancellationToken);
        await repository.AddAsync(transaction2, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await repository.GetByAccountAndPeriodAsync(account.Id, startDate, endDate, cancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == transaction1.Id);
        result.Should().Contain(t => t.Id == transaction2.Id);
        result.Should().BeInAscendingOrder(t => t.CompetenceDate);
    }

    [DockerAvailableFact]
    public async Task GetByAccountAndPeriodAsync_WithTransactionsOutsidePeriod_ShouldExclude()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var repository = new TransactionRepository(DbContext);

        var startDate = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 3, 4, 23, 59, 59, DateTimeKind.Utc);

        // Transação antes do período
        var transactionBefore = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Compra Antes",
            new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        // Transação durante o período
        var transactionDuring = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            200m,
            "Compra Durante",
            new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        // Transação depois do período
        var transactionAfter = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            300m,
            "Compra Depois",
            new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        await repository.AddAsync(transactionBefore, cancellationToken);
        await repository.AddAsync(transactionDuring, cancellationToken);
        await repository.AddAsync(transactionAfter, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await repository.GetByAccountAndPeriodAsync(account.Id, startDate, endDate, cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(t => t.Id == transactionDuring.Id);
    }

    [DockerAvailableFact]
    public async Task GetByAccountAndPeriodAsync_ShouldOnlyReturnPaidTransactions()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var repository = new TransactionRepository(DbContext);

        var startDate = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 3, 4, 23, 59, 59, DateTimeKind.Utc);

        // Transação paga
        var paidTransaction = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Compra Paga",
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        // Transação pendente
        var pendingTransaction = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            200m,
            "Compra Pendente",
            new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc),
            null,
            TransactionStatus.Pending,
            "integration-user");

        await repository.AddAsync(paidTransaction, cancellationToken);
        await repository.AddAsync(pendingTransaction, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await repository.GetByAccountAndPeriodAsync(account.Id, startDate, endDate, cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(t => t.Id == paidTransaction.Id);
        result.Should().OnlyContain(t => t.Status == TransactionStatus.Paid);
    }

    [DockerAvailableFact]
    public async Task GetByAccountAndPeriodAsync_WithDifferentAccounts_ShouldFilterByAccount()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var account1 = await CreateAccountAsync($"Conta-1-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var account2 = await CreateAccountAsync($"Conta-2-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var repository = new TransactionRepository(DbContext);

        var startDate = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 3, 4, 23, 59, 59, DateTimeKind.Utc);

        // Transação da conta 1
        var transaction1 = Transaction.Create(
            account1.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Compra Conta 1",
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        // Transação da conta 2
        var transaction2 = Transaction.Create(
            account2.Id,
            category.Id,
            TransactionType.Debit,
            200m,
            "Compra Conta 2",
            new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc),
            TransactionStatus.Paid,
            "integration-user");

        await repository.AddAsync(transaction1, cancellationToken);
        await repository.AddAsync(transaction2, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        // Act
        var result = await repository.GetByAccountAndPeriodAsync(account1.Id, startDate, endDate, cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(t => t.AccountId == account1.Id);
    }
}
