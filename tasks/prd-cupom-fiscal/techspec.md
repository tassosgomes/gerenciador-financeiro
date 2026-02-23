# Especificação Técnica — Importação de Cupom Fiscal (NFC-e)

## Resumo Executivo

Esta Tech Spec define a arquitetura e implementação do recurso de Importação de Cupom Fiscal (NFC-e) para o GestorFinanceiro. A solução é composta por três eixos: (1) um serviço de consulta e parsing de NFC-e da SEFAZ PB via web scraping no backend, (2) novas entidades de domínio (`ReceiptItem`, `Establishment`) vinculadas à `Transaction` existente, e (3) um fluxo de importação no frontend com preview, seleção de conta/categoria e confirmação.

A abordagem segue a Clean Architecture já estabelecida no projeto: a lógica de scraping é implementada na camada de Infra atrás de uma interface de domínio; o fluxo de criação atômica (transação + estabelecimento + itens) é orquestrado por um command handler na camada Application; e o frontend implementa uma página dedicada com stepper (input → preview → confirmação) dentro da feature `transactions` existente.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend                             │
│  TransactionsPage ─→ ImportReceiptPage (wizard 3 steps)     │
│    Step 1: Input chave/URL                                  │
│    Step 2: Preview dados + seleção conta/categoria          │
│    Step 3: Confirmação → POST /api/v1/receipts/lookup       │
│                          POST /api/v1/receipts/import       │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP
┌────────────────────────────▼────────────────────────────────┐
│                     API (Controllers)                       │
│  ReceiptsController                                         │
│    POST /lookup  → LookupReceiptCommand → SefazResponse     │
│    POST /import  → ImportReceiptCommand → TransactionResp   │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│                   Application (Handlers)                    │
│  LookupReceiptCommandHandler                                │
│    → ISefazNfceService.LookupAsync(accessKey)               │
│  ImportReceiptCommandHandler                                │
│    → TransactionDomainService.CreateTransaction(...)        │
│    → ReceiptItemRepository.AddRange(...)                    │
│    → EstablishmentRepository.Add(...)                       │
│    → UnitOfWork.Commit()                                    │
└──────────┬──────────────────────────────┬───────────────────┘
           │                              │
┌──────────▼──────────┐    ┌──────────────▼───────────────────┐
│   Domain (Core)     │    │       Infra (Implementations)    │
│  ReceiptItem        │    │  SefazPbNfceService              │
│  Establishment      │    │    → HttpClient + AngleSharp     │
│  ISefazNfceService  │    │  ReceiptItemRepository           │
│  IReceiptItemRepo   │    │  EstablishmentRepository         │
│  IEstablishmentRepo │    │  EF Core Configs + Migration     │
│  NfceData (DTO)     │    │                                  │
└─────────────────────┘    └──────────────────────────────────┘
```

**Componentes Principais:**

| Componente | Camada | Responsabilidade |
|---|---|---|
| `ISefazNfceService` | Domain (Interface) | Contrato para consulta de NFC-e na SEFAZ |
| `SefazPbNfceService` | Infra | Implementação de scraping HTTP + parsing HTML da SEFAZ PB |
| `ReceiptItem` | Domain (Entity) | Item individual de cupom fiscal vinculado a Transaction |
| `Establishment` | Domain (Entity) | Dados do estabelecimento (razão social, CNPJ) vinculado a Transaction |
| `LookupReceiptCommandHandler` | Application | Consulta NFC-e e retorna preview |
| `ImportReceiptCommandHandler` | Application | Cria transação + estabelecimento + itens atomicamente |
| `ReceiptsController` | API | Endpoints de lookup e import |
| `ImportReceiptPage` | Frontend | Wizard de 3 steps para importação |

---

## Design de Implementação

### Interfaces Principais

```csharp
// Domain/Interface/ISefazNfceService.cs
namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ISefazNfceService
{
    /// <summary>
    /// Consulta uma NFC-e na SEFAZ usando a chave de acesso.
    /// </summary>
    Task<NfceData> LookupAsync(string accessKey, CancellationToken cancellationToken);
}
```

```csharp
// Domain/Dto/NfceData.cs
namespace GestorFinanceiro.Financeiro.Domain.Dto;

/// <summary>
/// Dados extraídos de uma NFC-e da SEFAZ. DTO usado entre camadas.
/// </summary>
public record NfceData(
    string AccessKey,
    string EstablishmentName,
    string EstablishmentCnpj,
    DateTime IssuedAt,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PaidAmount,
    IReadOnlyList<NfceItemData> Items
);

public record NfceItemData(
    string Description,
    string? ProductCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal TotalPrice
);
```

```csharp
// Domain/Interface/IReceiptItemRepository.cs
namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IReceiptItemRepository
{
    Task AddRangeAsync(IEnumerable<ReceiptItem> items, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReceiptItem>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<ReceiptItem> items);
}
```

```csharp
// Domain/Interface/IEstablishmentRepository.cs
namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IEstablishmentRepository
{
    Task<Establishment> AddAsync(Establishment entity, CancellationToken cancellationToken);
    Task<Establishment?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);
    void Remove(Establishment entity);
    Task<bool> ExistsByAccessKeyAsync(string accessKey, CancellationToken cancellationToken);
}
```

### Modelos de Dados

#### Entidades de Domínio

```csharp
// Domain/Entity/ReceiptItem.cs
namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class ReceiptItem : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ProductCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public int ItemOrder { get; private set; }

    public Transaction Transaction { get; private set; } = null!;

    public static ReceiptItem Create(
        Guid transactionId,
        string description,
        string? productCode,
        decimal quantity,
        string unitOfMeasure,
        decimal unitPrice,
        decimal totalPrice,
        int itemOrder,
        string userId)
    {
        var item = new ReceiptItem
        {
            TransactionId = transactionId,
            Description = description,
            ProductCode = productCode,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            ItemOrder = itemOrder,
        };
        item.SetAuditOnCreate(userId);
        return item;
    }
}
```

```csharp
// Domain/Entity/Establishment.cs
namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Establishment : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;
    public string AccessKey { get; private set; } = string.Empty;

    public Transaction Transaction { get; private set; } = null!;

    public static Establishment Create(
        Guid transactionId,
        string name,
        string cnpj,
        string accessKey,
        string userId)
    {
        var establishment = new Establishment
        {
            TransactionId = transactionId,
            Name = name,
            Cnpj = cnpj,
            AccessKey = accessKey,
        };
        establishment.SetAuditOnCreate(userId);
        return establishment;
    }
}
```

#### Propriedades na Transaction (nenhuma nova coluna)

A entidade `Transaction` existente **não será alterada**. O vínculo entre uma transação e seus itens/estabelecimento se dá pela FK `TransactionId` nas novas entidades. A existência de registros em `ReceiptItem` e `Establishment` indica que a transação foi importada via cupom fiscal.

#### Esquema de Banco de Dados

**Tabela `receipt_items`:**

| Coluna | Tipo | Constraints |
|---|---|---|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `transaction_id` | `uuid` | FK → transactions(id), NOT NULL, ON DELETE CASCADE |
| `description` | `varchar(500)` | NOT NULL |
| `product_code` | `varchar(100)` | NULL |
| `quantity` | `numeric(18,4)` | NOT NULL |
| `unit_of_measure` | `varchar(20)` | NOT NULL |
| `unit_price` | `numeric(18,4)` | NOT NULL |
| `total_price` | `numeric(18,2)` | NOT NULL |
| `item_order` | `smallint` | NOT NULL |
| `created_by` | `varchar(100)` | NOT NULL |
| `created_at` | `timestamptz` | NOT NULL, default NOW() |
| `updated_by` | `varchar(100)` | NULL |
| `updated_at` | `timestamptz` | NULL |

Índices:
- `ix_receipt_items_transaction_id` em `transaction_id`

**Tabela `establishments`:**

| Coluna | Tipo | Constraints |
|---|---|---|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `transaction_id` | `uuid` | FK → transactions(id), NOT NULL, UNIQUE, ON DELETE CASCADE |
| `name` | `varchar(300)` | NOT NULL |
| `cnpj` | `varchar(14)` | NOT NULL |
| `access_key` | `varchar(44)` | NOT NULL, UNIQUE |
| `created_by` | `varchar(100)` | NOT NULL |
| `created_at` | `timestamptz` | NOT NULL, default NOW() |
| `updated_by` | `varchar(100)` | NULL |
| `updated_at` | `timestamptz` | NULL |

Índices:
- `ix_establishments_transaction_id` UNIQUE em `transaction_id` (1:1)
- `ix_establishments_access_key` UNIQUE em `access_key` (previne duplicidade)

**Cascade delete**: Ambas as tabelas usam `ON DELETE CASCADE` na FK de `transaction_id`. Quando a transação é deletada no banco, itens e estabelecimento são removidos automaticamente. Para o soft-cancel do sistema (que muda status mas não deleta), o handler de cancelamento fará a remoção explícita via repositórios.

### Endpoints de API

#### `POST /api/v1/receipts/lookup`

Consulta uma NFC-e na SEFAZ e retorna os dados para preview.

**Request:**
```json
{
  "input": "string"  // chave de acesso (44 dígitos) ou URL completa da NFC-e
}
```

**Response (200 OK):**
```json
{
  "accessKey": "12345678901234567890123456789012345678901234",
  "establishmentName": "SUPERMERCADO EXEMPLO LTDA",
  "establishmentCnpj": "12345678000190",
  "issuedAt": "2026-02-20T14:30:00Z",
  "totalAmount": 150.00,
  "discountAmount": 5.00,
  "paidAmount": 145.00,
  "items": [
    {
      "description": "ARROZ TIPO 1 5KG",
      "productCode": "7891234567890",
      "quantity": 2.000,
      "unitOfMeasure": "UN",
      "unitPrice": 25.90,
      "totalPrice": 51.80
    }
  ],
  "alreadyImported": false
}
```

**Erros:**
- `400 Bad Request` — Chave de acesso inválida (formato incorreto)
- `404 Not Found` — NFC-e não encontrada na SEFAZ
- `502 Bad Gateway` — SEFAZ indisponível (timeout, erro de conexão)

#### `POST /api/v1/receipts/import`

Importa o cupom fiscal, criando transação + estabelecimento + itens.

**Request:**
```json
{
  "accessKey": "12345678901234567890123456789012345678901234",
  "accountId": "guid",
  "categoryId": "guid",
  "description": "Supermercado Exemplo",
  "competenceDate": "2026-02-20",
  "operationId": "string (opcional)"
}
```

**Response (201 Created):**
```json
{
  "transaction": { /* TransactionResponse completo */ },
  "establishment": {
    "id": "guid",
    "name": "SUPERMERCADO EXEMPLO LTDA",
    "cnpj": "12345678000190",
    "accessKey": "12345678901234567890123456789012345678901234"
  },
  "items": [
    {
      "id": "guid",
      "description": "ARROZ TIPO 1 5KG",
      "productCode": "7891234567890",
      "quantity": 2.000,
      "unitOfMeasure": "UN",
      "unitPrice": 25.90,
      "totalPrice": 51.80,
      "itemOrder": 1
    }
  ]
}
```

**Erros:**
- `400 Bad Request` — Validação (conta/categoria inválida, chave inválida)
- `409 Conflict` — Cupom já importado (chave de acesso duplicada)
- `502 Bad Gateway` — SEFAZ indisponível

#### `GET /api/v1/transactions/{id}/receipt`

Retorna os itens e estabelecimento de uma transação importada via cupom fiscal.

**Response (200 OK):**
```json
{
  "establishment": {
    "id": "guid",
    "name": "SUPERMERCADO EXEMPLO LTDA",
    "cnpj": "12345678000190",
    "accessKey": "12345678901234567890123456789012345678901234"
  },
  "items": [
    {
      "id": "guid",
      "description": "ARROZ TIPO 1 5KG",
      "productCode": "7891234567890",
      "quantity": 2.000,
      "unitOfMeasure": "UN",
      "unitPrice": 25.90,
      "totalPrice": 51.80,
      "itemOrder": 1
    }
  ]
}
```

**Erros:**
- `404 Not Found` — Transação não encontrada ou não possui cupom fiscal

#### Extensão do `TransactionResponse`

Adicionar campo booleano `hasReceipt` ao DTO `TransactionResponse` existente para indicar na listagem se a transação possui cupom fiscal importado:

```csharp
// Adição ao TransactionResponse existente
bool HasReceipt  // true se existe registro em Establishment para esta transação
```

---

## Pontos de Integração

### SEFAZ Paraíba — Consulta de NFC-e

**Serviço externo**: Portal da SEFAZ PB (`https://www.sefaz.pb.gov.br/nfce/...`)

**Abordagem**: Web scraping via HTTP GET + parsing HTML com AngleSharp.

**Implementação** (`SefazPbNfceService`):
1. Recebe a chave de acesso (44 dígitos)
2. Monta a URL de consulta da SEFAZ PB
3. Faz HTTP GET com `HttpClient` (timeout configurável, default 15s)
4. Parseia o HTML retornado com AngleSharp para extrair dados estruturados
5. Retorna `NfceData` com todos os campos ou lança exceção específica

**Infraestrutura de HTTP**:
- `HttpClient` registrado via `IHttpClientFactory` com nome `"SefazPb"`
- Configuração de timeout, User-Agent e retry policy via `Microsoft.Extensions.Http.Resilience` (Polly)
- Retry: 2 tentativas com backoff exponencial (1s, 3s) para erros transientes (5xx, timeout)

**Tratamento de Erros**:
| Cenário | Exceção | HTTP Status |
|---|---|---|
| Chave de acesso inválida (formato) | `InvalidAccessKeyException` | 400 |
| NFC-e não encontrada na SEFAZ | `NfceNotFoundException` | 404 |
| SEFAZ indisponível (timeout/5xx) | `SefazUnavailableException` | 502 |
| Erro ao parsear HTML (formato inesperado) | `SefazParsingException` | 502 |

**Extensibilidade para outros estados**: A interface `ISefazNfceService` é genérica. A implementação `SefazPbNfceService` é registrada no DI. Para adicionar outro estado futuro, basta criar uma nova implementação e usar factory/strategy pattern para roteamento baseado no UF da chave de acesso (posições 1-2 da chave = código IBGE do estado).

**Autenticação**: Nenhuma — o portal de consulta NFC-e da SEFAZ PB é público.

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|---|---|---|---|
| `TransactionResponse` (DTO) | Mudança API (Compatível) | Adiciona campo `hasReceipt` boolean. Baixo risco — campos adicionais não quebram clientes. | Atualizar frontend para ler o campo |
| `CancelTransactionCommandHandler` | Mudança de Lógica | Ao cancelar transação com cupom, deve remover itens e estabelecimento. Risco médio. | Injetar `IReceiptItemRepository` e `IEstablishmentRepository` no handler existente |
| `FinanceiroDbContext` | Adição de DbSets | Adiciona `DbSet<ReceiptItem>` e `DbSet<Establishment>`. Baixo risco. | Criar migration |
| `ServiceCollectionExtensions` (DI) | Adição de Registros | Novos repositórios, serviço SEFAZ, HttpClientFactory. Baixo risco. | Registrar novos serviços |
| `ApplicationServiceExtensions` (DI) | Adição de Handlers | Novos command/query handlers. Baixo risco. | Registrar handlers |
| `GlobalExceptionHandler` | Adição de Mappings | Novas exceptions (SEFAZ, AccessKey, Duplicate). Baixo risco. | Adicionar cases no switch |
| `MappingConfig` | Adição de Mappings | Novos TypeAdapterConfig para ReceiptItem e Establishment. Baixo risco. | Adicionar configs |
| `Infra.csproj` | Nova dependência NuGet | AngleSharp (parsing HTML), Microsoft.Extensions.Http.Resilience (retry). Baixo risco. | Adicionar packages |
| Tabela `transactions` | Sem alteração de schema | Nenhuma coluna adicionada. Zero risco. | — |
| Frontend `TransactionDetailPage` | Extensão de UI | Adiciona seção "Itens do Cupom" condicional. Baixo risco. | Implementar componente |
| Frontend `TransactionsPage` | Adição de botão | Botão "Importar Cupom" no header. Baixo risco. | Adicionar botão com link |
| Frontend Routes | Nova rota | `/transactions/import-receipt`. Baixo risco. | Adicionar no router |

---

## Abordagem de Testes

### Testes Unitários

**Entidades de Domínio:**
- `ReceiptItem.Create()` — valida criação com todos os campos
- `Establishment.Create()` — valida criação e dados

**Command Handlers:**
- `LookupReceiptCommandHandler` — mock de `ISefazNfceService`, testa fluxo completo e cenários de erro
- `ImportReceiptCommandHandler` — mock de repositórios, testa criação atômica, detecção de duplicidade, cálculo de desconto na descrição
- `CancelTransactionCommandHandler` (extensão) — testa remoção de itens e estabelecimento em cascade

**Validators:**
- `LookupReceiptValidator` — formato chave acesso (44 dígitos numéricos), formato URL
- `ImportReceiptValidator` — campos obrigatórios (accessKey, accountId, categoryId)

**Serviço SEFAZ (unit):**
- `SefazPbNfceService` — mock de `HttpClient`, testa parsing de HTML fixture, tratamento de erros (timeout, HTML inesperado, nota não encontrada)

**Cenários críticos:**
- Importação com desconto: verifica que `PaidAmount` é usado como valor da transação
- Chave duplicada: verifica que `ImportReceiptCommandHandler` rejeita com `DuplicateReceiptException`
- URL como input: verifica extração da chave de acesso a partir da URL

### Testes de Integração

**Repository + EF Core (Testcontainers):**
- `ReceiptItemRepository` — AddRange, GetByTransactionId, RemoveRange
- `EstablishmentRepository` — Add, GetByTransactionId, ExistsByAccessKey, cascade delete
- Teste de cascade delete: verifica que deletar Transaction remove ReceiptItems e Establishment

**HTTP Integration (WebApplicationFactory):**
- `POST /api/v1/receipts/lookup` — mock de `ISefazNfceService` na DI, verifica response
- `POST /api/v1/receipts/import` — verifica criação completa e response
- `GET /api/v1/transactions/{id}/receipt` — verifica retorno de itens
- Teste de duplicidade: importar mesma chave duas vezes → 409

**Testes Frontend (Vitest + MSW):**
- `ImportReceiptPage` — teste de renderização dos 3 steps
- Testes de hooks `useReceiptLookup`, `useReceiptImport`
- Mock do endpoint lookup e import via MSW

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Task 1: Entidades de Domínio e Interfaces**
   - Criar `ReceiptItem`, `Establishment` (Domain/Entity)
   - Criar `NfceData`, `NfceItemData` (Domain/Dto)
   - Criar `ISefazNfceService`, `IReceiptItemRepository`, `IEstablishmentRepository` (Domain/Interface)
   - Criar exceptions: `InvalidAccessKeyException`, `NfceNotFoundException`, `SefazUnavailableException`, `SefazParsingException`, `DuplicateReceiptException`
   - **Justificativa**: Base foundation, sem dependências externas

2. **Task 2: Infraestrutura (EF Core + Repositórios)**
   - Criar `ReceiptItemConfiguration`, `EstablishmentConfiguration` (Infra/Config)
   - Adicionar `DbSet<ReceiptItem>`, `DbSet<Establishment>` ao `FinanceiroDbContext`
   - Criar `ReceiptItemRepository`, `EstablishmentRepository` (Infra/Repository)
   - Gerar migration EF Core
   - Registrar repositórios no DI (`ServiceCollectionExtensions`)
   - **Justificativa**: Persistência é base para testes de integração

3. **Task 3: Serviço SEFAZ PB (Scraping)**
   - Criar `SefazPbNfceService` (Infra) com `HttpClient` + AngleSharp
   - Configurar `HttpClientFactory` com retry policy
   - Criar classe de configuração `SefazSettings` para URL base e timeouts
   - Registrar no DI
   - Testes unitários com HTML fixtures
   - **Justificativa**: Componente isolado com maior risk/complexidade — validar cedo

4. **Task 4: Commands e Queries (Application Layer)**
   - `LookupReceiptCommand` + Handler + Validator
   - `ImportReceiptCommand` + Handler + Validator
   - `GetTransactionReceiptQuery` + Handler
   - Response DTOs: `ReceiptLookupResponse`, `ImportReceiptResponse`, `ReceiptItemResponse`, `EstablishmentResponse`
   - Adicionar `hasReceipt` ao `TransactionResponse`
   - Estender `CancelTransactionCommandHandler` para cascade delete de itens/estabelecimento
   - Registrar handlers no DI (`ApplicationServiceExtensions`)
   - Adicionar mappings ao `MappingConfig`
   - Testes unitários dos handlers
   - **Justificativa**: Depende de Tasks 1-3

5. **Task 5: API (Controller + Exception Handling)**
   - Criar `ReceiptsController` com endpoints
   - Criar Request DTOs
   - Adicionar exceptions no `GlobalExceptionHandler`
   - Testes HTTP Integration
   - **Justificativa**: Depende de Task 4

6. **Task 6: Frontend — Tipos, API, Hooks**
   - Tipos TypeScript para receipt (types)
   - Funções API: `lookupReceipt()`, `importReceipt()`, `getTransactionReceipt()` (api)
   - Hooks React Query: `useReceiptLookup`, `useReceiptImport`, `useTransactionReceipt` (hooks)
   - Atualizar `TransactionResponse` type com `hasReceipt`
   - **Justificativa**: Base para componentes UI

7. **Task 7: Frontend — Página de Importação e Integração UI**
   - `ImportReceiptPage` com wizard de 3 steps
   - Componente `ReceiptPreview` (tabela de itens)
   - Componente `ReceiptItemsSection` para detalhe da transação
   - Atualizar `TransactionDetailPage` para exibir itens do cupom
   - Atualizar `TransactionsPage` com botão "Importar Cupom"
   - Adicionar rota `/transactions/import-receipt`
   - Badge/indicador visual de cupom fiscal na listagem
   - Testes frontend
   - **Justificativa**: Depende de Task 6

### Dependências Técnicas

- **AngleSharp** (NuGet) — parsing HTML da SEFAZ. Licença MIT, amplamente usado, API estável
- **Microsoft.Extensions.Http.Resilience** (NuGet) — retry/timeout para HttpClient. Pacote oficial Microsoft
- **Docker** — necessário para testes de integração (Testcontainers)
- **SEFAZ PB disponível** — para testes manuais end-to-end (não bloqueia desenvolvimento, apenas validação final)

---

## Monitoramento e Observabilidade

**Logs estruturados (ILogger):**
- `Information`: Lookup iniciado, importação concluída com success (incluindo chave de acesso, transactionId)
- `Warning`: SEFAZ timeout ou NFC-e não encontrada (chave de acesso logada)
- `Error`: Erros inesperados de parsing, falhas de rede após retries
- `Debug`: HTML response truncado (para diagnóstico de parsing em dev)

**Auditoria:**
- `AuditService.LogAsync("Receipt", transactionId, "Imported", userId, ...)` — log de auditoria ao importar cupom
- `AuditService.LogAsync("Receipt", transactionId, "CancelledCascade", userId, ...)` — log ao remover itens por cancelamento

**Métricas observáveis (logs):**
- Tempo de consulta na SEFAZ (logado em Information)
- Contagem de itens por cupom importado
- Taxa de erros de SEFAZ (capturável via logs Warning/Error)

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Justificativa | Alternativas Rejeitadas |
|---|---|---|
| **Web scraping com AngleSharp** | Portal SEFAZ PB não oferece API REST/SOAP pública para consulta NFC-e. AngleSharp é parsing CSS selector-based, type-safe, sem dependências nativas. | HtmlAgilityPack (API menos moderna, XPath-centric); Puppeteer/Playwright (overhead de browser headless desnecessário para HTML estático) |
| **Establishment como tabela 1:1 com Transaction** | O PRD define Establishment vinculado à Transaction. Modelagem 1:1 com cascade delete é a mais simples e atende ao requisito sem over-engineering. | Normalização por CNPJ (complexidade prematura — categorização futura pode redefinir modelo); campos inline na Transaction (poluiria entidade existente) |
| **Sem alteração na entidade Transaction** | A Transaction existente é amplamente usada no sistema. Adicionar campos específicos de cupom fiscal criaria acoplamento desnecessário. O campo `hasReceipt` no DTO é computado via left join/exists. | Adicionar `AccessKey` como coluna na Transaction (acoplaria NFC-e à entidade genérica) |
| **Dois endpoints separados (lookup + import)** | Separa consulta (idempotente, sem side effects) da importação (mutação). Permite o frontend cachear o preview e refazer sem reimportar. | Endpoint único que consulta e importa (impediria preview e confirmação pelo usuário) |
| **`hasReceipt` computado no Query Handler** | Usar left join com `Establishment` no `ListTransactionsQueryHandler` para popular o campo. Evita coluna redundante na tabela `transactions`. | Coluna booleana na transactions (redundância, risco de inconsistência); query separada (N+1 problem na listagem) |
| **`HttpClientFactory` com retry (Polly)** | Resiliência contra indisponibilidade temporária da SEFAZ. Padrão Microsoft recomendado para HttpClient em .NET. | HttpClient manual (sem pool de conexões, sem retry); retry na Application layer (viola separation of concerns) |
| **Endpoint GET em `/transactions/{id}/receipt`** | Estende o recurso Transaction existente em vez de criar rota separada.  Follows REST convention for sub-resources. | `/receipts/{transactionId}` (criaria recurso top-level desnecessário); inline no GET `/transactions/{id}` (response muito grande para listagem) |
| **Cascade delete explícito no handler + DB cascade** | DB cascade garante integridade referencial. O handler faz remoção explícita para o soft-cancel (status change) da Transaction, onde o DB cascade não se aplica. | Apenas DB cascade (não funciona para soft-cancel); apenas handler (risco de inconsistência se acesso direto ao banco) |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| **SEFAZ PB muda layout HTML** | Média | Alto — quebra parsing | Isolar seletores CSS em constantes. Logs de warning quando parsing retorna dados incompletos. Testes com HTML fixtures versionados. |
| **NFC-e expira no portal SEFAZ** | Alta | Baixo | Informar ao usuário que a nota não está mais disponível (RF07a). |
| **SEFAZ indisponível (manutenção)** | Média | Médio | Retry com backoff + mensagem amigável ao usuário (RF07). |
| **Performance do scraping** | Baixa | Baixo | Timeout configurável (15s default). Feedback de loading no frontend. |
| **Precisão do parsing (encoding, HTML malformado)** | Baixa | Médio | AngleSharp é tolerante a HTML malformado. Testes com fixtures reais. Logging de HTML raw em nível Debug. |

### Requisitos Especiais

**Performance:**
- Consulta SEFAZ: timeout de 15 segundos (configurável)
- Import atômico: deve completar em < 500ms (operações locais de banco)

**Segurança:**
- CNPJ e razão social são dados públicos da NFC-e — sem restrições adicionais
- Todos os endpoints requerem `[Authorize]` (padrão existente)
- Chave de acesso é validada no formato antes de qualquer consulta externa

### Conformidade com Padrões

- [x] Segue Clean Architecture em 4 camadas (Domain → Application → Infra, API)
- [x] CQRS nativo com `ICommand<T>` / `IQuery<T>` e handlers
- [x] Repository Pattern com interfaces no Domain
- [x] UnitOfWork para atomicidade
- [x] FluentValidation para validação de commands
- [x] Mapster para mapeamento entity → DTO
- [x] DomainExceptions mapeadas para ProblemDetails (RFC 7807)
- [x] Idempotência via `OperationId`
- [x] Frontend feature-based architecture
- [x] React Query para server state, Zod para validação de formulários
- [x] Componentes shadcn/ui para consistência visual
- [x] Convenção de nomenclatura de banco: snake_case para tabelas e colunas
