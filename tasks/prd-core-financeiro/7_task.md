---
status: pending
parallelizable: true
blocked_by: ["4.0"]
---

<task_context>
<domain>infra/persistência</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"8.0", "11.0"</unblocks>
</task_context>

# Tarefa 7.0: Infra Layer — DbContext e Configurations

## Visão Geral

Implementar o `FinanceiroDbContext` com Entity Framework Core e as configurações Fluent API para todas as entidades. Gerar as migrations iniciais que criam o schema de banco de dados (6 tabelas: accounts, categories, transactions, recurrence_templates, operation_logs). O banco-alvo é PostgreSQL via Npgsql.

## Requisitos

- Techspec: schema de banco conforme tabelas definidas na seção "Esquema de Banco de Dados"
- `rules/dotnet-libraries-config.md`: usar Fluent API em arquivos de configuração separados (não Data Annotations)
- PRD F9 req 40: campos de auditoria em todas as entidades
- Techspec: `CompetenceDate` e `DueDate` como `DATE`, timestamps de auditoria como `TIMESTAMPTZ`
- Techspec: índices parciais para performance (installment_group, transfer_group, operation_id, status+due_date)

## Subtarefas

- [ ] 7.1 Criar `FinanceiroDbContext` com DbSets para `Account`, `Category`, `Transaction`, `RecurrenceTemplate`, `OperationLog`
- [ ] 7.2 Criar configuração Fluent API para `Account` (`AccountConfiguration : IEntityTypeConfiguration<Account>`)
- [ ] 7.3 Criar configuração Fluent API para `Category` (`CategoryConfiguration`)
- [ ] 7.4 Criar configuração Fluent API para `Transaction` (`TransactionConfiguration`)
- [ ] 7.5 Criar configuração Fluent API para `RecurrenceTemplate` (`RecurrenceTemplateConfiguration`)
- [ ] 7.6 Criar configuração Fluent API para `OperationLog` (`OperationLogConfiguration`)
- [ ] 7.7 Gerar migration inicial com `dotnet ef migrations add InitialCreate`
- [ ] 7.8 Validar que o schema gerado corresponde ao definido na techspec

## Sequenciamento

- Bloqueado por: 4.0 (interfaces e entidades devem existir)
- Desbloqueia: 8.0 (Repositories usam o DbContext), 11.0 (Seed depende das configurations)
- Paralelizável: Sim — pode ser executada em paralelo com 5.0 (Domain Services) e 6.0 (Testes unitários)

## Detalhes de Implementação

### Localização dos arquivos

```
4-Infra/GestorFinanceiro.Financeiro.Infra/
├── Context/
│   └── FinanceiroDbContext.cs
└── Config/
    ├── AccountConfiguration.cs
    ├── CategoryConfiguration.cs
    ├── TransactionConfiguration.cs
    ├── RecurrenceTemplateConfiguration.cs
    └── OperationLogConfiguration.cs
```

### FinanceiroDbContext

```csharp
public class FinanceiroDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RecurrenceTemplate> RecurrenceTemplates => Set<RecurrenceTemplate>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();

    public FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceiroDbContext).Assembly);
    }
}
```

### Mapeamentos Fluent API — pontos-chave

**Account:**
- Tabela: `accounts`
- `balance`: `DECIMAL(18,2)` NOT NULL DEFAULT 0
- `name`: `VARCHAR(150)` NOT NULL
- `type`: `SMALLINT` NOT NULL (enum)
- `allow_negative_balance`: `BOOLEAN` NOT NULL DEFAULT FALSE
- `is_active`: `BOOLEAN` NOT NULL DEFAULT TRUE

**Transaction:**
- Tabela: `transactions`
- FK `account_id` → `accounts(id)`
- FK `category_id` → `categories(id)`
- FK `original_transaction_id` → `transactions(id)` (self-referencing, nullable)
- FK `recurrence_template_id` → `recurrence_templates(id)` (nullable)
- `amount`: `DECIMAL(18,2)` NOT NULL CHECK > 0
- `competence_date`: `DATE` NOT NULL
- `due_date`: `DATE` nullable
- `description`: `VARCHAR(500)` NOT NULL
- `operation_id`: `VARCHAR(100)` nullable
- Índices parciais (WHERE):
  - `ix_transactions_installment_group` WHERE `installment_group_id IS NOT NULL`
  - `ix_transactions_transfer_group` WHERE `transfer_group_id IS NOT NULL`
  - `ix_transactions_operation_id` WHERE `operation_id IS NOT NULL`
  - `ix_transactions_status_due_date` WHERE `status = 2` (Pending)

**OperationLog:**
- Tabela: `operation_logs`
- `operation_id`: `VARCHAR(100)` NOT NULL com UNIQUE INDEX
- `result_payload`: `JSONB` NOT NULL
- `expires_at`: `TIMESTAMPTZ` NOT NULL com INDEX

### Convenção de nomes no banco (snake_case)

Usar `.HasColumnName("snake_case")` para cada propriedade, ou convenção global. Propriedades C# (PascalCase) → colunas PostgreSQL (snake_case).

### Timestamps

- `CreatedAt`, `UpdatedAt`, `CancelledAt`, `ExpiresAt` → `TIMESTAMPTZ` (UTC)
- `CompetenceDate`, `DueDate`, `LastGeneratedDate` → `DATE` (sem timezone)

### Propriedades calculadas (ignorar no EF)

- `Transaction.IsOverdue` → `.Ignore(t => t.IsOverdue)` (propriedade calculada, não persistida)
- Navigation properties → configurar com `.HasOne` / `.HasMany`

### Construtores privados

As entidades usam factory methods e construtores privados. Configurar o EF para usar o construtor sem parâmetros:
- Usar `.HasNoKey()` ou garantir que EF consiga construir via reflection (construtor `private` sem parâmetros)

## Critérios de Sucesso

- `FinanceiroDbContext` compila com todos os DbSets
- 5 arquivos de configuração Fluent API criados com todos os mapeamentos
- Migration inicial gerada com sucesso (`dotnet ef migrations add`)
- Schema de migration corresponde às tabelas SQL definidas na techspec
- Índices parciais configurados corretamente
- Propriedade `IsOverdue` ignorada pelo EF
- Timestamps de auditoria mapeados como `TIMESTAMPTZ`
- Datas de competência/vencimento mapeadas como `DATE`
- `dotnet build` compila sem erros
