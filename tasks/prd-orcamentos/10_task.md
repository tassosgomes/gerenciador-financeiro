```markdown
---
status: done
parallelizable: false
blocked_by: ["9.0"]
---

<task_context>
<domain>frontend/componentes</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>http_server</dependencies>
<unblocks>"11.0"</unblocks>
</task_context>

# Tarefa 10.0: Frontend — Componentes, Páginas e Navegação

## Visão Geral

Implementar todos os componentes React da feature de Orçamentos: `BudgetCard` (card individual com barra de progresso e indicadores visuais), `BudgetSummaryHeader` (resumo consolidado), `BudgetDashboard` (orquestrador com grid de cards e filtro), `BudgetForm` (formulário de criação/edição), `BudgetFormDialog` (dialog wrapper), `MonthYearFilter` (filtro de mês/ano reutilizável), e `BudgetsPage` (página principal lazy-loaded). Também adicionar rota `/budgets` e item "Orçamentos" no sidebar.

## Requisitos

- PRD F2 req 14-21: Dashboard com cards, resumo, filtro de mês, gastos fora de orçamento
- PRD F2 req 16: Barra de progresso com cores (verde < 70%, amarelo 70-89%, vermelho ≥ 90%)
- PRD F2 req 17: Estado "Estourado" com badge quando > 100%
- PRD F2 req 18: Ícone de alerta quando ≥ 80%
- PRD F2 req 19: Resumo consolidado no topo
- PRD F2 req 20: Gastos fora de orçamento sinalizados
- PRD F2 req 21: Filtro de mês/ano
- PRD F4 req 31: Meses passados em modo somente leitura
- PRD: Acessibilidade — cores não são único canal, usar ícones/textos complementares
- PRD: Dashboard responsivo (mobile e desktop)
- Techspec: Estrutura de componentes definida
- `rules/react-coding-standards.md`: Convenções React/TypeScript

## Subtarefas

### MonthYearFilter (Reutilizável)

- [x] 10.1 Criar `MonthYearFilter.tsx` em `frontend/src/features/budgets/components/MonthYearFilter.tsx`:
  - Props: `month: number`, `year: number`, `onChange: (month: number, year: number) => void`
  - Interface: dois selects (mês e ano) ou botões de navegação ← mês anterior | mês atual | mês seguinte →
  - Meses em português: Janeiro, Fevereiro, ..., Dezembro
  - Ano com range razoável (2020 até ano corrente + 1)
  - Responsivo: layout horizontal em desktop, empilhado em mobile

### BudgetCard

- [x] 10.2 Criar `BudgetCard.tsx` em `frontend/src/features/budgets/components/BudgetCard.tsx`:
  - Props: `budget: BudgetResponse`, `isReadOnly: boolean`, `onEdit?: () => void`, `onDelete?: () => void`
  - Layout (Card do shadcn/ui):
    - Header: Nome do orçamento + percentual (ex: "Moradia — 30%")
    - Badge "Recorrente" se `isRecurrent`
    - Categorias vinculadas como chips/badges
    - Valores: Limite (formatado R$), Consumido (formatado R$), Restante (formatado R$)
    - Barra de progresso com cor dinâmica:
      - Verde: `consumedPercentage < 70`
      - Amarelo: `consumedPercentage >= 70 && consumedPercentage < 90`
      - Vermelho: `consumedPercentage >= 90`
    - Se `consumedPercentage > 100`: badge "Estourado" com ícone AlertTriangle (Lucide)
    - Se `consumedPercentage >= 80 && consumedPercentage <= 100`: ícone de alerta (AlertCircle)
    - Texto do percentual consumido: "X% consumido"
  - Acessibilidade:
    - `aria-label` na barra de progresso: "Consumo do orçamento {name}: {percentage}%"
    - Ícones com título/tooltip descritivo
    - Cores complementadas com ícones e texto
  - Botões de ação (se não `isReadOnly`): Editar e Excluir com ícones
  - Responsivo: card ocupa largura total em mobile, grid em desktop

### BudgetSummaryHeader

- [x] 10.3 Criar `BudgetSummaryHeader.tsx` em `frontend/src/features/budgets/components/BudgetSummaryHeader.tsx`:
  - Props: `summary: BudgetSummaryResponse`
  - Layout: grid de cards resumo no topo (similar ao dashboard principal)
    - **Renda Mensal**: `monthlyIncome` formatado como R$
    - **Total Orçado**: `totalBudgetedAmount` formatado + `totalBudgetedPercentage`%
    - **Total Gasto**: `totalConsumedAmount` formatado
    - **Saldo Restante**: `totalRemainingAmount` formatado (verde se positivo, vermelho se negativo)
    - **Renda Não Orçada**: `unbudgetedAmount` formatado + `unbudgetedPercentage`%
    - **Gastos Fora de Orçamento**: `unbudgetedExpenses` formatado (com ícone de atenção se > 0)
  - Responsivo: 2 colunas em mobile, 3-6 colunas em desktop

### BudgetDashboard

- [x] 10.4 Criar `BudgetDashboard.tsx` em `frontend/src/features/budgets/components/BudgetDashboard.tsx`:
  - Estado local: `month` e `year` (inicializa com mês/ano correntes)
  - Usa hook `useBudgetSummary(month, year)` para buscar dados
  - Determina `isReadOnly` comparando mês/ano selecionado com mês/ano corrente
  - Layout:
    1. `MonthYearFilter` no topo
    2. `BudgetSummaryHeader` com resumo consolidado
    3. Grid de `BudgetCard` para cada orçamento (2 cols desktop, 1 col mobile)
    4. Botão "Novo Orçamento" (se não `isReadOnly` e `availablePercentage > 0`)
  - Empty state: quando não há orçamentos no mês — componente `EmptyState` com mensagem e botão para criar
  - Loading state: skeletons enquanto carrega
  - Callbacks:
    - `onEdit(budget)`: abre `BudgetFormDialog` em modo edição
    - `onDelete(budget)`: abre `ConfirmationModal` para confirmar exclusão
    - `onCreate()`: abre `BudgetFormDialog` em modo criação

### BudgetForm

- [x] 10.5 Criar `BudgetForm.tsx` em `frontend/src/features/budgets/components/BudgetForm.tsx`:
  - Props: `budget?: BudgetResponse` (para edição), `month: number`, `year: number`, `onSuccess: () => void`, `onCancel: () => void`
  - Usa React Hook Form + Zod (`budgetSchema`)
  - Usa `useAvailablePercentage(month, year, budget?.id)` para obter percentual disponível e categorias em uso
  - Usa `useCreateBudget()` ou `useUpdateBudget()` conforme modo
  - Campos:
    - **Nome**: input text (2-150 chars)
    - **Percentual da Renda**: input numérico com sufixo "%"
      - Mostra "Disponível: X%" em tempo real (baseado no `availablePercentage`)
      - Mostra cálculo em tempo real: "= R$ {renda × percentual / 100}" se tiver dados de renda
    - **Mês de Referência**: selects de mês e ano (apenas em modo criação; em edição, mostra readonly)
    - **Categorias**: seleção múltipla de categorias de despesa
      - Buscar categorias via hook existente de `useCategories` (feature categories)
      - Filtrar apenas tipo Despesa (`CategoryType.Despesa`)
      - Categorias já em uso em outro orçamento do mês: desabilitadas com tooltip "Em uso no orçamento X"
      - Mínimo 1 categoria selecionada
    - **Recorrente**: switch/checkbox "Repetir mensalmente"
  - Submit: chamar mutation e invocar `onSuccess`
  - Tratamento de erros: exibir mensagens de erro da API (ProblemDetails)

### BudgetFormDialog

- [x] 10.6 Criar `BudgetFormDialog.tsx` em `frontend/src/features/budgets/components/BudgetFormDialog.tsx`:
  - Props: `open: boolean`, `onOpenChange: (open: boolean) => void`, `budget?: BudgetResponse`, `month: number`, `year: number`
  - Usa Sheet (lateral) do shadcn/ui (padrão do projeto para formulários)
  - Título: "Novo Orçamento" ou "Editar Orçamento"
  - Renderiza `BudgetForm` internamente
  - Fecha ao salvar com sucesso

### BudgetsPage

- [x] 10.7 Criar `BudgetsPage.tsx` em `frontend/src/features/budgets/pages/BudgetsPage.tsx`:
  - Componente lazy-loaded (React.lazy)
  - Renderiza `BudgetDashboard` como conteúdo principal
  - Título da página: "Orçamentos"

### Rota

- [x] 10.8 Adicionar rota `/budgets` em `frontend/src/app/router/routes.tsx`:
  - Rota protegida (dentro de `ProtectedRoute`)
  - Lazy import de `BudgetsPage`
  - Seguir padrão das rotas existentes

### Sidebar

- [x] 10.9 Adicionar item "Orçamentos" no sidebar:
  - Em `frontend/src/shared/components/layout/Sidebar.tsx` ou `constants.ts`
  - Ícone: `PiggyBank` ou `Wallet` (Lucide icons)
  - Posição: após "Transações" e antes de "Categorias" (ou conforme hierarquia lógica)
  - Rota: `/budgets`

### Barrel Exports

- [x] 10.10 Atualizar `index.ts` para exportar página e componentes

### Validação

- [x] 10.11 Verificar que o frontend compila: `cd frontend && npm run build`
- [x] 10.12 Testar visualmente no browser:
  - Navegar para `/budgets`
  - Verificar empty state quando não há orçamentos
  - Criar orçamento via formulário
  - Verificar card com barra de progresso
  - Verificar filtro de mês funcional
  - Verificar responsividade (mobile e desktop)
  - Verificar modo somente leitura em meses passados

## Sequenciamento

- Bloqueado por: 9.0 (Frontend Types, API, Hooks, Schemas)
- Desbloqueia: 11.0 (Testes Frontend)
- Paralelizável: Não (depende da fundação frontend)

## Detalhes de Implementação

### Estrutura de Arquivos

```
frontend/src/features/budgets/
├── components/
│   ├── BudgetCard.tsx             ← NOVO
│   ├── BudgetDashboard.tsx        ← NOVO
│   ├── BudgetForm.tsx             ← NOVO
│   ├── BudgetFormDialog.tsx       ← NOVO
│   ├── BudgetSummaryHeader.tsx    ← NOVO
│   └── MonthYearFilter.tsx        ← NOVO
├── pages/
│   └── BudgetsPage.tsx            ← NOVO
└── index.ts                       ← MODIFICAR

frontend/src/app/router/
└── routes.tsx                     ← MODIFICAR (add rota /budgets)

frontend/src/shared/components/layout/
└── Sidebar.tsx                    ← MODIFICAR (add item "Orçamentos")
```

### Formatação de Valores

Usar utilitário existente de formatação monetária (`formatCurrency` ou similar em `shared/utils/`) para exibir valores em formato `R$ 1.234,56`.

### Cores da Barra de Progresso

```typescript
function getProgressColor(percentage: number): string {
  if (percentage >= 90) return 'bg-red-500';
  if (percentage >= 70) return 'bg-yellow-500';
  return 'bg-green-500';
}
```

### Padrões a Seguir

- Seguir padrão de `features/transactions/components/` para estrutura de componentes
- Seguir padrão de `features/accounts/components/` para cards e forms
- Usar componentes shadcn/ui: Card, Button, Sheet, Badge, Progress, Select, Switch, Input
- Usar `ConfirmationModal` existente para exclusão
- Usar `EmptyState` existente para empty state
- Usar `Skeleton` para loading states
- Formatação monetária consistente com o resto do app

## Critérios de Sucesso

- Página `/budgets` acessível via navegação no sidebar
- Dashboard exibe cards de orçamentos com dados corretos
- Barra de progresso muda de cor conforme faixas (verde/amarelo/vermelho)
- Badge "Estourado" aparece quando consumido > 100%
- Ícone de alerta aparece quando consumido ≥ 80%
- Resumo consolidado exibe renda, orçado, gasto, restante, gastos fora de orçamento
- Filtro de mês/ano funcional — muda dados exibidos
- Formulário valida campos (nome, percentual, categorias)
- Formulário mostra percentual disponível em tempo real
- Categorias em uso aparecem desabilitadas no formulário
- Meses passados exibidos em modo somente leitura (sem botões de ação)
- Empty state quando não há orçamentos
- Layout responsivo (mobile e desktop)
- Acessibilidade: aria-labels, ícones complementam cores
- Frontend compila sem erros
```
