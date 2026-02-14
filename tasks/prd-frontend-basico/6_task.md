---
status: pending
parallelizable: true
blocked_by: ["3.0", "4.0"]
---

<task_context>
<domain>frontend/accounts</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>tanstack-query, react-hook-form, zod</dependencies>
<unblocks>"8.0", "10.0"</unblocks>
</task_context>

# Tarefa 6.0: CRUD de Contas

## Visão Geral

Implementar a feature completa de gestão de contas: listagem em cards (grid responsivo), formulário de criação e edição (modal), toggle de ativar/inativar com confirmação, filtro por tipo de conta e footer com patrimônio consolidado. A tela deve reproduzir fielmente o mockup `screen-examples/gestao-contas/index.html`.

## Requisitos

- PRD F3 req. 15: Listagem com nome, tipo, saldo atual, status (ativa/inativa)
- PRD F3 req. 16: Formulário de criação com nome, tipo (dropdown), saldo inicial, flag "permitir saldo negativo"
- PRD F3 req. 17: Formulário de edição (nome, flag saldo negativo)
- PRD F3 req. 18: Botão para ativar/inativar com confirmação
- PRD F3 req. 19: Indicação visual do tipo de conta (ícones ou cores)
- PRD F3 req. 20: Saldo formatado em R$
- Layout fiel ao mockup `screen-examples/gestao-contas/index.html`

## Subtarefas

- [ ] 6.1 Criar `src/features/accounts/types/account.ts` — enums `AccountType` (Corrente=1, Cartao=2, Investimento=3, Carteira=4), interfaces `AccountResponse`, `CreateAccountRequest`, `UpdateAccountRequest`
- [ ] 6.2 Criar `src/features/accounts/api/accountsApi.ts` — funções: `getAccounts()`, `getAccount(id)`, `createAccount(data)`, `updateAccount(id, data)`, `toggleAccountStatus(id, isActive)` usando apiClient
- [ ] 6.3 Criar `src/features/accounts/hooks/useAccounts.ts` — hooks TanStack Query: `useAccounts()` (lista), `useAccount(id)`, `useCreateAccount()`, `useUpdateAccount()`, `useToggleAccountStatus()` com mutations e invalidação de cache
- [ ] 6.4 Criar `src/features/accounts/components/AccountCard.tsx` — card individual com: barra colorida no topo (azul=Corrente, roxo=Cartão, verde=Investimento, amarelo=Carteira), ícone do tipo, nome, subtipo ("Corrente", "Crédito"), saldo formatado em R$ (vermelho se negativo), toggle de ativar/inativar, botão editar, link "Ver Extrato"
- [ ] 6.5 Criar `src/features/accounts/components/AccountGrid.tsx` — grid responsivo (1-4 colunas via Tailwind: `grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3 2xl:grid-cols-4`) renderizando AccountCards
- [ ] 6.6 Criar schema Zod para criação de conta: `createAccountSchema` — nome (obrigatório, min 2 chars), tipo (obrigatório), saldo inicial (número), allowNegativeBalance (boolean)
- [ ] 6.7 Criar `src/features/accounts/components/AccountForm.tsx` — modal com formulário (react-hook-form + zod): campos nome, tipo (Select dropdown), saldo inicial (input numérico formatado), toggle "Permitir saldo negativo". Modo criação e edição (em edição: saldo inicial desabilitado, tipo desabilitado)
- [ ] 6.8 Criar `src/features/accounts/components/AccountSummaryFooter.tsx` — footer fixo na parte inferior: patrimônio total (soma de todos os saldos), número de contas ativas, dívida total de cartões
- [ ] 6.9 Criar `src/features/accounts/pages/AccountsPage.tsx` — composição: header com título "Minhas Contas" + botão "Adicionar Conta", barra de filtros (Todas, Bancárias, Cartões), AccountGrid, AccountSummaryFooter; modal de criação/edição, ConfirmationModal para toggle de status
- [ ] 6.10 Criar `src/features/accounts/index.ts` — barrel export
- [ ] 6.11 Criar MSW handlers: mock de GET/POST/PUT/PATCH para `/api/v1/accounts`
- [ ] 6.12 Testes: AccountCard (renderização, toggle status), AccountForm (validação, submit), AccountsPage (listagem, criação, fluxo completo)

## Sequenciamento

- Bloqueado por: 3.0 (Auth — rota protegida), 4.0 (Backend — DTO corrigido com Type e AllowNegativeBalance)
- Desbloqueia: 8.0 (Transações — select de contas no formulário), 10.0 (Polimento)
- Paralelizável: Sim, com 5.0 (Dashboard), 7.0 (Categorias), 9.0 (Admin)

## Detalhes de Implementação

### Cores e Ícones por Tipo de Conta

| Tipo | Cor da barra | Ícone Material | Cor de fundo do ícone |
|------|-------------|----------------|----------------------|
| Corrente | `bg-primary` | `account_balance` | `bg-primary/10 text-primary` |
| Cartão | `bg-purple-500` | `credit_card` | `bg-purple-500/10 text-purple-600` |
| Investimento | `bg-success` | `trending_up` | `bg-success/10 text-success` |
| Carteira | `bg-warning` | `wallet` | `bg-warning/10 text-warning` |

### AccountCard — Estrutura (do mockup)

```
┌────────────────────────────────┐
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ ← barra colorida 4px
│ 🏦 Banco Itaú          ✏️    │
│    Corrente                    │
│                                │
│ Saldo Atual                    │
│ R$ 5.230,45                    │
│                                │
│ [🔵 Toggle] Ativa   Ver Extrato→│
└────────────────────────────────┘
```

### AccountForm — Schema de Validação

```typescript
const createAccountSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres'),
  type: z.nativeEnum(AccountType, { errorMap: () => ({ message: 'Selecione o tipo' }) }),
  initialBalance: z.number({ invalid_type_error: 'Valor inválido' }).default(0),
  allowNegativeBalance: z.boolean().default(false),
});

const updateAccountSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres'),
  allowNegativeBalance: z.boolean(),
});
```

### Mutations com Invalidação de Cache

```typescript
function useCreateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAccountRequest) => createAccount(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      toast.success('Conta criada com sucesso!');
    },
    onError: (error) => {
      toast.error(handleApiError(error));
    },
  });
}
```

## Critérios de Sucesso

- Listagem exibe todas as contas em cards com ícones e cores corretos por tipo
- Saldos formatados em R$ (negativos em vermelho)
- Botão "Adicionar Conta" abre modal com formulário validado
- Criação de conta: formulário submete, toast de sucesso, lista atualizada
- Edição de conta: modal preenchido com dados atuais, campos restritos no modo edição
- Toggle ativar/inativar: confirmação antes de executar, toast de feedback
- Filtros de tipo funcionam (Todas, Bancárias, Cartões)
- Footer exibe patrimônio total consolidado
- Layout fiel ao mockup `screen-examples/gestao-contas/`
- Testes unitários e de integração passam
