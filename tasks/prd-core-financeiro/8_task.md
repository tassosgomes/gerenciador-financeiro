---
status: pending
parallelizable: false
blocked_by: ["4.0", "7.0"]
---

<task_context>
<domain>infra/persistência</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"9.0", "10.0"</unblocks>
</task_context>

# Tarefa 8.0: Infra Layer — Repositories e UnitOfWork

## Visão Geral

Implementar os repositories concretos e o `UnitOfWork`, que são a ponte entre o domínio e o banco de dados PostgreSQL via Entity Framework Core. Destaque para o `GetByIdWithLockAsync` do `AccountRepository`, que implementa `SELECT FOR UPDATE` para pessimistic locking.

## Requisitos

- PRD F10 req 43: row-level locking via `SELECT FOR UPDATE` na conta alvo
- PRD F10 req 44: operações dentro de transação ACID isolada → `UnitOfWork`
- Techspec: `IUnitOfWork` com begin/commit/rollback
- `rules/dotnet-performance.md`: usar `AsNoTracking()` em queries de leitura
- `rules/dotnet-observability.md`: `CancellationToken` em todos os métodos async

## Subtarefas

- [ ] 8.1 Criar repositório genérico base `Repository<T>` implementando `IRepository<T>` (abstrato)
- [ ] 8.2 Criar `AccountRepository` implementando `IAccountRepository` com `GetByIdWithLockAsync` usando `SELECT FOR UPDATE`
- [ ] 8.3 Criar `TransactionRepository` implementando `ITransactionRepository` com queries por `InstallmentGroupId`, `TransferGroupId`, `OperationId`
- [ ] 8.4 Criar `CategoryRepository` implementando `ICategoryRepository` com `ExistsByNameAndTypeAsync`
- [ ] 8.5 Criar `RecurrenceTemplateRepository` implementando `IRecurrenceTemplateRepository` com `GetActiveTemplatesAsync`
- [ ] 8.6 Criar `OperationLogRepository` implementando `IOperationLogRepository` com `CleanupExpiredAsync`
- [ ] 8.7 Criar `UnitOfWork` implementando `IUnitOfWork` com gerenciamento de `IDbContextTransaction`
- [ ] 8.8 Criar extensão de DI (`ServiceCollectionExtensions`) para registrar repositories e UnitOfWork

## Sequenciamento

- Bloqueado por: 4.0 (interfaces), 7.0 (DbContext)
- Desbloqueia: 9.0 (Application Handlers usam repositories), 10.0 (Testes de integração testam repositories)
- Paralelizável: Não (depende de 7.0 que pode estar em paralelo com 5.0)

## Detalhes de Implementação

### Localização dos arquivos

```
4-Infra/GestorFinanceiro.Financeiro.Infra/
├── Repository/
│   ├── Repository.cs              (genérico abstrato)
│   ├── AccountRepository.cs
│   ├── TransactionRepository.cs
│   ├── CategoryRepository.cs
│   ├── RecurrenceTemplateRepository.cs
│   └── OperationLogRepository.cs
├── UnitOfWork/
│   └── UnitOfWork.cs
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs
```

### Repository genérico

```csharp
public abstract class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly FinanceiroDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected Repository(FinanceiroDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbSet.FindAsync(new object[] { id }, cancellationToken);

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public void Update(T entity)
        => _dbSet.Update(entity);
}
```

### AccountRepository — SELECT FOR UPDATE

```csharp
public async Task<Account> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken)
{
    // PostgreSQL: row-level lock via raw SQL dentro da transaction ativa
    return await _context.Accounts
        .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {id} FOR UPDATE")
        .SingleAsync(cancellationToken);
}
```

**Importante**: O `SELECT FOR UPDATE` só funciona dentro de uma transação aberta via `UnitOfWork.BeginTransactionAsync()`. O lock é liberado automaticamente no `CommitAsync()` ou `RollbackAsync()`.

### UnitOfWork

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly FinanceiroDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(FinanceiroDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
        => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
        if (_transaction != null) await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction != null) await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

### OperationLogRepository — Cleanup

```csharp
public async Task CleanupExpiredAsync(CancellationToken cancellationToken)
{
    await _context.OperationLogs
        .Where(o => o.ExpiresAt < DateTime.UtcNow)
        .ExecuteDeleteAsync(cancellationToken);
}
```

### ServiceCollectionExtensions

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FinanceiroDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRecurrenceTemplateRepository, RecurrenceTemplateRepository>();
        services.AddScoped<IOperationLogRepository, OperationLogRepository>();

        return services;
    }
}
```

## Critérios de Sucesso

- Todos os 5 repositories + 1 repository de OperationLog implementados
- `UnitOfWork` gerencia transação ACID com begin/commit/rollback
- `GetByIdWithLockAsync` usa `SELECT FOR UPDATE` via SQL interpolado
- `CleanupExpiredAsync` remove registros com `ExpiresAt < now()`
- Extensão de DI registra todos os serviços da infraestrutura
- `dotnet build` compila sem erros
- Nenhum método assíncrono sem `CancellationToken`
