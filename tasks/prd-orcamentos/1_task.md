```markdown
---
status: done
parallelizable: false
blocked_by: []
---

<task_context>
<domain>domain/entidade</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"2.0", "3.0", "4.0"</unblocks>
</task_context>

# Tarefa 1.0: Domain Layer — Entidade Budget, Interfaces, Domain Service e Exceptions

## Visão Geral

Criar toda a camada de domínio da feature de Orçamentos: entidade `Budget` com factory methods e validações internas, interface `IBudgetRepository` com queries específicas, `BudgetDomainService` para validação de regras de negócio cross-entity (teto de 100%, unicidade de categoria por mês, restrição de período), e 7 domain exceptions fortemente tipadas. Esta é a tarefa fundacional — sem dependências externas e totalmente testável unitariamente.

## Requisitos

- PRD F1 req 1: Orçamento com nome, percentual, mês de referência, categorias associadas (mínimo 1)
- PRD F1 req 3: Valor limite calculado: `renda × (percentual / 100)`
- PRD F1 req 4: Soma dos percentuais ≤ 100% por mês
- PRD F1 req 5: Mês de referência = corrente ou futuro
- PRD F1 req 6: Apenas categorias tipo Despesa
- PRD F1 req 7: Categoria única por orçamento por mês
- PRD F1 req 8: Mínimo 1 categoria obrigatória
- PRD F5 req 33: Flag `IsRecurrent`
- Techspec: Entidade `Budget` herdando de `BaseEntity` com `Create`, `Restore`, `Update`, `CalculateLimit`
- Techspec: `IBudgetRepository` com 12 métodos de query específicos
- Techspec: `BudgetDomainService` com 3 métodos de validação
- Techspec: 7 domain exceptions herdando de `DomainException`
- `rules/dotnet-architecture.md`: Domain sem dependências externas
- `rules/dotnet-coding-standards.md`: Código em inglês, PascalCase

## Subtarefas

### Entidade Budget

- [x] 1.1 Criar `Budget` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Entity/Budget.cs`:
  - Herdar de `BaseEntity`
  - Propriedades: `Name` (string), `Percentage` (decimal), `ReferenceYear` (int), `ReferenceMonth` (int), `IsRecurrent` (bool)
  - Campo privado `_categoryIds` (List<Guid>) com propriedade pública `CategoryIds` (IReadOnlyList<Guid>)
  - Construtor `protected Budget() { }` para EF Core
  - Factory method estático `Create(name, percentage, referenceYear, referenceMonth, categoryIds, isRecurrent, userId)`:
    - Validar: `name` não vazio e entre 2-150 chars
    - Validar: `percentage > 0` e `percentage <= 100`
    - Validar: `referenceMonth` entre 1-12
    - Validar: `referenceYear` razoável (> 2000)
    - Validar: `categoryIds` não vazio (mínimo 1)
    - Setar `CreatedBy` e `CreatedAt`
  - Factory method estático `Restore(id, name, percentage, referenceYear, referenceMonth, categoryIds, isRecurrent, createdBy, createdAt, updatedBy, updatedAt)` — para reconstituição do EF Core
  - Método `Update(name, percentage, categoryIds, isRecurrent, userId)`:
    - Mesmas validações do `Create`
    - Setar `UpdatedBy` e `UpdatedAt`
  - Método `CalculateLimit(decimal monthlyIncome)` → `monthlyIncome * (Percentage / 100m)`

### Domain Exceptions

- [x] 1.2 Criar 7 exceptions em `3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/`:
  - `BudgetNotFoundException` — orçamento não encontrado (recebe budgetId)
  - `BudgetPercentageExceededException` — percentual excede 100% no mês (recebe percentage, available, month/year)
  - `CategoryAlreadyBudgetedException` — categoria já vinculada a outro orçamento no mês (recebe categoryId, budgetName, month/year)
  - `BudgetPeriodLockedException` — tentativa de editar/excluir orçamento de mês passado (recebe budgetId, month/year)
  - `BudgetMustHaveCategoriesException` — orçamento sem categorias (recebe budgetId ou name)
  - `BudgetNameAlreadyExistsException` — nome de orçamento já existe (recebe name)
  - `InvalidBudgetCategoryTypeException` — categoria não é do tipo Despesa (recebe categoryId)
  - Todas herdam de `DomainException` (padrão existente)
  - Mensagens descritivas em português

### Interface IBudgetRepository

- [x] 1.3 Criar `IBudgetRepository` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Interface/IBudgetRepository.cs`:
  - Herdar de `IRepository<Budget>`
  - `GetByMonthAsync(int year, int month, CancellationToken)` → `IReadOnlyList<Budget>`
  - `GetByIdWithCategoriesAsync(Guid id, CancellationToken)` → `Budget?`
  - `GetTotalPercentageForMonthAsync(int year, int month, Guid? excludeBudgetId, CancellationToken)` → `decimal`
  - `IsCategoryUsedInMonthAsync(Guid categoryId, int year, int month, Guid? excludeBudgetId, CancellationToken)` → `bool`
  - `GetUsedCategoryIdsForMonthAsync(int year, int month, Guid? excludeBudgetId, CancellationToken)` → `IReadOnlyList<Guid>`
  - `GetMonthlyIncomeAsync(int year, int month, CancellationToken)` → `decimal`
  - `GetConsumedAmountAsync(IReadOnlyList<Guid> categoryIds, int year, int month, CancellationToken)` → `decimal`
  - `GetUnbudgetedExpensesAsync(int year, int month, CancellationToken)` → `decimal`
  - `GetRecurrentBudgetsForMonthAsync(int year, int month, CancellationToken)` → `IReadOnlyList<Budget>`
  - `ExistsByNameAsync(string name, Guid? excludeBudgetId, CancellationToken)` → `bool`
  - `RemoveCategoryFromBudgetsAsync(Guid categoryId, CancellationToken)` → `Task`
  - `Remove(Budget budget)` → `void`

### BudgetDomainService

- [x] 1.4 Criar `BudgetDomainService` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Service/BudgetDomainService.cs`:
  - `ValidatePercentageCapAsync(IBudgetRepository repo, int year, int month, decimal newPercentage, Guid? excludeBudgetId, CancellationToken)`:
    - Buscar soma de percentuais do mês via repo
    - Se soma + newPercentage > 100 → lançar `BudgetPercentageExceededException`
  - `ValidateCategoryUniquenessAsync(IBudgetRepository repo, IReadOnlyList<Guid> categoryIds, int year, int month, Guid? excludeBudgetId, CancellationToken)`:
    - Para cada categoryId, verificar se já está em uso no mês via repo
    - Se duplicada → lançar `CategoryAlreadyBudgetedException`
  - `ValidateReferenceMonth(int year, int month)`:
    - Se mês/ano é anterior ao mês corrente → lançar `BudgetPeriodLockedException`
    - Mês corrente e futuros são aceitos

### Testes Unitários

- [x] 1.5 Criar testes para `Budget` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Entity/BudgetTests.cs`:
  - `Create_WithValidParameters_ShouldReturnInstance`
  - `Create_WithEmptyName_ShouldThrow`
  - `Create_WithNameTooShort_ShouldThrow`
  - `Create_WithNameTooLong_ShouldThrow`
  - `Create_WithZeroPercentage_ShouldThrow`
  - `Create_WithNegativePercentage_ShouldThrow`
  - `Create_WithPercentageOver100_ShouldThrow`
  - `Create_WithInvalidMonth_ShouldThrow`
  - `Create_WithEmptyCategoryIds_ShouldThrow`
  - `Create_ShouldSetCreatedByAndCreatedAt`
  - `Update_WithValidParameters_ShouldUpdateAllFields`
  - `Update_ShouldSetUpdatedByAndUpdatedAt`
  - `CalculateLimit_ShouldReturnCorrectValue`
  - `CalculateLimit_WithZeroIncome_ShouldReturnZero`

- [x] 1.6 Criar testes para `BudgetDomainService` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Service/BudgetDomainServiceTests.cs`:
  - `ValidatePercentageCap_WhenWithinLimit_ShouldNotThrow`
  - `ValidatePercentageCap_WhenExactly100_ShouldNotThrow`
  - `ValidatePercentageCap_WhenExceeds100_ShouldThrowBudgetPercentageExceededException`
  - `ValidatePercentageCap_WithExcludeBudgetId_ShouldExcludeFromSum`
  - `ValidateCategoryUniqueness_WhenNoDuplicate_ShouldNotThrow`
  - `ValidateCategoryUniqueness_WhenDuplicate_ShouldThrowCategoryAlreadyBudgetedException`
  - `ValidateReferenceMonth_WithCurrentMonth_ShouldNotThrow`
  - `ValidateReferenceMonth_WithFutureMonth_ShouldNotThrow`
  - `ValidateReferenceMonth_WithPastMonth_ShouldThrowBudgetPeriodLockedException`
  - Usar NSubstitute para mockar `IBudgetRepository`

- [x] 1.7 Criar testes para domain exceptions em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Exception/`:
  - Verificar que cada exception herda de `DomainException`
  - Verificar que mensagens contêm dados relevantes

### Validação

- [x] 1.8 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: Nenhum (primeira tarefa — fundação do domínio)
- Desbloqueia: 2.0 (Infra/Persistência), 3.0 (Commands), 4.0 (Queries)
- Paralelizável: Não (é a tarefa fundacional)

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/3-Domain/GestorFinanceiro.Financeiro.Domain/
├── Entity/
│   └── Budget.cs                              ← NOVO
├── Exception/
│   ├── BudgetNotFoundException.cs             ← NOVO
│   ├── BudgetPercentageExceededException.cs   ← NOVO
│   ├── CategoryAlreadyBudgetedException.cs    ← NOVO
│   ├── BudgetPeriodLockedException.cs         ← NOVO
│   ├── BudgetMustHaveCategoriesException.cs   ← NOVO
│   ├── BudgetNameAlreadyExistsException.cs    ← NOVO
│   └── InvalidBudgetCategoryTypeException.cs  ← NOVO
├── Interface/
│   └── IBudgetRepository.cs                   ← NOVO
└── Service/
    └── BudgetDomainService.cs                 ← NOVO

backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/
├── Domain/Entity/
│   └── BudgetTests.cs                         ← NOVO
├── Domain/Service/
│   └── BudgetDomainServiceTests.cs            ← NOVO
└── Domain/Exception/
    └── BudgetExceptionsTests.cs               ← NOVO
```

### Padrões a Seguir

- Seguir mesmo padrão de `Account.cs` para factory methods e encapsulamento
- Seguir mesmo padrão de `DomainException` existentes (ex: `InsufficientBalanceException`)
- `BudgetDomainService` segue padrão de `CreditCardDomainService` (métodos estáticos ou instância sem estado)
- Testes seguem padrão AAA (Arrange, Act, Assert) com xUnit + NSubstitute

## Critérios de Sucesso

- Entidade `Budget` compila sem dependências externas ao Domain
- Todos os factory methods validam parâmetros e lançam exceções apropriadas
- `CalculateLimit` retorna `monthlyIncome × (percentage / 100)`
- `BudgetDomainService` valida teto de 100% e unicidade de categoria usando mocks do repositório
- `ValidateReferenceMonth` rejeita meses passados e aceita corrente/futuros
- 7 domain exceptions criadas e herdando de `DomainException`
- Interface `IBudgetRepository` define 12 métodos de query com assinaturas corretas
- Todos os testes unitários passam
- Build do backend compila sem erros
```
