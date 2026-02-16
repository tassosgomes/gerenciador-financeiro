```markdown
---
status: pending
parallelizable: true
blocked_by: ["3.0", "5.0"]
---

<task_context>
<domain>application/query</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>3.0, 5.0</dependencies>
<unblocks>"9.0"</unblocks>
</task_context>

# Tarefa 7.0: Query de Fatura Mensal

## Visão Geral

Implementar a query `GetInvoiceQuery` que retorna a fatura (invoice) de um cartão de crédito para um mês/ano específico. A fatura é **calculada** (não materializada) — agrupa transações com status `Paid` no período de fechamento, calcula total e retorna lista de transações com informações de parcelamento.

## Requisitos

- PRD F4 req 17: Fatura agrupa transações `Paid` entre fechamento do mês anterior e do mês atual
- PRD F4 req 18: Calcular valor total da fatura
- PRD F4 req 19: Disponibilizar fatura do mês atual (aberta) e de meses anteriores (fechadas)
- PRD F4 req 22: Transações parceladas devem exibir "Parcela X/Y"
- PRD F5 req 29: Pagamento acima do valor gera crédito a favor, abatido na próxima fatura
- Techspec: `GetInvoiceQuery(AccountId, Month, Year)` → `InvoiceResponse`
- Techspec: Usa `CreditCardDomainService.CalculateInvoicePeriod` e `CalculateInvoiceTotal`
- Techspec: Usa `ITransactionRepository.GetByAccountAndPeriodAsync`

## Subtarefas

### Query e Handler

- [ ] 7.1 Criar `GetInvoiceQuery` em `2-Application/GestorFinanceiro.Financeiro.Application/Queries/Invoice/GetInvoiceQuery.cs`:
  ```csharp
  public record GetInvoiceQuery(
      Guid AccountId,
      int Month,
      int Year
  ) : IQuery<InvoiceResponse>;
  ```

- [ ] 7.2 Criar `GetInvoiceQueryHandler` em `2-Application/GestorFinanceiro.Financeiro.Application/Queries/Invoice/GetInvoiceQueryHandler.cs`:
  - Carregar conta via `IAccountRepository.GetByIdAsync`
  - Validar que conta existe e é tipo Cartão (`CreditCard != null`)
  - Calcular período de fatura via `CreditCardDomainService.CalculateInvoicePeriod(closingDay, month, year)`
  - Buscar transações via `ITransactionRepository.GetByAccountAndPeriodAsync(accountId, start, end, ct)`
  - Calcular total via `CreditCardDomainService.CalculateInvoiceTotal(transactions)`
  - Calcular `PreviousBalance` (saldo positivo do cartão que será abatido)
  - Calcular `AmountDue` = `TotalAmount - PreviousBalance` (mínimo 0)
  - Montar `InvoiceResponse` com lista de `InvoiceTransactionDto`

- [ ] 7.3 Criar `GetInvoiceQueryValidator`:
  - `AccountId` não pode ser `Guid.Empty`
  - `Month` entre 1 e 12
  - `Year` > 0

### DTOs

- [ ] 7.4 Criar `InvoiceResponse` em `2-Application/GestorFinanceiro.Financeiro.Application/Dtos/InvoiceResponse.cs`:
  ```csharp
  public record InvoiceResponse(
      Guid AccountId,
      string AccountName,
      int Month,
      int Year,
      DateTime PeriodStart,
      DateTime PeriodEnd,
      DateTime DueDate,
      decimal TotalAmount,
      decimal PreviousBalance,
      decimal AmountDue,
      IReadOnlyList<InvoiceTransactionDto> Transactions
  );
  ```

- [ ] 7.5 Criar `InvoiceTransactionDto` em `2-Application/GestorFinanceiro.Financeiro.Application/Dtos/InvoiceTransactionDto.cs`:
  ```csharp
  public record InvoiceTransactionDto(
      Guid Id,
      string Description,
      decimal Amount,
      TransactionType Type,
      DateTime CompetenceDate,
      int? InstallmentNumber,
      int? TotalInstallments
  );
  ```

### Testes Unitários

- [ ] 7.6 Criar testes para `GetInvoiceQueryHandler` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Application/Queries/GetInvoiceQueryHandlerTests.cs`:
  - `Handle_WithValidCardAndTransactions_ShouldReturnInvoiceResponse`
  - `Handle_WithNoTransactions_ShouldReturnZeroTotalAmount`
  - `Handle_AccountNotFound_ShouldThrowNotFoundException`
  - `Handle_AccountIsNotCard_ShouldThrowDomainException`
  - `Handle_WithParceledTransactions_ShouldIncludeInstallmentInfo`
  - `Handle_ShouldCalculateCorrectPeriod` (verificar start/end via CreditCardDomainService)
  - `Handle_WithPreviousPositiveBalance_ShouldAbateFromTotal`
  - `Handle_WithCreditTransactions_ShouldSubtractFromTotal`

- [ ] 7.7 Criar testes para `GetInvoiceQueryValidator`:
  - `Validate_WithEmptyAccountId_ShouldFail`
  - `Validate_WithInvalidMonth_ShouldFail`
  - `Validate_WithValidParameters_ShouldPass`

### Validação

- [ ] 7.8 Validar build com `dotnet build`
- [ ] 7.9 Executar testes com `dotnet test`

## Sequenciamento

- Bloqueado por: 3.0 (CreditCardDomainService), 5.0 (GetByAccountAndPeriodAsync)
- Desbloqueia: 9.0 (InvoicesController)
- Paralelizável: Sim — com 6.0 e 8.0 (compartilham dependências 3.0 e 5.0, sem dependência entre si)

## Detalhes de Implementação

### Handler (conforme techspec)

```csharp
public class GetInvoiceQueryHandler : IQueryHandler<GetInvoiceQuery, InvoiceResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly CreditCardDomainService _creditCardDomainService;

    public async Task<InvoiceResponse> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var account = await _accountRepository.GetByIdAsync(query.AccountId, ct);
        if (account == null)
            throw new NotFoundException("Conta não encontrada.");
        if (account.CreditCard == null)
            throw new DomainException("Conta não é um cartão de crédito.");

        var (start, end) = _creditCardDomainService.CalculateInvoicePeriod(
            account.CreditCard.ClosingDay, query.Month, query.Year);

        var transactions = await _transactionRepository.GetByAccountAndPeriodAsync(
            account.Id, start, end, ct);

        var totalAmount = _creditCardDomainService.CalculateInvoiceTotal(transactions);

        // PreviousBalance: se o cartão tem saldo positivo (crédito a favor),
        // esse valor é abatido da fatura
        var previousBalance = account.Balance > 0 ? account.Balance : 0;
        var amountDue = Math.Max(totalAmount - previousBalance, 0);

        // DueDate: dia de vencimento do mês da fatura
        var dueDate = new DateTime(query.Year, query.Month, account.CreditCard.DueDay);

        var transactionDtos = transactions.Select(t => new InvoiceTransactionDto(
            t.Id,
            t.Description,
            t.Amount,
            t.Type,
            t.CompetenceDate,
            t.InstallmentNumber,
            t.TotalInstallments
        )).ToList();

        return new InvoiceResponse(
            account.Id,
            account.Name,
            query.Month,
            query.Year,
            start,
            end,
            dueDate,
            totalAmount,
            previousBalance,
            amountDue,
            transactionDtos
        );
    }
}
```

### Observações

- **PreviousBalance**: No PRD (Decisão 4), pagamento acima do valor gera saldo positivo no cartão. Esse saldo positivo é abatido da próxima fatura. A implementação usa `account.Balance > 0` para detectar crédito a favor.
- **DueDate**: É o dia de vencimento (`DueDay`) no mês/ano da fatura consultada. Se a fatura for do mês atual, a due date pode estar no futuro.
- **InstallmentNumber/TotalInstallments**: Já existem na entidade `Transaction` (`InstallmentNumber`, `TotalInstallments` via `SetInstallmentInfo`). São mapeados diretamente para o DTO.
- **Injeção de CreditCardDomainService**: Como é um domain service puro (sem dependências de infra), pode ser instanciado diretamente ou registrado na DI como transient.

## Critérios de Sucesso

- `GetInvoiceQuery` recebe `AccountId`, `Month`, `Year` e retorna `InvoiceResponse`
- Handler valida que conta existe e é cartão
- Período de fatura é calculado corretamente pelo `CreditCardDomainService`
- Transações são filtradas por período e status `Paid`
- Total é calculado como soma de débitos − créditos
- `PreviousBalance` implementa abatimento de crédito a favor (saldo positivo)
- `AmountDue` nunca é negativo (mínimo 0)
- Transações parceladas incluem `InstallmentNumber` e `TotalInstallments`
- Validator rejeita parâmetros inválidos (month fora de 1-12, accountId vazio)
- Todos os testes passam
- Build compila sem erros
```
