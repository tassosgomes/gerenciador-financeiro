---
status: pending
parallelizable: false
blocked_by: ["3.0"]
---

<task_context>
<domain>engine/domínio</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"5.0", "7.0", "8.0"</unblocks>
</task_context>

# Tarefa 4.0: Domain Layer — Interfaces de Repositório

## Visão Geral

Definir os contratos (interfaces) que a camada de infraestrutura deverá implementar. Inclui o repositório genérico base, repositórios específicos por entidade, o contrato de `IUnitOfWork` e o repositório de `OperationLog` para idempotência.

As interfaces ficam no Domain layer para garantir inversão de dependência — a infraestrutura depende do domínio, nunca o contrário.

## Requisitos

- PRD F10 req 43: operações com row-level locking → `GetByIdWithLockAsync` no `IAccountRepository`
- PRD F10 req 44: transação de banco isolada → `IUnitOfWork` com `BeginTransactionAsync/CommitAsync/RollbackAsync`
- PRD F10 req 45–46: idempotência via `OperationId` → `IOperationLogRepository`
- PRD F6 req 28: consulta de parcelas por grupo → `GetByInstallmentGroupAsync`
- PRD F8 req 36: consulta de transferências por grupo → `GetByTransferGroupAsync`

## Subtarefas

- [ ] 4.1 Criar interface genérica `IRepository<T>` com métodos `GetByIdAsync`, `AddAsync`, `Update`
- [ ] 4.2 Criar interface `IAccountRepository` estendendo `IRepository<Account>` com `GetByIdWithLockAsync` e `ExistsByNameAsync`
- [ ] 4.3 Criar interface `ITransactionRepository` estendendo `IRepository<Transaction>` com `GetByInstallmentGroupAsync`, `GetByTransferGroupAsync`, `GetByOperationIdAsync`
- [ ] 4.4 Criar interface `ICategoryRepository` estendendo `IRepository<Category>` com `ExistsByNameAndTypeAsync`
- [ ] 4.5 Criar interface `IRecurrenceTemplateRepository` estendendo `IRepository<RecurrenceTemplate>` com `GetActiveTemplatesAsync`
- [ ] 4.6 Criar interface `IOperationLogRepository` com `ExistsByOperationIdAsync`, `AddAsync`, `CleanupExpiredAsync`
- [ ] 4.7 Criar interface `IUnitOfWork` com `SaveChangesAsync`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`

## Sequenciamento

- Bloqueado por: 3.0 (interfaces referenciam entidades)
- Desbloqueia: 5.0 (Domain Services dependem das interfaces), 7.0 (DbContext usa entidades definidas aqui), 8.0 (Repositories implementam estas interfaces)
- Paralelizável: Não (bloqueada por 3.0; mas após esta, 5.0 e 7.0 podem ser paralelas)

## Detalhes de Implementação

### Localização dos arquivos

```
3-Domain/GestorFinanceiro.Financeiro.Domain/
└── Interface/
    ├── IRepository.cs
    ├── IAccountRepository.cs
    ├── ITransactionRepository.cs
    ├── ICategoryRepository.cs
    ├── IRecurrenceTemplateRepository.cs
    ├── IOperationLogRepository.cs
    └── IUnitOfWork.cs
```

### Interfaces (conforme techspec)

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken);
    void Update(T entity);
}

public interface IAccountRepository : IRepository<Account>
{
    Task<Account> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
}

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByInstallmentGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<IEnumerable<Transaction>> GetByTransferGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<Transaction?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> ExistsByNameAndTypeAsync(string name, CategoryType type, CancellationToken cancellationToken);
}

public interface IRecurrenceTemplateRepository : IRepository<RecurrenceTemplate>
{
    Task<IEnumerable<RecurrenceTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken);
}

public interface IOperationLogRepository
{
    Task<bool> ExistsByOperationIdAsync(string operationId, CancellationToken cancellationToken);
    Task AddAsync(OperationLog log, CancellationToken cancellationToken);
    Task CleanupExpiredAsync(CancellationToken cancellationToken);
}

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
```

### Observações

- Todos os métodos assíncronos recebem `CancellationToken` (conforme `rules/dotnet-observability.md`)
- `IOperationLogRepository` NÃO estende `IRepository<T>` porque `OperationLog` não herda de `BaseEntity`
- `IUnitOfWork` implementa `IDisposable` para liberar recursos
- `GetByIdWithLockAsync` será implementado na Infra via `SELECT FOR UPDATE` no PostgreSQL

## Critérios de Sucesso

- Todas as 7 interfaces criadas no namespace correto
- Nenhuma interface referencia tipos da camada de Infra ou Application
- Todos os métodos assíncronos incluem `CancellationToken`
- `dotnet build` compila sem erros
