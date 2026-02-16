```markdown
---
status: completed
parallelizable: false
blocked_by: ["9.0"]
---

<task_context>
<domain>frontend/formulário</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>9.0</dependencies>
<unblocks>"11.0"</unblocks>
</task_context>

# Tarefa 10.0: Frontend — Formulário e Tipos Adaptados

## Visão Geral

Adaptar os tipos TypeScript, schemas Zod, API client e formulário de conta (`AccountForm`) para suportar cartão de crédito. O formulário deve alterar dinamicamente os campos exibidos quando o tipo selecionado for "Cartão de Crédito" — ocultando saldo inicial e permitir saldo negativo, e exibindo limite, dia de fechamento, dia de vencimento, conta de débito e flag de limite rígido.

## Requisitos

- PRD F1 req 1: Campos específicos: limite, fechamento, vencimento, conta de débito
- PRD F1 req 2: Cartão NÃO possui campo "saldo inicial"
- PRD F1 req 3: Cartão NÃO possui campo "permitir saldo negativo"
- PRD F1 req 6: Conta de débito deve ser ativa e do tipo Corrente ou Carteira
- PRD F6 req 36: Formulário altera campos dinamicamente por tipo
- PRD F6 req 37: Formulário de edição exibe campos específicos
- Techspec: Frontend em `features/accounts/`
- `rules/react-project-structure.md`: Feature-based, componentes em `components/`, hooks em `hooks/`

## Subtarefas

### Tipos TypeScript

- [x] 10.1 Estender `AccountResponse` em `features/accounts/types/account.ts`:
  ```typescript
  export interface CreditCardDetailsResponse {
    creditLimit: number;
    closingDay: number;
    dueDay: number;
    debitAccountId: string;
    enforceCreditLimit: boolean;
    availableLimit: number;
  }

  export interface AccountResponse {
    // ... campos existentes ...
    creditCard: CreditCardDetailsResponse | null;
  }
  ```

- [x] 10.2 Estender `CreateAccountRequest` e `UpdateAccountRequest` com campos opcionais:
  ```typescript
  export interface CreateAccountRequest {
    name: string;
    type: AccountType;
    initialBalance?: number;
    allowNegativeBalance?: boolean;
    // Campos de cartão
    creditLimit?: number;
    closingDay?: number;
    dueDay?: number;
    debitAccountId?: string;
    enforceCreditLimit?: boolean;
  }
  ```

- [x] 10.3 Criar tipos de fatura em `features/accounts/types/invoice.ts`:
  ```typescript
  export interface InvoiceResponse {
    accountId: string;
    accountName: string;
    month: number;
    year: number;
    periodStart: string;
    periodEnd: string;
    dueDate: string;
    totalAmount: number;
    previousBalance: number;
    amountDue: number;
    transactions: InvoiceTransactionDto[];
  }

  export interface InvoiceTransactionDto {
    id: string;
    description: string;
    amount: number;
    type: number;
    competenceDate: string;
    installmentNumber: number | null;
    totalInstallments: number | null;
  }

  export interface PayInvoiceRequest {
    amount: number;
    competenceDate: string;
    operationId?: string;
  }
  ```

### Schemas Zod

- [x] 10.4 Estender `accountSchema.ts` com validação condicional:
  ```typescript
  export const createAccountSchema = z.discriminatedUnion('type', [
    // Schema para Corrente/Investimento/Carteira (existente)
    z.object({
      type: z.literal(AccountType.Corrente), // ou Investimento, Carteira
      name: z.string().min(2).max(150),
      initialBalance: z.number(),
      allowNegativeBalance: z.boolean(),
    }),
    // Schema para Cartão
    z.object({
      type: z.literal(AccountType.Cartao),
      name: z.string().min(2).max(150),
      creditLimit: z.number().positive('Limite deve ser maior que zero'),
      closingDay: z.number().int().min(1).max(28),
      dueDay: z.number().int().min(1).max(28),
      debitAccountId: z.string().uuid('Conta de débito obrigatória'),
      enforceCreditLimit: z.boolean().default(true),
    }),
  ]);
  ```
  - **Nota**: Se `discriminatedUnion` não se encaixar, usar `.refine()` ou `.superRefine()` com validação condicional por `type`

### API Client

- [x] 10.5 Estender `accountsApi.ts` com funções de fatura:
  ```typescript
  export const getInvoice = async (
    accountId: string, month: number, year: number
  ): Promise<InvoiceResponse> => {
    const { data } = await api.get(
      `/accounts/${accountId}/invoices`,
      { params: { month, year } }
    );
    return data;
  };

  export const payInvoice = async (
    accountId: string, request: PayInvoiceRequest
  ): Promise<TransactionResponse[]> => {
    const { data } = await api.post(
      `/accounts/${accountId}/invoices/pay`,
      request
    );
    return data;
  };
  ```

### Hooks React Query

- [x] 10.6 Criar hook `useInvoice` em `features/accounts/hooks/useInvoice.ts`:
  ```typescript
  export function useInvoice(accountId: string, month: number, year: number) {
    return useQuery({
      queryKey: ['invoice', accountId, month, year],
      queryFn: () => getInvoice(accountId, month, year),
      enabled: !!accountId,
    });
  }

  export function usePayInvoice() {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: ({ accountId, request }: { accountId: string; request: PayInvoiceRequest }) =>
        payInvoice(accountId, request),
      onSuccess: (_, { accountId }) => {
        queryClient.invalidateQueries({ queryKey: ['accounts'] });
        queryClient.invalidateQueries({ queryKey: ['invoice', accountId] });
        queryClient.invalidateQueries({ queryKey: ['dashboard'] });
        toast.success('Fatura paga com sucesso');
      },
      onError: () => toast.error('Erro ao pagar fatura'),
    });
  }
  ```

### Formulário Dinâmico

- [x] 10.7 Adaptar `AccountForm.tsx` para campos condicionais por tipo:
  - Observar o valor do campo `type` via `watch('type')` (react-hook-form)
  - Quando `type === AccountType.Cartao`:
    - **Ocultar**: "Saldo Inicial", "Permitir Saldo Negativo"
    - **Mostrar**: "Limite de Crédito", "Dia de Fechamento", "Dia de Vencimento", "Conta de Débito" (select), "Limite Rígido" (toggle)
  - Quando `type !== AccountType.Cartao`:
    - **Mostrar**: "Saldo Inicial", "Permitir Saldo Negativo" (fluxo existente)
    - **Ocultar**: campos de cartão
  - Transição suave entre os campos (animação CSS, conforme PRD)

- [x] 10.8 Implementar select de "Conta de Débito" no formulário:
  - Listar contas ativas do tipo Corrente e Carteira (usar `useAccounts` com filtro)
  - O select deve excluir a própria conta sendo editada (para edição)
  - Mostrar nome e saldo de cada conta no options

- [x] 10.9 Adaptar formulário de edição (`AccountForm` modo edição):
  - Quando editando cartão, preencher campos de cartão com dados de `account.creditCard`
  - Campos de cartão visíveis, tipo não editável (select desabilitado)

### Constantes

- [x] 10.10 Atualizar `constants.ts` se necessário:
  - Verificar se `ACCOUNT_TYPE_LABELS` e `ACCOUNT_TYPE_ICONS` já mapeiam tipo 2 (Cartão) — sim, já existem: `{2: 'Cartão'}` e `{2: CreditCard}`

### Testes Frontend

- [x] 10.11 Criar/estender testes para `AccountForm` em `features/accounts/__tests__/AccountForm.test.tsx`:
  - `should show credit card fields when type is Cartao`
  - `should hide credit card fields when type is Corrente`
  - `should show initial balance when type is Corrente`
  - `should hide initial balance when type is Cartao`
  - `should validate credit limit is positive`
  - `should validate closing day between 1-28`
  - `should populate debit account select with active Corrente/Carteira accounts`
  - `should populate credit card fields in edit mode`

### Validação

- [x] 10.12 Validar build frontend: `npm run build` e `npm run lint`
- [x] 10.13 Executar testes frontend: `npm test`

## Sequenciamento

- Bloqueado por: 9.0 (API deve estar pronta para integração)
- Desbloqueia: 11.0 (Card de cartão e Drawer de fatura usam tipos/hooks aqui definidos)
- Paralelizável: Não

## Detalhes de Implementação

### Campos Condicionais no AccountForm

```tsx
const watchType = watch('type');
const isCreditCard = watchType === AccountType.Cartao;

return (
  <form onSubmit={handleSubmit(onSubmit)}>
    {/* Campos comuns */}
    <Input label="Nome" {...register('name')} />
    <Select label="Tipo" {...register('type')}>
      {/* options existentes */}
    </Select>

    {/* Campos para conta regular */}
    {!isCreditCard && (
      <>
        <Input label="Saldo Inicial" type="number" {...register('initialBalance')} />
        <Switch label="Permitir Saldo Negativo" {...register('allowNegativeBalance')} />
      </>
    )}

    {/* Campos para cartão de crédito */}
    {isCreditCard && (
      <>
        <Input label="Limite de Crédito" type="number" {...register('creditLimit')} />
        <Input label="Dia de Fechamento" type="number" min={1} max={28} {...register('closingDay')} />
        <Input label="Dia de Vencimento" type="number" min={1} max={28} {...register('dueDay')} />
        <Select label="Conta de Débito" {...register('debitAccountId')}>
          {debitAccounts.map(a => (
            <option key={a.id} value={a.id}>{a.name}</option>
          ))}
        </Select>
        <Switch label="Limite Rígido" {...register('enforceCreditLimit')} />
      </>
    )}
  </form>
);
```

### Observações

- **react-hook-form**: Ao trocar o tipo, os campos condicionais mudam. Usar `reset` ou `unregister` para limpar valores de campos que saem do DOM.
- **Zod + discriminatedUnion**: Pode ser complexo com Zod. Alternativa: usar `z.object({...}).refine()` com validação manual por tipo.
- **Select de conta de débito**: Filtrar `accounts.filter(a => (a.type === AccountType.Corrente || a.type === AccountType.Carteira) && a.isActive)`.
- **Animação**: Usar `transition-all` do Tailwind CSS para suavizar aparecimento/desaparecimento de campos.

## Critérios de Sucesso

- Tipos TypeScript incluem `CreditCardDetailsResponse` e tipos de fatura
- Schema Zod valida condicionalmente por tipo
- API client tem funções para fatura (get e pay)
- Hooks React Query para fatura criados e funcionais
- Formulário mostra campos de cartão quando tipo = Cartão
- Formulário mostra campos de conta regular quando tipo ≠ Cartão
- Select de conta de débito lista apenas contas ativas Corrente/Carteira
- Formulário de edição preenche campos de cartão com dados existentes
- Transição suave entre campos ao trocar tipo
- Todos os testes frontend passam
- Build e lint passam sem erros
```
