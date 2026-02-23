```markdown
---
status: done
parallelizable: false
blocked_by: ["5.0"]
---

<task_context>
<domain>frontend/fundação</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 9.0: Frontend — Tipos, API Client, Hooks e Schemas

## Visão Geral

Criar a fundação do módulo frontend `features/budgets/`: tipos TypeScript que espelham os DTOs do backend, funções de API client (Axios), hooks React Query (useQuery/useMutation) e schemas Zod para validação de formulários. Esta tarefa não cria componentes visuais — apenas a infraestrutura de dados que será consumida pelos componentes na tarefa 10.0.

## Requisitos

- Techspec: Estrutura `features/budgets/` com subpastas types/, api/, hooks/, schemas/
- Techspec: Tipos espelhando `BudgetResponse`, `BudgetSummaryResponse`, `AvailablePercentageResponse`
- Techspec: 6 hooks (3 queries + 3 mutations)
- Techspec: Schema Zod para validação de formulário de criação/edição
- `rules/react-project-structure.md`: Feature-based architecture
- `rules/react-coding-standards.md`: Convenções TypeScript/React

## Subtarefas

### Tipos TypeScript

- [x] 9.1 Criar `types/index.ts` em `frontend/src/features/budgets/types/index.ts`:
  ```typescript
  export interface BudgetCategoryDto {
    id: string;
    name: string;
  }

  export interface BudgetResponse {
    id: string;
    name: string;
    percentage: number;
    referenceYear: number;
    referenceMonth: number;
    isRecurrent: boolean;
    monthlyIncome: number;
    limitAmount: number;
    consumedAmount: number;
    remainingAmount: number;
    consumedPercentage: number;
    categories: BudgetCategoryDto[];
    createdAt: string;
    updatedAt: string | null;
  }

  export interface BudgetSummaryResponse {
    referenceYear: number;
    referenceMonth: number;
    monthlyIncome: number;
    totalBudgetedPercentage: number;
    totalBudgetedAmount: number;
    totalConsumedAmount: number;
    totalRemainingAmount: number;
    unbudgetedPercentage: number;
    unbudgetedAmount: number;
    unbudgetedExpenses: number;
    budgets: BudgetResponse[];
  }

  export interface AvailablePercentageResponse {
    usedPercentage: number;
    availablePercentage: number;
    usedCategoryIds: string[];
  }

  export interface CreateBudgetRequest {
    name: string;
    percentage: number;
    referenceYear: number;
    referenceMonth: number;
    categoryIds: string[];
    isRecurrent: boolean;
  }

  export interface UpdateBudgetRequest {
    name: string;
    percentage: number;
    categoryIds: string[];
    isRecurrent: boolean;
  }
  ```

### API Client

- [x] 9.2 Criar `api/budgetsApi.ts` em `frontend/src/features/budgets/api/budgetsApi.ts`:
  - Importar `apiClient` de `shared/services/apiClient`
  - Funções:
    - `createBudget(data: CreateBudgetRequest): Promise<BudgetResponse>`
      - `POST /api/v1/budgets`
    - `updateBudget(id: string, data: UpdateBudgetRequest): Promise<BudgetResponse>`
      - `PUT /api/v1/budgets/${id}`
    - `deleteBudget(id: string): Promise<void>`
      - `DELETE /api/v1/budgets/${id}`
    - `getBudgetById(id: string): Promise<BudgetResponse>`
      - `GET /api/v1/budgets/${id}`
    - `listBudgets(month: number, year: number): Promise<BudgetResponse[]>`
      - `GET /api/v1/budgets?month=${month}&year=${year}`
    - `getBudgetSummary(month: number, year: number): Promise<BudgetSummaryResponse>`
      - `GET /api/v1/budgets/summary?month=${month}&year=${year}`
    - `getAvailablePercentage(month: number, year: number, excludeBudgetId?: string): Promise<AvailablePercentageResponse>`
      - `GET /api/v1/budgets/available-percentage?month=${month}&year=${year}&excludeBudgetId=${excludeBudgetId}`

### Hooks React Query

- [x] 9.3 Criar hooks de query em `frontend/src/features/budgets/hooks/`:
  - `useBudgets.ts`:
    ```typescript
    export function useBudgets(month: number, year: number) {
      return useQuery({
        queryKey: ['budgets', year, month],
        queryFn: () => listBudgets(month, year),
      });
    }
    ```
  - `useBudgetSummary.ts`:
    ```typescript
    export function useBudgetSummary(month: number, year: number) {
      return useQuery({
        queryKey: ['budgets', 'summary', year, month],
        queryFn: () => getBudgetSummary(month, year),
      });
    }
    ```
  - `useAvailablePercentage.ts`:
    ```typescript
    export function useAvailablePercentage(
      month: number, year: number, excludeBudgetId?: string
    ) {
      return useQuery({
        queryKey: ['budgets', 'available-percentage', year, month, excludeBudgetId],
        queryFn: () => getAvailablePercentage(month, year, excludeBudgetId),
      });
    }
    ```

- [x] 9.4 Criar hooks de mutation em `frontend/src/features/budgets/hooks/`:
  - `useCreateBudget.ts`:
    - `useMutation` com `createBudget`
    - `onSuccess`: invalidar queries `['budgets']` e `['budgets', 'summary']`
    - Toast de sucesso: "Orçamento criado com sucesso"
  - `useUpdateBudget.ts`:
    - `useMutation` com `updateBudget`
    - `onSuccess`: invalidar queries de budgets
    - Toast de sucesso: "Orçamento atualizado com sucesso"
  - `useDeleteBudget.ts`:
    - `useMutation` com `deleteBudget`
    - `onSuccess`: invalidar queries de budgets
    - Toast de sucesso: "Orçamento excluído com sucesso"

### Schema Zod

- [x] 9.5 Criar `schemas/budgetSchema.ts` em `frontend/src/features/budgets/schemas/budgetSchema.ts`:
  ```typescript
  import { z } from 'zod';

  export const budgetSchema = z.object({
    name: z
      .string()
      .min(2, 'Nome deve ter ao menos 2 caracteres')
      .max(150, 'Nome deve ter no máximo 150 caracteres'),
    percentage: z
      .number({ required_error: 'Percentual é obrigatório' })
      .gt(0, 'Percentual deve ser maior que 0')
      .lte(100, 'Percentual deve ser no máximo 100%'),
    referenceYear: z.number().int().min(2020),
    referenceMonth: z.number().int().min(1).max(12),
    categoryIds: z
      .array(z.string().uuid())
      .min(1, 'Selecione ao menos uma categoria'),
    isRecurrent: z.boolean().default(false),
  });

  export type BudgetFormData = z.infer<typeof budgetSchema>;
  ```

### Barrel Export

- [x] 9.6 Criar `index.ts` em `frontend/src/features/budgets/index.ts`:
  - Re-exportar tipos, hooks e componentes (página será adicionada na tarefa 10.0)

### Validação

- [x] 9.7 Verificar que o frontend compila sem erros: `cd frontend && npm run build`

## Sequenciamento

- Bloqueado por: 5.0 (API Layer — endpoints devem existir para que os tipos e API client sejam corretos)
- Desbloqueia: 10.0 (Frontend — Componentes e Páginas)
- Paralelizável: Sim com 8.0 (Testes Backend são independentes)

## Detalhes de Implementação

### Estrutura de Arquivos

```
frontend/src/features/budgets/
├── api/
│   └── budgetsApi.ts              ← NOVO
├── hooks/
│   ├── useBudgets.ts              ← NOVO
│   ├── useBudgetSummary.ts        ← NOVO
│   ├── useAvailablePercentage.ts  ← NOVO
│   ├── useCreateBudget.ts         ← NOVO
│   ├── useUpdateBudget.ts         ← NOVO
│   └── useDeleteBudget.ts         ← NOVO
├── schemas/
│   └── budgetSchema.ts            ← NOVO
├── types/
│   └── index.ts                   ← NOVO
└── index.ts                       ← NOVO
```

### Padrões a Seguir

- Seguir padrão de `features/transactions/api/transactionsApi.ts` para funções de API
- Seguir padrão de `features/transactions/hooks/` para hooks React Query
- Seguir padrão de `features/transactions/schemas/` para schemas Zod
- Seguir padrão de `features/transactions/types/index.ts` para tipos
- Query keys hierárquicas: `['budgets', ...]` para facilitar invalidação
- Mutations invalidam queries pai para refetch automático
- Toasts via `sonner` (padrão existente)

### Convenções de Nomenclatura

- Hooks: `use[Verbo][Substantivo]` — `useBudgets`, `useCreateBudget`
- API functions: `[verbo][Substantivo]` — `createBudget`, `listBudgets`
- Types: PascalCase interfaces com sufixo `Response`/`Request`
- Schema: camelCase fields matching API fields

## Critérios de Sucesso

- Tipos TypeScript espelham exatamente os DTOs do backend (camelCase)
- API client cobre todos os 7 endpoints da API
- 6 hooks React Query criados (3 queries + 3 mutations)
- Mutations invalidam queries corretas no `onSuccess`
- Schema Zod valida todos os campos do formulário com mensagens em português
- Barrel export funcional
- Frontend compila sem erros TypeScript
```
