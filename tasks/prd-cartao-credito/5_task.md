```markdown
---
status: pending
parallelizable: false
blocked_by: ["4.0"]
---

<task_context>
<domain>infra/repositórios+seed</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>4.0</dependencies>
<unblocks>"6.0", "7.0", "8.0"</unblocks>
</task_context>

# Tarefa 5.0: Extensão de Repositórios e Seed de Categoria

## Visão Geral

Estender os repositórios existentes com novos métodos para suportar a feature de cartão de crédito e criar o startup task que faz seed da categoria "Pagamento de Fatura" no banco de dados. Os novos métodos no repositório incluem busca de contas por tipo e busca de transações por conta e período.

## Requisitos

- Techspec: `IAccountRepository.GetActiveByTypeAsync(AccountType type, CancellationToken ct)` — listar contas ativas por tipo (para dropdown de conta de débito)
- Techspec: `ITransactionRepository.GetByAccountAndPeriodAsync(Guid accountId, DateTime start, DateTime end, CancellationToken ct)` — transações de um período de fatura
- Techspec: Seed de categoria "Pagamento de Fatura" com `IsSystem = true` e `CategoryType.Despesa` — não editável/deletável
- PRD F1 req 6: Conta de débito deve ser Corrente ou Carteira e ativa
- PRD F4 req 17: Fatura agrupa transações no período de fechamento
- `rules/dotnet-architecture.md`: Repositórios na camada Infra implementam interfaces do Domain

## Subtarefas

### Extensão de Interfaces (Domain)

- [ ] 5.1 Adicionar método `GetActiveByTypeAsync(AccountType type, CancellationToken ct)` em `IAccountRepository`:
  - Retorna `Task<IReadOnlyList<Account>>`
  - Filtra por `IsActive == true` e tipo informado

- [ ] 5.2 Adicionar método `GetByAccountAndPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken ct)` em `ITransactionRepository`:
  - Retorna `Task<IReadOnlyList<Transaction>>`
  - Filtra por `AccountId`, `CompetenceDate` entre `startDate` e `endDate`, `Status == Paid`

### Implementação de Repositórios (Infra)

- [ ] 5.3 Implementar `GetActiveByTypeAsync` em `AccountRepository`:
  ```csharp
  public async Task<IReadOnlyList<Account>> GetActiveByTypeAsync(
      AccountType type, CancellationToken ct)
  {
      return await DbSet
          .Where(a => a.Type == type && a.IsActive)
          .OrderBy(a => a.Name)
          .ToListAsync(ct);
  }
  ```

- [ ] 5.4 Implementar `GetByAccountAndPeriodAsync` em `TransactionRepository`:
  ```csharp
  public async Task<IReadOnlyList<Transaction>> GetByAccountAndPeriodAsync(
      Guid accountId, DateTime startDate, DateTime endDate, CancellationToken ct)
  {
      return await DbSet
          .Where(t => t.AccountId == accountId
              && t.CompetenceDate > startDate
              && t.CompetenceDate <= endDate
              && t.Status == TransactionStatus.Paid)
          .OrderBy(t => t.CompetenceDate)
          .ThenBy(t => t.CreatedAt)
          .ToListAsync(ct);
  }
  ```

### Seed de Categoria

- [ ] 5.5 Criar `SeedInvoicePaymentCategoryStartupTask` em `4-Infra/GestorFinanceiro.Financeiro.Infra/StartupTasks/SeedInvoicePaymentCategoryStartupTask.cs`:
  - Implementa `IStartupTask` (padrão existente)
  - Verifica se já existe categoria com nome "Pagamento de Fatura" e `IsSystem == true`
  - Se não existir, cria via `Category.Create("Pagamento de Fatura", CategoryType.Despesa, "system")`
  - Define `IsSystem = true` na entidade (via reflection ou método dedicado se existir)
  - Persiste via `ICategoryRepository` + `IUnitOfWork`

- [ ] 5.6 Registrar `SeedInvoicePaymentCategoryStartupTask` na DI:
  - Em `ServiceCollectionExtensions` ou onde os `IStartupTask` são registrados
  - Garantir que executa após o seed do admin user (ordem de registro)

### Testes Unitários

- [ ] 5.7 Testes de integração para os novos métodos de repositório (se o projeto tiver suporte a Testcontainers):
  - `GetActiveByTypeAsync_WithMatchingType_ShouldReturnActiveAccounts`
  - `GetActiveByTypeAsync_WithInactiveAccounts_ShouldExclude`
  - `GetByAccountAndPeriodAsync_WithTransactionsInPeriod_ShouldReturnFiltered`
  - `GetByAccountAndPeriodAsync_WithTransactionsOutsidePeriod_ShouldExclude`
  - `GetByAccountAndPeriodAsync_ShouldOnlyReturnPaidTransactions`

### Validação

- [ ] 5.8 Validar build com `dotnet build` a partir de `backend/`
- [ ] 5.9 Executar testes com `dotnet test`
- [ ] 5.10 Verificar que o seed cria a categoria no banco de desenvolvimento

## Sequenciamento

- Bloqueado por: 4.0 (Migration deve existir antes de queries e seeds)
- Desbloqueia: 6.0 (Commands usam repositórios estendidos), 7.0 (GetInvoiceQuery usa GetByAccountAndPeriodAsync), 8.0 (PayInvoice usa categoria seed e TransferDomainService)
- Paralelizável: Não

## Detalhes de Implementação

### Padrão de Seed (conforme SeedAdminUserStartupTask existente)

```csharp
public class SeedInvoicePaymentCategoryStartupTask : IStartupTask
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SeedInvoicePaymentCategoryStartupTask> _logger;

    public SeedInvoicePaymentCategoryStartupTask(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<SeedInvoicePaymentCategoryStartupTask> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Verificar se já existe
        var existing = await _categoryRepository.GetByNameAsync("Pagamento de Fatura", ct);
        if (existing != null)
        {
            _logger.LogInformation("Categoria 'Pagamento de Fatura' já existe. Seed ignorado.");
            return;
        }

        var category = Category.Create("Pagamento de Fatura", CategoryType.Despesa, "system");
        // Marcar como sistema (não editável/deletável)
        // Verificar se Category tem método SetIsSystem ou se precisa ser via reflection
        
        await _categoryRepository.AddAsync(category, ct);
        await _unitOfWork.CommitAsync(ct);
        
        _logger.LogInformation("Categoria 'Pagamento de Fatura' criada via seed.");
    }
}
```

### Observações

- **Atenção** ao marcar `IsSystem = true`: verificar se `Category` possui um método público para isso ou se é necessário ajustar a entidade. Se `Category.Create` não aceitar `isSystem`, pode ser necessário adicionar um overload ou um método `MarkAsSystem()`.
- **`GetByAccountAndPeriodAsync`**: usa `CompetenceDate > startDate && CompetenceDate <= endDate` — start é exclusivo (dia de fechamento do mês anterior + 1), end é inclusivo (dia de fechamento do mês atual). Confirmar com a implementação de `CreditCardDomainService.CalculateInvoicePeriod`.
- **`GetActiveByTypeAsync`**: será usado para popular dropdown de "conta de débito" no frontend — filtra apenas contas ativas do tipo informado (Corrente ou Carteira).

## Critérios de Sucesso

- `IAccountRepository` e `ITransactionRepository` possuem os novos métodos
- `AccountRepository.GetActiveByTypeAsync` retorna apenas contas ativas do tipo solicitado
- `TransactionRepository.GetByAccountAndPeriodAsync` retorna transações `Paid` no período correto
- Categoria "Pagamento de Fatura" é criada pelo seed com `IsSystem = true` e `CategoryType.Despesa`
- Seed é idempotente — executar múltiplas vezes não duplica a categoria
- Startup task registrado na DI e executado na inicialização
- Build compila sem erros
- Testes passam
```
