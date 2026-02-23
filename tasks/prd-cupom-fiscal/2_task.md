---
status: done
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>backend/infra</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"4.0"</unblocks>
</task_context>

# Tarefa 2.0: Infraestrutura — EF Core, Repositórios e Migration

## Visão Geral

Implementar a camada de infraestrutura de persistência para as novas entidades `ReceiptItem` e `Establishment`. Isso inclui: configurações EF Core (Fluent API), DbSets no contexto, implementações concretas dos repositórios, geração da migration de banco de dados e registro dos repositórios no container de DI. Esta tarefa pode ser executada em paralelo com a Task 3.0 (SEFAZ).

## Requisitos

- Criar configurações EF Core (Fluent API) para `ReceiptItem` e `Establishment` seguindo o padrão snake_case do projeto
- Adicionar `DbSet<ReceiptItem>` e `DbSet<Establishment>` ao `FinanceiroDbContext`
- Implementar `ReceiptItemRepository` e `EstablishmentRepository` conforme interfaces da Task 1.0
- Gerar migration EF Core para criar as tabelas `receipt_items` e `establishments`
- Registrar os repositórios no DI via `ServiceCollectionExtensions`
- Configurar cascade delete via FK (`ON DELETE CASCADE`)
- Criar testes de integração dos repositórios com Testcontainers

## Subtarefas

- [x] 2.1 Criar `ReceiptItemConfiguration` em `Infra/Config/ReceiptItemConfiguration.cs`
  - Tabela: `receipt_items` (snake_case)
  - Colunas: `id` (PK), `transaction_id` (FK → transactions, ON DELETE CASCADE), `description` (varchar 500), `product_code` (varchar 100, nullable), `quantity` (numeric 18,4), `unit_of_measure` (varchar 20), `unit_price` (numeric 18,4), `total_price` (numeric 18,2), `item_order` (smallint)
  - Campos de auditoria: `created_by` (varchar 100), `created_at` (timestamptz), `updated_by` (varchar 100, nullable), `updated_at` (timestamptz, nullable)
  - Índice: `ix_receipt_items_transaction_id` em `transaction_id`

- [x] 2.2 Criar `EstablishmentConfiguration` em `Infra/Config/EstablishmentConfiguration.cs`
  - Tabela: `establishments` (snake_case)
  - Colunas: `id` (PK), `transaction_id` (FK → transactions, ON DELETE CASCADE, UNIQUE), `name` (varchar 300), `cnpj` (varchar 14), `access_key` (varchar 44, UNIQUE)
  - Campos de auditoria: `created_by` (varchar 100), `created_at` (timestamptz), `updated_by` (varchar 100, nullable), `updated_at` (timestamptz, nullable)
  - Índices: `ix_establishments_transaction_id` UNIQUE, `ix_establishments_access_key` UNIQUE

- [x] 2.3 Adicionar DbSets ao `FinanceiroDbContext`
  - `public DbSet<ReceiptItem> ReceiptItems { get; set; }`
  - `public DbSet<Establishment> Establishments { get; set; }`
  - Aplicar configs no `OnModelCreating` (se não detectadas automaticamente)

- [x] 2.4 Implementar `ReceiptItemRepository` em `Infra/Repository/ReceiptItemRepository.cs`
  - `AddRangeAsync(IEnumerable<ReceiptItem>, CancellationToken)` — usa `DbSet.AddRangeAsync`
  - `GetByTransactionIdAsync(Guid, CancellationToken)` — filtra por `TransactionId`, ordena por `ItemOrder`
  - `RemoveRange(IEnumerable<ReceiptItem>)` — usa `DbSet.RemoveRange`

- [x] 2.5 Implementar `EstablishmentRepository` em `Infra/Repository/EstablishmentRepository.cs`
  - `AddAsync(Establishment, CancellationToken)` — usa `DbSet.AddAsync`
  - `GetByTransactionIdAsync(Guid, CancellationToken)` — filtra por `TransactionId`
  - `Remove(Establishment)` — usa `DbSet.Remove`
  - `ExistsByAccessKeyAsync(string, CancellationToken)` — usa `AnyAsync` com filtro de `AccessKey`

- [x] 2.6 Gerar migration EF Core
  - Executar: `dotnet ef migrations add AddReceiptItemsAndEstablishments`
  - Verificar que a migration cria as tabelas corretas com todas as constraints
  - Verificar FK com cascade delete, índices UNIQUE

- [x] 2.7 Registrar repositórios no DI
  - Adicionar em `ServiceCollectionExtensions.AddInfrastructure()`:
    - `services.AddScoped<IReceiptItemRepository, ReceiptItemRepository>()`
    - `services.AddScoped<IEstablishmentRepository, EstablishmentRepository>()`

- [x] 2.8 Testes de integração (Testcontainers)
  - Testar `ReceiptItemRepository`: AddRange, GetByTransactionId (com ordenação), RemoveRange
  - Testar `EstablishmentRepository`: Add, GetByTransactionId, ExistsByAccessKey (true/false), Remove
  - Testar cascade delete: deletar Transaction e verificar que ReceiptItems e Establishment são removidos
  - Testar constraint UNIQUE de `access_key`: inserir dois Establishments com mesma chave → exceção

## Sequenciamento

- Bloqueado por: 1.0 (Entidades de Domínio e Interfaces)
- Desbloqueia: 4.0 (Commands e Queries)
- Paralelizável: Sim (pode ser executada em paralelo com a Task 3.0)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| `ReceiptItemConfiguration.cs` | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Config/` |
| `EstablishmentConfiguration.cs` | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Config/` |
| `ReceiptItemRepository.cs` | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Repository/` |
| `EstablishmentRepository.cs` | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Repository/` |
| Migration | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Migrations/` |
| DI Registration | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/DependencyInjection/ServiceCollectionExtensions.cs` |

### Padrões a Seguir

- Consultar configurações EF Core existentes em `Infra/Config/` (ex: `TransactionConfiguration`, `AccountConfiguration`) para manter consistência de estilo e convenções snake_case
- Consultar repositórios existentes em `Infra/Repository/` para padrão de implementação
- Consultar `FinanceiroDbContext` para padrão de adição de DbSets
- Consultar `ServiceCollectionExtensions` para padrão de registro de repositórios

### Esquema de Referência

**Tabela `receipt_items`:**
```sql
CREATE TABLE receipt_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id uuid NOT NULL REFERENCES transactions(id) ON DELETE CASCADE,
    description varchar(500) NOT NULL,
    product_code varchar(100),
    quantity numeric(18,4) NOT NULL,
    unit_of_measure varchar(20) NOT NULL,
    unit_price numeric(18,4) NOT NULL,
    total_price numeric(18,2) NOT NULL,
    item_order smallint NOT NULL,
    created_by varchar(100) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_by varchar(100),
    updated_at timestamptz
);
CREATE INDEX ix_receipt_items_transaction_id ON receipt_items(transaction_id);
```

**Tabela `establishments`:**
```sql
CREATE TABLE establishments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id uuid NOT NULL UNIQUE REFERENCES transactions(id) ON DELETE CASCADE,
    name varchar(300) NOT NULL,
    cnpj varchar(14) NOT NULL,
    access_key varchar(44) NOT NULL UNIQUE,
    created_by varchar(100) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_by varchar(100),
    updated_at timestamptz
);
CREATE UNIQUE INDEX ix_establishments_transaction_id ON establishments(transaction_id);
CREATE UNIQUE INDEX ix_establishments_access_key ON establishments(access_key);
```

## Critérios de Sucesso

- Migration gerada corretamente e aplicável sem erros
- Tabelas `receipt_items` e `establishments` criadas com todas as colunas, constraints e índices especificados
- Cascade delete funciona: deletar uma Transaction remove automaticamente seus ReceiptItems e Establishment
- Constraint UNIQUE em `access_key` impede duplicidade de cupons
- Constraint UNIQUE em `transaction_id` na tabela `establishments` garante relação 1:1
- Repositórios implementam corretamente todas as operações definidas nas interfaces
- Repositórios registrados no DI container
- Testes de integração passam com Testcontainers (PostgreSQL real)
- Todos os testes existentes continuam passando
- Projeto `GestorFinanceiro.Financeiro.Infra` compila sem erros
