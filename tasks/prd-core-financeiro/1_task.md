---
status: pending
parallelizable: false
blocked_by: []
---

<task_context>
<domain>infra/configuração</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>dotnet_sdk</dependencies>
<unblocks>"2.0", "3.0", "4.0", "5.0", "6.0", "7.0", "8.0", "9.0", "10.0", "11.0"</unblocks>
</task_context>

# Tarefa 1.0: Estrutura de Solução e Projetos

## Visão Geral

Criar a estrutura completa da solution .NET 8, incluindo todos os projetos (.csproj), referências entre projetos e pacotes NuGet. A estrutura segue Clean Architecture com pastas numeradas conforme `rules/dotnet-folders.md`. Esta é a fundação para todo o desenvolvimento subsequente.

> **IMPORTANTE**: Toda a estrutura da solution deve ser criada dentro da pasta **`backend/`** na raiz do repositório (`gerenciador-financeiro/backend/`). Essa pasta é o diretório raiz do projeto .NET. Todos os comandos `dotnet` devem ser executados a partir dela.

## Requisitos

- Criar a solution (.sln) e todos os projetos conforme a arquitetura definida na techspec
- Configurar as referências entre projetos respeitando a regra de dependência: Domain não referencia nenhum outro; Application depende de Domain; Infra depende de Domain; Services depende de Application; Tests depende de todos
- Adicionar todos os pacotes NuGet listados na techspec
- Garantir que o build compila sem erros

## Subtarefas

- [ ] 1.1 Criar a pasta `backend/` na raiz do repositório e dentro dela criar a solution `GestorFinanceiro.Financeiro.sln`
- [ ] 1.2 Criar o projeto `1-Services/GestorFinanceiro.Financeiro.API/GestorFinanceiro.Financeiro.API.csproj` (placeholder vazio — Web API sem código funcional)
- [ ] 1.3 Criar o projeto `2-Application/GestorFinanceiro.Financeiro.Application/GestorFinanceiro.Financeiro.Application.csproj` (classlib)
- [ ] 1.4 Criar o projeto `3-Domain/GestorFinanceiro.Financeiro.Domain/GestorFinanceiro.Financeiro.Domain.csproj` (classlib — zero dependências externas)
- [ ] 1.5 Criar o projeto `4-Infra/GestorFinanceiro.Financeiro.Infra/GestorFinanceiro.Financeiro.Infra.csproj` (classlib)
- [ ] 1.6 Criar o projeto `5-Tests/GestorFinanceiro.Financeiro.UnitTests/GestorFinanceiro.Financeiro.UnitTests.csproj` (xunit)
- [ ] 1.7 Criar o projeto `5-Tests/GestorFinanceiro.Financeiro.IntegrationTests/GestorFinanceiro.Financeiro.IntegrationTests.csproj` (xunit)
- [ ] 1.8 Configurar referências entre projetos (`<ProjectReference>`)
- [ ] 1.9 Adicionar pacotes NuGet conforme techspec (FluentValidation, Mapster, EF Core, Npgsql, xUnit, Moq, AwesomeAssertions, AutoFixture, Testcontainers, coverlet)
- [ ] 1.10 Validar build com `dotnet build` a partir da pasta `backend/`

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 2.0 (e transitivamente todas as demais)
- Paralelizável: Não (é a primeira tarefa)

## Detalhes de Implementação

### Estrutura de pastas (conforme `rules/dotnet-folders.md` e techspec)

Toda a estrutura abaixo fica dentro de `backend/` na raiz do repositório:

```
gerenciador-financeiro/
└── backend/                                          ← raiz do projeto .NET
    ├── GestorFinanceiro.Financeiro.sln
    ├── 1-Services/
    │   └── GestorFinanceiro.Financeiro.API/
    │       └── GestorFinanceiro.Financeiro.API.csproj
    ├── 2-Application/
    │   └── GestorFinanceiro.Financeiro.Application/
    │       └── GestorFinanceiro.Financeiro.Application.csproj
    ├── 3-Domain/
    │   └── GestorFinanceiro.Financeiro.Domain/
    │       └── GestorFinanceiro.Financeiro.Domain.csproj
    ├── 4-Infra/
    │   └── GestorFinanceiro.Financeiro.Infra/
    │       └── GestorFinanceiro.Financeiro.Infra.csproj
    └── 5-Tests/
        ├── GestorFinanceiro.Financeiro.UnitTests/
        │   └── GestorFinanceiro.Financeiro.UnitTests.csproj
        └── GestorFinanceiro.Financeiro.IntegrationTests/
            └── GestorFinanceiro.Financeiro.IntegrationTests.csproj
```

### Referências entre projetos

| Projeto | Referencia |
|---------|-----------|
| API | Application |
| Application | Domain |
| Infra | Domain |
| UnitTests | Domain, Application |
| IntegrationTests | Domain, Application, Infra |

### Pacotes NuGet (conforme techspec)

**3-Domain**: Nenhum pacote NuGet — apenas .NET BCL

**2-Application**:
- `FluentValidation` 11.8.1
- `Mapster` 7.4.0
- `Microsoft.Extensions.Logging.Abstractions` 8.0.0

**4-Infra**:
- `Microsoft.EntityFrameworkCore` 8.0.0
- `Microsoft.EntityFrameworkCore.Design` 8.0.0
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.0

**5-Tests/UnitTests**:
- `xunit` 2.6.6
- `xunit.runner.visualstudio` 2.5.6
- `Microsoft.NET.Test.Sdk` 17.8.0
- `Moq` 4.20.70
- `AwesomeAssertions` 6.15.1
- `AutoFixture` 4.18.1
- `coverlet.collector` 6.0.0

**5-Tests/IntegrationTests**:
- `Testcontainers.PostgreSql` 3.7.0
- `Microsoft.EntityFrameworkCore.InMemory` 8.0.0
- (mesmos pacotes base de testes: xunit, Microsoft.NET.Test.Sdk, AwesomeAssertions)

### Observações

- O projeto **1-Services (API)** é um placeholder vazio nesta fase — apenas o `.csproj` com template `webapi` e um `Program.cs` mínimo
- O projeto **3-Domain** NÃO deve ter nenhum `PackageReference` externo
- Configurar `<Nullable>enable</Nullable>` e `<ImplicitUsings>enable</ImplicitUsings>` em todos os projetos
- Target framework: `net8.0`

## Critérios de Sucesso

- Pasta `backend/` criada na raiz do repositório com toda a estrutura .NET dentro
- `dotnet build` da solution compila sem erros (executado a partir de `backend/`)
- Todos os 7 projetos estão na solution
- Referências entre projetos respeitam a regra de dependência (Domain não referencia nenhum outro)
- Todos os pacotes NuGet da techspec estão configurados nos projetos corretos
- A solução segue a estrutura numerada de pastas conforme `rules/dotnet-folders.md`
