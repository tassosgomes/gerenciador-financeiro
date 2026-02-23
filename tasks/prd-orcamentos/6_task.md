```markdown
---
status: done
parallelizable: true
blocked_by: ["2.0"]
---

<task_context>
<domain>application/integração</domain>
<type>integration</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"8.0"</unblocks>
</task_context>

# Tarefa 6.0: Impacto em Category — Desassociação de Categorias em Orçamentos

## Visão Geral

Alterar os componentes existentes de Category para integrarem com a nova feature de Orçamentos. Quando uma categoria é excluída ou migrada, ela deve ser automaticamente desassociada de todos os orçamentos que a utilizam. Isso envolve alterações em `CategoryRepository.HasLinkedDataAsync()`, `CategoryRepository.MigrateLinkedDataAsync()` e `DeleteCategoryCommandHandler`. Se um orçamento ficar sem categorias após a desassociação, o sistema deve logar warning (o orçamento permanece no banco, mas é sinalizado no front como "sem categorias").

## Requisitos

- PRD F1 req 9: Ao excluir/migrar categoria vinculada a orçamento, desassociar automaticamente. Se orçamento ficar sem categorias, sinalizar ao usuário
- Techspec "Impacto na Exclusão/Migração de Categorias":
  - `ICategoryRepository.HasLinkedDataAsync()` — incluir check em `budget_categories`
  - `CategoryRepository.MigrateLinkedDataAsync()` — incluir `DELETE FROM budget_categories WHERE category_id = @sourceCategoryId`
  - `DeleteCategoryCommandHandler` — após migração/remoção, verificar se algum orçamento ficou sem categorias e logar warning
- Techspec: `IBudgetRepository.RemoveCategoryFromBudgetsAsync(Guid categoryId)` para remoção direta

## Subtarefas

### Alteração em CategoryRepository

- [x] 6.1 Modificar `CategoryRepository.HasLinkedDataAsync()`:
  - Em `4-Infra/GestorFinanceiro.Financeiro.Infra/Repository/CategoryRepository.cs`
  - Adicionar check em `budget_categories`:
    ```sql
    SELECT EXISTS(SELECT 1 FROM budget_categories WHERE category_id = @categoryId)
    ```
  - O método deve retornar `true` se a categoria estiver vinculada a transações OU a orçamentos

- [x] 6.2 Modificar `CategoryRepository.MigrateLinkedDataAsync()`:
  - Adicionar `DELETE FROM budget_categories WHERE category_id = @sourceCategoryId` ao SQL de migração
  - A desassociação é uma remoção simples (não migra para outra categoria — orçamentos ficam sem a categoria)
  - Nota: A desnormalização de `reference_year/month` em `budget_categories` é tratada pelo `ON DELETE CASCADE`; mas como a migração usa SQL direto, deve incluir o DELETE explícito

### Alteração em DeleteCategoryCommandHandler

- [x] 6.3 Modificar `DeleteCategoryCommandHandler`:
  - Em `2-Application/.../Commands/Category/DeleteCategoryCommandHandler.cs`
  - Após a migração/remoção, chamar `IBudgetRepository.RemoveCategoryFromBudgetsAsync(categoryId)` se necessário
  - Ou confiar no SQL de migração que já remove de `budget_categories`
  - Verificar se algum orçamento ficou sem categorias:
    - Buscar orçamentos que tinham a categoria
    - Para cada orçamento que agora tem 0 categorias, logar `LogWarning("Orçamento '{BudgetName}' ficou sem categorias após remoção da categoria {CategoryId}")`
  - Nota: O handler não precisa impedir a exclusão — apenas logar warning

### Adição de Dependência

- [x] 6.4 Se necessário, adicionar `IBudgetRepository` como dependência do `DeleteCategoryCommandHandler`:
  - Registrar no construtor
  - Usar para consultar orçamentos afetados após a desassociação

### Testes Unitários

- [x] 6.5 Atualizar testes de `DeleteCategoryCommandHandler`:
  - Em `5-Tests/.../UnitTests/Application/Commands/Category/DeleteCategoryCommandHandlerTests.cs`
  - Adicionar cenários:
    - `Handle_WhenCategoryLinkedToBudget_ShouldDesassociate`
    - `Handle_WhenCategoryLinkedToBudget_AndBudgetBecomesEmpty_ShouldLogWarning`
    - `Handle_WhenCategoryNotLinkedToBudget_ShouldNotCallRemove`

- [x] 6.6 Atualizar testes de `CategoryRepository` (se existirem):
  - Verificar que `HasLinkedDataAsync` retorna `true` quando categoria está em `budget_categories`
  - Verificar que `MigrateLinkedDataAsync` remove de `budget_categories`

### Validação

- [x] 6.7 Testar cenário end-to-end:
  - Criar orçamento com 2 categorias
  - Excluir uma das categorias → orçamento deve ficar com 1 categoria
  - Excluir a última categoria → orçamento deve ficar com 0 categorias + warning no log

- [x] 6.8 Validar build e rodar testes unitários

## Sequenciamento

- Bloqueado por: 2.0 (Infra — tabela `budget_categories` deve existir)
- Desbloqueia: 8.0 (Testes de Integração Backend)
- Paralelizável: Sim com 3.0, 4.0, 5.0 (alterações em Category são independentes do CRUD de Budget)

## Detalhes de Implementação

### Arquivos Modificados

```
backend/4-Infra/GestorFinanceiro.Financeiro.Infra/
└── Repository/
    └── CategoryRepository.cs                   ← MODIFICAR (HasLinkedData, MigrateLinkedData)

backend/2-Application/GestorFinanceiro.Financeiro.Application/
└── Commands/
    └── Category/
        └── DeleteCategoryCommandHandler.cs     ← MODIFICAR (add budget check)

backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/
└── Application/Commands/Category/
    └── DeleteCategoryCommandHandlerTests.cs    ← MODIFICAR (add budget scenarios)
```

### SQL de Migração Atualizado

```sql
-- Em MigrateLinkedDataAsync, adicionar antes do DELETE de categorias:
DELETE FROM budget_categories WHERE category_id = @sourceCategoryId;
```

### Padrões a Seguir

- Seguir padrão existente de `MigrateLinkedDataAsync` para SQL de migração
- Usar `ILogger<T>.LogWarning()` para logging de orçamentos sem categorias
- Manter compatibilidade com fluxo existente de exclusão de categorias

## Critérios de Sucesso

- `HasLinkedDataAsync` detecta categorias vinculadas a orçamentos
- `MigrateLinkedDataAsync` remove categoria de `budget_categories`
- `DeleteCategoryCommandHandler` loga warning quando orçamento fica sem categorias
- Exclusão de categoria não é bloqueada por vínculo com orçamento (apenas desassocia)
- Testes unitários atualizados passam
- Testes existentes de Category continuam passando (sem regressão)
- Build compila sem erros
```
