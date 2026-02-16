```markdown
---
status: pending
parallelizable: false
blocked_by: ["6.0", "7.0", "8.0"]
---

<task_context>
<domain>api/controller</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>6.0, 7.0, 8.0</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 9.0: Endpoints API (Controllers e Requests)

## Visão Geral

Adaptar `AccountsController` para aceitar campos de cartão no POST e PUT, criar `InvoicesController` com endpoints de consulta de fatura e pagamento de fatura, e criar/adaptar os DTOs de request correspondentes. A API segue os padrões REST existentes: versionamento `v1`, recursos em inglês plural, codes HTTP corretos.

## Requisitos

- Techspec: `POST /api/v1/accounts` adaptado para aceitar campos de cartão quando `type == 2` (Cartão)
- Techspec: `PUT /api/v1/accounts/{id}` adaptado para campos de cartão
- Techspec: `GET /api/v1/accounts/{id}/invoices?month=2&year=2026` — consulta de fatura
- Techspec: `POST /api/v1/accounts/{id}/invoices/pay` — pagamento de fatura
- PRD F1 req 7: Demais tipos mantêm o cadastro atual inalterado
- `rules/restful.md`: versionamento v1, recursos plural, POST para mutações, 201/200/400/404

## Subtarefas

### Adaptação de Requests

- [ ] 9.1 Estender `CreateAccountRequest` para incluir campos opcionais de cartão:
  ```csharp
  public record CreateAccountRequest(
      string Name,
      AccountType Type,
      decimal InitialBalance,
      bool AllowNegativeBalance,
      // Campos de cartão (opcionais — obrigatórios apenas quando Type == Cartao)
      decimal? CreditLimit,
      int? ClosingDay,
      int? DueDay,
      Guid? DebitAccountId,
      bool? EnforceCreditLimit
  );
  ```

- [ ] 9.2 Estender `UpdateAccountRequest` com campos de cartão similares

- [ ] 9.3 Criar `PayInvoiceRequest`:
  ```csharp
  public record PayInvoiceRequest(
      decimal Amount,
      DateTime CompetenceDate,
      string? OperationId
  );
  ```

### Adaptação de AccountsController

- [ ] 9.4 Adaptar endpoint `POST /api/v1/accounts`:
  - Mapear campos de cartão do request para o `CreateAccountCommand`
  - Quando `type != Cartao`, campos de cartão no request são ignorados
  - Retorna `201 Created` com `AccountResponse` (que agora inclui `CreditCard?`)

- [ ] 9.5 Adaptar endpoint `PUT /api/v1/accounts/{id}`:
  - Mapear campos de cartão do request para o `UpdateAccountCommand`
  - Retorna `200 OK` com `AccountResponse` atualizado

### Novo InvoicesController

- [ ] 9.6 Criar `InvoicesController` em `1-Services/GestorFinanceiro.Financeiro.API/Controllers/InvoicesController.cs`:
  - Rota base: `api/v1/accounts/{accountId}/invoices` (sub-recurso de accounts)
  - Injeção de `IDispatcher` (padrão existente)

- [ ] 9.7 Implementar `GET /api/v1/accounts/{accountId}/invoices`:
  - Query parameters: `month` (int), `year` (int)
  - Mapeia para `GetInvoiceQuery(accountId, month, year)`
  - Retorna `200 OK` com `InvoiceResponse`
  - Retorna `404 Not Found` se conta não existe
  - Retorna `400 Bad Request` se conta não é cartão

- [ ] 9.8 Implementar `POST /api/v1/accounts/{accountId}/invoices/pay`:
  - Body: `PayInvoiceRequest`
  - Mapeia para `PayInvoiceCommand(accountId, amount, competenceDate, userId, operationId)`
  - Extrai `userId` do token JWT (padrão existente)
  - Retorna `200 OK` com `IReadOnlyList<TransactionResponse>`
  - Retorna `404 Not Found` se conta não existe
  - Retorna `400 Bad Request` se conta não é cartão ou validação falha

### Testes de Integração HTTP

- [ ] 9.9 Estender testes de integração HTTP em `5-Tests/GestorFinanceiro.Financeiro.HttpIntegrationTests/`:
  - `POST_CreateCreditCardAccount_ShouldReturn201WithCreditCardDetails`
  - `POST_CreateCreditCardAccount_WithInvalidClosingDay_ShouldReturn400`
  - `POST_CreateRegularAccount_ShouldReturn201WithoutCreditCardDetails`
  - `PUT_UpdateCreditCardAccount_ShouldReturn200WithUpdatedDetails`
  - `GET_Invoice_WithValidCard_ShouldReturn200WithInvoice`
  - `GET_Invoice_WithNonCardAccount_ShouldReturn400`
  - `POST_PayInvoice_WithValidPayment_ShouldReturn200WithTransactions`
  - `POST_PayInvoice_WithInactiveDebitAccount_ShouldReturn400`

### Validação

- [ ] 9.10 Validar build com `dotnet build`
- [ ] 9.11 Executar testes de integração HTTP

## Sequenciamento

- Bloqueado por: 6.0 (Commands adaptados), 7.0 (GetInvoiceQuery), 8.0 (PayInvoiceCommand)
- Desbloqueia: 10.0 (Frontend precisa de API pronta para integrar)
- Paralelizável: Não

## Detalhes de Implementação

### InvoicesController (conforme techspec)

```csharp
[ApiController]
[Route("api/v1/accounts/{accountId}/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public InvoicesController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoice(
        [FromRoute] Guid accountId,
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        var query = new GetInvoiceQuery(accountId, month, year);
        var result = await _dispatcher.QueryAsync(query, ct);
        return Ok(result);
    }

    [HttpPost("pay")]
    public async Task<IActionResult> PayInvoice(
        [FromRoute] Guid accountId,
        [FromBody] PayInvoiceRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId(); // Extension method existente
        var command = new PayInvoiceCommand(
            accountId,
            request.Amount,
            request.CompetenceDate,
            userId,
            request.OperationId);
        var result = await _dispatcher.SendAsync(command, ct);
        return Ok(result);
    }
}
```

### Adaptação de AccountsController (POST)

```csharp
// Dentro do POST existente:
var command = new CreateAccountCommand(
    request.Name,
    request.Type,
    request.InitialBalance,
    request.AllowNegativeBalance,
    userId,
    operationId,
    // Novos campos de cartão
    request.CreditLimit,
    request.ClosingDay,
    request.DueDay,
    request.DebitAccountId,
    request.EnforceCreditLimit
);
```

### Exemplos de Request/Response

**POST /api/v1/accounts (cartão de crédito):**
```json
// Request
{
  "name": "Nubank",
  "type": 2,
  "creditLimit": 5000.00,
  "closingDay": 3,
  "dueDay": 10,
  "debitAccountId": "guid-da-conta-corrente",
  "enforceCreditLimit": true
}

// Response 201
{
  "id": "guid",
  "name": "Nubank",
  "type": 2,
  "balance": 0,
  "allowNegativeBalance": true,
  "isActive": true,
  "creditCard": {
    "creditLimit": 5000.00,
    "closingDay": 3,
    "dueDay": 10,
    "debitAccountId": "guid-da-conta-corrente",
    "enforceCreditLimit": true,
    "availableLimit": 5000.00
  }
}
```

**GET /api/v1/accounts/{id}/invoices?month=2&year=2026:**
```json
// Response 200
{
  "accountId": "guid",
  "accountName": "Nubank",
  "month": 2,
  "year": 2026,
  "periodStart": "2026-01-04",
  "periodEnd": "2026-02-03",
  "dueDate": "2026-02-10",
  "totalAmount": 1500.00,
  "previousBalance": 0,
  "amountDue": 1500.00,
  "transactions": [...]
}
```

### Observações

- **Retrocompatibilidade**: O campo `CreditCard` no `AccountResponse` é nullable. Clientes existentes que consomem a API ignoram campos desconhecidos (JSON padrão).
- **`initialBalance` e `allowNegativeBalance`**: Quando `type == Cartao`, esses campos são ignorados pelo handler (forçados a 0 e true). O request pode enviá-los, mas são descartados.
- **Sub-recurso**: `invoices` é sub-recurso de `accounts` — URL: `/api/v1/accounts/{accountId}/invoices`. Isso segue o padrão REST de recursos aninhados.

## Critérios de Sucesso

- `POST /api/v1/accounts` aceita campos de cartão e retorna `AccountResponse` com `CreditCard?`
- `POST /api/v1/accounts` sem campos de cartão (tipo Corrente) continua funcionando (retrocompatibilidade)
- `PUT /api/v1/accounts/{id}` aceita campos de cartão e atualiza
- `GET /api/v1/accounts/{id}/invoices` retorna `InvoiceResponse` correto
- `POST /api/v1/accounts/{id}/invoices/pay` executa pagamento e retorna transações
- Erros retornam HTTP codes corretos (400, 404)
- Testes de integração HTTP passam
- Build compila sem erros
```
