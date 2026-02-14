```markdown
---
status: pending
parallelizable: false
blocked_by: ["4.0", "5.0", "6.0"]
---

<task_context>
<domain>engine/aplicação+infra</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>"9.0"</unblocks>
</task_context>

# Tarefa 7.0: Histórico e Auditoria

## Visão Geral

Implementar o sistema de auditoria e histórico de transações: (1) nova entidade `AuditLog` com tabela dedicada; (2) inserção automática de registros de auditoria nos command handlers existentes; (3) endpoint de histórico de transação (`/transactions/{id}/history`); (4) endpoint de auditoria (`/audit`) com filtros, restrito a admins.

O histórico de transações é baseado na consulta ao campo `OriginalTransactionId` — a transação original + ajustes que referenciam o mesmo `OriginalTransactionId`, em ordem cronológica.

## Requisitos

- Techspec F5: Endpoints de histórico e auditoria
- PRD F5 req 31-33: Histórico de transação e log de auditoria
- Techspec: Tabela `audit_logs` com campos EntityType, EntityId, Action, UserId, Timestamp, PreviousData (JSONB)
- Techspec: Ações registradas: Created, Updated, Deactivated, Cancelled
- `rules/restful.md`: Filtros via query string, paginação

## Subtarefas

### Entidade AuditLog

- [ ] 7.1 Criar entidade `AuditLog` em `3-Domain/Entity/AuditLog.cs`:
  - Propriedades: `Id`, `EntityType` (string), `EntityId` (Guid), `Action` (string — Created/Updated/Deactivated/Cancelled), `UserId` (string), `Timestamp` (DateTime), `PreviousData` (string? — JSON serializado)
  - Factory method `Create(entityType, entityId, action, userId, previousData?)`
  - **Nota**: `AuditLog` NÃO herda de `BaseEntity` — é uma entidade de log simples sem campos de auditoria

### Configuração EF Core

- [ ] 7.2 Criar `AuditLogConfiguration` em `4-Infra/Config/AuditLogConfiguration.cs`:
  - Tabela `audit_logs`
  - `EntityType` VARCHAR(100) NOT NULL
  - `EntityId` UUID NOT NULL
  - `Action` VARCHAR(50) NOT NULL
  - `UserId` VARCHAR(100) NOT NULL
  - `Timestamp` TIMESTAMP NOT NULL
  - `PreviousData` JSONB nullable
  - Índices: `(entity_type, entity_id)`, `(user_id)`, `(timestamp)`
- [ ] 7.3 Adicionar `DbSet<AuditLog>` ao `FinanceiroDbContext`
- [ ] 7.4 Criar migration EF Core para a tabela `audit_logs`

### Repositório de Auditoria

- [ ] 7.5 Criar `IAuditLogRepository` em `3-Domain/Interface/IAuditLogRepository.cs`:
  - `Task AddAsync(AuditLog auditLog, CancellationToken ct)`
  - `Task<IEnumerable<AuditLog>> GetByFiltersAsync(string? entityType, Guid? entityId, string? userId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)`
- [ ] 7.6 Criar `AuditLogRepository` em `4-Infra/Repository/AuditLogRepository.cs`
- [ ] 7.7 Registrar `IAuditLogRepository` na DI

### Inserção de Auditoria nos Handlers Existentes

- [ ] 7.8 Criar serviço de auditoria `IAuditService` em `2-Application/Common/IAuditService.cs`:
  - `Task LogAsync(string entityType, Guid entityId, string action, string userId, object? previousData, CancellationToken ct)`
  - Simplifica a criação de `AuditLog` e serialização de `previousData` como JSON
- [ ] 7.9 Implementar `AuditService` em `4-Infra/` ou `2-Application/`:
  - Serializar `previousData` com `System.Text.Json`
  - Usar `IAuditLogRepository.AddAsync`
- [ ] 7.10 Inserir chamadas de auditoria nos command handlers de **Account**:
  - `CreateAccountCommandHandler` → log `Created`
  - `DeactivateAccountCommandHandler` → log `Deactivated` com dados anteriores
  - `ActivateAccountCommandHandler` → log `Updated` (reativação)
  - `UpdateAccountCommandHandler` → log `Updated` com dados anteriores
- [ ] 7.11 Inserir chamadas de auditoria nos command handlers de **Category**:
  - `CreateCategoryCommandHandler` → log `Created`
  - `UpdateCategoryCommandHandler` → log `Updated` com dados anteriores
- [ ] 7.12 Inserir chamadas de auditoria nos command handlers de **Transaction**:
  - `CreateTransactionCommandHandler` → log `Created`
  - `AdjustTransactionCommandHandler` → log `Updated` com dados anteriores
  - `CancelTransactionCommandHandler` → log `Cancelled` com dados anteriores
  - `CreateInstallmentCommandHandler` → log `Created` para cada parcela
  - `CancelInstallmentGroupCommandHandler` → log `Cancelled` para cada parcela
  - `CreateTransferCommandHandler` → log `Created` para ambas transações
  - `CreateRecurrenceCommandHandler` → log `Created`
- [ ] 7.13 Inserir chamadas de auditoria nos command handlers de **User**:
  - `CreateUserCommandHandler` → log `Created`
  - `UpdateUserStatusCommandHandler` → log `Updated`/`Deactivated`
  - `ChangePasswordCommandHandler` → log `Updated` (sem dados sensíveis)

### Query de Histórico de Transação

- [ ] 7.14 Criar `GetTransactionHistoryQuery` + `GetTransactionHistoryQueryHandler`:
  - Input: TransactionId
  - Buscar transação pelo ID
  - Se tiver `OriginalTransactionId`, buscar a original
  - Buscar todas as transações com mesmo `OriginalTransactionId` (ajustes)
  - Ordenar cronologicamente
  - Retornar lista de `TransactionResponse` com indicação de tipo (original, ajuste, cancelamento)
- [ ] 7.15 Criar DTO `TransactionHistoryResponse`:
  - Lista de `TransactionHistoryEntry` (TransactionResponse + ActionType: Original/Adjustment/Cancellation)

### Query de Auditoria

- [ ] 7.16 Criar `ListAuditLogsQuery` + `ListAuditLogsQueryHandler`:
  - Filtros: entityType, entityId, userId, dateFrom, dateTo
  - Paginação: `_page`, `_size`
  - Retornar `PagedResult<AuditLogDto>`
  - `AsNoTracking()` para performance
- [ ] 7.17 Criar `AuditLogDto`:
  - Id, EntityType, EntityId, Action, UserId, Timestamp, PreviousData

### Controllers

- [ ] 7.18 Adicionar endpoint ao `TransactionsController`:
  - `GET /api/v1/transactions/{id}/history`
  - Retornar 200 com `TransactionHistoryResponse`
  - Atributo `[Authorize]`
- [ ] 7.19 Criar `AuditController` em `1-Services/Controllers/AuditController.cs`:
  - Route: `api/v1/audit`
  - Atributo `[Authorize(Policy = "AdminOnly")]`
  - `GET /api/v1/audit` com filtros via query string (entityType, entityId, userId, dateFrom, dateTo, _page, _size)
  - Retornar 200 com `PagedResult<AuditLogDto>`

### Testes Unitários

- [ ] 7.20 Testes para `AuditLog`:
  - `Create` gera entidade com campos corretos
  - `Timestamp` é preenchido automaticamente
- [ ] 7.21 Testes para `AuditService`:
  - Serializa `previousData` corretamente
  - Chama repositório com dados corretos
- [ ] 7.22 Testes para `GetTransactionHistoryQueryHandler`:
  - Transação sem ajustes retorna apenas a original
  - Transação com 2 ajustes retorna 3 entradas ordenadas
  - Transação cancelada inclui indicação de cancelamento
- [ ] 7.23 Testes para `ListAuditLogsQueryHandler`:
  - Filtros aplicados corretamente
  - Paginação funcional

### Validação

- [ ] 7.24 Validar build com `dotnet build` a partir de `backend/`
- [ ] 7.25 Verificar que testes existentes continuam passando

## Sequenciamento

- Bloqueado por: 4.0, 5.0, 6.0 (precisa dos handlers adaptados com userId para inserir audit)
- Desbloqueia: 9.0 (Testes de Integração)
- Paralelizável: Não (precisa modificar handlers de todas as tarefas anteriores)

## Detalhes de Implementação

### Entidade AuditLog

```csharp
public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? PreviousData { get; private set; }

    protected AuditLog() { }

    public static AuditLog Create(string entityType, Guid entityId,
        string action, string userId, string? previousData = null)
    {
        return new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            PreviousData = previousData
        };
    }
}
```

### Esquema SQL

```sql
CREATE TABLE audit_logs (
    id              UUID PRIMARY KEY,
    entity_type     VARCHAR(100) NOT NULL,
    entity_id       UUID NOT NULL,
    action          VARCHAR(50) NOT NULL,
    user_id         VARCHAR(100) NOT NULL,
    timestamp       TIMESTAMP NOT NULL,
    previous_data   JSONB
);
CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_type, entity_id);
CREATE INDEX ix_audit_logs_user_id ON audit_logs (user_id);
CREATE INDEX ix_audit_logs_timestamp ON audit_logs (timestamp);
```

### Histórico de Transação (fluxo)

```
1. Buscar transação pelo ID
2. Se OriginalTransactionId != null → usar OriginalTransactionId como referência
3. Senão → usar o próprio Id como referência
4. Buscar todas transações com OriginalTransactionId == referência
5. Incluir a transação original (referência)
6. Ordenar por CreatedAt ASC
7. Mapear para TransactionHistoryEntry com ActionType:
   - Original: não tem OriginalTransactionId
   - Adjustment: Type == Adjustment (ou tem OriginalTransactionId)
   - Cancellation: Status == Cancelled
```

## Critérios de Sucesso

- Tabela `audit_logs` criada via migration
- Todas as operações de escrita registram auditoria automaticamente
- Endpoint de histórico retorna cadeia completa (original → ajustes → cancelamentos)
- Endpoint de auditoria com filtros e paginação funcional
- Auditoria não inclui dados sensíveis (senhas, tokens)
- Apenas admin acessa o endpoint de auditoria
- Testes unitários para entidade, serviço e handlers passam
- Build compila sem erros
```
