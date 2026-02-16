```markdown
---
status: pending
parallelizable: false
blocked_by: ["2.0"]
---

<task_context>
<domain>infra/persistência</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>2.0</dependencies>
<unblocks>"5.0"</unblocks>
</task_context>

# Tarefa 4.0: Migration EF Core e Configuração de Persistência

## Visão Geral

Configurar a persistência da composição `Account` ↔ `CreditCardDetails` via EF Core `OwnsOne`, criando a tabela `credit_card_details` (1:1 com `accounts`). Gerar a migration aditiva que cria a nova tabela sem alterar a tabela `accounts` existente. A tabela `accounts` permanece **inalterada** — toda a configuração de cartão fica na tabela separada.

## Requisitos

- Techspec: Tabela `credit_card_details` com colunas `account_id` (PK+FK), `credit_limit`, `closing_day`, `due_day`, `debit_account_id`, `enforce_credit_limit`
- Techspec: Mapeamento via `OwnsOne` no `AccountConfiguration`
- Techspec: FK `account_id` → `accounts.id` com `ON DELETE CASCADE`
- Techspec: FK `debit_account_id` → `accounts.id` com `ON DELETE RESTRICT`
- Techspec: Migration aditiva (ADD TABLE), sem ALTER em tabelas existentes
- Techspec: Índice composto `(account_id, competence_date, status)` em `transactions` (verificar se já existe)
- `rules/dotnet-architecture.md`: Infra depende somente de Domain

## Subtarefas

### Configuração EF Core

- [ ] 4.1 Estender `AccountConfiguration.cs` para mapear o owned entity `CreditCardDetails`:
  - Adicionar bloco `OwnsOne(account => account.CreditCard, cc => { ... })` no método `Configure`
  - Dentro do bloco:
    - `cc.ToTable("credit_card_details")`
    - `cc.Property(c => c.CreditLimit).HasColumnName("credit_limit").HasColumnType("numeric(18,2)")`
    - `cc.Property(c => c.ClosingDay).HasColumnName("closing_day").HasColumnType("smallint")`
    - `cc.Property(c => c.DueDay).HasColumnName("due_day").HasColumnType("smallint")`
    - `cc.Property(c => c.DebitAccountId).HasColumnName("debit_account_id").HasColumnType("uuid")`
    - `cc.Property(c => c.EnforceCreditLimit).HasColumnName("enforce_credit_limit").HasColumnType("boolean").HasDefaultValue(true)`
  - Configurar FK `debit_account_id` com `ON DELETE RESTRICT`

### Migration

- [ ] 4.2 Gerar migration EF Core: `dotnet ef migrations add AddCreditCardDetailsTable`
  - Verificar que a migration **somente** cria a tabela `credit_card_details`
  - Verificar que **não** altera a tabela `accounts`
  - Verificar FKs geradas: `account_id` → `accounts(id)` CASCADE, `debit_account_id` → `accounts(id)` RESTRICT

- [ ] 4.3 Verificar/criar índice composto em `transactions`:
  - Verificar se já existe `idx_transactions_account_competence_status` em `(account_id, competence_date, status)`
  - Se não existir, adicionar na mesma migration ou em migration separada

### Validação

- [ ] 4.4 Aplicar migration em banco de desenvolvimento: `dotnet ef database update`
- [ ] 4.5 Verificar schema gerado no PostgreSQL — confirmar tabela, colunas, FKs e índices
- [ ] 4.6 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 2.0 (propriedade `CreditCard?` deve existir em `Account` para mapear `OwnsOne`)
- Desbloqueia: 5.0 (Repositórios e Seed)
- Paralelizável: Não (migration é sequencial)

## Detalhes de Implementação

### Configuração OwnsOne (conforme techspec)

```csharp
// Em AccountConfiguration.Configure()
builder.OwnsOne(account => account.CreditCard, cc =>
{
    cc.ToTable("credit_card_details");

    cc.Property(c => c.CreditLimit)
        .HasColumnName("credit_limit")
        .HasColumnType("numeric(18,2)");

    cc.Property(c => c.ClosingDay)
        .HasColumnName("closing_day")
        .HasColumnType("smallint");

    cc.Property(c => c.DueDay)
        .HasColumnName("due_day")
        .HasColumnType("smallint");

    cc.Property(c => c.DebitAccountId)
        .HasColumnName("debit_account_id")
        .HasColumnType("uuid");

    cc.Property(c => c.EnforceCreditLimit)
        .HasColumnName("enforce_credit_limit")
        .HasColumnType("boolean")
        .HasDefaultValue(true);

    // FK para conta de débito vinculada — RESTRICT impede remoção
    cc.HasOne<Account>()
        .WithMany()
        .HasForeignKey(c => c.DebitAccountId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

### Schema SQL esperado

```sql
CREATE TABLE credit_card_details (
    account_id              UUID        NOT NULL PRIMARY KEY
        REFERENCES accounts(id) ON DELETE CASCADE,
    credit_limit            NUMERIC(18,2) NOT NULL,
    closing_day             SMALLINT    NOT NULL,
    due_day                 SMALLINT    NOT NULL,
    debit_account_id        UUID        NOT NULL
        REFERENCES accounts(id) ON DELETE RESTRICT,
    enforce_credit_limit    BOOLEAN     NOT NULL DEFAULT TRUE
);

-- Índice para performance de queries de fatura
CREATE INDEX IF NOT EXISTS idx_transactions_account_competence_status
    ON transactions (account_id, competence_date, status);
```

### Observações

- **EF Core Owned Entity**: O EF Core carrega `CreditCardDetails` automaticamente junto com `Account` (owned entities são sempre incluídas). Nenhum `.Include()` manual é necessário.
- **Contas existentes**: Contas Corrente, Investimento e Carteira não terão linha na tabela `credit_card_details`. A propriedade `Account.CreditCard` será `null` para essas contas.
- **`ON DELETE CASCADE`** em `account_id`: Se a conta cartão for removida, o registro em `credit_card_details` é removido junto.
- **`ON DELETE RESTRICT`** em `debit_account_id`: Impede desativar/remover a conta de débito vinculada enquanto houver cartão apontando para ela.
- **Não alterar** `Program.cs`, `DbContext`, DI ou qualquer outro arquivo — esta tarefa é exclusivamente configuração EF + migration.

## Critérios de Sucesso

- Migration criada com sucesso (aditiva — somente ADD TABLE)
- Tabela `accounts` permanece inalterada (zero ALTER)
- Tabela `credit_card_details` criada com colunas corretas (tipos, nullability, defaults)
- FK `account_id` → `accounts(id)` com `ON DELETE CASCADE`
- FK `debit_account_id` → `accounts(id)` com `ON DELETE RESTRICT`
- Índice `idx_transactions_account_competence_status` existe
- Migration aplica sem erros em banco PostgreSQL
- EF Core carrega `Account` com `CreditCard` automaticamente (sem `.Include()`)
- `Account.CreditCard` é `null` para contas sem registro em `credit_card_details`
- Build compila sem erros
```
