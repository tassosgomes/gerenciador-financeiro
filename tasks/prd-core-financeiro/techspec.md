# Especificação Técnica — Core Financeiro (Fase 1)

## Resumo Executivo

O Core Financeiro é a camada de domínio e persistência do GestorFinanceiro, implementada em .NET 8 / C# com Clean Architecture. Esta fase entrega o engine contábil completo — contas, categorias, transações, ajustes, cancelamentos, parcelamentos, recorrência e transferências — sem API HTTP nem UI.

A estratégia arquitetural centraliza regras de negócio no Domain layer (entidades ricas, value objects, domain services), usa CQRS nativo na Application layer para orquestrar operações, e delega persistência ao Infra layer via Entity Framework Core + PostgreSQL. O saldo é materializado (campo persistido na conta), atualizado incrementalmente a cada operação `Paid`. Concorrência é tratada com pessimistic locking (`SELECT FOR UPDATE`), e todas as escritas são idempotentes via `OperationId` com TTL de 24h.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌─────────────────────────────────────────────────────────────┐
│  1-Services (API) — Placeholder vazio nesta fase            │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  2-Application                                              │
│  ├─ Commands/   (CriarTransacao, CriarConta, Ajustar...)    │
│  ├─ Queries/    (ObterConta, ListarTransacoes...)            │
│  ├─ Handlers/   (ICommandHandler, IQueryHandler)            │
│  ├─ Dtos/       (Responses, Requests)                       │
│  └─ Validators/ (FluentValidation)                          │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  3-Domain                                                   │
│  ├─ Entity/     (Account, Transaction, Category...)         │
│  ├─ Enum/       (AccountType, TransactionType, Status...)   │
│  ├─ ValueObject/(Money)                                     │
│  ├─ Service/    (TransactionDomainService,                  │
│  │               TransferDomainService...)                   │
│  ├─ Interface/  (IAccountRepository, IUnitOfWork...)        │
│  └─ Exception/  (DomainException, InsufficientBalanceEx...) │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │
┌─────────────────────────────────────────────────────────────┐
│  4-Infra                                                    │
│  ├─ Context/    (FinanceiroDbContext)                        │
│  ├─ Repository/ (AccountRepository, TransactionRepo...)     │
│  ├─ Config/     (EF Fluent API configurations)              │
│  ├─ Migration/  (EF Core migrations)                        │
│  └─ UnitOfWork/ (UnitOfWork implementation)                 │
└─────────────────────────────────────────────────────────────┘
```

**Fluxo de dados**: Application recebe commands/queries → valida via FluentValidation → invoca domain services/entities → persiste via repositories + UnitOfWork → retorna DTOs.

**Regra de dependência**: Domain não referencia nenhum outro projeto. Application depende de Domain. Infra depende de Domain. Services depende de Application. Tests depende de todos.

---

## Design de Implementação

### Interfaces Principais

```csharp
// === Domain Layer — Interfaces ===

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

```csharp
// === Application Layer — CQRS Interfaces ===

public interface ICommand<TResponse> { }
public interface IQuery<TResponse> { }

public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public interface IDispatcher
{
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken);
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken);
}
```

### Modelos de Dados

#### Entidades de Domínio

```csharp
// === Base Entity (Auditoria — F9) ===
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string CreatedBy { get; protected set; } = string.Empty;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    public void SetAuditOnCreate(string userId)
    {
        CreatedBy = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetAuditOnUpdate(string userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

```csharp
// === Account (F1) ===
public class Account : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public decimal Balance { get; private set; }
    public bool AllowNegativeBalance { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Factory method
    public static Account Create(string name, AccountType type, decimal initialBalance,
        bool allowNegativeBalance, string userId)
    {
        var account = new Account
        {
            Name = name,
            Type = type,
            Balance = initialBalance,
            AllowNegativeBalance = allowNegativeBalance
        };
        account.SetAuditOnCreate(userId);
        return account;
    }

    public void Activate(string userId) { IsActive = true; SetAuditOnUpdate(userId); }
    public void Deactivate(string userId) { IsActive = false; SetAuditOnUpdate(userId); }

    public void ApplyDebit(decimal amount, string userId)
    {
        if (!AllowNegativeBalance && Balance - amount < 0)
            throw new InsufficientBalanceException(Id, Balance, amount);
        Balance -= amount;
        SetAuditOnUpdate(userId);
    }

    public void ApplyCredit(decimal amount, string userId)
    {
        Balance += amount;
        SetAuditOnUpdate(userId);
    }

    public void RevertDebit(decimal amount, string userId)
    {
        Balance += amount;
        SetAuditOnUpdate(userId);
    }

    public void RevertCredit(decimal amount, string userId)
    {
        Balance -= amount;
        SetAuditOnUpdate(userId);
    }

    public void ValidateCanReceiveTransaction()
    {
        if (!IsActive)
            throw new InactiveAccountException(Id);
    }
}
```

```csharp
// === Category (F2) ===
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public CategoryType Type { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Category Create(string name, CategoryType type, string userId)
    {
        var category = new Category { Name = name, Type = type };
        category.SetAuditOnCreate(userId);
        return category;
    }

    public void UpdateName(string newName, string userId)
    {
        Name = newName;
        SetAuditOnUpdate(userId);
    }
}
```

```csharp
// === Transaction (F3, F4, F5, F6) ===
public class Transaction : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TransactionType Type { get; private set; }        // Debit, Credit
    public decimal Amount { get; private set; }               // > 0 sempre
    public string Description { get; private set; } = string.Empty;
    public DateTime CompetenceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public TransactionStatus Status { get; private set; }     // Paid, Pending, Cancelled

    // Flags de rastreabilidade (F9 req 41)
    public bool IsAdjustment { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }  // Ref ao original se ajuste
    public bool HasAdjustment { get; private set; }           // Indica que esta tx foi ajustada

    // Parcelamento (F6)
    public Guid? InstallmentGroupId { get; private set; }
    public int? InstallmentNumber { get; private set; }
    public int? TotalInstallments { get; private set; }

    // Recorrência (F7)
    public bool IsRecurrent { get; private set; }
    public Guid? RecurrenceTemplateId { get; private set; }

    // Transferência (F8)
    public Guid? TransferGroupId { get; private set; }

    // Cancelamento (F5)
    public string? CancellationReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Idempotência (F10)
    public string? OperationId { get; private set; }

    // Overdue calculado on-the-fly (PRD req 14)
    public bool IsOverdue => Status == TransactionStatus.Pending
                             && DueDate.HasValue
                             && DueDate.Value.Date < DateTime.UtcNow.Date;

    // Navigation
    public Account Account { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public Transaction? OriginalTransaction { get; private set; }

    // Factory — transação simples
    public static Transaction Create(Guid accountId, Guid categoryId,
        TransactionType type, decimal amount, string description,
        DateTime competenceDate, DateTime? dueDate,
        TransactionStatus status, string userId, string? operationId = null)
    {
        if (amount <= 0) throw new InvalidTransactionAmountException(amount);

        var tx = new Transaction
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Description = description,
            CompetenceDate = competenceDate,
            DueDate = dueDate,
            Status = status,
            OperationId = operationId
        };
        tx.SetAuditOnCreate(userId);
        return tx;
    }

    // Cancelamento (F5)
    public void Cancel(string userId, string? reason = null)
    {
        if (Status == TransactionStatus.Cancelled)
            throw new TransactionAlreadyCancelledException(Id);

        Status = TransactionStatus.Cancelled;
        CancellationReason = reason;
        CancelledBy = userId;
        CancelledAt = DateTime.UtcNow;
        SetAuditOnUpdate(userId);
    }

    // Marcar como ajustada (F4)
    public void MarkAsAdjusted(string userId)
    {
        HasAdjustment = true;
        SetAuditOnUpdate(userId);
    }

    // Factory — ajuste (F4)
    public static Transaction CreateAdjustment(Guid accountId, Guid categoryId,
        TransactionType type, decimal differenceAmount,
        Guid originalTransactionId, string description,
        DateTime competenceDate, string userId, string? operationId = null)
    {
        var tx = Create(accountId, categoryId, type, differenceAmount,
            description, competenceDate, null, TransactionStatus.Paid, userId, operationId);
        tx.IsAdjustment = true;
        tx.OriginalTransactionId = originalTransactionId;
        return tx;
    }

    // Helpers para parcelamento
    public void SetInstallmentInfo(Guid groupId, int number, int total)
    {
        InstallmentGroupId = groupId;
        InstallmentNumber = number;
        TotalInstallments = total;
    }

    // Helpers para recorrência
    public void SetRecurrenceInfo(Guid templateId)
    {
        IsRecurrent = true;
        RecurrenceTemplateId = templateId;
    }

    // Helpers para transferência
    public void SetTransferGroup(Guid transferGroupId)
    {
        TransferGroupId = transferGroupId;
    }
}
```

```csharp
// === RecurrenceTemplate (F7) ===
public class RecurrenceTemplate : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int DayOfMonth { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastGeneratedDate { get; private set; }    // Mês/ano da última geração
    public TransactionStatus DefaultStatus { get; private set; } // Status padrão das tx geradas

    public static RecurrenceTemplate Create(Guid accountId, Guid categoryId,
        TransactionType type, decimal amount, string description,
        int dayOfMonth, TransactionStatus defaultStatus, string userId)
    {
        var template = new RecurrenceTemplate
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Description = description,
            DayOfMonth = dayOfMonth,
            DefaultStatus = defaultStatus
        };
        template.SetAuditOnCreate(userId);
        return template;
    }

    public void Deactivate(string userId)
    {
        IsActive = false;
        SetAuditOnUpdate(userId);
    }

    public void MarkGenerated(DateTime generatedDate, string userId)
    {
        LastGeneratedDate = generatedDate;
        SetAuditOnUpdate(userId);
    }

    public bool ShouldGenerateForMonth(DateTime referenceDate)
    {
        if (!IsActive) return false;
        if (LastGeneratedDate is null) return true;
        return referenceDate.Year > LastGeneratedDate.Value.Year
            || (referenceDate.Year == LastGeneratedDate.Value.Year
                && referenceDate.Month > LastGeneratedDate.Value.Month);
    }
}
```

```csharp
// === OperationLog (F10 — Idempotência) ===
public class OperationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty; // "CreateTransaction", "Transfer"...
    public Guid ResultEntityId { get; set; }                   // Id da entidade criada
    public string ResultPayload { get; set; } = string.Empty;  // JSON serializado do resultado
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
```

#### Enums

```csharp
public enum AccountType   { Corrente = 1, Cartao = 2, Investimento = 3, Carteira = 4 }
public enum CategoryType  { Receita = 1, Despesa = 2 }
public enum TransactionType   { Debit = 1, Credit = 2 }
public enum TransactionStatus { Paid = 1, Pending = 2, Cancelled = 3 }
```

#### Exceções de Domínio

```csharp
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class InsufficientBalanceException : DomainException { ... }
public class InactiveAccountException : DomainException { ... }
public class InvalidTransactionAmountException : DomainException { ... }
public class TransactionAlreadyCancelledException : DomainException { ... }
public class TransactionNotPendingException : DomainException { ... }
public class DuplicateOperationException : DomainException { ... }
public class InstallmentPaidCannotBeCancelledException : DomainException { ... }
```

### Esquema de Banco de Dados

```sql
-- Tabela: accounts
CREATE TABLE accounts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(150) NOT NULL,
    type            SMALLINT NOT NULL,          -- AccountType enum
    balance         DECIMAL(18,2) NOT NULL DEFAULT 0,
    allow_negative_balance BOOLEAN NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_by      VARCHAR(100) NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by      VARCHAR(100),
    updated_at      TIMESTAMPTZ
);

-- Tabela: categories
CREATE TABLE categories (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(150) NOT NULL,
    type            SMALLINT NOT NULL,          -- CategoryType enum
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_by      VARCHAR(100) NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by      VARCHAR(100),
    updated_at      TIMESTAMPTZ
);

-- Tabela: transactions
CREATE TABLE transactions (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id              UUID NOT NULL REFERENCES accounts(id),
    category_id             UUID NOT NULL REFERENCES categories(id),
    type                    SMALLINT NOT NULL,
    amount                  DECIMAL(18,2) NOT NULL CHECK (amount > 0),
    description             VARCHAR(500) NOT NULL,
    competence_date         DATE NOT NULL,
    due_date                DATE,
    status                  SMALLINT NOT NULL,
    is_adjustment           BOOLEAN NOT NULL DEFAULT FALSE,
    original_transaction_id UUID REFERENCES transactions(id),
    has_adjustment          BOOLEAN NOT NULL DEFAULT FALSE,
    installment_group_id    UUID,
    installment_number      SMALLINT,
    total_installments      SMALLINT,
    is_recurrent            BOOLEAN NOT NULL DEFAULT FALSE,
    recurrence_template_id  UUID REFERENCES recurrence_templates(id),
    transfer_group_id       UUID,
    cancellation_reason     VARCHAR(500),
    cancelled_by            VARCHAR(100),
    cancelled_at            TIMESTAMPTZ,
    operation_id            VARCHAR(100),
    created_by              VARCHAR(100) NOT NULL,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by              VARCHAR(100),
    updated_at              TIMESTAMPTZ
);

CREATE INDEX ix_transactions_account_id ON transactions(account_id);
CREATE INDEX ix_transactions_category_id ON transactions(category_id);
CREATE INDEX ix_transactions_installment_group ON transactions(installment_group_id) WHERE installment_group_id IS NOT NULL;
CREATE INDEX ix_transactions_transfer_group ON transactions(transfer_group_id) WHERE transfer_group_id IS NOT NULL;
CREATE INDEX ix_transactions_operation_id ON transactions(operation_id) WHERE operation_id IS NOT NULL;
CREATE INDEX ix_transactions_status_due_date ON transactions(status, due_date) WHERE status = 2; -- Pending

-- Tabela: recurrence_templates
CREATE TABLE recurrence_templates (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id          UUID NOT NULL REFERENCES accounts(id),
    category_id         UUID NOT NULL REFERENCES categories(id),
    type                SMALLINT NOT NULL,
    amount              DECIMAL(18,2) NOT NULL,
    description         VARCHAR(500) NOT NULL,
    day_of_month        SMALLINT NOT NULL CHECK (day_of_month BETWEEN 1 AND 31),
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    last_generated_date DATE,
    default_status      SMALLINT NOT NULL DEFAULT 2, -- Pending
    created_by          VARCHAR(100) NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by          VARCHAR(100),
    updated_at          TIMESTAMPTZ
);

-- Tabela: operation_logs (idempotência)
CREATE TABLE operation_logs (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_id    VARCHAR(100) NOT NULL,
    operation_type  VARCHAR(50) NOT NULL,
    result_entity_id UUID NOT NULL,
    result_payload  JSONB NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at      TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX ix_operation_logs_operation_id ON operation_logs(operation_id);
CREATE INDEX ix_operation_logs_expires_at ON operation_logs(expires_at);
```

### Domain Services

```csharp
// === TransactionDomainService (F3, F4, F5) ===
public class TransactionDomainService
{
    // Cria transação e aplica saldo se Paid
    public Transaction CreateTransaction(Account account, Guid categoryId,
        TransactionType type, decimal amount, string description,
        DateTime competenceDate, DateTime? dueDate,
        TransactionStatus status, string userId, string? operationId = null)
    {
        account.ValidateCanReceiveTransaction();

        var transaction = Transaction.Create(accountId: account.Id, categoryId,
            type, amount, description, competenceDate, dueDate, status, userId, operationId);

        if (status == TransactionStatus.Paid)
            ApplyBalanceImpact(account, type, amount, userId);

        return transaction;
    }

    // Cria ajuste por diferença (F4)
    public Transaction CreateAdjustment(Account account, Transaction original,
        decimal correctAmount, string userId, string? operationId = null)
    {
        var difference = correctAmount - original.Amount;
        if (difference == 0) throw new DomainException("Valor correto é igual ao original.");

        TransactionType adjustmentType;
        decimal absDifference;

        if (original.Type == TransactionType.Debit)
        {
            // Original Debit 100, correto 130 → Debit 30 (mais debito)
            // Original Debit 100, correto  80 → Credit 20 (reversão parcial)
            adjustmentType = difference > 0 ? TransactionType.Debit : TransactionType.Credit;
            absDifference = Math.Abs(difference);
        }
        else // Credit
        {
            adjustmentType = difference > 0 ? TransactionType.Credit : TransactionType.Debit;
            absDifference = Math.Abs(difference);
        }

        var adjustment = Transaction.CreateAdjustment(account.Id, original.CategoryId,
            adjustmentType, absDifference, original.Id,
            $"Ajuste ref. transação {original.Id}", original.CompetenceDate,
            userId, operationId);

        // Aplica impacto incremental no saldo (req 23)
        ApplyBalanceImpact(account, adjustmentType, absDifference, userId);

        // Marca original como ajustada
        original.MarkAsAdjusted(userId);

        return adjustment;
    }

    // Cancelamento lógico (F5)
    public void CancelTransaction(Account account, Transaction transaction,
        string userId, string? reason = null)
    {
        var previousStatus = transaction.Status;
        transaction.Cancel(userId, reason);

        // Reverte saldo se era Paid (req 25)
        if (previousStatus == TransactionStatus.Paid)
            RevertBalanceImpact(account, transaction.Type, transaction.Amount, userId);
    }

    private void ApplyBalanceImpact(Account account, TransactionType type,
        decimal amount, string userId)
    {
        if (type == TransactionType.Debit)
            account.ApplyDebit(amount, userId);
        else
            account.ApplyCredit(amount, userId);
    }

    private void RevertBalanceImpact(Account account, TransactionType type,
        decimal amount, string userId)
    {
        if (type == TransactionType.Debit)
            account.RevertDebit(amount, userId);
        else
            account.RevertCredit(amount, userId);
    }
}
```

```csharp
// === InstallmentDomainService (F6) ===
public class InstallmentDomainService
{
    private readonly TransactionDomainService _transactionService;

    // Cria grupo de N parcelas
    public IReadOnlyList<Transaction> CreateInstallmentGroup(Account account,
        Guid categoryId, TransactionType type, decimal totalAmount,
        int installmentCount, string description, DateTime firstCompetenceDate,
        DateTime firstDueDate, string userId, string? operationId = null)
    {
        account.ValidateCanReceiveTransaction();

        var groupId = Guid.NewGuid();
        var installmentAmount = Math.Round(totalAmount / installmentCount, 2);
        var remainder = totalAmount - (installmentAmount * installmentCount);

        var transactions = new List<Transaction>();

        for (int i = 0; i < installmentCount; i++)
        {
            var amount = installmentAmount;
            if (i == installmentCount - 1) amount += remainder; // Resíduo na última

            var competenceDate = firstCompetenceDate.AddMonths(i);
            var dueDate = firstDueDate.AddMonths(i);

            var tx = _transactionService.CreateTransaction(account, categoryId,
                type, amount, $"{description} ({i + 1}/{installmentCount})",
                competenceDate, dueDate, TransactionStatus.Pending, userId,
                i == 0 ? operationId : null);

            tx.SetInstallmentInfo(groupId, i + 1, installmentCount);
            transactions.Add(tx);
        }

        return transactions.AsReadOnly();
    }

    // Ajuste em grupo de parcelas (Decisão 5 do PRD)
    public IReadOnlyList<Transaction> AdjustInstallmentGroup(
        Account account, IEnumerable<Transaction> groupTransactions,
        decimal newTotalAmount, string userId, string? operationId = null)
    {
        var pending = groupTransactions
            .Where(t => t.Status == TransactionStatus.Pending)
            .OrderBy(t => t.InstallmentNumber)
            .ToList();

        if (!pending.Any())
            throw new DomainException("Nenhuma parcela pendente para ajustar.");

        var paidTotal = groupTransactions
            .Where(t => t.Status == TransactionStatus.Paid)
            .Sum(t => t.Amount);

        var remainingAmount = newTotalAmount - paidTotal;
        var perInstallment = Math.Round(remainingAmount / pending.Count, 2);
        var remainder = remainingAmount - (perInstallment * pending.Count);

        var adjustments = new List<Transaction>();

        for (int i = 0; i < pending.Count; i++)
        {
            var target = pending[i];
            var correctAmount = perInstallment;
            if (i == pending.Count - 1) correctAmount += remainder;

            if (correctAmount != target.Amount)
            {
                var adj = _transactionService.CreateAdjustment(
                    account, target, correctAmount, userId,
                    i == 0 ? operationId : null);
                adjustments.Add(adj);
            }
        }

        return adjustments.AsReadOnly();
    }

    // Cancelamento de parcela individual (req 31)
    public void CancelSingleInstallment(Account account, Transaction installment,
        string userId, string? reason = null)
    {
        if (installment.Status == TransactionStatus.Paid)
            throw new InstallmentPaidCannotBeCancelledException(installment.Id);

        _transactionService.CancelTransaction(account, installment, userId, reason);
    }

    // Cancelamento do grupo (req 32)
    public void CancelInstallmentGroup(Account account,
        IEnumerable<Transaction> groupTransactions, string userId, string? reason = null)
    {
        foreach (var tx in groupTransactions.Where(t => t.Status == TransactionStatus.Pending))
        {
            _transactionService.CancelTransaction(account, tx, userId, reason);
        }
        // Parcelas Paid permanecem inalteradas
    }
}
```

```csharp
// === TransferDomainService (F8) ===
public class TransferDomainService
{
    private readonly TransactionDomainService _transactionService;

    public (Transaction debit, Transaction credit) CreateTransfer(
        Account sourceAccount, Account destinationAccount,
        Guid categoryId, decimal amount, string description,
        DateTime competenceDate, string userId, string? operationId = null)
    {
        var transferGroupId = Guid.NewGuid();

        var debitTx = _transactionService.CreateTransaction(
            sourceAccount, categoryId, TransactionType.Debit, amount,
            $"Transf. para {destinationAccount.Name}: {description}",
            competenceDate, null, TransactionStatus.Paid, userId, operationId);
        debitTx.SetTransferGroup(transferGroupId);

        var creditTx = _transactionService.CreateTransaction(
            destinationAccount, categoryId, TransactionType.Credit, amount,
            $"Transf. de {sourceAccount.Name}: {description}",
            competenceDate, null, TransactionStatus.Paid, userId);
        creditTx.SetTransferGroup(transferGroupId);

        return (debitTx, creditTx);
    }

    public void CancelTransfer(Account sourceAccount, Account destinationAccount,
        Transaction debitTx, Transaction creditTx, string userId, string? reason = null)
    {
        _transactionService.CancelTransaction(sourceAccount, debitTx, userId, reason);
        _transactionService.CancelTransaction(destinationAccount, creditTx, userId, reason);
    }
}
```

```csharp
// === RecurrenceDomainService (F7) ===
public class RecurrenceDomainService
{
    private readonly TransactionDomainService _transactionService;

    // Geração lazy — apenas 1 mês à frente (req 33)
    public Transaction? GenerateNextOccurrence(RecurrenceTemplate template,
        Account account, DateTime referenceDate, string userId)
    {
        if (!template.ShouldGenerateForMonth(referenceDate))
            return null;

        var competenceDate = new DateTime(referenceDate.Year, referenceDate.Month,
            Math.Min(template.DayOfMonth, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)));

        var tx = _transactionService.CreateTransaction(account, template.CategoryId,
            template.Type, template.Amount, template.Description,
            competenceDate, competenceDate, template.DefaultStatus, userId);

        tx.SetRecurrenceInfo(template.Id);
        template.MarkGenerated(competenceDate, userId);

        return tx;
    }
}
```

### Endpoints de API

**Não aplicável nesta fase.** A camada 1-Services será criada como placeholder vazio. Todos os contratos de entrada/saída estão definidos como Commands/Queries na Application layer, prontos para serem expostos via Controllers na Fase 2.

---

## Pontos de Integração

**Nenhuma integração externa nesta fase.** O único ponto de integração é o PostgreSQL via Entity Framework Core. A connection string será configurada via `appsettings.json` / variáveis de ambiente.

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|---|---|---|---|
| Banco PostgreSQL | Criação de schema | 6 tabelas novas, sem dados anteriores. Risco baixo. | Executar migrations iniciais |
| Projeto API (1-Services) | Placeholder | Projeto vazio sem código funcional. Risco zero. | Apenas criar .csproj |
| Fase 2 (API REST) | Dependência futura | Contratos CQRS definidos aqui serão consumidos pelos Controllers. Risco baixo. | Manter interfaces estáveis |
| Fase 3 (Frontend) | Sem impacto | Nenhum. | Nenhuma |

---

## Abordagem de Testes

### Testes Unitários

**Framework**: xUnit + AwesomeAssertions + Moq + AutoFixture

**Projeto**: `GestorFinanceiro.Financeiro.UnitTests`

**Componentes a testar**:

1. **Entidades de domínio** (sem mocks — lógica pura):
   - `Account`: criação, ativação/inativação, `ApplyDebit`/`ApplyCredit`, validação saldo negativo, `ValidateCanReceiveTransaction`
   - `Transaction`: factory methods, `Cancel()`, `MarkAsAdjusted()`, cálculo `IsOverdue`
   - `Category`: criação, `UpdateName()`
   - `RecurrenceTemplate`: `ShouldGenerateForMonth()`, `Deactivate()`

2. **Domain Services** (mock de repositories):
   - `TransactionDomainService`: criação com/sem saldo, ajuste por diferença (cenários positivos/negativos), cancelamento com/sem reversão de saldo
   - `InstallmentDomainService`: criação N parcelas com arredondamento, ajuste de grupo, cancelamento individual vs grupo
   - `TransferDomainService`: criação transferência, cancelamento bilateral
   - `RecurrenceDomainService`: geração lazy, skip quando já gerado, ajuste dia do mês

3. **Application Handlers** (mock de repositories + UnitOfWork):
   - Cenários de sucesso e falha para cada command handler
   - Validação de idempotência (operationId duplicado)

**Cenários críticos**:
- Saldo negativo rejeitado em conta sem `AllowNegativeBalance`
- Parcela Paid não pode ser cancelada
- Ajuste em Debit original gera corretamente Credit ou Debit conforme diferença
- Arredondamento de parcelas: resíduo aplicado na última parcela
- Transferência reverte ambos os lados no cancelamento
- Recorrência não gera duplicata para mês já gerado
- `IsOverdue` retorna true/false conforme data atual e status

**Naming convention**: `MetodoTestado_Cenario_ResultadoEsperado`

### Testes de Integração

**Framework**: xUnit + Testcontainers (PostgreSQL)

**Projeto**: `GestorFinanceiro.Financeiro.IntegrationTests`

**Componentes a testar**:
- EF Core migrations aplicam corretamente
- Repositories persistem e recuperam entidades
- `SELECT FOR UPDATE` efetivamente bloqueia linha concorrente (teste com Task paralela)
- UnitOfWork faz rollback em exceção
- OperationLog cleanup funciona
- Seed de categorias padrão

**Skip quando Docker indisponível**: Os testes de integração devem verificar se o Docker engine está disponível e serem pulados (skip) de forma limpa caso contrário, sem quebrar o build — conforme regra do `copilot-instructions.md`.

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Criar estrutura de solução** — Solution, projetos (.csproj), referências entre projetos
   - *Motivo*: fundação para todos os passos seguintes

2. **Domain Layer — Enums e Base Entity** — `AccountType`, `CategoryType`, `TransactionType`, `TransactionStatus`, `BaseEntity`
   - *Motivo*: tipos básicos usados por todas as entidades

3. **Domain Layer — Entidades e Exceções** — `Account`, `Category`, `Transaction`, `RecurrenceTemplate`, `OperationLog` + exceções de domínio
   - *Motivo*: lógica de negócio pura, testável isoladamente

4. **Domain Layer — Interfaces de repositório** — `IRepository<T>`, `IAccountRepository`, `ITransactionRepository`, `ICategoryRepository`, `IRecurrenceTemplateRepository`, `IOperationLogRepository`, `IUnitOfWork`
   - *Motivo*: contratos para a camada de infraestrutura

5. **Domain Layer — Domain Services** — `TransactionDomainService`, `InstallmentDomainService`, `TransferDomainService`, `RecurrenceDomainService`
   - *Motivo*: orquestração de regras de negócio entre entidades

6. **Testes unitários do Domain** — testes para entidades e domain services
   - *Motivo*: validar toda a lógica de negócio antes de avançar para persistência

7. **Infra Layer — DbContext e Configurations** — `FinanceiroDbContext`, Fluent API configs, migrations iniciais
   - *Motivo*: schema de banco definido e migrations geradas

8. **Infra Layer — Repositories e UnitOfWork** — implementações concretas com EF Core + PostgreSQL
   - *Motivo*: persistência funcional

9. **Application Layer — CQRS** — Commands, Queries, Handlers, Validators, Dispatcher
   - *Motivo*: orquestração de use cases com validação e idempotência

10. **Testes de integração** — repositories, migrations, concorrência, seed
    - *Motivo*: validar persistência real com PostgreSQL em container

11. **Seed de categorias padrão** — migration ou `IDataSeeder` com categorias iniciais
    - *Motivo*: req 11 do PRD

### Dependências Técnicas

| Dependência | Tipo | Bloqueante? |
|---|---|---|
| .NET 8 SDK | Infraestrutura local | Sim |
| PostgreSQL (ou Docker) | Testes de integração | Não — testes pulam se indisponível |
| Pacotes NuGet (EF Core, xUnit, etc.) | Restore | Sim — primeira execução |

---

## Monitoramento e Observabilidade

### Nesta fase (sem API HTTP)

- **Logging estruturado**: `ILogger<T>` do `Microsoft.Extensions.Logging` nos domain services e handlers. Formato JSON com campos padronizados conforme `dotnet-logging.md`.
- **Níveis de log**:
  - `Information` — criação de transação, transferência, ajuste
  - `Warning` — tentativa de operação em conta inativa, saldo negativo rejeitado
  - `Error` — exceções não esperadas
- **Métricas**: não aplicável nesta fase (sem endpoint HTTP)
- **Health checks**: não aplicável nesta fase (sem servidor)

### Preparação para Fase 2

Os domain services já recebem `ILogger<T>` via DI, garantindo que o logging estará ativo quando a API for exposta. Os structured logs incluirão campos como `accountId`, `transactionId`, `operationId` para correlação.

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Justificativa | Alternativas Rejeitadas |
|---|---|---|
| **CQRS nativo (sem MediatR)** | Menor footprint, sem dependência externa, conforme `dotnet-architecture.md` | MediatR — adiciona dependência desnecessária nesta fase |
| **Saldo materializado** | Performance O(1) para consulta de saldo; evita recalcular histórico | Saldo calculado — O(n) a cada consulta, inviável com volume |
| **Pessimistic locking (SELECT FOR UPDATE)** | Simplicidade, consistência imediata, adequado para baixa concorrência familiar | Optimistic concurrency — requer retries, mais complexo |
| **PostgreSQL** | Open source, robusto, suporte nativo a JSONB e row-level locking | SQLite (sem concorrência real), SQL Server (licenciamento) |
| **Guid como PK** | Geração client-side, sem coordenação centralizada, merge-friendly | Auto-increment — problemas em cenários distribuídos futuros |
| **Ajuste por diferença contábil** | Imutabilidade do histórico, auditoria completa, simplicidade | Edição direta — perde histórico, viola auditoria |
| **Overdue calculado on-the-fly** | Sem job periódico, sem inconsistência temporal, zero manutenção | Status persistido — requer scheduled job, pode ficar dessincronizado |
| **Recorrência lazy (1 mês)** | Evita poluir banco com transações futuras, menor footprint | Pré-gerar 12 meses — dados desnecessários, difícil de corrigir |
| **OperationId com TTL 24h** | Previne duplicidade em chamadas repetidas, cleanup automático | Sem TTL — tabela cresce indefinidamente |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Complexidade de arredondamento em parcelas | Média | Baixo | Testes exaustivos com centavos; resíduo na última parcela |
| Lock de conta em operações longas | Baixa | Médio | Transações ACID curtas; sem I/O externo dentro do lock |
| Recorrência: dia 31 em mês com 28 dias | Certa | Baixo | `Math.Min(dayOfMonth, DaysInMonth)` — normalizar para último dia |
| Migration concorrente em deploy | Baixa | Alto | Usar `dotnet ef migrations` com lock de schema |
| Volume de OperationLog sem cleanup | Média | Baixo | Job de cleanup com `expires_at < now()` |

### Conformidade com Padrões

| Regra | Status | Observação |
|---|---|---|
| `dotnet-architecture.md` — Clean Architecture | ✅ Conforme | 4 camadas com inversão de dependência |
| `dotnet-architecture.md` — CQRS nativo | ✅ Conforme | Sem MediatR, interfaces próprias |
| `dotnet-architecture.md` — Repository Pattern | ✅ Conforme | Genérico + específicos |
| `dotnet-architecture.md` — Result Pattern | ⚠️ Parcial | Usado em handlers; entidades usam exceptions |
| `dotnet-coding-standards.md` — Código em inglês | ✅ Conforme | Classes, métodos e variáveis em inglês |
| `dotnet-coding-standards.md` — Nomenclatura | ✅ Conforme | PascalCase, camelCase, kebab-case dirs |
| `dotnet-coding-standards.md` — Max 3 params | ⚠️ Desvio mínimo | Factory methods de entidades > 3 params; mitigado com construtores descritivos |
| `dotnet-folders.md` — Estrutura numerada | ✅ Conforme | 1-Services, 2-Application, 3-Domain, 4-Infra, 5-Tests |
| `dotnet-testing.md` — xUnit + AwesomeAssertions | ✅ Conforme | Padrão AAA, naming convention |
| `dotnet-testing.md` — Testcontainers | ✅ Conforme | PostgreSQL em container para integração |
| `dotnet-libraries-config.md` — EF Core | ✅ Conforme | Fluent API, configurations separadas |
| `dotnet-libraries-config.md` — FluentValidation | ✅ Conforme | Validators nos handlers |
| `dotnet-libraries-config.md` — Mapster | ✅ Conforme | DTOs ↔ Entities |
| `dotnet-libraries-config.md` — Provider DB | ⚠️ Desvio | PostgreSQL (PRD) em vez de Oracle (rules). PRD tem precedência |
| `dotnet-performance.md` — AsNoTracking | ✅ Conforme | Queries de leitura sem tracking |
| `dotnet-observability.md` — CancellationToken | ✅ Conforme | Em todos os métodos async |
| `dotnet-logging.md` — Structured logging | ✅ Conforme | ILogger com campos estruturados |
| `copilot-instructions.md` — Cobertura ≥ 90% | ✅ Alvo | Configurar gate de cobertura no projeto de testes |
| `copilot-instructions.md` — Skip Docker tests | ✅ Conforme | Integration tests pulam limpo sem Docker |

### Pacotes NuGet Planejados

```xml
<!-- 3-Domain (zero dependências externas) -->
<!-- Nenhum pacote NuGet — apenas .NET BCL -->

<!-- 2-Application -->
<PackageReference Include="FluentValidation" Version="11.8.1" />
<PackageReference Include="Mapster" Version="7.4.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />

<!-- 4-Infra -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />

<!-- 5-Tests/UnitTests -->
<PackageReference Include="xunit" Version="2.6.6" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="AwesomeAssertions" Version="6.15.1" />
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />

<!-- 5-Tests/IntegrationTests -->
<PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
```

### Seed de Categorias Padrão (F2 req 11)

```csharp
// Categorias iniciais via migration ou IDataSeeder
new[] {
    Category.Create("Alimentação",  CategoryType.Despesa,  "system"),
    Category.Create("Transporte",   CategoryType.Despesa,  "system"),
    Category.Create("Moradia",      CategoryType.Despesa,  "system"),
    Category.Create("Lazer",        CategoryType.Despesa,  "system"),
    Category.Create("Saúde",        CategoryType.Despesa,  "system"),
    Category.Create("Educação",     CategoryType.Despesa,  "system"),
    Category.Create("Vestuário",    CategoryType.Despesa,  "system"),
    Category.Create("Salário",      CategoryType.Receita,  "system"),
    Category.Create("Freelance",    CategoryType.Receita,  "system"),
    Category.Create("Investimento", CategoryType.Receita,  "system"),
    Category.Create("Outros",       CategoryType.Despesa,  "system"),
    Category.Create("Outros",       CategoryType.Receita,  "system"),
};
```

### Concorrência — SELECT FOR UPDATE com EF Core (F10)

```csharp
// No AccountRepository (Infra)
public async Task<Account> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken)
{
    // PostgreSQL: row-level lock via raw SQL dentro da transaction
    return await _context.Accounts
        .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {id} FOR UPDATE")
        .SingleAsync(cancellationToken);
}
```

O lock é adquirido dentro da transação ACID aberta pelo `UnitOfWork.BeginTransactionAsync()` e liberado automaticamente no `CommitAsync()` ou `RollbackAsync()`. Isso garante que duas operações concorrentes na mesma conta sejam serializadas sem deadlock (lock em uma única linha).

### Questões Resolvidas

1. **Timezone**: Todos os timestamps em UTC. `CompetenceDate` e `DueDate` são armazenados como `DATE` (sem timezone). Timestamps de auditoria (`CreatedAt`, `UpdatedAt`, `CancelledAt`) em `TIMESTAMPTZ` (UTC).
2. **Multi-tenancy**: Não aplicável. O sistema é self-hosted com **uma família por instância**. Sem necessidade de `FamilyId` ou separação de dados.
3. **Cleanup job do OperationLog**: Implementação adiada para a **Fase 2** (API). Na Fase 1, cleanup pode ser invocado via teste ou manualmente.
