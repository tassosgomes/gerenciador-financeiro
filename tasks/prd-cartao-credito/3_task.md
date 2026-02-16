```markdown
---
status: pending
parallelizable: false
blocked_by: ["1.0", "2.0"]
---

<task_context>
<domain>domain/serviço</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>1.0, 2.0</dependencies>
<unblocks>"4.0", "7.0", "8.0"</unblocks>
</task_context>

# Tarefa 3.0: CreditCardDomainService e Integração com TransactionDomainService

## Visão Geral

Criar o `CreditCardDomainService` — responsável por cálculo do período de fatura (fechamento) e total de fatura — e integrar a validação de limite de crédito no fluxo existente do `TransactionDomainService`. Quando uma transação de débito é criada em conta tipo Cartão, o `TransactionDomainService` deve chamar `Account.ValidateCreditLimit(amount)` antes de aplicar o débito.

## Requisitos

- PRD F3 req 13: Se `EnforceCreditLimit=true`, rejeitar débito acima do limite
- PRD F4 req 17: Fatura agrupa transações entre fechamento do mês anterior e fechamento do mês atual
- PRD F4 req 18: Calcular valor total da fatura (soma dos débitos − soma dos créditos no período)
- Techspec: `CreditCardDomainService` com `CalculateInvoicePeriod` e `CalculateInvoiceTotal`
- Techspec: Integrar `ValidateCreditLimit` no `ApplyDebit` flow do `TransactionDomainService`
- `rules/dotnet-architecture.md`: Domain services operam sobre entidades sem dependência de infra

## Subtarefas

### CreditCardDomainService

- [ ] 3.1 Criar `CreditCardDomainService` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Service/CreditCardDomainService.cs`:
  - Método `CalculateInvoicePeriod(int closingDay, int month, int year)` retorna `(DateTime start, DateTime end)`:
    - `start`: dia seguinte ao fechamento do mês anterior (closingDay + 1 do mês anterior)
    - `end`: dia de fechamento do mês atual
    - Tratar troca de ano (ex: fechamento dia 3 de janeiro → start: 4 de dezembro do ano anterior)
  - Método `CalculateInvoiceTotal(IEnumerable<Transaction> transactions)` retorna `decimal`:
    - Soma de débitos (tipo `Debit`) como positivo
    - Soma de créditos (tipo `Credit`) como negativo (abatimento)
    - Resultado é o valor líquido da fatura

### Integração com TransactionDomainService

- [ ] 3.2 Estender `TransactionDomainService.CreateTransaction(...)`:
  - Antes de chamar `ApplyBalanceImpact`, verificar se a transação é de débito (`TransactionType.Debit`)
  - Se for débito, chamar `account.ValidateCreditLimit(amount)` antes de `account.ApplyDebit(amount)`
  - A chamada é segura para contas não-cartão (bypass interno no `ValidateCreditLimit`)

### Testes Unitários

- [ ] 3.3 Criar testes para `CreditCardDomainService` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Service/CreditCardDomainServiceTests.cs`:
  - `CalculateInvoicePeriod_January_ShouldCrossYear` (fechamento dia 3 de janeiro → start: 4/dez do ano anterior)
  - `CalculateInvoicePeriod_RegularMonth_ShouldReturnCorrectPeriod` (fechamento dia 10 de março → start: 11/fev, end: 10/mar)
  - `CalculateInvoicePeriod_ClosingDay28_ShouldHandleFebruary` (edge case com dia 28)
  - `CalculateInvoicePeriod_December_ShouldHandleYearEnd` (fechamento dia 5 de dezembro)
  - `CalculateInvoiceTotal_WithDebitsOnly_ShouldReturnPositiveSum`
  - `CalculateInvoiceTotal_WithDebitsAndCredits_ShouldReturnNetAmount`
  - `CalculateInvoiceTotal_WithCreditsOnly_ShouldReturnNegativeAmount` (crédito a favor)
  - `CalculateInvoiceTotal_WithNoTransactions_ShouldReturnZero`

- [ ] 3.4 Estender testes em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Service/TransactionDomainServiceTests.cs`:
  - `CreateTransaction_DebitOnCreditCardWithEnforceLimit_ExceedsLimit_ShouldThrowCreditLimitExceededException`
  - `CreateTransaction_DebitOnCreditCardWithEnforceLimit_WithinLimit_ShouldSucceed`
  - `CreateTransaction_DebitOnCreditCardWithoutEnforceLimit_ExceedsLimit_ShouldSucceed`
  - `CreateTransaction_DebitOnRegularAccount_ShouldNotCallValidateCreditLimit` (bypass)
  - `CreateTransaction_CreditOnCreditCard_ShouldNotValidateLimit` (créditos nunca são bloqueados)

### Validação

- [ ] 3.5 Validar build com `dotnet build` a partir de `backend/`
- [ ] 3.6 Executar todos os testes unitários com `dotnet test`

## Sequenciamento

- Bloqueado por: 1.0 (CreditCardDetails), 2.0 (Account extensão com ValidateCreditLimit)
- Desbloqueia: 4.0 (EF Config precisa de domain completo), 7.0 (GetInvoiceQuery usa CalculateInvoicePeriod), 8.0 (PayInvoice reutiliza TransferDomainService)
- Paralelizável: Não

## Detalhes de Implementação

### CreditCardDomainService (conforme techspec)

```csharp
public class CreditCardDomainService
{
    /// <summary>
    /// Calcula o período de fatura com base no dia de fechamento.
    /// Start: dia seguinte ao fechamento do mês anterior.
    /// End: dia de fechamento do mês do parâmetro.
    /// </summary>
    public (DateTime start, DateTime end) CalculateInvoicePeriod(
        int closingDay, int month, int year)
    {
        // End: dia de fechamento do mês atual
        var end = new DateTime(year, month, closingDay);

        // Start: dia seguinte ao fechamento do mês anterior
        var previousMonth = end.AddMonths(-1);
        var start = previousMonth.AddDays(1);

        return (start, end);
    }

    /// <summary>
    /// Calcula o total da fatura: soma de débitos (positivo) - soma de créditos (negativo).
    /// </summary>
    public decimal CalculateInvoiceTotal(IEnumerable<Transaction> transactions)
    {
        return transactions.Sum(t =>
            t.Type == TransactionType.Debit ? t.Amount : -t.Amount);
    }
}
```

### Integração em TransactionDomainService

O ponto de integração é no método que aplica o impacto de saldo. Antes do `ApplyDebit`, chamar:

```csharp
// Dentro do método que cria transação de débito:
if (transaction.Type == TransactionType.Debit)
{
    account.ValidateCreditLimit(transaction.Amount);  // Bypass automático para não-cartão
    account.ApplyDebit(transaction.Amount);
}
```

### Observações

- `CreditCardDomainService` é um domain service puro sem dependências externas (não injeta repositórios)
- `CalculateInvoicePeriod` restringe `closingDay` a 1-28 (validação já feita no `CreditCardDetails.Create`)
- A integração no `TransactionDomainService` é minimamente invasiva — uma única linha de chamada antes do `ApplyDebit` existente

## Critérios de Sucesso

- `CreditCardDomainService` compila e não depende de infra
- `CalculateInvoicePeriod` retorna datas corretas para meses normais e edge cases (janeiro, dezembro, dia 28)
- `CalculateInvoiceTotal` soma débitos e subtrai créditos corretamente
- `TransactionDomainService` chama `ValidateCreditLimit` antes de `ApplyDebit` para transações de débito
- Transação de débito em cartão com limite rígido e valor acima do disponível → `CreditLimitExceededException`
- Transação de débito em cartão com limite informativo → aceita sem exceção
- Transação de débito em conta corrente → sem alteração de comportamento (regressão zero)
- Transação de crédito em cartão → sem validação de limite (PRD req 16)
- Todos os testes unitários passam
- Build compila sem erros
```
