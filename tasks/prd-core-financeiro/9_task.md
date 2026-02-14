---
status: pending
parallelizable: false
blocked_by: ["5.0", "8.0"]
---

<task_context>
<domain>engine/aplicação</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 9.0: Application Layer — CQRS

## Visão Geral

Implementar a camada de aplicação com CQRS nativo (sem MediatR). Inclui: interfaces CQRS (`ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`, `IDispatcher`), todos os Commands/Queries, Handlers, Validators (FluentValidation), DTOs de resposta (mapeados com Mapster) e o Dispatcher.

A Application Layer é a porta de entrada para os use cases — orquestra repositórios, UnitOfWork, domain services e validação. Cada handler segue o padrão: abrir transação → carregar entidades (com lock se necessário) → executar lógica de domínio → persistir → commitar → retornar DTO.

## Requisitos

- Techspec: CQRS nativo sem MediatR conforme `rules/dotnet-architecture.md`
- PRD F10 req 44: toda operação de escrita dentro de transação ACID
- PRD F10 req 45–46: idempotência via `OperationId` em operações de escrita
- `rules/dotnet-libraries-config.md`: FluentValidation para validação, Mapster para mapeamento DTOs

## Subtarefas

### Infraestrutura CQRS

- [ ] 9.1 Criar interfaces: `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, `IDispatcher`
- [ ] 9.2 Implementar `Dispatcher` que resolve handlers via DI (ServiceProvider)
- [ ] 9.3 Criar extensão de DI para registrar todos os handlers e o Dispatcher

### Commands e Handlers — Contas (F1)

- [ ] 9.4 `CreateAccountCommand` + `CreateAccountCommandHandler` + `CreateAccountValidator`
- [ ] 9.5 `DeactivateAccountCommand` + `DeactivateAccountCommandHandler`
- [ ] 9.6 `ActivateAccountCommand` + `ActivateAccountCommandHandler`

### Commands e Handlers — Categorias (F2)

- [ ] 9.7 `CreateCategoryCommand` + `CreateCategoryCommandHandler` + `CreateCategoryValidator`
- [ ] 9.8 `UpdateCategoryCommand` + `UpdateCategoryCommandHandler`

### Commands e Handlers — Transações (F3)

- [ ] 9.9 `CreateTransactionCommand` + `CreateTransactionCommandHandler` + `CreateTransactionValidator`

### Commands e Handlers — Ajuste (F4)

- [ ] 9.10 `AdjustTransactionCommand` + `AdjustTransactionCommandHandler`

### Commands e Handlers — Cancelamento (F5)

- [ ] 9.11 `CancelTransactionCommand` + `CancelTransactionCommandHandler`

### Commands e Handlers — Parcelamento (F6)

- [ ] 9.12 `CreateInstallmentCommand` + `CreateInstallmentCommandHandler` + `CreateInstallmentValidator`
- [ ] 9.13 `AdjustInstallmentGroupCommand` + `AdjustInstallmentGroupCommandHandler`
- [ ] 9.14 `CancelInstallmentCommand` + `CancelInstallmentCommandHandler` (individual)
- [ ] 9.15 `CancelInstallmentGroupCommand` + `CancelInstallmentGroupCommandHandler`

### Commands e Handlers — Recorrência (F7)

- [ ] 9.16 `CreateRecurrenceCommand` + `CreateRecurrenceCommandHandler` + `CreateRecurrenceValidator`
- [ ] 9.17 `DeactivateRecurrenceCommand` + `DeactivateRecurrenceCommandHandler`
- [ ] 9.18 `GenerateRecurrenceCommand` + `GenerateRecurrenceCommandHandler`

### Commands e Handlers — Transferência (F8)

- [ ] 9.19 `CreateTransferCommand` + `CreateTransferCommandHandler` + `CreateTransferValidator`
- [ ] 9.20 `CancelTransferCommand` + `CancelTransferCommandHandler`

### Queries (leitura)

- [ ] 9.21 `GetAccountByIdQuery` + `GetAccountByIdQueryHandler`
- [ ] 9.22 `ListAccountsQuery` + `ListAccountsQueryHandler`
- [ ] 9.23 `GetTransactionByIdQuery` + `GetTransactionByIdQueryHandler`
- [ ] 9.24 `ListTransactionsByAccountQuery` + `ListTransactionsByAccountQueryHandler`
- [ ] 9.25 `ListCategoriesQuery` + `ListCategoriesQueryHandler`

### DTOs

- [ ] 9.26 Criar DTOs de resposta: `AccountResponse`, `TransactionResponse`, `CategoryResponse`, `RecurrenceTemplateResponse`
- [ ] 9.27 Configurar mapeamentos Mapster (Entity → DTO)

### Testes Unitários dos Handlers

- [ ] 9.28 Testes para `CreateTransactionCommandHandler` (sucesso, idempotência, conta inativa, saldo negativo)
- [ ] 9.29 Testes para `CancelTransactionCommandHandler` (sucesso com Paid, sucesso com Pending, já cancelada)
- [ ] 9.30 Testes para `CreateTransferCommandHandler` (sucesso, saldo insuficiente na origem)
- [ ] 9.31 Testes para `CreateInstallmentCommandHandler` (sucesso, arredondamento)
- [ ] 9.32 Testes para handlers de Account e Category (sucesso, validação)

## Sequenciamento

- Bloqueado por: 5.0 (Domain Services), 8.0 (Repositories + UnitOfWork)
- Desbloqueia: 10.0 (Testes de integração)
- Paralelizável: Não (depende de duas tarefas paralelas anteriores)

## Detalhes de Implementação

### Localização dos arquivos

```
2-Application/GestorFinanceiro.Financeiro.Application/
├── Common/
│   ├── ICommand.cs
│   ├── IQuery.cs
│   ├── ICommandHandler.cs
│   ├── IQueryHandler.cs
│   ├── IDispatcher.cs
│   └── Dispatcher.cs
├── Commands/
│   ├── Account/
│   │   ├── CreateAccountCommand.cs
│   │   ├── CreateAccountCommandHandler.cs
│   │   ├── CreateAccountValidator.cs
│   │   ├── DeactivateAccountCommand.cs
│   │   ├── DeactivateAccountCommandHandler.cs
│   │   ├── ActivateAccountCommand.cs
│   │   └── ActivateAccountCommandHandler.cs
│   ├── Category/
│   │   ├── CreateCategoryCommand.cs
│   │   ├── CreateCategoryCommandHandler.cs
│   │   ├── CreateCategoryValidator.cs
│   │   ├── UpdateCategoryCommand.cs
│   │   └── UpdateCategoryCommandHandler.cs
│   ├── Transaction/
│   │   ├── CreateTransactionCommand.cs
│   │   ├── CreateTransactionCommandHandler.cs
│   │   ├── CreateTransactionValidator.cs
│   │   ├── AdjustTransactionCommand.cs
│   │   ├── AdjustTransactionCommandHandler.cs
│   │   ├── CancelTransactionCommand.cs
│   │   └── CancelTransactionCommandHandler.cs
│   ├── Installment/
│   │   ├── CreateInstallmentCommand.cs
│   │   ├── CreateInstallmentCommandHandler.cs
│   │   ├── CreateInstallmentValidator.cs
│   │   ├── AdjustInstallmentGroupCommand.cs
│   │   ├── AdjustInstallmentGroupCommandHandler.cs
│   │   ├── CancelInstallmentCommand.cs
│   │   ├── CancelInstallmentCommandHandler.cs
│   │   ├── CancelInstallmentGroupCommand.cs
│   │   └── CancelInstallmentGroupCommandHandler.cs
│   ├── Recurrence/
│   │   ├── CreateRecurrenceCommand.cs
│   │   ├── CreateRecurrenceCommandHandler.cs
│   │   ├── CreateRecurrenceValidator.cs
│   │   ├── DeactivateRecurrenceCommand.cs
│   │   ├── DeactivateRecurrenceCommandHandler.cs
│   │   ├── GenerateRecurrenceCommand.cs
│   │   └── GenerateRecurrenceCommandHandler.cs
│   └── Transfer/
│       ├── CreateTransferCommand.cs
│       ├── CreateTransferCommandHandler.cs
│       ├── CreateTransferValidator.cs
│       ├── CancelTransferCommand.cs
│       └── CancelTransferCommandHandler.cs
├── Queries/
│   ├── Account/
│   │   ├── GetAccountByIdQuery.cs
│   │   ├── GetAccountByIdQueryHandler.cs
│   │   ├── ListAccountsQuery.cs
│   │   └── ListAccountsQueryHandler.cs
│   ├── Transaction/
│   │   ├── GetTransactionByIdQuery.cs
│   │   ├── GetTransactionByIdQueryHandler.cs
│   │   ├── ListTransactionsByAccountQuery.cs
│   │   └── ListTransactionsByAccountQueryHandler.cs
│   └── Category/
│       ├── ListCategoriesQuery.cs
│       └── ListCategoriesQueryHandler.cs
├── Dtos/
│   ├── AccountResponse.cs
│   ├── TransactionResponse.cs
│   ├── CategoryResponse.cs
│   └── RecurrenceTemplateResponse.cs
└── Mapping/
    └── MappingConfig.cs
```

### Padrão do Command Handler

```csharp
public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public async Task<TransactionResponse> HandleAsync(
        CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        // 1. Verificar idempotência (OperationId)
        // 2. Validar command (FluentValidation)
        // 3. Iniciar transação ACID
        // 4. Carregar conta com lock (GetByIdWithLockAsync)
        // 5. Executar lógica de domínio (TransactionDomainService)
        // 6. Persistir (AddAsync + SaveChanges)
        // 7. Commitar transação
        // 8. Registrar OperationLog
        // 9. Mapear e retornar DTO
    }
}
```

### Idempotência (todos os CommandHandlers de escrita)

```csharp
// No início do handler:
if (!string.IsNullOrEmpty(command.OperationId))
{
    var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(
        command.OperationId, cancellationToken);
    if (existingLog)
        throw new DuplicateOperationException(command.OperationId);
}

// No final do handler (após commit):
if (!string.IsNullOrEmpty(command.OperationId))
{
    await _operationLogRepository.AddAsync(new OperationLog
    {
        OperationId = command.OperationId,
        OperationType = "CreateTransaction",
        ResultEntityId = transaction.Id,
        ResultPayload = JsonSerializer.Serialize(response)
    }, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

### Queries — AsNoTracking

```csharp
// Queries de leitura usam AsNoTracking para performance
public async Task<AccountResponse> HandleAsync(GetAccountByIdQuery query, CancellationToken ct)
{
    var account = await _accountRepository.GetByIdAsync(query.AccountId, ct);
    return account.Adapt<AccountResponse>(); // Mapster
}
```

### Observações

- Logging estruturado com `ILogger<T>` em todos os handlers
- FluentValidation executado no início do handler (antes de abrir transação)
- Todos os handlers recebem `CancellationToken`
- Mapster para mapeamento Entity → DTO (configuração centralizada em `MappingConfig`)
- Command handlers que alteram saldo devem usar `GetByIdWithLockAsync`

## Critérios de Sucesso

- Dispatcher funcional resolva handlers via DI
- Todos os Commands/Queries/Handlers/Validators criados
- FluentValidation em todos os commands que criam entidades
- Idempotência via `OperationId` em todos os command handlers de escrita
- Todas as operações de escrita dentro de transação ACID (BeginTransaction/Commit/Rollback)
- Handlers de leitura usam `AsNoTracking`
- DTOs mapeados com Mapster
- Testes unitários dos handlers passam (mock de repositories + UnitOfWork)
- `dotnet build` compila sem erros
- `dotnet test` passa sem falhas
