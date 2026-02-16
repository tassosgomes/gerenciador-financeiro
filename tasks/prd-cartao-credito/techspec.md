# Especificação Técnica — Cartão de Crédito (Evolução do Modelo de Contas)

## Resumo Executivo

Evolução do modelo de contas para tratar o tipo `Cartão` de forma diferenciada, adicionando configuração específica de cartão de crédito (limite, dia de fechamento, dia de vencimento, conta de débito, flag de limite rígido) e implementando operações dedicadas: consulta de fatura mensal e pagamento de fatura.

A estratégia arquitetural mantém a Clean Architecture existente. Cartão de crédito continua sendo uma `Account` (no domínio contábil, cartão é uma conta de passivo circulante), mas sua configuração específica é isolada num **value object `CreditCardDetails`**, mapeado como **owned entity** do EF Core em tabela separada `credit_card_details` (composição 1:1). Isso preserva a coesão da entidade `Account` — que não é poluída com campos/métodos exclusivos de cartão — e permite evolução independente. A fatura é calculada (query agregada por período de fechamento), não materializada. O pagamento de fatura é modelado como transferência especializada via `TransferDomainService` estendido, com `TransferGroupId` e descrição semântica. O frontend adapta formulários e cards condicionalmente por `AccountType === Cartao`, com drawer lateral para visualização de fatura detalhada.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌──────────────────────────────────────────────────────────────┐
│  1-Services (API)                                            │
│  ├─ AccountsController  (POST/PUT adaptados para Cartão)     │
│  ├─ InvoicesController  (GET fatura por cartão/mês)          │
│  └─ Requests/           (novos DTOs de request)              │
└──────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────────┐
│  2-Application                                               │
│  ├─ Commands/Account/   (Create/Update adaptados)            │
│  ├─ Commands/Invoice/   (PayInvoiceCommand)                  │
│  ├─ Queries/Invoice/    (GetInvoiceQuery)                    │
│  ├─ Dtos/               (AccountResponse estendido,          │
│  │                        InvoiceResponse, InvoiceSummary)   │
│  └─ Validators/         (validações específicas cartão)      │
└──────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────────┐
│  3-Domain                                                    │
│  ├─ Entity/Account      (propriedade CreditCard? nullable)   │
│  ├─ Entity/CreditCard   (Value Object: CreditLimit, Closing  │
│  │   Details              Day, DueDay, DebitAccountId,        │
│  │                        EnforceCreditLimit)                 │
│  ├─ Service/CreditCard  (CreditCardDomainService —           │
│  │   DomainService        validação limite, cálculo fatura)   │
│  ├─ Exception/          (CreditLimitExceededException,       │
│  │                        InvalidCreditCardConfigException)   │
│  └─ Interface/          (métodos novos em ITransactionRepo,   │
│                           IAccountRepository)                 │
└──────────────────────────────────────────────────────────────┘
                          ▲
                          │
┌──────────────────────────────────────────────────────────────┐
│  4-Infra                                                     │
│  ├─ Config/Account      (owned entity CreditCardDetails)     │
│  ├─ Config/CreditCard   (tabela credit_card_details)         │
│  ├─ Repository/         (queries de fatura, filtros)         │
│  ├─ Migrations/         (AddCreditCardDetailsTable)          │
│  └─ StartupTasks/       (seed categoria Pagamento Fatura)    │
└──────────────────────────────────────────────────────────────┘
                          │
┌──────────────────────────────────────────────────────────────┐
│  Frontend (React + TypeScript)                               │
│  ├─ features/accounts/  (AccountForm, AccountCard adaptados) │
│  ├─ features/accounts/  (InvoiceDrawer — novo componente)    │
│  └─ features/dashboard/ (SummaryCards — dados estendidos)    │
└──────────────────────────────────────────────────────────────┘
```

**Fluxo de dados — criação de cartão**: Controller recebe `CreateAccountRequest` com campos de cartão → valida via FluentValidation (regras condicionais por tipo) → `Account.CreateCreditCard(...)` (factory method que cria Account + CreditCardDetails) → persiste via `AccountRepository` + `UnitOfWork` (EF serializa owned entity na tabela `credit_card_details`) → retorna `AccountResponse` estendido.

**Fluxo de dados — pagamento de fatura**: Controller recebe `PayInvoiceRequest` → handler carrega cartão e conta de débito com lock → `TransferDomainService.CreateInvoicePayment(...)` → gera par de transações com `TransferGroupId` → persiste → retorna `TransactionResponse[]`.

**Fluxo de dados — consulta de fatura**: Controller recebe `GET /api/v1/accounts/{id}/invoices?month=2026-02` → handler calcula período de fechamento → `TransactionRepository.GetInvoiceTransactionsAsync(...)` → agrega total → retorna `InvoiceResponse`.

---

## Design de Implementação

### Interfaces Principais

```csharp
// === Domain Layer — Value Object CreditCardDetails ===

// Encapsula toda configuração específica de cartão de crédito
public class CreditCardDetails
{
    public decimal CreditLimit { get; private set; }
    public int ClosingDay { get; private set; }       // 1-28
    public int DueDay { get; private set; }            // 1-28
    public Guid DebitAccountId { get; private set; }   // FK → Account (Corrente/Carteira)
    public bool EnforceCreditLimit { get; private set; }

    private CreditCardDetails() { }  // EF Core

    public static CreditCardDetails Create(
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit)
    {
        if (creditLimit <= 0)
            throw new InvalidCreditCardConfigException("Limite de crédito deve ser maior que zero.");
        if (closingDay < 1 || closingDay > 28)
            throw new InvalidCreditCardConfigException("Dia de fechamento deve estar entre 1 e 28.");
        if (dueDay < 1 || dueDay > 28)
            throw new InvalidCreditCardConfigException("Dia de vencimento deve estar entre 1 e 28.");

        return new CreditCardDetails
        {
            CreditLimit = creditLimit,
            ClosingDay = closingDay,
            DueDay = dueDay,
            DebitAccountId = debitAccountId,
            EnforceCreditLimit = enforceCreditLimit
        };
    }

    public void Update(
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit)
    {
        // Mesmas validações do Create
        if (creditLimit <= 0)
            throw new InvalidCreditCardConfigException("Limite de crédito deve ser maior que zero.");
        if (closingDay < 1 || closingDay > 28)
            throw new InvalidCreditCardConfigException("Dia de fechamento deve estar entre 1 e 28.");
        if (dueDay < 1 || dueDay > 28)
            throw new InvalidCreditCardConfigException("Dia de vencimento deve estar entre 1 e 28.");

        CreditLimit = creditLimit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        DebitAccountId = debitAccountId;
        EnforceCreditLimit = enforceCreditLimit;
    }
}

// === Domain Layer — Extensão de Account (composição) ===

public class Account : BaseEntity
{
    // ... campos existentes inalterados ...

    // Owned entity — null para Corrente/Investimento/Carteira
    public CreditCardDetails? CreditCard { get; private set; }

    public static Account CreateCreditCard(
        string name,
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit,
        string userId)
    {
        var account = new Account
        {
            Name = name,
            Type = AccountType.Cartao,
            Balance = 0,                    // Cartão sempre inicia com saldo 0
            AllowNegativeBalance = true,     // Compras geram saldo negativo
            CreditCard = CreditCardDetails.Create(
                creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit)
        };
        account.SetAuditOnCreate(userId);
        return account;
    }

    public void UpdateCreditCard(
        string name,
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit,
        string userId)
    {
        if (CreditCard == null)
            throw new InvalidCreditCardConfigException("Conta não é um cartão de crédito.");

        Name = name;
        CreditCard.Update(creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit);
        SetAuditOnUpdate(userId);
    }

    public decimal GetAvailableLimit()
    {
        if (CreditCard == null) return 0;
        return CreditCard.CreditLimit - Math.Abs(Balance);
    }

    public void ValidateCreditLimit(decimal amount)
    {
        if (CreditCard == null) return;               // Não é cartão — bypass
        if (!CreditCard.EnforceCreditLimit) return;    // Limite informativo
        if (GetAvailableLimit() < amount)
            throw new CreditLimitExceededException(Id, GetAvailableLimit(), amount);
    }
}

// === Domain Layer — Novo Domain Service ===

public class CreditCardDomainService
{
    public (DateTime start, DateTime end) CalculateInvoicePeriod(
        int closingDay, int month, int year);

    public decimal CalculateInvoiceTotal(
        IEnumerable<Transaction> transactions);
}

// === Domain Layer — Interface Extensões ===

public interface IAccountRepository : IRepository<Account>
{
    // ... existentes ...
    Task<IReadOnlyList<Account>> GetActiveByTypeAsync(
        AccountType type, CancellationToken ct);
}

public interface ITransactionRepository : IRepository<Transaction>
{
    // ... existentes ...
    Task<IReadOnlyList<Transaction>> GetByAccountAndPeriodAsync(
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct);
}

// === Application Layer — Commands ===

public record PayInvoiceCommand(
    Guid CreditCardAccountId,
    decimal Amount,
    DateTime CompetenceDate,
    string UserId,
    string? OperationId = null
) : ICommand<IReadOnlyList<TransactionResponse>>;

// === Application Layer — Queries ===

public record GetInvoiceQuery(
    Guid AccountId,
    int Month,
    int Year
) : IQuery<InvoiceResponse>;
```

### Modelos de Dados

#### Tabela `accounts` (inalterada)

Nenhuma coluna nova é adicionada à tabela `accounts`. A tabela permanece exatamente como está.

#### Nova Tabela `credit_card_details` (owned entity, relação 1:1)

| Coluna                  | Tipo             | Nullable | Default | Descrição                                     |
|-------------------------|------------------|----------|---------|-----------------------------------------------|
| `account_id`            | `uuid`           | Não      | —       | PK + FK → `accounts.id` (ON DELETE CASCADE)    |
| `credit_limit`          | `numeric(18,2)`  | Não      | —       | Limite de crédito do cartão                    |
| `closing_day`           | `smallint`       | Não      | —       | Dia de fechamento 1-28                         |
| `due_day`               | `smallint`       | Não      | —       | Dia de vencimento 1-28                         |
| `debit_account_id`      | `uuid`           | Não      | —       | FK → `accounts.id` (conta de débito vinculada) |
| `enforce_credit_limit`  | `boolean`        | Não      | `true`  | Se `true`, bloqueia compras além do limite     |

A tabela só possui registros para contas do tipo `Cartão`. Contas existentes dos tipos Corrente, Investimento e Carteira não são afetadas (não possuem linha nesta tabela). A propriedade `Account.CreditCard` é `null` para esses tipos.

**Configuração EF Core (owned entity):**

```csharp
// Em AccountConfiguration.Configure()
builder.OwnsOne(account => account.CreditCard, cc =>
{
    cc.ToTable("credit_card_details");
    cc.Property(c => c.CreditLimit).HasColumnName("credit_limit").HasColumnType("numeric(18,2)");
    cc.Property(c => c.ClosingDay).HasColumnName("closing_day").HasColumnType("smallint");
    cc.Property(c => c.DueDay).HasColumnName("due_day").HasColumnType("smallint");
    cc.Property(c => c.DebitAccountId).HasColumnName("debit_account_id").HasColumnType("uuid");
    cc.Property(c => c.EnforceCreditLimit).HasColumnName("enforce_credit_limit").HasColumnType("boolean").HasDefaultValue(true);
});
```

O EF Core carrega `CreditCardDetails` automaticamente junto com `Account` (owned entities são sempre incluídas). Nenhum `.Include()` manual é necessário.

#### DTOs de Resposta

```csharp
// Sub-DTO para dados de cartão de crédito
public record CreditCardDetailsResponse(
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    Guid DebitAccountId,
    bool EnforceCreditLimit,
    decimal AvailableLimit       // Calculado: CreditLimit - |Balance|
);

// AccountResponse estendido (retrocompatível — campo novo nullable)
public record AccountResponse(
    Guid Id,
    string Name,
    AccountType Type,
    decimal Balance,
    bool AllowNegativeBalance,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CreditCardDetailsResponse? CreditCard   // null para Corrente/Investimento/Carteira
);

// Fatura
public record InvoiceResponse(
    Guid AccountId,
    string AccountName,
    int Month,
    int Year,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime DueDate,
    decimal TotalAmount,
    decimal PreviousBalance,      // Saldo positivo anterior (crédito a favor)
    decimal AmountDue,            // TotalAmount - PreviousBalance
    IReadOnlyList<InvoiceTransactionDto> Transactions
);

public record InvoiceTransactionDto(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateTime CompetenceDate,
    int? InstallmentNumber,
    int? TotalInstallments
);
```

### Endpoints de API

| Método | Rota                                          | Descrição                                       |
|--------|-----------------------------------------------|-------------------------------------------------|
| `POST` | `/api/v1/accounts`                            | Criar conta (adaptado para aceitar campos cartão)|
| `PUT`  | `/api/v1/accounts/{id}`                       | Editar conta (adaptado para campos cartão)       |
| `GET`  | `/api/v1/accounts/{id}/invoices`              | Consultar fatura do cartão (query: `month`, `year`)|
| `POST` | `/api/v1/accounts/{id}/invoices/pay`          | Pagar fatura do cartão                           |

#### `POST /api/v1/accounts` — Campos adicionais para tipo Cartão

```json
{
  "name": "Nubank",
  "type": 2,
  "creditLimit": 5000.00,
  "closingDay": 3,
  "dueDay": 10,
  "debitAccountId": "guid-da-conta-corrente",
  "enforceCreditLimit": true
}
```

Quando `type` ≠ `Cartao`, os campos de cartão são ignorados e o fluxo atual (`initialBalance`, `allowNegativeBalance`) é mantido.

#### `GET /api/v1/accounts/{id}/invoices?month=2&year=2026`

Retorna `InvoiceResponse` com lista de transações do ciclo de fechamento.

#### `POST /api/v1/accounts/{id}/invoices/pay`

```json
{
  "amount": 1500.00,
  "competenceDate": "2026-02-10",
  "operationId": "op-uuid-opcional"
}
```

Retorna `TransactionResponse[]` (par débito/crédito).

---

## Pontos de Integração

- **Nenhum serviço externo** — todas as integrações são internas ao monolito.
- **TransferDomainService** — reutilizado e estendido para pagamento de fatura com descrição semântica ("Pagamento Fatura" ao invés de "Transf.").
- **Seed de categoria** — categoria fixa "Pagamento de Fatura" (`CategoryType.System`) adicionada via `StartupTasks`, similar a categorias de sistema existentes.
- **Dashboard** — `DashboardRepository.GetTotalBalanceAsync` já inclui cartões no cálculo (saldo negativo é subtraído). `GetCreditCardDebtAsync` já filtra por `AccountType.Cartao`. Novos campos agregados (limite total, % utilização) serão adicionados ao `DashboardSummaryResponse`.

---

## Análise de Impacto

| Componente Afetado                   | Tipo de Impacto          | Descrição & Nível de Risco                                          | Ação Requerida                         |
|--------------------------------------|--------------------------|----------------------------------------------------------------------|----------------------------------------|
| `Account` (Entity)                   | Composição               | 1 propriedade nova (`CreditCard?`) + 3 métodos. Baixo risco — campos existentes inalterados. | Testar Restore/Create/Update           |
| `CreditCardDetails` (Value Object)   | Nova classe              | Owned entity com 5 propriedades + validação interna. Baixo risco.   | Testes unitários de Create/Update      |
| `AccountConfiguration` (EF Config)   | Tabela nova              | `OwnsOne` mapeia tabela `credit_card_details` (1:1). Baixo risco.   | Criar migration EF                     |
| `AccountResponse` (DTO)              | Extensão de contrato     | Campo novo `CreditCard?` nullable. Compatível: clientes ignoram.    | Atualizar mapeamento Mapster           |
| `CreateAccountRequest`               | Extensão de contrato     | Campos novos opcionais no request. Compatível.                       | Atualizar validação condicional        |
| `CreateAccountCommandHandler`        | Mudança lógica           | Bifurcação: factory genérica vs `CreateCreditCard`. Risco médio.     | Testes unitários extensivos            |
| `TransactionDomainService`           | Extensão de lógica       | `ApplyDebit`: chamar `ValidateCreditLimit` antes. Risco médio.       | Testes de limite rígido/informativo    |
| `TransferDomainService`              | Método novo              | `CreateInvoicePayment`: reutiliza `CreateTransfer` com desc custom.  | Testes unitários                       |
| `DashboardRepository`                | Query nova               | Campos: limite total agregado, % utilização. Join com `credit_card_details`. Baixo risco. | Atualizar DTO e query            |
| `DashboardSummaryResponse`           | Extensão de DTO          | 2 campos novos nullable. Compatível.                                 | Atualizar frontend                     |
| `AccountCard.tsx`                    | Extensão visual          | Exibição condicional: fatura, limite, alertas. Risco médio.          | Testes de componente                   |
| `AccountForm.tsx`                    | Formulário dinâmico      | Campos condicionais por tipo Cartão. Risco médio.                    | Testes de formulário                   |
| `SummaryCards.tsx`                   | Extensão visual          | Card de dívida com limite total e %. Baixo risco.                    | Atualizar com novos campos             |

---

## Abordagem de Testes

### Testes Unitários (xUnit + AwesomeAssertions + Moq)

**Value Object `CreditCardDetails`** — cenários prioritários:
- `Create` com parâmetros válidos → propriedades preenchidas
- `Create` com `closingDay` fora de 1-28 → `InvalidCreditCardConfigException`
- `Create` com `creditLimit` ≤ 0 → `InvalidCreditCardConfigException`
- `Create` com `dueDay` fora de 1-28 → `InvalidCreditCardConfigException`
- `Update` com parâmetros válidos → propriedades atualizadas
- `Update` com parâmetros inválidos → exceção (mesmas regras do Create)

**Entidade `Account`** — cenários prioritários:
- `CreateCreditCard` → saldo = 0, `AllowNegativeBalance` = true, `CreditCard` preenchido
- `ValidateCreditLimit` com `EnforceCreditLimit=true` e compra acima do limite → `CreditLimitExceededException`
- `ValidateCreditLimit` com `EnforceCreditLimit=false` → não lança exceção
- `ValidateCreditLimit` para conta sem `CreditCard` (não-cartão) → bypass
- `GetAvailableLimit` com saldo negativo → `CreditLimit - |Balance|`
- `GetAvailableLimit` com saldo positivo (crédito a favor) → `CreditLimit + Balance`
- `GetAvailableLimit` para conta sem `CreditCard` → retorna 0
- `UpdateCreditCard` em conta sem `CreditCard` → exceção
- `UpdateCreditCard` → campos atualizados, auditoria registrada

**`CreditCardDomainService`**:
- `CalculateInvoicePeriod` — edge cases: mês com fechamento dia 28, troca de ano
- `CalculateInvoiceTotal` — soma correta de débitos e créditos no período

**`TransactionDomainService`** (extensão):
- `CreateTransaction` em conta cartão com `EnforceCreditLimit=true` ultrapassando limite → exceção
- `CreateTransaction` em conta cartão com `EnforceCreditLimit=false` → sucesso

**Application Handlers**:
- `CreateAccountCommandHandler` com tipo Cartão → usa `CreateCreditCard`, saldo = 0
- `CreateAccountCommandHandler` com tipo Corrente → fluxo inalterado
- `PayInvoiceCommandHandler` → par de transações criadas, saldos atualizados
- `PayInvoiceCommandHandler` com conta de débito sem saldo → `InsufficientBalanceException`
- `GetInvoiceQueryHandler` → período calculado corretamente, transações filtradas

**Meta de cobertura**: ≥ 90% em novas classes de domínio e handlers.

### Testes de Integração

**`AccountsControllerHttpTests`** (estender):
- POST criar cartão de crédito → 201 com campos de cartão no response
- POST criar cartão com fechamento inválido → 400
- PUT editar cartão → campos atualizados
- PUT editar conta corrente → campos de cartão ignorados

**Testes de fatura (novo)**:
- GET fatura com transações no período de fechamento → response correto
- GET fatura sem transações → totais zerados
- POST pagar fatura → par de transações, saldos atualizados

### Testes Frontend (Vitest + Testing Library)

- `AccountForm` — ao selecionar tipo "Cartão de Crédito", campos específicos aparecem (limite, fechamento, vencimento, conta débito)
- `AccountForm` — ao selecionar tipo "Corrente", campos de cartão não aparecem
- `AccountCard` — cartão exibe "Fatura Atual" ao invés de "Saldo Atual"
- `AccountCard` — alertas de limite < 20% e esgotado
- `InvoiceDrawer` — renderiza lista de transações e total

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Domain: Value Object `CreditCardDetails`** — classe com Create/Update, validação interna (limite > 0, dias 1-28). Por que primeiro: base para toda a feature.

2. **Domain: Extensão da entidade `Account`** — propriedade `CreditCard?`, factory method `CreateCreditCard`, `UpdateCreditCard`, `ValidateCreditLimit`, `GetAvailableLimit`.

3. **Domain: Exceções e `CreditCardDomainService`** — `CreditLimitExceededException`, `InvalidCreditCardConfigException`, cálculo de período de fatura.

4. **Domain: Extensão de `TransactionDomainService`** — integrar `ValidateCreditLimit` no fluxo de `ApplyDebit` para contas tipo Cartão.

5. **Infra: Migration e configuração EF** — tabela `credit_card_details` via `OwnsOne`, FK `debit_account_id`, seed da categoria "Pagamento de Fatura".

6. **Infra: Extensão de repositórios** — `ITransactionRepository.GetByAccountAndPeriodAsync`, `IAccountRepository.GetActiveByTypeAsync`.

7. **Application: Commands adaptados** — `CreateAccountCommand` e handler com bifurcação por tipo, `UpdateAccountCommand` com campos de cartão, novos validators.

8. **Application: Query de fatura** — `GetInvoiceQuery` + handler com cálculo de período e agregação.

9. **Application: Pagamento de fatura** — `PayInvoiceCommand` + handler reutilizando `TransferDomainService`.

10. **API: Endpoints** — `InvoicesController` novo, adaptação de `AccountsController` e requests.

11. **Frontend: Formulários adaptados** — `AccountForm` com campos condicionais, schemas Zod atualizados, tipos TypeScript estendidos.

12. **Frontend: Card e Drawer** — `AccountCard` adaptado para cartão, `InvoiceDrawer` novo componente, botão "Pagar Fatura".

13. **Frontend: Dashboard** — `SummaryCards` com limite total agregado e percentual de utilização.

### Dependências Técnicas

- **EF Migration** deve rodar antes de qualquer handler novo (passos 1-5 devem ser completos antes de 7-10).
- **Categoria seed** ("Pagamento de Fatura") deve existir antes de `PayInvoiceCommandHandler`.
- **Endpoint de listagem de contas** (GET `/api/v1/accounts?type=1&type=4`) deve funcionar para popular o dropdown de conta de débito no frontend.

---

## Monitoramento e Observabilidade

- **Logs estruturados (Serilog)**: seguir padrão existente
  - `LogInformation("Creating credit card account: {Name}, CreditLimit: {Limit}", ...)`
  - `LogInformation("Invoice payment: CardId={CardId}, Amount={Amount}", ...)`
  - `LogWarning("Credit limit exceeded (soft): CardId={CardId}, Available={Available}", ...)`
- **Métricas**: não há Prometheus/Grafana configurado neste projeto; usar logs como proxy.
- **Auditoria**: todas as operações novas registradas via `IAuditService.LogAsync` (padrão existente).

---

## Considerações Técnicas

### Decisões Principais

| # | Decisão                                           | Justificativa                                                                                                  |
|---|---------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
| 1 | **Composição com Owned Entity** (`CreditCardDetails` em tabela `credit_card_details`) | Cartão é conta no domínio contábil — `Account` permanece a abstração correta. `CreditCardDetails` isola configuração e validação de cartão como value object, mantendo `Account` coesa. EF Core `OwnsOne` mapeia para tabela 1:1 automaticamente carregada (sem `.Include()`). Alternativa rejeitada: Single Table com colunas nullable — polui entidade com campos/métodos que só existem para 1 dos 4 tipos, tendência a virar god entity. Alternativa rejeitada: herança TPH/TPT — complexidade de EF Core, repositórios polimórficos, Restore ambíguo. |
| 2 | **Fatura calculada, não materializada**           | Conforme definido no PRD. Query por período é simples com índice em `(account_id, competence_date, status)`. Alternativa rejeitada: tabela `invoices` — complexidade de sincronização sem ganho claro.     |
| 3 | **Pagamento via `TransferDomainService`**         | Reutiliza infra de transferência existente (lock, TransferGroupId, auditoria). Descrição customizada ("Pgto. Fatura") distingue de transferências comuns. Alternativa rejeitada: domain service separado — duplicação de lógica de transferência.                              |
| 4 | **Categoria seed "Pagamento de Fatura"**          | Decisão confirmada pelo usuário. Permite filtrar/reportar pagamentos de fatura no dashboard e relatórios. Criada como `CategoryType.System` (não editável/deletável pelo usuário).                         |
| 5 | **Drawer lateral para fatura**                    | Decisão confirmada pelo usuário. Mantém contexto da página de contas. Menos intrusivo que página separada. O drawer lista transações do ciclo com subtotal e botão "Pagar Fatura".                         |
| 6 | **Cartões legacy: sem linha em `credit_card_details`** | Decisão confirmada pelo usuário. Cartões criados antes desta evolução não possuem `CreditCardDetails` (`Account.CreditCard == null`). Continuam funcionando como conta genérica. Na próxima edição, o formulário solicita preenchimento dos campos de cartão, criando a linha em `credit_card_details`. |
| 7 | **`ApplyDebit` chama `ValidateCreditLimit`**      | Centraliza validação de limite no domínio. O método verifica se `CreditCard != null` e se `EnforceCreditLimit=true` antes de permitir o débito. Bypass automático para contas sem CreditCardDetails.       |
| 8 | **Saldo do cartão sempre inicia em 0**            | Cartão não tem "saldo inicial". O campo `initialBalance` é ignorado (forçado a 0) quando `type == Cartao`. `AllowNegativeBalance` é forçado a `true` (compras geram saldo negativo).                       |

### Riscos Conhecidos

| Risco                                         | Probabilidade | Impacto | Mitigação                                                              |
|-----------------------------------------------|---------------|---------|------------------------------------------------------------------------|
| Cálculo de período de fechamento com edge cases (ano bissexto, mês com menos de 28 dias) | Média | Médio | Restringir dias a 1-28 (conforme PRD). Testes unitários extensivos com edge cases de datas. |
| Retrocompatibilidade de `AccountResponse` com frontend existente | Baixa | Alto | Campos novos são nullable no JSON — frontend existente só lê o que conhece. Verificar com testes E2E. |
| Concorrência em pagamento de fatura (duas requisições simultâneas) | Baixa | Alto | Reutilizar `GetByIdWithLockAsync` (`SELECT FOR UPDATE`) existente nas duas contas envolvidas. Idempotência via `OperationId`. |
| Performance de query de fatura em cartões com muitas transações | Baixa | Médio | Índice composto `(account_id, competence_date, status)` já existe. Filtragem por período limita volume. |

### Requisitos Especiais

- **Índice composto recomendado**: `CREATE INDEX IF NOT EXISTS idx_transactions_account_competence_status ON transactions (account_id, competence_date, status)` — verificar se já existe na migration vigente.
- **FK `debit_account_id`**: na tabela `credit_card_details`, referência à tabela `accounts` com `ON DELETE RESTRICT` — impede desativar/remover a conta de débito vinculada enquanto houver cartão apontando para ela.
- **FK `account_id`** (PK): na tabela `credit_card_details`, referência à tabela `accounts` com `ON DELETE CASCADE` — se a conta cartão for removida, o detalhe é removido junto.

### Conformidade com Padrões

- ✅ Segue Clean Architecture (`rules/dotnet-architecture.md`) — regras de negócio no Domain, orquestração na Application
- ✅ Segue CQRS nativo sem MediatR (`rules/dotnet-architecture.md`) — Commands e Queries separados com handlers
- ✅ Segue padrões de teste (`rules/dotnet-testing.md`) — xUnit + AwesomeAssertions + Moq, padrão AAA
- ✅ Segue padrões REST (`rules/restful.md`) — versionamento `v1`, recursos em inglês plural, POST para mutações
- ✅ Segue estrutura de projeto React (`rules/react-project-structure.md`) — feature-based em `features/accounts/`
- ✅ Segue padrões de codificação .NET (`rules/dotnet-coding-standards.md`) — naming, encapsulamento, DTOs record
- ✅ Segue padrões de logging (`rules/dotnet-logging.md`) — log estruturado com Serilog
- ✅ Segue padrões de commit (`rules/git-commit.md`) — mensagens em português, formato convencional
