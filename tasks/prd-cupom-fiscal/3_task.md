---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>backend/infra</domain>
<type>integration</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>external_apis</dependencies>
<unblocks>"4.0"</unblocks>
</task_context>

# Tarefa 3.0: Serviço SEFAZ PB — Scraping e Parsing

## Visão Geral

Implementar o serviço de consulta de NFC-e na SEFAZ da Paraíba via web scraping. Este é o componente com maior complexidade técnica e risco do projeto, pois depende do layout HTML do portal da SEFAZ. A implementação usa `HttpClient` para fazer requisições HTTP e `AngleSharp` para parsing do HTML retornado. Também configura resiliência com retry policy via `Microsoft.Extensions.Http.Resilience`.

Esta tarefa pode ser executada em paralelo com a Task 2.0 (Infraestrutura), pois ambas dependem apenas da Task 1.0 (entidades e interfaces).

## Requisitos

- Implementar `SefazPbNfceService` que consulta o portal da SEFAZ PB e extrai os dados da NFC-e
- Aceitar chave de acesso (44 dígitos) e URL como entrada, detectando automaticamente o formato
- Extrair do HTML: dados do estabelecimento (razão social, CNPJ), data/hora de emissão, lista de itens (descrição, código, quantidade, unidade, valor unitário, valor total), descontos e valor total pago
- Configurar `HttpClientFactory` com retry policy (2 tentativas, backoff exponencial)
- Tratar erros: SEFAZ indisponível (timeout/5xx), NFC-e não encontrada, erro de parsing
- Criar classe de configuração `SefazSettings` para URL base e timeouts
- Instalar pacotes NuGet necessários: `AngleSharp`, `Microsoft.Extensions.Http.Resilience`
- Testes unitários usando HTML fixtures (mock de HttpClient)

## Subtarefas

- [ ] 3.1 Instalar pacotes NuGet no projeto `Infra`
  - `AngleSharp` (parsing HTML, licença MIT)
  - `Microsoft.Extensions.Http.Resilience` (retry/timeout, pacote oficial Microsoft)

- [ ] 3.2 Criar classe de configuração `SefazSettings`
  - Propriedades: `BaseUrl` (string), `TimeoutSeconds` (int, default 15), `UserAgent` (string)
  - Carregar de `appsettings.json` na seção `Sefaz`
  - Adicionar configuração padrão no `appsettings.Development.json`

- [ ] 3.3 Implementar `SefazPbNfceService` em `Infra/Services/SefazPbNfceService.cs`
  - Construtor: recebe `HttpClient` (via DI/named client), `ILogger<SefazPbNfceService>`
  - Método `LookupAsync(string accessKey, CancellationToken)`:
    1. Validar formato da chave de acesso (44 dígitos numéricos). Se inválido, lançar `InvalidAccessKeyException`
    2. Montar URL de consulta da SEFAZ PB usando a chave de acesso
    3. Fazer HTTP GET com o `HttpClient`
    4. Em caso de timeout ou erro de conexão, lançar `SefazUnavailableException`
    5. Parsear HTML com AngleSharp usando CSS selectors
    6. Se a página indica que a NFC-e não foi encontrada, lançar `NfceNotFoundException`
    7. Extrair e retornar `NfceData` com todos os campos
    8. Em caso de erro de parsing (campos faltantes, formato inesperado), lançar `SefazParsingException`

- [ ] 3.4 Implementar método auxiliar para extração de chave de acesso de URL
  - Detectar se o input é uma URL (contém `http` ou `sefaz`)
  - Extrair a chave de acesso (44 dígitos) da URL usando regex
  - Se não encontrar chave válida na URL, lançar `InvalidAccessKeyException`

- [ ] 3.5 Implementar parsing detalhado do HTML da SEFAZ PB
  - Isolar seletores CSS em constantes privadas (facilita manutenção quando layout mudar)
  - Extrair dados do estabelecimento: razão social, CNPJ
  - Extrair data/hora de emissão
  - Extrair lista de itens: percorrer tabela de produtos, extrair cada campo
  - Extrair totais: valor bruto, descontos, valor pago
  - Log de `Debug` com HTML truncado para diagnóstico
  - Log de `Warning` quando parsing retorna dados incompletos

- [ ] 3.6 Configurar `HttpClientFactory` e retry policy no DI
  - Registrar named HttpClient `"SefazPb"` via `IHttpClientFactory`
  - Configurar `BaseAddress` a partir de `SefazSettings.BaseUrl`
  - Configurar `Timeout` a partir de `SefazSettings.TimeoutSeconds`
  - Configurar `User-Agent` header
  - Adicionar retry policy: 2 tentativas, backoff exponencial (1s, 3s) para erros transientes (5xx, timeout)
  - Registrar `ISefazNfceService` → `SefazPbNfceService` no DI como Scoped

- [ ] 3.7 Criar HTML fixtures para testes
  - Fixture de NFC-e válida com múltiplos itens (caso feliz)
  - Fixture de NFC-e com descontos
  - Fixture de NFC-e com item sem código de produto
  - Fixture de página "NFC-e não encontrada"
  - Fixture de HTML inesperado/malformado (simular mudança de layout)

- [ ] 3.8 Testes unitários do `SefazPbNfceService`
  - Teste de parsing bem-sucedido com fixture de NFC-e válida
  - Teste de extração de todos os campos de cada item (descrição, código, qty, unidade, preço unitário, total)
  - Teste de NFC-e com descontos: verificar `TotalAmount`, `DiscountAmount`, `PaidAmount`
  - Teste de item sem código de produto: `ProductCode` deve ser null
  - Teste de NFC-e não encontrada: fixture de "não encontrada" → `NfceNotFoundException`
  - Teste de HTML malformado: fixture inesperada → `SefazParsingException`
  - Teste de chave de acesso inválida (não 44 dígitos) → `InvalidAccessKeyException`
  - Teste de timeout/erro de conexão (mock HttpClient lançando exceção) → `SefazUnavailableException`
  - Teste de extração de chave de acesso a partir de URL válida
  - Teste de URL sem chave válida → `InvalidAccessKeyException`

## Sequenciamento

- Bloqueado por: 1.0 (Entidades de Domínio e Interfaces — usa `NfceData`, `ISefazNfceService`, exceptions)
- Desbloqueia: 4.0 (Commands e Queries)
- Paralelizável: Sim (pode ser executada em paralelo com a Task 2.0)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| `SefazPbNfceService.cs` | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Services/` |
| `SefazSettings.cs` | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Services/` |
| `appsettings.json` (seção Sefaz) | `backend/1-Services/GestorFinanceiro.Financeiro.API/` |
| DI Registration | `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/DependencyInjection/ServiceCollectionExtensions.cs` |
| HTML Fixtures | `backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/Fixtures/` |
| Testes | `backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/Services/` |

### Estratégia de Scraping

O portal da SEFAZ PB retorna uma página HTML com a NFC-e quando consultada via chave de acesso. Os dados são extraídos usando CSS selectors com AngleSharp. Os seletores devem ser isolados em constantes para facilitar manutenção caso o layout da SEFAZ mude.

**Fluxo do scraping:**
```
Chave de acesso → Montar URL SEFAZ PB → HTTP GET → HTML → AngleSharp → NfceData
```

**Tratamento de erros por cenário:**
| Cenário | Comportamento |
|---------|--------------|
| Chave inválida (formato) | `InvalidAccessKeyException` antes de fazer HTTP |
| HTTP timeout | `SefazUnavailableException` |
| HTTP 5xx | `SefazUnavailableException` (após retries) |
| HTML indica "não encontrada" | `NfceNotFoundException` |
| Parsing falha (campo obrigatório faltante) | `SefazParsingException` |

### Configuração de Retry

```csharp
// Pseudo-código de configuração
services.AddHttpClient("SefazPb", client =>
{
    client.BaseAddress = new Uri(sefazSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(sefazSettings.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(sefazSettings.UserAgent);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
});
```

### Extensibilidade Futura

A interface `ISefazNfceService` é genérica. Para suportar outros estados no futuro:
1. Criar nova implementação (ex: `SefazSpNfceService`)
2. Usar factory/strategy para rotear pelo código UF (posições 1-2 da chave de acesso = código IBGE do estado)
3. Registrar no DI com factory

## Critérios de Sucesso

- `SefazPbNfceService` implementa `ISefazNfceService` e é registrado no DI
- Parsing de HTML fixture extrai corretamente: nome do estabelecimento, CNPJ, data/hora, todos os itens com todos os campos, descontos, valor total pago
- Todos os 5 cenários de erro são tratados corretamente com exceções específicas
- Extração de chave de acesso a partir de URL funciona corretamente
- `HttpClientFactory` configurado com retry policy (2 tentativas, backoff exponencial)
- `SefazSettings` carregado de `appsettings.json` com valores padrão sensatos
- Todos os testes unitários passam (mínimo 10 testes cobrindo casos felizes e de erro)
- HTML fixtures representam cenários reais da SEFAZ PB
- Pacotes NuGet `AngleSharp` e `Microsoft.Extensions.Http.Resilience` adicionados ao projeto Infra
- Projeto compila sem erros
