````markdown
# Resumo de Tarefas de Implementação da API Completa (Fase 2)

## Visão Geral

Exposição do Core Financeiro (Fase 1) como uma API REST segura via ASP.NET Core Web API. Inclui: autenticação JWT com refresh token, controllers para todos os recursos (contas, categorias, transações), histórico/auditoria consultável, backup manual JSON, tratamento global de erros (RFC 9457), paginação padronizada e documentação Swagger. Ao final, o backend estará funcional e testável via Postman/curl.

## Fases de Implementação

### Fase 1 — Fundação de Autenticação
Criação das entidades `User` e `RefreshToken`, enum `UserRole`, exceções de autenticação, interfaces de repositório, configurações EF Core e migration. Pré-requisito para todos os endpoints protegidos.

### Fase 2 — Serviços de Autenticação e Pipeline HTTP
Implementação de `IPasswordHasher`, `ITokenService`, handlers CQRS de autenticação, e toda a pipeline HTTP (DI, JWT Bearer, CORS, Swagger, `GlobalExceptionHandler`, `ValidationActionFilter`).

### Fase 3 — Controllers REST
Controllers para Auth, Users, Accounts, Categories e Transactions. Delegam para o `IDispatcher` CQRS existente. Inclui novas queries com filtros e paginação.

### Fase 4 — Funcionalidades Complementares
Auditoria (tabela `audit_logs` + endpoint), backup JSON (export/import) e health check.

### Fase 5 — Testes de Integração HTTP
Testes end-to-end via `WebApplicationFactory` + Testcontainers cobrindo todos os controllers e fluxos críticos.

## Tarefas

- [X] 1.0 Entidades e Infraestrutura de Usuário
- [X] 2.0 Serviços de Autenticação (JWT + Refresh Token)
- [X] 3.0 Pipeline HTTP e Tratamento de Erros
- [ ] 4.0 Controllers de Auth e Users
- [ ] 5.0 Controllers de Contas e Categorias
- [ ] 6.0 Controller de Transações
- [ ] 7.0 Histórico e Auditoria
- [ ] 8.0 Backup Manual (Export/Import)
- [ ] 9.0 Testes de Integração HTTP

## Análise de Paralelização

### Lanes de Execução Paralela

| Lane | Tarefas | Descrição |
|------|---------|-----------|
| Lane A (Auth) | 1.0 → 2.0 → 4.0 | Caminho de autenticação: entidades → serviços → controllers |
| Lane B (Pipeline) | 3.0 | Pipeline HTTP — depende de 1.0, paralela com 2.0 |
| Lane C (CRUD) | 5.0, 6.0 | Controllers de negócio — dependem de 3.0, paralelos entre si |
| Lane D (Complementar) | 7.0, 8.0 | Auditoria e Backup — dependem de 3.0, parcialmente paralelos com Lane C |
| Lane E (Testes) | 9.0 | Testes de integração — dependem de todas as tarefas anteriores |

### Caminho Crítico

```
1.0 → 2.0 → 3.0 → 6.0 → 7.0 → 9.0
```

O caminho mais longo passa por: fundação de usuário → auth → pipeline → transactions (maior controller) → auditoria → testes de integração.

### Diagrama de Dependências

```
┌──────┐
│ 1.0  │ Entidades e Infra de Usuário
└──┬───┘
   │
   ├────────────────┐
   │                │
┌──▼───┐      ┌─────▼────┐
│ 2.0  │      │   3.0    │ Pipeline HTTP + Erros
│ Auth │      └────┬─────┘
│Servs │           │
└──┬───┘     ┌─────┼──────────┬────────┐
   │         │     │          │        │
   │    ┌────▼─┐ ┌─▼────┐ ┌─ ─▼──┐ ┌───▼───┐
   └───►│ 4.0  │ │ 5.0  │ │ 6.0  │ │ 8.0   │
        │Auth+ │ │Contas│ │Trans.│ │Backup │
        │Users │ │Categ.│ │      │ │       │
        └──┬───┘ └──┬───┘ └──┬───┘ └────┬──┘
           │        │        │          │
           └────────┴────┬───┘          │
                         │              │
                    ┌────▼─────┐        │
                    │   7.0    │        │
                    │ Audit    │        │
                    └────┬─────┘        │
                         │              │
                    ┌────▼──────────────▼┐
                    │       9.0          │
                    │ Testes Integração  │
                    └────────────────────┘
```

### Oportunidades de Paralelização

1. **Após 1.0**: Tarefas **2.0** (Auth Services) e **3.0** (Pipeline HTTP) podem ser executadas em paralelo
2. **Após 2.0 + 3.0**: Tarefa **4.0** (Auth Controllers) depende de ambas
3. **Após 3.0**: Tarefas **5.0** (Contas/Categorias), **6.0** (Transações) e **8.0** (Backup) podem ser executadas em paralelo
4. **7.0** (Auditoria) depende de 4.0, 5.0 e 6.0 (precisa inserir audit nos handlers)
5. **9.0** (Testes) é a tarefa final, depende de todas as anteriores
````
