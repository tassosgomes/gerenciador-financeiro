using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class EstablishmentRepositoryTests : IntegrationTestBase
{
    public EstablishmentRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task EstablishmentRepository_AddAndGetByTransactionId_PersistERecuperaCorretamente()
    {
        var cancellationToken = CancellationToken.None;
        var transaction = await CreatePaidTransactionAsync(cancellationToken);
        var repository = new EstablishmentRepository(DbContext);

        var establishment = Establishment.Create(
            transaction.Id,
            "SUPERMERCADO TESTE LTDA",
            "12345678000190",
            "12345678901234567890123456789012345678901234",
            "integration-user");

        await repository.AddAsync(establishment, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedEstablishment = await repository.GetByTransactionIdAsync(transaction.Id, cancellationToken);

        persistedEstablishment.Should().NotBeNull();
        persistedEstablishment!.Id.Should().Be(establishment.Id);
        persistedEstablishment.AccessKey.Should().Be(establishment.AccessKey);
        persistedEstablishment.Cnpj.Should().Be(establishment.Cnpj);
    }

    [DockerAvailableFact]
    public async Task EstablishmentRepository_ExistsByAccessKeyAsync_DeveRetornarTrueQuandoExiste()
    {
        var cancellationToken = CancellationToken.None;
        var transaction = await CreatePaidTransactionAsync(cancellationToken);
        var repository = new EstablishmentRepository(DbContext);
        var accessKey = "99999999999999999999999999999999999999999999";

        var establishment = Establishment.Create(
            transaction.Id,
            "FARMACIA TESTE LTDA",
            "98765432000111",
            accessKey,
            "integration-user");

        await repository.AddAsync(establishment, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var exists = await repository.ExistsByAccessKeyAsync(accessKey, cancellationToken);

        exists.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task EstablishmentRepository_ExistsByAccessKeyAsync_DeveRetornarFalseQuandoNaoExiste()
    {
        var repository = new EstablishmentRepository(DbContext);

        var exists = await repository.ExistsByAccessKeyAsync(
            "00000000000000000000000000000000000000000000",
            CancellationToken.None);

        exists.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task EstablishmentRepository_Remove_DeveExcluirRegistro()
    {
        var cancellationToken = CancellationToken.None;
        var transaction = await CreatePaidTransactionAsync(cancellationToken);
        var repository = new EstablishmentRepository(DbContext);

        var establishment = Establishment.Create(
            transaction.Id,
            "LOJA TESTE LTDA",
            "11111111000111",
            "11111111111111111111111111111111111111111111",
            "integration-user");

        await repository.AddAsync(establishment, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        repository.Remove(establishment);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedEstablishment = await repository.GetByTransactionIdAsync(transaction.Id, cancellationToken);
        persistedEstablishment.Should().BeNull();
    }

    [DockerAvailableFact]
    public async Task EstablishmentRepository_InsertDuplicateAccessKey_ShouldThrowException()
    {
        var cancellationToken = CancellationToken.None;
        var firstTransaction = await CreatePaidTransactionAsync(cancellationToken);
        var secondTransaction = await CreatePaidTransactionAsync(cancellationToken);
        var repository = new EstablishmentRepository(DbContext);
        var accessKey = "22222222222222222222222222222222222222222222";

        var firstEstablishment = Establishment.Create(
            firstTransaction.Id,
            "MERCADO A",
            "22222222000122",
            accessKey,
            "integration-user");

        var secondEstablishment = Establishment.Create(
            secondTransaction.Id,
            "MERCADO B",
            "33333333000133",
            accessKey,
            "integration-user");

        await repository.AddAsync(firstEstablishment, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        await repository.AddAsync(secondEstablishment, cancellationToken);

        var act = async () => await DbContext.SaveChangesAsync(cancellationToken);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(act);

        var postgresException = exception.InnerException as PostgresException;
        postgresException.Should().NotBeNull();
        postgresException!.ConstraintName.Should().Be("ix_establishments_access_key");
    }

    [DockerAvailableFact]
    public async Task DeleteTransaction_ShouldCascadeDeleteReceiptItemsAndEstablishment()
    {
        var cancellationToken = CancellationToken.None;
        var transaction = await CreatePaidTransactionAsync(cancellationToken);
        var establishmentRepository = new EstablishmentRepository(DbContext);
        var receiptItemRepository = new ReceiptItemRepository(DbContext);

        var establishment = Establishment.Create(
            transaction.Id,
            "ATACADO CASCADE LTDA",
            "44444444000144",
            "44444444444444444444444444444444444444444444",
            "integration-user");

        var items = new[]
        {
            ReceiptItem.Create(transaction.Id, "Item 1", "I1", 1m, "UN", 5m, 5m, 1, "integration-user"),
            ReceiptItem.Create(transaction.Id, "Item 2", "I2", 2m, "UN", 3m, 6m, 2, "integration-user"),
        };

        await establishmentRepository.AddAsync(establishment, cancellationToken);
        await receiptItemRepository.AddRangeAsync(items, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        DbContext.Transactions.Remove(transaction);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedEstablishment = await DbContext.Establishments
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.TransactionId == transaction.Id, cancellationToken);
        var persistedItems = await DbContext.ReceiptItems
            .AsNoTracking()
            .Where(entity => entity.TransactionId == transaction.Id)
            .ToListAsync(cancellationToken);

        persistedEstablishment.Should().BeNull();
        persistedItems.Should().BeEmpty();
    }

    private async Task<Transaction> CreatePaidTransactionAsync(CancellationToken cancellationToken)
    {
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);

        var transaction = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            120m,
            "Compra estabelecimento",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            TransactionStatus.Paid,
            "integration-user");

        await DbContext.Transactions.AddAsync(transaction, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}
