```markdown
---
status: done
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>application/commands</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"5.0"</unblocks>
</task_context>

# Tarefa 3.0: Application Layer — Commands (Create, Update, Delete) e Validators

## Visão Geral

Implementar os 3 commands da feature de Orçamentos seguindo o padrão CQRS simplificado do projeto: `CreateBudgetCommand`, `UpdateBudgetCommand` e `DeleteBudgetCommand`, cada um com seu handler correspondente. Os commands de criação e edição incluem validators FluentValidation. Os handlers orquestram validações de domínio (teto de 100%, unicidade de categoria, período), persistência via `IUnitOfWork` e auditoria via `IAuditService`. Todos os handlers são testáveis unitariamente com mocks (NSubstitute).

## Requisitos

- PRD F1 req 1: Criar orçamento com nome, percentual, mês, categorias
- PRD F1 req 4: Soma dos percentuais ≤ 100% — bloquear criação/edição
- PRD F1 req 5: Mês corrente ou futuro para criação
- PRD F1 req 6: Apenas categorias tipo Despesa
- PRD F1 req 7: Categoria única por mês
- PRD F1 req 8: Mínimo 1 categoria
- PRD F1 req 10: Editar nome, percentual, categorias (mês corrente ou futuro)
- PRD F1 req 11: Excluir orçamento (mês corrente ou futuro)
- PRD F1 req 13: Auditoria (CreatedBy, UpdatedBy)
- Techspec: Nome único globalmente (`ExistsByNameAsync`)
- Techspec: Validators com FluentValidation
- `rules/dotnet-architecture.md`: CQRS com `ICommand<T>`, handlers via `IDispatcher`

## Subtarefas

### CreateBudgetCommand

- [x] 3.1 Criar `CreateBudgetCommand` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/CreateBudgetCommand.cs`:
  - Implementar `ICommand<BudgetResponse>`
  - Propriedades: `Name`, `Percentage`, `ReferenceYear`, `ReferenceMonth`, `CategoryIds` (List<Guid>), `IsRecurrent`

- [x] 3.2 Criar `CreateBudgetCommandHandler` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/CreateBudgetCommandHandler.cs`:
  - Implementar `ICommandHandler<CreateBudgetCommand, BudgetResponse>`
  - Dependências via DI: `IBudgetRepository`, `ICategoryRepository`, `IUnitOfWork`, `IAuditService`, `BudgetDomainService`
  - Fluxo:
    1. Verificar se nome já existe via `IBudgetRepository.ExistsByNameAsync()` → `BudgetNameAlreadyExistsException`
    2. Validar categorias existem e são tipo Despesa via `ICategoryRepository` → `InvalidBudgetCategoryTypeException`
    3. Chamar `BudgetDomainService.ValidateReferenceMonth()` → `BudgetPeriodLockedException`
    4. Chamar `BudgetDomainService.ValidatePercentageCapAsync()` → `BudgetPercentageExceededException`
    5. Chamar `BudgetDomainService.ValidateCategoryUniquenessAsync()` → `CategoryAlreadyBudgetedException`
    6. Criar entidade via `Budget.Create()`
    7. Persistir via `IBudgetRepository.AddAsync()` + `IUnitOfWork.SaveChangesAsync()`
    8. Registrar auditoria via `IAuditService`
    9. Buscar renda e consumido para montar response
    10. Retornar `BudgetResponse` completo

- [x] 3.3 Criar `CreateBudgetValidator` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/CreateBudgetValidator.cs`:
  - Implementar `AbstractValidator<CreateBudgetCommand>` (FluentValidation)
  - Regras:
    - `Name`: NotEmpty, MinLength(2), MaxLength(150)
    - `Percentage`: GreaterThan(0), LessThanOrEqualTo(100)
    - `ReferenceYear`: GreaterThan(2000)
    - `ReferenceMonth`: InclusiveBetween(1, 12)
    - `CategoryIds`: NotEmpty (mínimo 1 item)

### UpdateBudgetCommand

- [x] 3.4 Criar `UpdateBudgetCommand` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/UpdateBudgetCommand.cs`:
  - Implementar `ICommand<BudgetResponse>`
  - Propriedades: `Id` (Guid), `Name`, `Percentage`, `CategoryIds` (List<Guid>), `IsRecurrent`

- [x] 3.5 Criar `UpdateBudgetCommandHandler` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/UpdateBudgetCommandHandler.cs`:
  - Implementar `ICommandHandler<UpdateBudgetCommand, BudgetResponse>`
  - Fluxo:
    1. Buscar budget via `IBudgetRepository.GetByIdWithCategoriesAsync()` → `BudgetNotFoundException`
    2. Validar período (mês corrente ou futuro) via `BudgetDomainService.ValidateReferenceMonth()`
    3. Verificar se novo nome já existe (excluindo budget atual) via `ExistsByNameAsync()` → `BudgetNameAlreadyExistsException`
    4. Validar categorias existem e são tipo Despesa
    5. Validar teto de 100% excluindo budget atual via `ValidatePercentageCapAsync(excludeBudgetId)`
    6. Validar unicidade de categorias excluindo budget atual via `ValidateCategoryUniquenessAsync(excludeBudgetId)`
    7. Atualizar entidade via `Budget.Update()`
    8. Persistir + auditoria
    9. Retornar `BudgetResponse` atualizado

- [x] 3.6 Criar `UpdateBudgetValidator` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/UpdateBudgetValidator.cs`:
  - Mesmas regras do `CreateBudgetValidator`
  - Adicionar: `Id` NotEmpty

### DeleteBudgetCommand

- [x] 3.7 Criar `DeleteBudgetCommand` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/DeleteBudgetCommand.cs`:
  - Implementar `ICommand<Unit>` (ou void equivalent)
  - Propriedade: `Id` (Guid)

- [x] 3.8 Criar `DeleteBudgetCommandHandler` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Budget/DeleteBudgetCommandHandler.cs`:
  - Fluxo:
    1. Buscar budget → `BudgetNotFoundException`
    2. Validar período (mês corrente ou futuro) → `BudgetPeriodLockedException`
    3. Remover via `IBudgetRepository.Remove()`
    4. `IUnitOfWork.SaveChangesAsync()`
    5. Registrar auditoria

### Registro DI

- [x] 3.9 Registrar handlers e validators em `ApplicationServiceExtensions`:
  - `CreateBudgetCommandHandler`, `UpdateBudgetCommandHandler`, `DeleteBudgetCommandHandler`
  - `CreateBudgetValidator`, `UpdateBudgetValidator`
  - `BudgetDomainService`

### Testes Unitários

- [x] 3.10 Criar testes para `CreateBudgetCommandHandler` em `5-Tests/.../UnitTests/Application/Commands/Budget/CreateBudgetCommandHandlerTests.cs`:
  - `Handle_WithValidCommand_ShouldCreateBudgetAndReturnResponse`
  - `Handle_WhenNameAlreadyExists_ShouldThrowBudgetNameAlreadyExistsException`
  - `Handle_WhenCategoryNotExpense_ShouldThrowInvalidBudgetCategoryTypeException`
  - `Handle_WhenPercentageExceeds100_ShouldThrowBudgetPercentageExceededException`
  - `Handle_WhenCategoryAlreadyBudgeted_ShouldThrowCategoryAlreadyBudgetedException`
  - `Handle_WhenPastMonth_ShouldThrowBudgetPeriodLockedException`
  - `Handle_ShouldCallAuditService`

- [x] 3.11 Criar testes para `UpdateBudgetCommandHandler` em `5-Tests/.../UnitTests/Application/Commands/Budget/UpdateBudgetCommandHandlerTests.cs`:
  - `Handle_WithValidCommand_ShouldUpdateBudgetAndReturnResponse`
  - `Handle_WhenBudgetNotFound_ShouldThrowBudgetNotFoundException`
  - `Handle_WhenPastMonth_ShouldThrowBudgetPeriodLockedException`
  - `Handle_WhenNewNameAlreadyExists_ShouldThrowBudgetNameAlreadyExistsException`
  - `Handle_WhenPercentageExceeds100_ShouldExcludeCurrentBudgetFromSum`

- [x] 3.12 Criar testes para `DeleteBudgetCommandHandler` em `5-Tests/.../UnitTests/Application/Commands/Budget/DeleteBudgetCommandHandlerTests.cs`:
  - `Handle_WithValidId_ShouldDeleteBudget`
  - `Handle_WhenBudgetNotFound_ShouldThrowBudgetNotFoundException`
  - `Handle_WhenPastMonth_ShouldThrowBudgetPeriodLockedException`
  - `Handle_ShouldCallAuditService`

- [x] 3.13 Criar testes para validators em `5-Tests/.../UnitTests/Application/Commands/Budget/`:
  - `CreateBudgetValidatorTests`:
    - `Validate_WithValidCommand_ShouldPass`
    - `Validate_WithEmptyName_ShouldFail`
    - `Validate_WithNameTooLong_ShouldFail`
    - `Validate_WithZeroPercentage_ShouldFail`
    - `Validate_WithPercentageOver100_ShouldFail`
    - `Validate_WithInvalidMonth_ShouldFail`
    - `Validate_WithEmptyCategoryIds_ShouldFail`
  - `UpdateBudgetValidatorTests`: Mesmos cenários + `Validate_WithEmptyId_ShouldFail`

### Validação

- [x] 3.14 Validar build com `dotnet build` e rodar testes unitários

## Sequenciamento

- Bloqueado por: 1.0 (Domain Layer — entidade, interfaces, domain service necessários)
- Desbloqueia: 5.0 (API Layer — controller usa os handlers)
- Paralelizável: Sim com 2.0 (Infra) e 4.0 (Queries) — handlers usam mocks nos testes

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/2-Application/GestorFinanceiro.Financeiro.Application/
└── Commands/
    └── Budget/
        ├── CreateBudgetCommand.cs              ← NOVO
        ├── CreateBudgetCommandHandler.cs       ← NOVO
        ├── CreateBudgetValidator.cs            ← NOVO
        ├── UpdateBudgetCommand.cs              ← NOVO
        ├── UpdateBudgetCommandHandler.cs       ← NOVO
        ├── UpdateBudgetValidator.cs            ← NOVO
        ├── DeleteBudgetCommand.cs              ← NOVO
        └── DeleteBudgetCommandHandler.cs       ← NOVO

backend/2-Application/GestorFinanceiro.Financeiro.Application/
└── Common/
    └── ApplicationServiceExtensions.cs         ← MODIFICAR (add registros)

backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/
└── Application/Commands/Budget/
    ├── CreateBudgetCommandHandlerTests.cs      ← NOVO
    ├── UpdateBudgetCommandHandlerTests.cs      ← NOVO
    ├── DeleteBudgetCommandHandlerTests.cs      ← NOVO
    ├── CreateBudgetValidatorTests.cs           ← NOVO
    └── UpdateBudgetValidatorTests.cs           ← NOVO
```

### Padrões a Seguir

- Seguir padrão de `CreateTransactionCommandHandler` para fluxo de criação
- Seguir padrão de `CreateAccountValidator` para FluentValidation
- Handlers recebem dependências via construtor (DI)
- Usar `CancellationToken` em todas as operações async
- Registrar auditoria com action "Create"/"Update"/"Delete" e entity "Budget"

## Critérios de Sucesso

- 3 commands + 3 handlers + 2 validators criados e compilando
- `CreateBudgetCommandHandler` valida nome único, categorias tipo Despesa, teto 100%, unicidade de categoria, período
- `UpdateBudgetCommandHandler` exclui budget atual na validação de teto e unicidade
- `DeleteBudgetCommandHandler` valida existência e período
- Validators cobrem todos os campos obrigatórios e limites
- Todos os handlers registrados no DI via `ApplicationServiceExtensions`
- Todos os testes unitários passam com mocks NSubstitute
- Build compila sem erros
```
