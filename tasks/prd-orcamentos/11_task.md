```markdown
---
status: pending
parallelizable: true
blocked_by: ["10.0"]
---

<task_context>
<domain>frontend/testing</domain>
<type>testing</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>nenhuma</unblocks>
</task_context>

# Tarefa 11.0: Testes Frontend

## Visão Geral

Implementar testes frontend para a feature de Orçamentos usando Vitest + Testing Library + MSW. Criar MSW handlers para mockar a API de orçamentos nos testes e testar os componentes principais: `BudgetCard` (renderização de cores e badges), `BudgetForm` (validação e interação), `BudgetDashboard` (integração com dados mockados e filtro). Os testes garantem que a UI responde corretamente a diferentes estados de dados.

## Requisitos

- Techspec: Testes de `BudgetCard`, `BudgetForm`, `BudgetDashboard`, hooks React Query
- PRD F2: Validar visualmente cores, badges, ícones de alerta
- `rules/react-testing.md`: Vitest + Testing Library + MSW, testar comportamento não implementação

## Subtarefas

### MSW Handlers

- [ ] 11.1 Criar `handlers.ts` em `frontend/src/features/budgets/test/handlers.ts`:
  - Mock para `GET /api/v1/budgets/summary`:
    - Retornar `BudgetSummaryResponse` com dados de teste variados
  - Mock para `GET /api/v1/budgets`:
    - Retornar lista de `BudgetResponse`
  - Mock para `GET /api/v1/budgets/available-percentage`:
    - Retornar `AvailablePercentageResponse` com percentual disponível
  - Mock para `POST /api/v1/budgets`:
    - Retornar `BudgetResponse` criado
  - Mock para `PUT /api/v1/budgets/:id`:
    - Retornar `BudgetResponse` atualizado
  - Mock para `DELETE /api/v1/budgets/:id`:
    - Retornar 204
  - Criar dados de teste (fixtures):
    - Budget com consumo baixo (30% — verde)
    - Budget com consumo médio-alto (75% — amarelo)
    - Budget com consumo alto (92% — vermelho)
    - Budget estourado (115% — vermelho + badge)
    - Budget com alerta (82% — ícone de alerta)
    - Summary com dados consolidados

### Testes do BudgetCard

- [ ] 11.2 Criar `BudgetCard.test.tsx` em `frontend/src/features/budgets/test/BudgetCard.test.tsx`:
  - `should render budget name and percentage`
  - `should render categories as badges/chips`
  - `should render formatted values (limit, consumed, remaining)`
  - `should render green progress bar when consumed < 70%`
  - `should render yellow progress bar when consumed between 70-89%`
  - `should render red progress bar when consumed >= 90%`
  - `should render "Estourado" badge when consumed > 100%`
  - `should render alert icon when consumed >= 80% and <= 100%`
  - `should render "Recorrente" badge when isRecurrent is true`
  - `should render edit and delete buttons when not read-only`
  - `should not render edit and delete buttons when read-only`
  - `should call onEdit when edit button is clicked`
  - `should call onDelete when delete button is clicked`
  - `should have accessible aria-label on progress bar`

### Testes do BudgetForm

- [ ] 11.3 Criar `BudgetForm.test.tsx` em `frontend/src/features/budgets/test/BudgetForm.test.tsx`:
  - `should render all form fields`
  - `should show validation errors for empty name`
  - `should show validation errors for percentage out of range`
  - `should show validation errors when no categories selected`
  - `should show available percentage in real-time`
  - `should disable categories already used in another budget`
  - `should pre-fill fields when editing existing budget`
  - `should show reference month as read-only when editing`
  - `should call onSuccess after successful creation`
  - `should call onCancel when cancel button is clicked`
  - `should only show expense categories (not income categories)`

### Testes do BudgetDashboard

- [ ] 11.4 Criar `BudgetDashboard.test.tsx` em `frontend/src/features/budgets/test/BudgetDashboard.test.tsx`:
  - `should render summary header with consolidated data`
  - `should render budget cards for each budget`
  - `should render empty state when no budgets`
  - `should render loading skeleton while fetching`
  - `should change displayed data when month/year filter changes`
  - `should hide action buttons for past months (read-only mode)`
  - `should show "Novo Orçamento" button for current/future months`
  - `should show unbudgeted expenses in summary`

### Testes dos Hooks (opcional)

- [ ] 11.5 Criar testes para hooks React Query (se applicable):
  - `useBudgets` — que chama API com parâmetros corretos
  - `useCreateBudget` — que invalida queries após mutation
  - `useDeleteBudget` — que invalida queries após mutation

### Registrar Handlers no MSW

- [ ] 11.6 Registrar os handlers de Budget no setup global de MSW:
  - Em `frontend/src/shared/test/mocks/handlers.ts` ou importar no setup de teste

### Validação

- [ ] 11.7 Rodar todos os testes frontend:
  - `cd frontend && npm test`
  - Verificar que todos os novos testes passam
  - Verificar que testes existentes não quebraram

## Sequenciamento

- Bloqueado por: 10.0 (Frontend Componentes — componentes devem existir para serem testados)
- Desbloqueia: Nenhum (fase final de validação frontend)
- Paralelizável: Sim com 8.0 (Testes Backend são independentes)

## Detalhes de Implementação

### Estrutura de Arquivos

```
frontend/src/features/budgets/
└── test/
    ├── handlers.ts                ← NOVO (MSW handlers + fixtures)
    ├── BudgetCard.test.tsx        ← NOVO
    ├── BudgetForm.test.tsx        ← NOVO
    └── BudgetDashboard.test.tsx   ← NOVO

frontend/src/shared/test/mocks/
└── handlers.ts                    ← MODIFICAR (registrar budget handlers)
```

### Fixtures de Teste

```typescript
// Exemplos de fixtures
export const lowConsumptionBudget: BudgetResponse = {
  id: 'budget-1',
  name: 'Alimentação',
  percentage: 25,
  referenceYear: 2026,
  referenceMonth: 2,
  isRecurrent: false,
  monthlyIncome: 10000,
  limitAmount: 2500,
  consumedAmount: 750,
  remainingAmount: 1750,
  consumedPercentage: 30,
  categories: [{ id: 'cat-1', name: 'Supermercado' }, { id: 'cat-2', name: 'Restaurante' }],
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: null,
};

export const exceededBudget: BudgetResponse = {
  // ... consumedPercentage: 115, consumedAmount > limitAmount
};
```

### Padrões a Seguir

- Seguir padrão de `features/transactions/test/` para estrutura de testes
- Usar `render()` do Testing Library com providers necessários (QueryClientProvider)
- Usar `screen.getByRole()`, `screen.getByText()` para queries acessíveis
- Usar `userEvent` para simulação de interações
- Usar `waitFor` para assertions assíncronas (React Query)
- MSW handlers devem ser tipados e retornar dados realistas

### Wrapper de Teste

Criar um wrapper que configura os providers necessários:
```typescript
function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}
```

## Critérios de Sucesso

- MSW handlers cobrem todos os endpoints da API de budgets
- Testes de `BudgetCard` verificam todas as faixas de cor (verde, amarelo, vermelho)
- Testes de `BudgetCard` verificam badge "Estourado" e ícone de alerta
- Testes de `BudgetCard` verificam modo somente leitura
- Testes de `BudgetForm` verificam validação de campos (nome, percentual, categorias)
- Testes de `BudgetForm` verificam categorias desabilitadas
- Testes de `BudgetDashboard` verificam empty state, loading state e dados carregados
- Testes de `BudgetDashboard` verificam filtro de mês/ano
- Todos os novos testes passam
- Testes existentes continuam passando (sem regressão)
- Coverage mantido ou melhorado
```
