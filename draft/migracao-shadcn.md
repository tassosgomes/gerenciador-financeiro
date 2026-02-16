# Migracao de Componentes para ShadCN UI

**Branch:** `feature/migrate-to-shadcn-components`
**Status:** Planejamento concluido, aguardando implementacao

---

## Contexto

O frontend ja utiliza ShadCN UI como biblioteca de componentes, com 13 componentes instalados:
`Badge`, `Button`, `Card`, `Dialog`, `Input`, `Progress`, `Select`, `Sheet`, `Skeleton`, `Sonner`, `Switch`, `Table`, `Tabs`.

Porem, diversos padroes de UI foram criados manualmente com HTML raw + Tailwind onde existem
componentes ShadCN equivalentes. Esta migracao visa padronizar a codebase, reduzir codigo
duplicado e melhorar acessibilidade.

---

## Comando de instalacao

Executar na raiz do frontend:

```bash
bunx shadcn@latest add alert-dialog alert label form pagination separator avatar checkbox textarea scroll-area tooltip
```

Isso instala 11 novos componentes em `src/shared/components/ui/`.

---

## Migracoes por fase

### FASE 1 - Fundacao (baixo risco, alto impacto)

#### 1.1 Label

**O que:** Substituir todos os `<label className="block text-sm font-medium...">` raw por `<Label>` do ShadCN.

**Componente ShadCN:** `@shadcn/label` (Radix UI)

**Arquivos afetados:**

| Arquivo | Linhas com `<label>` |
|---------|---------------------|
| `features/auth/components/LoginForm.tsx` | 54, 83, 112 |
| `features/transactions/components/TransactionFilters.tsx` | 49, 70, 91, 111, 132, 142 |
| `features/transactions/components/TransactionForm.tsx` | 236, 256, 272, 296, 323, 342, 352, 398, 418, 436, 452, 476, 502, 520, 532, 565, 585, 601, 625, 651, 669, 679, 709, 729, 745, 769, 795, 820 |
| `features/transactions/components/AdjustModal.tsx` | 90, 105 |
| `features/transactions/components/CancelModal.tsx` | 55 |
| `features/accounts/components/AccountForm.tsx` | 233, 249, 274, 293, 311, 328, 347, 366, 386, 411, 431, 450, 469, 489, 514 |
| `features/accounts/components/PaymentDialog.tsx` | 91, 122 |
| `features/categories/components/CategoryForm.tsx` | 138, 155, 178 |
| `features/admin/components/UserForm.tsx` | 124, 141, 157, 189 |

**Total:** ~60 instancias em 9 arquivos

**Como migrar:**
```tsx
// ANTES
<label htmlFor="name" className="block text-sm font-medium text-slate-700 mb-1.5">
  Nome
</label>

// DEPOIS
import { Label } from '@/shared/components/ui/label';

<Label htmlFor="name">Nome</Label>
```

**Checklist:**
- [ ] Instalar componente `label`
- [ ] Exportar `Label` em `shared/components/ui/index.ts`
- [ ] Substituir em LoginForm.tsx (3 labels)
- [ ] Substituir em TransactionFilters.tsx (6 labels)
- [ ] Substituir em TransactionForm.tsx (28 labels)
- [ ] Substituir em AdjustModal.tsx (2 labels)
- [ ] Substituir em CancelModal.tsx (1 label)
- [ ] Substituir em AccountForm.tsx (15 labels)
- [ ] Substituir em PaymentDialog.tsx (2 labels)
- [ ] Substituir em CategoryForm.tsx (3 labels)
- [ ] Substituir em UserForm.tsx (4 labels)
- [ ] Verificar que nenhum `<label` raw resta no projeto

---

#### 1.2 Separator

**O que:** Substituir divs custom usadas como divisores visuais por `<Separator>` do ShadCN.

**Componente ShadCN:** `@shadcn/separator` (Radix UI)

**Arquivos afetados:**

| Arquivo | Linha | Codigo atual |
|---------|-------|-------------|
| `shared/components/layout/Topbar.tsx` | 91 | `<div className="h-6 w-px bg-slate-200" />` |
| `shared/components/layout/Sidebar.tsx` | 56 | `<div className="mt-4 border-t border-slate-200 pt-4">` |
| `features/transactions/components/TransactionDetail.tsx` | 198 | `<div className="flex gap-2 pt-4 border-t">` |
| `features/accounts/components/AccountCard.tsx` | 137 | `<div className="... pt-2 border-t">` |

**Como migrar:**
```tsx
// ANTES (vertical)
<div className="h-6 w-px bg-slate-200" />

// DEPOIS
import { Separator } from '@/shared/components/ui/separator';
<Separator orientation="vertical" className="h-6" />

// ANTES (horizontal)
<div className="mt-4 border-t border-slate-200 pt-4">

// DEPOIS
<Separator className="my-4" />
<div className="pt-4">
```

**Checklist:**
- [ ] Instalar componente `separator`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Substituir em Topbar.tsx (separador vertical)
- [ ] Substituir em Sidebar.tsx (divisor de secao admin)
- [ ] Substituir em TransactionDetail.tsx (divisor de acoes)
- [ ] Substituir em AccountCard.tsx (divisor de footer)

---

#### 1.3 Checkbox

**O que:** Substituir `<input type="checkbox">` raw por `<Checkbox>` do ShadCN.

**Componente ShadCN:** `@shadcn/checkbox` (Radix UI)

**Arquivo afetado:** `features/auth/components/LoginForm.tsx` linhas 113-118

**Codigo atual:**
```tsx
<input
  className="h-4 w-4 rounded border-slate-300 text-primary focus:ring-primary"
  disabled
  id="remember-me"
  type="checkbox"
/>
```

**Como migrar:**
```tsx
import { Checkbox } from '@/shared/components/ui/checkbox';

<Checkbox id="remember-me" disabled />
```

**Checklist:**
- [ ] Instalar componente `checkbox`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Substituir em LoginForm.tsx

---

#### 1.4 Textarea

**O que:** Substituir `<Input>` usado para campos de texto longo por `<Textarea>`.

**Componente ShadCN:** `@shadcn/textarea`

**Arquivos afetados:**

| Arquivo | Linha | Campo |
|---------|-------|-------|
| `features/transactions/components/AdjustModal.tsx` | 108-114 | Justificativa do ajuste |
| `features/transactions/components/CancelModal.tsx` | 58-64 | Motivo do cancelamento |

**Como migrar:**
```tsx
// ANTES
<Input
  id="justification"
  placeholder="Ex: Correcao de valor incorreto"
  value={justification}
  onChange={(e) => setJustification(e.target.value)}
  maxLength={200}
/>

// DEPOIS
import { Textarea } from '@/shared/components/ui/textarea';

<Textarea
  id="justification"
  placeholder="Ex: Correcao de valor incorreto"
  value={justification}
  onChange={(e) => setJustification(e.target.value)}
  maxLength={200}
  rows={3}
/>
```

**Checklist:**
- [ ] Instalar componente `textarea`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Substituir em AdjustModal.tsx
- [ ] Substituir em CancelModal.tsx

---

#### 1.5 Avatar

**O que:** Substituir div circular com inicial do nome do usuario por `<Avatar>` do ShadCN.

**Componente ShadCN:** `@shadcn/avatar` (Radix UI)

**Arquivo afetado:** `shared/components/layout/Topbar.tsx` linhas 99-101

**Codigo atual:**
```tsx
<div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary ring-2 ring-slate-100">
  {userName.charAt(0).toUpperCase()}
</div>
```

**Como migrar:**
```tsx
import { Avatar, AvatarFallback } from '@/shared/components/ui/avatar';

<Avatar className="h-9 w-9 ring-2 ring-slate-100">
  <AvatarFallback className="bg-primary/10 text-sm font-semibold text-primary">
    {userName.charAt(0).toUpperCase()}
  </AvatarFallback>
</Avatar>
```

**Checklist:**
- [ ] Instalar componente `avatar`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Substituir em Topbar.tsx

---

### FASE 2 - Componentes compostos (risco medio)

#### 2.1 Alert

**O que:** Substituir divs custom de alerta/banner por `<Alert>` do ShadCN.

**Componente ShadCN:** `@shadcn/alert`

**Arquivos afetados:**

| Arquivo | Linha | Tipo de alerta |
|---------|-------|---------------|
| `features/auth/components/LoginForm.tsx` | 44-50 | Erro de login (vermelho) |
| `features/auth/pages/LoginPage.tsx` | 33-36 | Sessao expirada (amarelo) |
| `features/transactions/components/TransactionForm.tsx` | 684-690 | Info recorrencia (azul) |
| `features/transactions/components/TransactionDetail.tsx` | 153-157 | Info ajuste (azul) |
| `features/transactions/components/TransactionDetail.tsx` | 170-176 | Alerta ajuste (laranja) |
| `features/transactions/components/TransactionDetail.tsx` | 179-186 | Info cancelamento (cinza) |
| `features/transactions/components/TransactionDetail.tsx` | 189-195 | Alerta vencido (vermelho) |
| `features/categories/components/CategoryForm.tsx` | 128-133 | Aviso sistema (amarelo) |
| `features/admin/components/BackupImport.tsx` | 90-99 | Aviso importacao (amarelo) |

**Como migrar:**
```tsx
// ANTES
<div className="rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
  {errorMessage}
</div>

// DEPOIS
import { Alert, AlertDescription } from '@/shared/components/ui/alert';
import { AlertCircle } from 'lucide-react';

<Alert variant="destructive">
  <AlertCircle className="h-4 w-4" />
  <AlertDescription>{errorMessage}</AlertDescription>
</Alert>
```

**Nota:** O ShadCN Alert so tem `default` e `destructive`. Para alertas info (azul) e warning
(amarelo), sera necessario adicionar variantes custom no componente alert.tsx apos instalacao:

```tsx
// Adicionar em alert.tsx cva variants:
warning: "border-amber-200 bg-amber-50 text-amber-800 [&>svg]:text-amber-600",
info: "border-blue-200 bg-blue-50 text-blue-800 [&>svg]:text-blue-600",
```

**Checklist:**
- [ ] Instalar componente `alert`
- [ ] Adicionar variantes `warning` e `info` ao alert.tsx
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Substituir em LoginForm.tsx (destructive)
- [ ] Substituir em LoginPage.tsx (warning)
- [ ] Substituir em TransactionForm.tsx (info)
- [ ] Substituir em TransactionDetail.tsx (4 alertas: info, warning, default, destructive)
- [ ] Substituir em CategoryForm.tsx (warning)
- [ ] Substituir em BackupImport.tsx (warning)

---

#### 2.2 AlertDialog (substituir ConfirmationModal)

**O que:** Substituir `ConfirmationModal.tsx` custom pelo `AlertDialog` do ShadCN.

**Componente ShadCN:** `@shadcn/alert-dialog` (Radix UI)

**Definicao atual:** `shared/components/ui/ConfirmationModal.tsx` (linhas 1-61)

**Consumers (3 arquivos):**

| Arquivo | Import (linha) | Uso (linha) |
|---------|----------------|-------------|
| `features/accounts/pages/AccountsPage.tsx` | 11 | 128 |
| `features/admin/components/UserTable.tsx` | 6 | 119 |
| `features/admin/components/BackupImport.tsx` | 6 | 181 |

**Estrategia:** Criar um wrapper `ConfirmationDialog` que mantem a mesma API (props) mas usa
AlertDialog internamente. Isso minimiza mudancas nos consumers.

**Como migrar:**
```tsx
// NOVO: shared/components/ui/ConfirmationDialog.tsx
import { AlertTriangle } from 'lucide-react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from './alert-dialog';

interface ConfirmationDialogProps {
  open: boolean;
  title: string;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
  onOpenChange?: (open: boolean) => void;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'warning';
}

export function ConfirmationDialog({ ... }: ConfirmationDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="flex items-center gap-2">
            <AlertTriangle className={iconColor} size={18} />
            {title}
          </AlertDialogTitle>
          <AlertDialogDescription>{message}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={onCancel}>{cancelLabel}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm} className={...}>
            {confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
```

**Checklist:**
- [ ] Instalar componente `alert-dialog`
- [ ] Criar `ConfirmationDialog.tsx` usando AlertDialog
- [ ] Atualizar export em `shared/components/ui/index.ts`
- [ ] Atualizar import em `AccountsPage.tsx` (linha 11)
- [ ] Atualizar import em `UserTable.tsx` (linha 6)
- [ ] Atualizar import em `BackupImport.tsx` (linha 6)
- [ ] Remover `ConfirmationModal.tsx` antigo
- [ ] Verificar que testes dos consumers continuam passando

---

#### 2.3 Pagination

**O que:** Substituir `Pagination.tsx` custom pelo componente ShadCN.

**Componente ShadCN:** `@shadcn/pagination`

**Definicao atual:** `features/transactions/components/Pagination.tsx` (linhas 1-72)

**Consumer:** `features/transactions/pages/TransactionsPage.tsx` (import linha 10, uso linha 69)

**Re-export:** `features/transactions/index.ts` (linha 14)

**Nota:** O componente ShadCN Pagination e puramente visual (nao gerencia estado). A logica de
page change e page size precisa ser mantida. Recomenda-se criar um wrapper que mantem a mesma
interface de props mas usa os componentes ShadCN internamente.

**Checklist:**
- [ ] Instalar componente `pagination`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Reescrever `features/transactions/components/Pagination.tsx` usando componentes ShadCN
- [ ] Manter a mesma interface `PaginationProps` para nao quebrar TransactionsPage
- [ ] Verificar que testes de integracao de TransactionsPage continuam passando

---

### FASE 3 - Refinamentos (baixo risco)

#### 3.1 ScrollArea

**O que:** Substituir `overflow-y-auto` manual por `<ScrollArea>` para scrollbars customizadas.

**Componente ShadCN:** `@shadcn/scroll-area` (Radix UI)

**Arquivos afetados:**

| Arquivo | Linha | Contexto |
|---------|-------|---------|
| `features/transactions/components/InstallmentPreview.tsx` | 88 | Lista de parcelas |
| `features/transactions/components/TransactionForm.tsx` | 218 | DialogContent do form |
| `features/accounts/components/AccountForm.tsx` | 220 | DialogContent do form |
| `features/accounts/components/InvoiceDrawer.tsx` | 84 | SheetContent da fatura |
| `shared/components/layout/Sidebar.tsx` | 34 | Nav principal |

**Como migrar:**
```tsx
// ANTES
<div className="max-h-64 overflow-y-auto rounded-md border">
  {content}
</div>

// DEPOIS
import { ScrollArea } from '@/shared/components/ui/scroll-area';

<ScrollArea className="max-h-64 rounded-md border">
  {content}
</ScrollArea>
```

**Checklist:**
- [ ] Instalar componente `scroll-area`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Substituir em InstallmentPreview.tsx
- [ ] Substituir em TransactionForm.tsx (DialogContent)
- [ ] Substituir em AccountForm.tsx (DialogContent)
- [ ] Substituir em InvoiceDrawer.tsx (SheetContent)
- [ ] Substituir em Sidebar.tsx (nav area)

---

#### 3.2 Tooltip

**O que:** Adicionar tooltips visuais em botoes que so tem `aria-label`.

**Componente ShadCN:** `@shadcn/tooltip` (Radix UI)

**Arquivo afetado:** `shared/components/layout/Topbar.tsx`

| Linha | Botao | Tooltip text |
|-------|-------|-------------|
| 69-76 | Hamburger menu | "Abrir menu" |
| 82-89 | Notificacoes | "Notificacoes" |
| 103-111 | Logout | "Sair" |

**Nota:** Requer envolver o app (ou o Topbar) com `<TooltipProvider>`. Adicionar em
`AppProviders.tsx` ou diretamente no Topbar.

**Como migrar:**
```tsx
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/shared/components/ui/tooltip';

<TooltipProvider>
  <Tooltip>
    <TooltipTrigger asChild>
      <button aria-label="Sair" ...>
        <span className="material-icons">logout</span>
      </button>
    </TooltipTrigger>
    <TooltipContent>Sair</TooltipContent>
  </Tooltip>
</TooltipProvider>
```

**Checklist:**
- [ ] Instalar componente `tooltip`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Adicionar `TooltipProvider` (em AppProviders.tsx ou Topbar)
- [ ] Adicionar Tooltip no botao de notificacoes
- [ ] Adicionar Tooltip no botao de logout
- [ ] Avaliar se hamburger menu precisa (so aparece em mobile, tooltip pode nao ser util)

---

### FASE 4 - Migracao de Forms (maior complexidade)

#### 4.1 Form (react-hook-form wrapper)

**O que:** Migrar forms de `react-hook-form` manual para o wrapper ShadCN `Form` que
fornece `FormField`, `FormItem`, `FormLabel`, `FormControl`, `FormDescription`, `FormMessage`.

**Componente ShadCN:** `@shadcn/form` (depende de `react-hook-form` e `@hookform/resolvers`, ja instalados)

**IMPORTANTE:** Esta e a migracao mais complexa e de maior risco. Deve ser feita formulario
por formulario, com testes apos cada um. O `TransactionForm.tsx` usa `@ts-nocheck` por problemas
de tipagem com react-hook-form + Zod - essa migracao pode resolver ou complicar isso.

**Arquivos afetados:**

| Arquivo | Forms | Error tags manuais |
|---------|-------|--------------------|
| `features/auth/components/LoginForm.tsx` | 1 | 2 |
| `features/transactions/components/TransactionForm.tsx` | 4 (simples, parcela, recorrencia, transferencia) | ~20 |
| `features/accounts/components/AccountForm.tsx` | 1 | ~10 |
| `features/accounts/components/PaymentDialog.tsx` | 1 | 0 |
| `features/categories/components/CategoryForm.tsx` | 1 | 2 |
| `features/admin/components/UserForm.tsx` | 1 | 4 |

**Ordem de migracao recomendada (do mais simples ao mais complexo):**
1. CategoryForm (1 form, 2 erros, usa estado local - pode nem compensar)
2. UserForm (1 form, 4 erros, estado local simples)
3. LoginForm (1 form, 2 erros, usa react-hook-form + zod)
4. PaymentDialog (1 form, sem erros manuais)
5. AccountForm (1 form, ~10 erros, estado local com validacao manual)
6. TransactionForm (4 forms, ~20 erros, usa react-hook-form + zod, tem @ts-nocheck)

**Como migrar (exemplo com react-hook-form + zod):**
```tsx
// ANTES
<form onSubmit={handleSubmit(onSubmit)}>
  <div>
    <label htmlFor="email" className="block text-sm font-medium">Email</label>
    <Input id="email" {...register('email')} />
    {errors.email && <p className="text-sm text-red-600">{errors.email.message}</p>}
  </div>
</form>

// DEPOIS
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form';

<Form {...form}>
  <form onSubmit={form.handleSubmit(onSubmit)}>
    <FormField
      control={form.control}
      name="email"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Email</FormLabel>
          <FormControl>
            <Input {...field} />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  </form>
</Form>
```

**Nota sobre forms com estado local (AccountForm, CategoryForm, UserForm):**
Esses forms usam `useState` + validacao manual em vez de react-hook-form. A migracao para
ShadCN Form exigiria refatorar para react-hook-form primeiro. Alternativa: usar apenas
`Label` (Fase 1) e manter a estrutura atual. Avaliar caso a caso.

**Checklist:**
- [ ] Instalar componente `form`
- [ ] Exportar em `shared/components/ui/index.ts`
- [ ] Migrar LoginForm.tsx (ja usa react-hook-form)
- [ ] Migrar TransactionForm.tsx (ja usa react-hook-form, 4 sub-forms)
- [ ] Avaliar se compensa migrar forms com estado local (AccountForm, CategoryForm, UserForm)
- [ ] Remover `@ts-nocheck` do TransactionForm.tsx se a migracao resolver os tipos
- [ ] Executar testes apos cada form migrado

---

## Componentes que NAO serao migrados

| Componente | Motivo |
|-----------|--------|
| **ErrorBoundary** | Class component (requer `componentDidCatch`). Ja usa Card/Button ShadCN. Sem equivalente ShadCN. |
| **EmptyState** | Sem equivalente ShadCN. Componente simples e bem implementado. |
| **TransactionHistoryTimeline** | ShadCN nao tem Timeline. Implementacao custom adequada. |
| **BarChartWidget / DonutChartWidget** | Usam Recharts. ShadCN Chart e wrapper de Recharts, ja compativel. |
| **Sidebar (layout)** | Migrar para ShadCN Sidebar exigiria reestruturar o layout inteiro. Fazer em task separada se desejado. |
| **AppShell / ProtectedRoute / AdminRoute** | Componentes de roteamento/layout sem equivalente UI. |

---

## Resumo de impacto

| Metrica | Valor |
|---------|-------|
| Componentes ShadCN a instalar | 11 |
| Arquivos a modificar | ~25 |
| Componentes custom a remover | 2 (ConfirmationModal, Pagination custom) |
| Labels raw a substituir | ~60 |
| Error messages manuais a eliminar | ~38 |
| Alertas custom a padronizar | ~10 |

---

## Validacao apos cada fase

Apos completar cada fase, executar:

```bash
# Na raiz do frontend
npm run typecheck   # Verificar tipos TypeScript
npm run lint        # Verificar lint
npm run test        # Executar testes unitarios e integracao
npm run build       # Verificar build de producao
```

---

## Dependencias entre fases

```
Fase 1 (Label, Separator, Checkbox, Textarea, Avatar)
  |
  v
Fase 2 (Alert, AlertDialog, Pagination)
  |
  v
Fase 3 (ScrollArea, Tooltip)
  |
  v
Fase 4 (Form) -- depende de Label estar migrado
```

Fase 1 e pre-requisito porque Label e usado dentro de Form (Fase 4).
Fases 2 e 3 sao independentes entre si mas recomenda-se manter a ordem para
facilitar code review.
