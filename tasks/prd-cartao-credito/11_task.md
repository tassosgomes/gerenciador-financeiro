```markdown
---
status: completed
parallelizable: false
blocked_by: ["10.0"]
---

<task_context>
<domain>frontend/componente</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>10.0</dependencies>
<unblocks>"12.0"</unblocks>
</task_context>

# Tarefa 11.0: Frontend — Card de Cartão e Drawer de Fatura

## Visão Geral

Adaptar o componente `AccountCard` para exibir informações diferenciadas quando a conta é cartão de crédito — "Fatura Atual" ao invés de "Saldo Atual", limite disponível, alertas visuais de limite, botão de fatura e pagamento. Criar o componente `InvoiceDrawer` (drawer lateral) para exibir a fatura detalhada com lista de transações, total e botão "Pagar Fatura".

## Requisitos

- PRD F6 req 32: Card exibe nome, fatura atual, limite total, limite disponível, fechamento e vencimento
- PRD F6 req 33: Label "Fatura Atual" ao invés de "Saldo Atual"
- PRD F6 req 34: Saldo positivo exibe "Crédito disponível: R$ X"
- PRD F6 req 35: Alerta amarelo < 20% do limite; alerta vermelho se esgotado/ultrapassado
- PRD F6 req 38: Acessar fatura detalhada do ciclo atual
- PRD F6 req 39: Transações parceladas exibem "Parcela X/Y"
- PRD F5 req 27: Pagamento parcial
- PRD F5 req 28: Pagamento total (atalho com valor total preenchido)
- Techspec: Drawer lateral para fatura (decisão 5 confirmada pelo usuário)

## Subtarefas

### Adaptação do AccountCard

- [x] 11.1 Adaptar `AccountCard.tsx` para exibição condicional:
  - Quando `account.type === AccountType.Cartao` e `account.creditCard`:
    - Label principal: "Fatura Atual" (Math.abs(account.balance)) ao invés de "Saldo Atual"
    - Exibir: "Limite: R$ {creditCard.creditLimit}"
    - Exibir: "Disponível: R$ {creditCard.availableLimit}"
    - Exibir: "Fecha dia {creditCard.closingDay} | Vence dia {creditCard.dueDay}"
    - Botão "Ver Fatura" que abre o `InvoiceDrawer`
  - Quando `account.balance > 0` (crédito a favor):
    - Exibir badge: "Crédito disponível: R$ {account.balance}" com cor verde
  - Quando não é cartão: manter exibição inalterada

- [x] 11.2 Implementar alertas visuais de limite:
  - Se `creditCard.availableLimit / creditCard.creditLimit < 0.20`:
    - Badge amarelo: "Limite baixo ({percentage}% disponível)"
  - Se `creditCard.availableLimit <= 0`:
    - Badge vermelho: "Limite esgotado"
  - Os alertas são badges não-intrusivos (conforme PRD — sem modais)

- [x] 11.3 Manter compatibilidade para contas sem `CreditCardDetails` (legacy):
  - Se `account.type === AccountType.Cartao` mas `account.creditCard === null`:
    - Exibir como conta normal (sem campos de cartão)
    - Opcionalmente: badge "Configure seu cartão" incentivando edição

### InvoiceDrawer

- [x] 11.4 Criar componente `InvoiceDrawer` em `features/accounts/components/InvoiceDrawer.tsx`:
  - Drawer lateral (shadcn/ui `Sheet` component)
  - Props: `accountId`, `isOpen`, `onClose`
  - Usa `useInvoice(accountId, currentMonth, currentYear)` para buscar dados
  - Navegação de mês: botões < > para navegar entre meses

- [x] 11.5 Implementar conteúdo do drawer:
  - Header: "Fatura de {mês/ano}" + nome do cartão
  - Resumo:
    - "Período: {periodStart} a {periodEnd}"
    - "Vencimento: {dueDate}"
    - "Total da Fatura: R$ {totalAmount}"
    - Se `previousBalance > 0`: "Crédito anterior: -R$ {previousBalance}"
    - "Valor a Pagar: R$ {amountDue}"
  - Lista de transações:
    - Cada item: data, descrição, valor
    - Transações parceladas: "Parcela X/Y — {descrição}" (PRD req 22/39)
    - Débitos com valor positivo, créditos com valor negativo (com cor diferenciada)
  - Estado vazio: "Nenhuma transação neste período"

- [x] 11.6 Implementar botão "Pagar Fatura" no drawer:
  - Abre um dialog/modal de pagamento com:
    - Input de valor (pré-preenchido com `amountDue` — atalho para pagamento total, PRD req 28)
    - Botão "Pagar Total" que preenche automaticamente o valor total
    - Data de competência (padrão: hoje)
    - Botão de confirmação
  - Ao confirmar: chama `usePayInvoice().mutate(...)` com `accountId`, `amount`, `competenceDate`
  - Em caso de sucesso: fecha dialog, invalida queries, mostra toast
  - Desabilitar botão se `amountDue <= 0` (nada a pagar)

- [x] 11.7 Implementar navegação de meses no drawer:
  - Estado local: `currentMonth`, `currentYear`
  - Botão "<" (mês anterior) e ">" (próximo mês)
  - Usar `useInvoice` com os novos parâmetros

### Testes Frontend

- [x] 11.8 Criar testes para `AccountCard` adaptações:
  - `should display "Fatura Atual" for credit card accounts`
  - `should display "Saldo Atual" for regular accounts`
  - `should show limit and available limit for credit cards`
  - `should show yellow alert when available limit < 20%`
  - `should show red alert when limit exhausted`
  - `should show green badge for positive balance (credit favor)`
  - `should show "Ver Fatura" button for credit cards`

- [x] 11.9 Criar testes para `InvoiceDrawer`:
  - `should render invoice transactions`
  - `should display installment info for parceled transactions`
  - `should show empty state when no transactions`
  - `should navigate between months`
  - `should open payment dialog with amountDue prefilled`
  - `should disable pay button when nothing to pay`

### Validação

- [x] 11.10 Validar build: `npm run build` e `npm run lint`
- [x] 11.11 Executar testes: `npm test`

## Sequenciamento

- Bloqueado por: 10.0 (Tipos, hooks e API client devem existir)
- Desbloqueia: 12.0 (Dashboard — parcialmente, pode ser paralelo)
- Paralelizável: Com 12.0 (Dashboard não depende diretamente do drawer)

## Detalhes de Implementação

### AccountCard — Cartão de Crédito

```tsx
// Dentro de AccountCard.tsx, seção condicional:
{account.type === AccountType.Cartao && account.creditCard ? (
  <div>
    <p className="text-sm text-muted-foreground">Fatura Atual</p>
    <p className="text-2xl font-bold">
      {formatCurrency(Math.abs(account.balance))}
    </p>

    <div className="flex justify-between text-sm mt-2">
      <span>Limite: {formatCurrency(account.creditCard.creditLimit)}</span>
      <span>Disponível: {formatCurrency(account.creditCard.availableLimit)}</span>
    </div>

    <p className="text-xs text-muted-foreground mt-1">
      Fecha dia {account.creditCard.closingDay} | Vence dia {account.creditCard.dueDay}
    </p>

    {/* Alertas de limite */}
    {account.creditCard.availableLimit <= 0 && (
      <Badge variant="destructive">Limite esgotado</Badge>
    )}
    {account.creditCard.availableLimit > 0 &&
     account.creditCard.availableLimit / account.creditCard.creditLimit < 0.2 && (
      <Badge variant="warning">
        Limite baixo ({Math.round(account.creditCard.availableLimit / account.creditCard.creditLimit * 100)}%)
      </Badge>
    )}

    {/* Crédito a favor */}
    {account.balance > 0 && (
      <Badge variant="success">
        Crédito disponível: {formatCurrency(account.balance)}
      </Badge>
    )}

    <Button variant="outline" size="sm" onClick={() => setDrawerOpen(true)}>
      Ver Fatura
    </Button>
  </div>
) : (
  // Exibição padrão para conta regular (inalterada)
  <div>
    <p className="text-sm text-muted-foreground">Saldo Atual</p>
    <p className="text-2xl font-bold">{formatCurrency(account.balance)}</p>
  </div>
)}
```

### InvoiceDrawer — Estrutura

```tsx
<Sheet open={isOpen} onOpenChange={onClose}>
  <SheetContent side="right" className="w-[400px]">
    <SheetHeader>
      <SheetTitle>Fatura de {monthLabel}</SheetTitle>
    </SheetHeader>

    {/* Navegação de mês */}
    <div className="flex items-center justify-between">
      <Button variant="ghost" onClick={prevMonth}><ChevronLeft /></Button>
      <span>{monthLabel} {year}</span>
      <Button variant="ghost" onClick={nextMonth}><ChevronRight /></Button>
    </div>

    {/* Resumo */}
    <div className="space-y-2">
      <div>Período: {formatDate(invoice.periodStart)} — {formatDate(invoice.periodEnd)}</div>
      <div>Vencimento: {formatDate(invoice.dueDate)}</div>
      <div className="text-lg font-bold">Total: {formatCurrency(invoice.totalAmount)}</div>
      {invoice.previousBalance > 0 && (
        <div className="text-green-600">Crédito anterior: -{formatCurrency(invoice.previousBalance)}</div>
      )}
      <div className="text-xl font-bold">A Pagar: {formatCurrency(invoice.amountDue)}</div>
    </div>

    {/* Lista de transações */}
    <div className="space-y-1">
      {invoice.transactions.map(t => (
        <div key={t.id} className="flex justify-between">
          <div>
            <span className="text-sm">{formatDate(t.competenceDate)}</span>
            <span className="ml-2">
              {t.installmentNumber
                ? `Parcela ${t.installmentNumber}/${t.totalInstallments} — ${t.description}`
                : t.description}
            </span>
          </div>
          <span className={t.type === TransactionType.Credit ? 'text-green-600' : ''}>
            {t.type === TransactionType.Credit ? '-' : ''}{formatCurrency(t.amount)}
          </span>
        </div>
      ))}
    </div>

    {/* Botão Pagar */}
    <Button
      onClick={() => setPaymentDialogOpen(true)}
      disabled={invoice.amountDue <= 0}
    >
      Pagar Fatura
    </Button>
  </SheetContent>
</Sheet>
```

### Observações

- **shadcn/ui Sheet**: Usar o componente `Sheet` do shadcn/ui como drawer lateral. Já deve estar instalado ou disponível via CLI.
- **Formatação monetária**: Reutilizar `formatCurrency` existente (ver `shared/utils`).
- **Badge de warning**: Se shadcn `Badge` não tem variante `warning`, criar ou usar `className` custom com Tailwind.
- **Estado de loading**: O `useInvoice` hook deve tratar loading e error states (skeleton, retry).

## Critérios de Sucesso

- Card de cartão mostra "Fatura Atual" com valor absoluto do saldo
- Card mostra limite total e disponível
- Card mostra dias de fechamento e vencimento
- Alerta amarelo quando disponível < 20% do limite
- Alerta vermelho quando limite esgotado
- Badge verde para crédito a favor (saldo positivo)
- Botão "Ver Fatura" abre drawer lateral
- Drawer mostra resumo da fatura com período, vencimento, total e valor a pagar
- Drawer lista transações com informação de parcela (X/Y)
- Navegação entre meses funciona
- Dialog de pagamento pré-preenche valor total
- Pagamento parcial e total funcionam
- Contas normais mantêm exibição inalterada
- Contas cartão legacy (sem CreditCardDetails) não quebram
- Todos os testes passam
- Build e lint passam
```
