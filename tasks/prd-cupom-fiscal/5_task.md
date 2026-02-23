---
status: pending
parallelizable: false
blocked_by: ["4.0"]
---

<task_context>
<domain>backend/api</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"6.0"</unblocks>
</task_context>

# Tarefa 5.0: API — Controller, Requests e Exception Handling

## Visão Geral

Criar o `ReceiptsController` com os endpoints REST para lookup e import de cupom fiscal, os Request DTOs de entrada, e estender o `GlobalExceptionHandler` para mapear as novas domain exceptions em respostas HTTP adequadas (ProblemDetails RFC 7807). Também inclui a adição do endpoint `GET /api/v1/transactions/{id}/receipt` no `TransactionsController` (ou no `ReceiptsController`, conforme padrão REST). Esta tarefa finaliza o backend com testes HTTP Integration.

## Requisitos

- Criar `ReceiptsController` com endpoints `POST /api/v1/receipts/lookup` e `POST /api/v1/receipts/import`
- Criar endpoint `GET /api/v1/transactions/{id}/receipt` para retornar itens do cupom
- Criar Request DTOs: `LookupReceiptRequest`, `ImportReceiptRequest`
- Estender `GlobalExceptionHandler` com mappings das 5 novas exceptions
- Todos os endpoints requerem `[Authorize]`
- Testes HTTP Integration com `WebApplicationFactory`

## Subtarefas

- [ ] 5.1 Criar Request DTOs em `Controllers/Requests/`
  - `LookupReceiptRequest` — campo `Input` (string, chave ou URL)
  - `ImportReceiptRequest` — campos `AccessKey` (string), `AccountId` (Guid), `CategoryId` (Guid), `Description` (string), `CompetenceDate` (DateOnly), `OperationId` (string?)

- [ ] 5.2 Criar `ReceiptsController` em `Controllers/ReceiptsController.cs`
  - Rota base: `api/v1/receipts`
  - `[Authorize]` no controller
  - Injetar `IDispatcher`
  - **`POST /lookup`**:
    - Recebe `LookupReceiptRequest`
    - Converte para `LookupReceiptCommand`
    - Retorna `200 OK` com `ReceiptLookupResponse`
  - **`POST /import`**:
    - Recebe `ImportReceiptRequest`
    - Converte para `ImportReceiptCommand` (inclui userId dos claims)
    - Retorna `201 Created` com `ImportReceiptResponse`

- [ ] 5.3 Adicionar endpoint de receipt no `TransactionsController` (ou `ReceiptsController`)
  - **`GET /api/v1/transactions/{id}/receipt`**:
    - Recebe `id` (Guid) da rota
    - Converte para `GetTransactionReceiptQuery`
    - Retorna `200 OK` com `TransactionReceiptResponse`
    - Retorna `404 Not Found` se transação não existe ou não tem cupom

- [ ] 5.4 Estender `GlobalExceptionHandler` com novas exceptions
  - `InvalidAccessKeyException` → `400 Bad Request` com mensagem sobre formato correto (44 dígitos numéricos)
  - `NfceNotFoundException` → `404 Not Found` com mensagem que a NFC-e não está disponível na SEFAZ
  - `SefazUnavailableException` → `502 Bad Gateway` com mensagem sobre SEFAZ indisponível, orientando tentar novamente
  - `SefazParsingException` → `502 Bad Gateway` com mensagem sobre erro ao processar dados da SEFAZ
  - `DuplicateReceiptException` → `409 Conflict` com mensagem sobre cupom já importado

- [ ] 5.5 Testes HTTP Integration (`WebApplicationFactory`)
  - **POST /api/v1/receipts/lookup:**
    - Teste lookup bem-sucedido (mock ISefazNfceService no DI retorna dados)
    - Teste com input inválido (vazio) → 400
    - Teste com SEFAZ indisponível (mock lança SefazUnavailableException) → 502
    - Teste com NFC-e não encontrada (mock lança NfceNotFoundException) → 404
  - **POST /api/v1/receipts/import:**
    - Teste importação bem-sucedida → 201 com response completo
    - Teste com chave duplicada → 409
    - Teste com dados inválidos (accountId inválido) → 400
    - Verificar que transação, estabelecimento e itens existem no banco após import
  - **GET /api/v1/transactions/{id}/receipt:**
    - Teste com transação que tem cupom → 200 com itens e estabelecimento
    - Teste com transação sem cupom → 404
    - Teste com transação inexistente → 404
  - **Autenticação:**
    - Teste sem token → 401 em todos os endpoints
  - **Integração com cancelamento:**
    - Importar cupom → cancelar transação → verificar que itens e estabelecimento foram removidos

## Sequenciamento

- Bloqueado por: 4.0 (Commands, Queries e Handlers)
- Desbloqueia: 6.0 (Frontend — Tipos, API, Hooks)
- Paralelizável: Não (depende do backend completo)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| `LookupReceiptRequest.cs` | `backend/1-Services/GestorFinanceiro.Financeiro.API/Controllers/Requests/` |
| `ImportReceiptRequest.cs` | `backend/1-Services/GestorFinanceiro.Financeiro.API/Controllers/Requests/` |
| `ReceiptsController.cs` | `backend/1-Services/GestorFinanceiro.Financeiro.API/Controllers/` |
| `GlobalExceptionHandler.cs` | `backend/1-Services/GestorFinanceiro.Financeiro.API/Middleware/` |
| Testes HTTP | `backend/5-Tests/GestorFinanceiro.Financeiro.HttpIntegrationTests/` |

### Padrões a Seguir

- Consultar controllers existentes (ex: `TransactionsController`, `AccountsController`) para padrão de controller
- Consultar Request DTOs existentes para consistência
- Consultar `GlobalExceptionHandler` para padrão de mapeamento de exceptions → ProblemDetails
- Consultar testes HTTP integration existentes para padrão de WebApplicationFactory e mock de serviços

### Exemplo de Controller

```csharp
[ApiController]
[Route("api/v1/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ReceiptsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("lookup")]
    public async Task<ActionResult<ReceiptLookupResponse>> Lookup(
        LookupReceiptRequest request, CancellationToken ct)
    {
        var command = new LookupReceiptCommand(request.Input);
        var result = await _dispatcher.DispatchAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportReceiptResponse>> Import(
        ImportReceiptRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new ImportReceiptCommand(
            request.AccessKey, request.AccountId, request.CategoryId,
            request.Description, request.CompetenceDate, request.OperationId);
        var result = await _dispatcher.DispatchAsync(command, ct);
        return CreatedAtAction(nameof(Import), result);
    }
}
```

### Mapeamento de Exceptions no GlobalExceptionHandler

```csharp
// Adicionar aos cases existentes no switch
InvalidAccessKeyException => (StatusCodes.Status400BadRequest, "Chave de acesso inválida", ex.Message),
NfceNotFoundException => (StatusCodes.Status404NotFound, "NFC-e não encontrada", ex.Message),
SefazUnavailableException => (StatusCodes.Status502BadGateway, "SEFAZ indisponível", ex.Message),
SefazParsingException => (StatusCodes.Status502BadGateway, "Erro ao processar dados", ex.Message),
DuplicateReceiptException => (StatusCodes.Status409Conflict, "Cupom já importado", ex.Message),
```

## Critérios de Sucesso

- `POST /api/v1/receipts/lookup` retorna preview da NFC-e com todos os campos
- `POST /api/v1/receipts/import` cria transação e retorna 201 com response completo
- `GET /api/v1/transactions/{id}/receipt` retorna itens e estabelecimento
- Todas as 5 exceptions mapeadas corretamente para HTTP status codes
- Respostas de erro seguem formato ProblemDetails (RFC 7807)
- Todos os endpoints exigem autenticação (401 sem token)
- Testes HTTP integration passam (mínimo 12 testes)
- Testes existentes continuam passando
- Backend completo e funcional para o recurso de cupom fiscal
