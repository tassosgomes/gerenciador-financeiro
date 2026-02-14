---
status: pending
parallelizable: false
blocked_by: ["5.0", "6.0", "7.0", "8.0", "9.0"]
---

<task_context>
<domain>frontend/quality</domain>
<type>testing</type>
<scope>performance</scope>
<complexity>medium</complexity>
<dependencies>vitest, rtl, msw, axe-core</dependencies>
<unblocks>""</unblocks>
</task_context>

# Tarefa 10.0: Polimento, Acessibilidade e Testes

## Visão Geral

Tarefa final de qualidade: adicionar skeleton loaders em todas as telas, toasts de feedback em todas as operações, empty states para listas vazias, garantir acessibilidade WCAG AA (labels, teclado, contraste), lazy loading de rotas, tratamento global de erros e completar a suíte de testes unitários e de integração para atingir cobertura mínima de 70% nas features críticas.

## Requisitos

- Skeleton loaders em todas as telas durante carregamento de dados
- Toasts de sucesso/erro em todas as operações CRUD
- Empty states em listas vazias (contas, categorias, transações, usuários)
- Acessibilidade WCAG AA: labels em todos os campos, navegação por teclado, contraste 4.5:1, aria-labels em ícones
- Lazy loading de rotas via `React.lazy()` + `Suspense`
- Error boundaries para capturar erros de renderização
- Tratamento global de erros de API via interceptor (Problem Details → toast)
- Cobertura de testes ≥ 70% nas features críticas (auth, transactions, dashboard)

## Subtarefas

### Skeleton Loaders

- [ ] 10.1 Adicionar skeleton loaders na DashboardPage (cards, gráficos, tabela)
- [ ] 10.2 Adicionar skeleton loaders na AccountsPage (grid de cards)
- [ ] 10.3 Adicionar skeleton loaders na CategoriesPage (lista)
- [ ] 10.4 Adicionar skeleton loaders na TransactionsPage (tabela, filtros)
- [ ] 10.5 Adicionar skeleton loaders na AdminPage (tabela de usuários)

### Toasts e Feedback

- [ ] 10.6 Instalar e configurar toast provider (Sonner/Shadcn Toast) no AppProviders
- [ ] 10.7 Garantir toast de sucesso em: criar/editar conta, criar/editar categoria, criar transação (todos os tipos), cancelar transação, ajustar transação, criar usuário, toggle status, export/import backup
- [ ] 10.8 Garantir toast de erro mapeado (Problem Details → mensagem pt-BR) em todas as mutations
- [ ] 10.9 Criar `src/shared/utils/errorMessages.ts` — mapeamento de error types para mensagens amigáveis em português

### Empty States

- [ ] 10.10 Criar componente genérico `src/shared/components/ui/EmptyState.tsx` — ícone, título, descrição, botão de ação opcional. Props: icon, title, description, actionLabel, onAction
- [ ] 10.11 Adicionar empty state em: AccountsGrid (nenhuma conta), CategoryList (nenhuma categoria), TransactionTable (nenhuma transação), UserTable (nenhum usuário)

### Acessibilidade (WCAG AA)

- [ ] 10.12 Auditar e corrigir labels: garantir `<label htmlFor>` em todos os campos de formulário (LoginForm, AccountForm, CategoryForm, TransactionForm, UserForm, TransactionFilters)
- [ ] 10.13 Auditar e corrigir navegação por teclado: focus management em modals (focus trap), tab order correto na sidebar, Enter/Space em botões e toggles
- [ ] 10.14 Auditar e corrigir contraste: verificar tokens de cor com ratio ≥ 4.5:1 para texto, ≥ 3:1 para elementos interativos; ajustar se necessário
- [ ] 10.15 Adicionar `aria-label` em ícones informativos (ícones de status, tipos de conta, botões de ação icon-only)
- [ ] 10.16 Adicionar roles semânticos: `role="navigation"` na sidebar, `role="main"` na área de conteúdo, `role="dialog"` em modals
- [ ] 10.17 Instalar `@axe-core/react` (dev only) para auditoria automática de acessibilidade em desenvolvimento

### Performance e Error Handling

- [ ] 10.18 Implementar lazy loading de rotas: `const DashboardPage = React.lazy(() => import(...))` com `<Suspense fallback={<PageSkeleton />}>`
- [ ] 10.19 Criar `src/shared/components/ui/ErrorBoundary.tsx` — error boundary que exibe mensagem amigável e botão "Tentar novamente"
- [ ] 10.20 Wrappear rotas principais com ErrorBoundary
- [ ] 10.21 Revisar tratamento de erros no interceptor do Axios: garantir que erros de rede (timeout, offline) geram toasts adequados

### Testes

- [ ] 10.22 Completar testes unitários de componentes faltantes: garantir cobertura de LoginForm, TransactionForm, DashboardSummaryCards, TransactionFilters, AccountCard, ConfirmationModal
- [ ] 10.23 Completar testes de hooks: useAuth, useDashboard, useTransactionFilters
- [ ] 10.24 Criar testes de integração end-to-end (com MSW): Login → Dashboard → Nova Transação → Listagem; CRUD completo de Contas; Filtros e paginação de transações; Backup export/import (admin)
- [ ] 10.25 Criar `src/shared/test/renderWithProviders.tsx` definitivo — wrapper com todos os providers (QueryClient, MemoryRouter, Zustand initial state)
- [ ] 10.26 Executar `npm run test -- --coverage` e verificar ≥ 70% em features críticas
- [ ] 10.27 Corrigir testes falhando e ajustar cobertura se necessário

### Validação Final

- [ ] 10.28 Executar `npm run build` — zero erros e warnings
- [ ] 10.29 Executar `npm run lint` — zero erros
- [ ] 10.30 Testar fluxo completo manualmente: login → dashboard → navegar por todas as telas → criar dados → logout

## Sequenciamento

- Bloqueado por: 5.0, 6.0, 7.0, 8.0, 9.0 (todas as features devem estar implementadas)
- Desbloqueia: Nenhum (tarefa final)
- Paralelizável: Não (depende de todas as features para polimento transversal)

## Detalhes de Implementação

### EmptyState Component

```tsx
interface EmptyStateProps {
  icon: string;        // Material Icons name
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
}

function EmptyState({ icon, title, description, actionLabel, onAction }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <span className="material-icons text-slate-300 text-6xl mb-4">{icon}</span>
      <h3 className="text-lg font-semibold text-slate-700 mb-2">{title}</h3>
      <p className="text-sm text-slate-500 mb-6 max-w-md">{description}</p>
      {actionLabel && onAction && (
        <Button onClick={onAction}>{actionLabel}</Button>
      )}
    </div>
  );
}
```

### Error Messages Mapping

```typescript
export const ERROR_MESSAGES: Record<string, string> = {
  'AccountNameAlreadyExists': 'Já existe uma conta com este nome.',
  'InsufficientBalance': 'Saldo insuficiente para esta operação.',
  'InvalidCredentials': 'Credenciais inválidas.',
  'CategoryNameAlreadyExists': 'Já existe uma categoria com este nome.',
  'TransactionAlreadyCancelled': 'Esta transação já foi cancelada.',
  'AccountInactive': 'Conta inativa. Ative a conta para realizar operações.',
  'UserEmailAlreadyExists': 'Já existe um usuário com este e-mail.',
  'InvalidBackupFile': 'Arquivo de backup inválido ou corrompido.',
};

export function getErrorMessage(error: AxiosError<ProblemDetails>): string {
  const problem = error.response?.data;
  if (problem?.type) {
    const key = problem.type.split('/').pop() ?? '';
    return ERROR_MESSAGES[key] ?? problem.detail ?? 'Erro inesperado.';
  }
  if (error.code === 'ECONNABORTED') return 'Tempo de conexão esgotado. Tente novamente.';
  if (!error.response) return 'Erro de conexão. Verifique sua internet.';
  return 'Erro inesperado. Tente novamente.';
}
```

### ErrorBoundary

```tsx
class ErrorBoundary extends React.Component<Props, State> {
  state = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center h-full p-8">
          <span className="material-icons text-danger text-6xl mb-4">error_outline</span>
          <h2 className="text-xl font-bold mb-2">Algo deu errado</h2>
          <p className="text-slate-500 mb-6">Ocorreu um erro inesperado.</p>
          <Button onClick={() => this.setState({ hasError: false })}>
            Tentar novamente
          </Button>
        </div>
      );
    }
    return this.props.children;
  }
}
```

### Lazy Loading de Rotas

```typescript
const DashboardPage = lazy(() => import('@/features/dashboard/pages/DashboardPage'));
const AccountsPage = lazy(() => import('@/features/accounts/pages/AccountsPage'));
const CategoriesPage = lazy(() => import('@/features/categories/pages/CategoriesPage'));
const TransactionsPage = lazy(() => import('@/features/transactions/pages/TransactionsPage'));
const TransactionDetailPage = lazy(() => import('@/features/transactions/pages/TransactionDetailPage'));
const AdminPage = lazy(() => import('@/features/admin/pages/AdminPage'));
```

## Critérios de Sucesso

- Skeleton loaders visíveis durante carregamento em todas as telas
- Toasts de sucesso/erro exibidos em todas as operações CRUD
- Empty states exibidos quando listas estão vazias
- Todas as labels de formulário conectadas aos inputs via `htmlFor`
- Navegação por teclado funcional: Tab percorre elementos interativos, Enter/Space ativa botões, Escape fecha modals
- Contraste WCAG AA: nenhuma violação de contraste nos tokens de cor
- Aria-labels presentes em ícones informativos
- Rotas carregam via lazy loading (bundles separados no build)
- Error boundary captura erros de renderização e exibe UI de recuperação
- Erros de API mapeados para mensagens em português amigáveis
- `npm run test -- --coverage` → ≥ 70% em auth, transactions, dashboard
- `npm run build` → zero erros e warnings
- `npm run lint` → zero erros
- Fluxo completo manual funciona sem erros
