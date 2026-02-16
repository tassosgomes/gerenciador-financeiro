```markdown
---
status: pending
parallelizable: true
blocked_by: ["10.0"]
---

<task_context>
<domain>frontend/dashboard</domain>
<type>implementation</type>
<scope>enhancement</scope>
<complexity>low</complexity>
<dependencies>10.0</dependencies>
<unblocks></unblocks>
</task_context>

# Tarefa 12.0: Frontend — Dashboard Estendido

## Visão Geral

Estender o card "Dívida de cartão total" no dashboard para exibir adicionalmente o limite total agregado de todos os cartões e o percentual de utilização. O backend precisa de extensão no `DashboardRepository` e `DashboardSummaryResponse` para fornecer esses dados agregados. O frontend adapta o `SummaryCards` para exibir as novas informações.

## Requisitos

- PRD F7 req 40: Card de dívida continua exibindo soma das faturas de todos os cartões
- PRD F7 req 41: Exibir limite total agregado e percentual de utilização
- PRD F7 req 42: Click no card direciona para página de contas filtrada por tipo Cartão
- PRD F7 req 43: Saldo Total NÃO deve incluir limite disponível dos cartões
- Techspec: `DashboardSummaryResponse` estendido com `TotalCreditLimit?` e `CreditUtilizationPercent?`
- Techspec: `DashboardRepository` com query que faz JOIN com `credit_card_details`

## Subtarefas

### Backend — DashboardRepository

- [ ] 12.1 Estender `DashboardSummaryResponse` com campos nullable:
  ```csharp
  public record DashboardSummaryResponse(
      decimal TotalBalance,
      decimal MonthlyIncome,
      decimal MonthlyExpenses,
      decimal CreditCardDebt,
      // Novos campos
      decimal? TotalCreditLimit,         // Soma de credit_limit de todos os cartões ativos
      decimal? CreditUtilizationPercent  // (|CreditCardDebt| / TotalCreditLimit) * 100
  );
  ```

- [ ] 12.2 Estender query em `DashboardRepository` para calcular novos campos:
  ```csharp
  // Dentro do método que retorna DashboardSummaryResponse:
  var totalCreditLimit = await _dbContext.Accounts
      .Where(a => a.Type == AccountType.Cartao && a.IsActive && a.CreditCard != null)
      .SumAsync(a => a.CreditCard!.CreditLimit, ct);

  var creditUtilizationPercent = totalCreditLimit > 0
      ? Math.Round(Math.Abs(creditCardDebt) / totalCreditLimit * 100, 1)
      : (decimal?)null;
  ```

- [ ] 12.3 Testes unitários para novas queries:
  - `GetTotalCreditLimit_WithActiveCards_ShouldSumLimits`
  - `GetTotalCreditLimit_WithNoCards_ShouldReturnNull`
  - `CreditUtilizationPercent_ShouldCalculateCorrectly`

### Frontend — Tipos

- [ ] 12.4 Estender `DashboardSummaryResponse` em tipos TypeScript:
  ```typescript
  export interface DashboardSummaryResponse {
    totalBalance: number;
    monthlyIncome: number;
    monthlyExpenses: number;
    creditCardDebt: number;
    totalCreditLimit: number | null;
    creditUtilizationPercent: number | null;
  }
  ```

### Frontend — SummaryCards

- [ ] 12.5 Adaptar card "Dívida de cartão total" em `SummaryCards.tsx`:
  - Manter valor principal: soma das faturas (já existente)
  - Adicionar sub-informação: "Limite total: R$ {totalCreditLimit}"
  - Adicionar barra de progresso visual: `{creditUtilizationPercent}% utilizado`
  - Cor da barra: verde (< 50%), amarela (50-80%), vermelha (> 80%)
  - Se `totalCreditLimit === null`: não exibir sub-informações (sem cartões configurados)

- [ ] 12.6 Implementar click no card de dívida (PRD req 42):
  - Ao clicar, navegar para `/contas?type=2` (página de contas filtrada por tipo Cartão)
  - Usar `useNavigate` do React Router
  - A página de contas precisará ler o query param `type` e filtrar (se não implementado, adicionar filtro)

### Testes Frontend

- [ ] 12.7 Criar/estender testes para `SummaryCards`:
  - `should display total credit limit when available`
  - `should display utilization percentage`
  - `should show green bar when utilization < 50%`
  - `should show red bar when utilization > 80%`
  - `should not display credit info when totalCreditLimit is null`
  - `should navigate to accounts page filtered by Cartao on click`

### Validação

- [ ] 12.8 Validar build backend: `dotnet build`
- [ ] 12.9 Validar build frontend: `npm run build`
- [ ] 12.10 Executar testes: `dotnet test` e `npm test`

## Sequenciamento

- Bloqueado por: 10.0 (Tipos TypeScript devem existir)
- Desbloqueia: Nenhum (última tarefa)
- Paralelizável: Sim — com 11.0 (Card e Drawer são independentes do Dashboard)

## Detalhes de Implementação

### SummaryCards — Card de Dívida Estendido

```tsx
<Card
  onClick={() => navigate('/contas?type=2')}
  className="cursor-pointer hover:bg-accent/50 transition-colors"
>
  <CardHeader className="flex flex-row items-center justify-between">
    <CardTitle className="text-sm font-medium">Dívida de Cartão</CardTitle>
    <CreditCard className="h-4 w-4 text-muted-foreground" />
  </CardHeader>
  <CardContent>
    <div className="text-2xl font-bold">
      {formatCurrency(summary.creditCardDebt)}
    </div>

    {summary.totalCreditLimit && (
      <div className="mt-2 space-y-1">
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>Limite total: {formatCurrency(summary.totalCreditLimit)}</span>
          <span>{summary.creditUtilizationPercent}% utilizado</span>
        </div>
        <Progress
          value={summary.creditUtilizationPercent ?? 0}
          className={cn(
            summary.creditUtilizationPercent! > 80 ? 'bg-red-200' :
            summary.creditUtilizationPercent! > 50 ? 'bg-yellow-200' :
            'bg-green-200'
          )}
        />
      </div>
    )}
  </CardContent>
</Card>
```

### Observações

- **Campos nullable**: `totalCreditLimit` e `creditUtilizationPercent` são `null` quando não há cartões de crédito configurados com `CreditCardDetails`. Isso ocorre para contas legacy ou quando não há cartões ativos.
- **Progress component**: Usar o `Progress` do shadcn/ui. Se não existir, implementar com `div` e `width: {percent}%`.
- **Navegação**: O click no card navega para a lista de contas com filtro. Se a página de contas não suporta filtro por query param, será necessário adicionar suporte (escopo mínimo: ler `searchParams.get('type')` e filtrar).
- **Saldo Total**: Conforme PRD req 43, o TotalBalance já subtrai o saldo negativo dos cartões (funcionalidade existente no `GetTotalBalanceAsync`). Nenhuma mudança necessária.

## Critérios de Sucesso

- `DashboardSummaryResponse` inclui `TotalCreditLimit` e `CreditUtilizationPercent`
- Query agrega limites de todos os cartões ativos com `CreditCardDetails`
- Percentual de utilização calculado corretamente: `|debt| / totalLimit * 100`
- Card mostra barra de progresso com cores por faixa (verde/amarelo/vermelho)
- Card é clicável e navega para contas filtradas por Cartão
- Campos nullable tratados quando não há cartões
- Sem alteração no cálculo de Saldo Total (regressão zero)
- Todos os testes passam (backend e frontend)
- Build compila sem erros
```
