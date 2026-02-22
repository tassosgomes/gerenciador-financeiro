```markdown
---
status: pending
parallelizable: true
blocked_by: ["2.0"]
---

<task_context>
<domain>infra/background-service</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"8.0"</unblocks>
</task_context>

# Tarefa 7.0: Background Service — BudgetRecurrenceWorker

## Visão Geral

Implementar o `BudgetRecurrenceWorker`, um `BackgroundService` que replica orçamentos recorrentes mensalmente de forma lazy. Executa uma vez por dia e, no início de cada mês, detecta orçamentos marcados como `IsRecurrent = true` do mês anterior que ainda não foram replicados, criando cópias para o mês corrente. Categorias inativas são excluídas automaticamente. Se a soma dos percentuais ultrapassar 100%, o worker cria o orçamento mesmo assim (conforme PRD) e loga warning.

## Requisitos

- PRD F5 req 33: Opção "Recorrente" ao criar/editar orçamento
- PRD F5 req 34: Orçamento recorrente replicado automaticamente para o mês seguinte
- PRD F5 req 35: Geração lazy — apenas o próximo mês
- PRD F5 req 36: Se soma > 100%, gerar mas sinalizar
- PRD F5 req 38: Edição individual não afeta template ou meses futuros
- Techspec: Novo `BackgroundService` separado do `RecurrenceMaintenanceWorker`
- Techspec: Execução diária (mesma estratégia do worker existente)
- Techspec: Categorias inativas removidas ao gerar próximo mês
- Questão aberta do PRD: Categorias desativadas → decisão: ignorar (excluir da cópia)

## Subtarefas

### BudgetRecurrenceWorker

- [ ] 7.1 Criar `BudgetRecurrenceWorker` em `4-Infra/GestorFinanceiro.Financeiro.Infra/StartupTasks/BudgetRecurrenceWorker.cs`:
  - Herdar de `BackgroundService`
  - Dependências: `IServiceScopeFactory`, `ILogger<BudgetRecurrenceWorker>`
  - Ciclo de execução: uma vez por dia (mesma estratégia do `RecurrenceMaintenanceWorker`)
  - Usar `PeriodicTimer` ou `Task.Delay` com intervalo de 24h
  - Execução resiliente: try/catch com log de erro, continua no próximo ciclo

- [ ] 7.2 Implementar lógica principal `ProcessRecurrenceAsync`:
  - Obter mês corrente (`DateTime.UtcNow` → year, month)
  - Calcular mês anterior (month-1, ajustar para dezembro do ano anterior se janeiro)
  - Buscar orçamentos recorrentes do mês anterior via `IBudgetRepository.GetRecurrentBudgetsForMonthAsync(prevYear, prevMonth)`
  - Para cada orçamento recorrente:
    1. Verificar se já existe orçamento com mesmo nome no mês corrente via `ExistsByNameAsync(name, null)`
    2. Se já existe → skip (log info: "Orçamento '{Name}' já existe para {month}/{year}")
    3. Filtrar categorias: buscar categorias ativas via `ICategoryRepository`:
       - Para cada categoryId, verificar se categoria existe e `IsActive == true`
       - Excluir categorias inativas da cópia
       - Se nenhuma categoria ativa restante → skip + log warning: "Orçamento recorrente '{Name}' ignorado: todas as categorias estão inativas"
    4. Criar novo orçamento via `Budget.Create(name, percentage, currentYear, currentMonth, activeCategoryIds, isRecurrent: true, userId: "system")`
    5. Persistir via `IBudgetRepository.AddAsync()` + `IUnitOfWork.SaveChangesAsync()`
    6. Log info: "Orçamento recorrente '{Name}' criado para {month}/{year}"
  - Após processar todos, verificar soma de percentuais do mês corrente:
    - Se > 100% → log warning: "Soma de percentuais para {month}/{year} excede 100%: {total}%"

### Registro no DI

- [ ] 7.3 Registrar `BudgetRecurrenceWorker` como `HostedService` em `ServiceCollectionExtensions`:
  - `services.AddHostedService<BudgetRecurrenceWorker>()`

### Testes Unitários

- [ ] 7.4 Criar testes para `BudgetRecurrenceWorker` em `5-Tests/.../UnitTests/Infra/StartupTasks/BudgetRecurrenceWorkerTests.cs`:
  - `ProcessRecurrence_WithRecurrentBudgets_ShouldCreateForCurrentMonth`
  - `ProcessRecurrence_WhenBudgetAlreadyExists_ShouldSkip`
  - `ProcessRecurrence_WhenCategoryInactive_ShouldExcludeFromCopy`
  - `ProcessRecurrence_WhenAllCategoriesInactive_ShouldSkipBudget`
  - `ProcessRecurrence_WhenPercentageExceeds100_ShouldCreateAndLogWarning`
  - `ProcessRecurrence_WithNoBudgets_ShouldDoNothing`
  - `ProcessRecurrence_WhenExceptionThrown_ShouldLogErrorAndContinue`
  - Usar NSubstitute para mockar repositórios, IUnitOfWork e ILogger

### Validação

- [ ] 7.5 Validar build e rodar testes unitários

## Sequenciamento

- Bloqueado por: 2.0 (Infra — repositório e tabelas necessários)
- Desbloqueia: 8.0 (Testes de Integração Backend)
- Paralelizável: Sim com 3.0, 4.0, 5.0, 6.0 (feature independente do CRUD principal)

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/4-Infra/GestorFinanceiro.Financeiro.Infra/
├── StartupTasks/
│   └── BudgetRecurrenceWorker.cs              ← NOVO
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs          ← MODIFICAR (add HostedService)

backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/
└── Infra/StartupTasks/
    └── BudgetRecurrenceWorkerTests.cs          ← NOVO
```

### Referência: RecurrenceMaintenanceWorker

O `BudgetRecurrenceWorker` deve seguir o mesmo padrão estrutural do `RecurrenceMaintenanceWorker` existente:
- `BackgroundService` com `ExecuteAsync` override
- `IServiceScopeFactory` para criar scopes de DI
- Try/catch global com log de erro
- `Task.Delay` entre ciclos
- `stoppingToken` para shutdown gracioso

### Lógica de Mês Anterior

```csharp
var now = DateTime.UtcNow;
var currentYear = now.Year;
var currentMonth = now.Month;

// Mês anterior
var prevMonth = currentMonth - 1;
var prevYear = currentYear;
if (prevMonth < 1)
{
    prevMonth = 12;
    prevYear--;
}
```

### Tratamento de userId

Orçamentos criados pelo worker usam `userId: "system"` nos campos `CreatedBy`, pois não há contexto de usuário autenticado.

### Padrões a Seguir

- Seguir padrão de `RecurrenceMaintenanceWorker` para estrutura de BackgroundService
- Logs estruturados com campos nomeados: `{BudgetName}`, `{Year}`, `{Month}`
- Resiliente a falhas: never crash, always retry next cycle
- Scoped dependencies: criar novo scope para cada execução

## Critérios de Sucesso

- Worker executa sem erros em ambiente de desenvolvimento
- Orçamentos recorrentes do mês anterior são replicados corretamente para o mês corrente
- Orçamentos já existentes não são duplicados
- Categorias inativas são excluídas da cópia
- Se todas as categorias são inativas, orçamento é ignorado (não criado sem categorias)
- Se soma > 100%, worker cria e loga warning
- Worker é resiliente a falhas (log error, continua no próximo ciclo)
- Registrado como HostedService no DI
- Todos os testes unitários passam
- Build compila sem erros
```
