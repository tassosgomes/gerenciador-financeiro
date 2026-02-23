# Review — Tarefa 7.0: BudgetRecurrenceWorker

**Status:** ✅ APROVADA  
**Revisada em:** 2026-02-23  
**Revisora:** GitHub Copilot (modo review)

---

## 1. Resultados da Validação da Definição da Tarefa

### Arquivos Implementados

| Arquivo | Status |
|---|---|
| `backend/4-Infra/.../StartupTasks/BudgetRecurrenceWorker.cs` | ✅ Novo |
| `backend/4-Infra/.../DependencyInjection/ServiceCollectionExtensions.cs` | ✅ Modificado |
| `backend/5-Tests/.../UnitTests/Infra/StartupTasks/BudgetRecurrenceWorkerTests.cs` | ✅ Novo |

---

## 2. Resultados por Subtarefa

### 7.1 — Criação do BudgetRecurrenceWorker ✅

- `public sealed class BudgetRecurrenceWorker : BackgroundService` — herda corretamente
- Dependências injetadas: `IServiceScopeFactory`, `ILogger<BudgetRecurrenceWorker>` ✅
- Ciclo de execução: `Task.Delay(TimeSpan.FromHours(24), stoppingToken)` — idêntico ao `RecurrenceMaintenanceWorker` ✅
- Shutdown gracioso: `OperationCanceledException when (stoppingToken.IsCancellationRequested)` tratado ✅
- Resiliência: try/catch em `RunCycleAsync` loga erro e prossegue no próximo ciclo ✅
- Padrão `ExecuteAsync`: roda ciclo inicial imediatamente ao subir, depois aguarda 24h ✅
- Cada iteração do forEach por orçamento tem seu próprio try/catch individual (falha em um não cancela os outros) ✅

### 7.2 — Lógica de ProcessRecurrenceAsync ✅

| Requisito | Implementação | Status |
|---|---|---|
| Obter mês corrente via `DateTime.UtcNow` | `var now = DateTime.UtcNow; var currentYear = now.Year; var currentMonth = now.Month;` | ✅ |
| Ajuste de fronteira de ano (janeiro → dezembro do ano anterior) | `if (previousMonth < 1) { previousMonth = 12; previousYear--; }` | ✅ |
| Buscar recorrentes do mês anterior | `GetRecurrentBudgetsForMonthAsync(previousYear, previousMonth, ...)` | ✅ |
| Verificar existência pelo nome | `ExistsByNameAsync(recurrentBudget.Name, null, ...)` | ✅ |
| Log info quando skip por duplicidade | `LogInformation("Orçamento '{BudgetName}' já existe para {Month}/{Year}", ...)` | ✅ |
| Filtrar categorias inativas | `GetActiveCategoryIdsAsync` checa `category.IsActive` via repositório | ✅ |
| Skip e log warning quando todas inativas | `LogWarning("Orçamento recorrente '{BudgetName}' ignorado: todas as categorias estão inativas")` | ✅ |
| Criar via `Budget.Create` com `userId: "system"`, `isRecurrent: true` | Chamada correta com todos os parâmetros | ✅ |
| Persistir via `AddAsync` + `SaveChangesAsync` | Chamados dentro do loop por orçamento | ✅ |
| Log info ao criar | `LogInformation("Orçamento recorrente '{BudgetName}' criado para {Month}/{Year}", ...)` | ✅ |
| Verificar soma > 100% após processar todos | `GetTotalPercentageForMonthAsync` + `LogWarning("...excede 100%...")` | ✅ |

### 7.3 — Registro no DI ✅

```csharp
// ServiceCollectionExtensions.cs, linha 53
services.AddHostedService<BudgetRecurrenceWorker>();
```

Registrado em sequência após `RecurrenceMaintenanceWorker`, conforme padrão da infra.

### 7.4 — Testes Unitários ✅

Todos os 7 testes requeridos pela task implementados:

| Teste | Resultado |
|---|---|
| `ProcessRecurrence_WithRecurrentBudgets_ShouldCreateForCurrentMonth` | ✅ PASS |
| `ProcessRecurrence_WhenBudgetAlreadyExists_ShouldSkip` | ✅ PASS |
| `ProcessRecurrence_WhenCategoryInactive_ShouldExcludeFromCopy` | ✅ PASS |
| `ProcessRecurrence_WhenAllCategoriesInactive_ShouldSkipBudget` | ✅ PASS |
| `ProcessRecurrence_WhenPercentageExceeds100_ShouldCreateAndLogWarning` | ✅ PASS |
| `ProcessRecurrence_WithNoBudgets_ShouldDoNothing` | ✅ PASS |
| `ProcessRecurrence_WhenExceptionThrown_ShouldLogErrorAndContinue` | ✅ PASS |

**Qualidade dos testes:**
- NSubstitute para mocks de repositórios, `IUnitOfWork` e `ILogger` (via `TestLogger<T>` customizado) ✅
- Acesso ao método privado `ProcessRecurrenceAsync` via reflection — abordagem válida para testar lógica de worker privada ✅
- Asserções com AwesomeAssertions (`Should()`) ✅
- `TestLogger<T>` captura entradas de log com `Level` e `Message`, permitindo asserções sobre logs de warning/error ✅
- Padrão AAA (Arrange/Act/Assert) ✅
- `CreateSut()` centraliza criação do SUT com `ServiceCollection` real para `IServiceScopeFactory` ✅

### 7.5 — Build e Testes ✅

```
Build: 0 errors, 0 warnings
Tests (BudgetRecurrenceWorker filter): 7 passed, 0 failed
```

---

## 3. Análise de Conformidade com Regras do Projeto

### Arquitetura (dotnet-architecture.md)
- ✅ Worker está corretamente na camada Infra (`4-Infra/StartupTasks/`)
- ✅ Usa `IServiceScopeFactory` para criar scopes (padrão correto para BackgroundService com dependências scoped)
- ✅ Depende de interfaces do Domain (`IBudgetRepository`, `ICategoryRepository`, `IUnitOfWork`) — sem acoplamento para implementações concretas
- ✅ Separação de responsabilidades: novo worker independente do `RecurrenceMaintenanceWorker`

### Padrão de Código (dotnet-coding-standards.md)
- ✅ Classe `sealed` (boa prática para BackgroundService)
- ✅ Logs com campos nomeados estruturados: `{BudgetName}`, `{Month}`, `{Year}`
- ✅ `GetActiveCategoryIdsAsync` extrai lógica auxiliar em método próprio com `private static` (correto, não acessa estado da instância)
- ✅ `Distinct()` em `budget.CategoryIds` previne category IDs duplicados na cópia
- ✅ Código em inglês (propriedades, parâmetros, variáveis)
- ✅ Métodos com responsabilidade única e bem definida

### Resiliência
- ✅ Falha em um orçamento individual não interrompe processamento dos demais (try/catch interno por orçamento)
- ✅ Falha global em `ProcessRecurrenceAsync` é capturada em `RunCycleAsync`, preservando o loop de execução
- ✅ `OperationCanceledException` com `stoppingToken` garante shutdown gracioso

### Testes (dotnet-testing.md)
- ✅ xUnit + NSubstitute + AwesomeAssertions — stack correto do projeto
- ✅ Padrão AAA
- ✅ Isolação completa via mocks (sem dependências externas nos testes unitários)

---

## 4. Problemas Identificados e Resoluções

### Problemas Críticos
Nenhum.

### Problemas de Média Severidade
Nenhum.

### Observações de Baixa Severidade

1. **`ExistsByNameAsync` sem filtro de mês:** A verificação de duplicidade usa o nome globalmente (constraint global de nome único por design da techspec). Isso é correto conforme a decisão arquitetural — nomes são únicos globalmente. Sem ação necessária.

2. **`SaveChangesAsync` dentro do loop (por orçamento):** Garante persistência isolada por orçamento e semântica de "melhor esforço". Alinhado com a task spec. Sem ação necessária.

3. **Teste `WhenExceptionThrown` não verifica log de erro do segundo Budget sendo criado:** O teste verifica que o segundo budget foi adicionado e que `LogError` foi emitido, mas não confirma o `LogInformation` de sucesso para o segundo. Cobertura suficiente para o critério de aceitação. Sem ação necessária.

---

## 5. Conformidade com Critérios de Sucesso da Tarefa

| Critério | Status |
|---|---|
| Worker executa sem erros em ambiente de desenvolvimento | ✅ Build limpo, 0 warnings |
| Orçamentos recorrentes do mês anterior são replicados para o mês corrente | ✅ Implementado e testado |
| Orçamentos já existentes não são duplicados | ✅ `ExistsByNameAsync` + skip/log |
| Categorias inativas são excluídas da cópia | ✅ `GetActiveCategoryIdsAsync` filtra `IsActive` |
| Se todas as categorias são inativas, orçamento é ignorado | ✅ Skip + `LogWarning` |
| Se soma > 100%, worker cria e loga warning | ✅ Após loop, `GetTotalPercentageForMonthAsync` + `LogWarning` |

---

## 6. Confirmação de Conclusão

```markdown
- [x] 7.0 Background Service — BudgetRecurrenceWorker ✅ CONCLUÍDA
  - [x] 7.1 BudgetRecurrenceWorker criado e estruturado conforme padrão existing
  - [x] 7.2 ProcessRecurrenceAsync implementado com toda a lógica requerida
  - [x] 7.3 Registrado como HostedService no ServiceCollectionExtensions
  - [x] 7.4 7/7 testes unitários implementados e passando
  - [x] 7.5 Build: 0 errors, 0 warnings | Tests: 7 passed, 0 failed
  - [x] Definição da tarefa, PRD e tech spec validados
  - [x] Conformidade com regras de arquitetura e codificação verificadas
  - [x] Pronto para integração com tarefa 8.0 (Testes de Integração Backend)
```

---

## Resultado Final

**APROVADA** — A implementação da Tarefa 7.0 está completa, correta e em conformidade com todos os critérios de aceitação, padrões arquiteturais e regras do projeto. O `BudgetRecurrenceWorker` segue fielmente o padrão do `RecurrenceMaintenanceWorker` existente, com lógica resiliente, testes abrangentes e registro correto no DI.
