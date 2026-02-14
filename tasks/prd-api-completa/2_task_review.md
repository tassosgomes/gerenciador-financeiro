# Task 2.0 Review - Servicos de Autenticacao (JWT + Refresh Token)

## 1) Resultado da validacao da definicao da tarefa

- **Task analisada:** `tasks/prd-api-completa/2_task.md`
- **Tech Spec analisada:** `tasks/prd-api-completa/techspec.md`
- **Regras analisadas (stack .NET):**
  - `rules/dotnet-index.md`
  - `rules/dotnet-architecture.md`
  - `rules/dotnet-coding-standards.md`
  - `rules/dotnet-libraries-config.md`
  - `rules/dotnet-testing.md`
  - `rules/restful.md` (referencia de contrato HTTP/DTO para fase API)

Validacao geral: a implementacao cobre boa parte dos fluxos de autenticacao e gestao de usuarios, com testes abrangentes e build/test passando. Porem, ha gaps relevantes de seguranca e aderencia funcional em relacao ao task spec/techspec, impedindo aprovacao nesta rodada.

## 2) Cobertura de subtarefas (checklist)

- [x] **2.1** `IPasswordHasher` criado
- [x] **2.2** `ITokenService` criado
- [x] **2.3** `PasswordHasher` com BCrypt work factor 12 + pacote `BCrypt.Net-Next`
- [x] **2.4** `TokenService` implementado (JWT claims + refresh por RNG + validacao)
- [x] **2.5** `JwtSettings` criado com defaults
- [~] **2.6** DTOs de autenticacao: implementado com nomes diferentes (`AuthResponse`, `UserResponse`) e **sem `TokenRefreshResponse` dedicado**
- [x] **2.7** Login command/handler/validator implementados
- [x] **2.8** Refresh command/handler com rotacao implementados
- [x] **2.9** Logout command/handler implementados
- [~] **2.10** Change password command/handler/validator implementados, mas **validator sem regras completas de senha forte**
- [~] **2.11** Create user command/handler/validator implementados, mas **validator sem regras completas de senha forte**
- [x] **2.12** Update status (implementado como `ToggleUserStatusCommand`) com revogacao ao desativar
- [x] **2.13** List users query/handler implementados (com `AsNoTracking` no repositorio)
- [x] **2.14** Get user by id query/handler implementados
- [ ] **2.15** Records de paginacao (`PagedResult`, `PaginationMetadata`, `PaginationQuery`) **nao encontrados**
- [~] **2.16** Registro DI de `IPasswordHasher`/`ITokenService`/`JwtSettings` parcial (`AddOptions<JwtSettings>()` sem bind de configuracao)
- [x] **2.17** Handlers e validators registrados na Application
- [x] **2.18** Testes de `PasswordHasher`
- [~] **2.19** Testes de `TokenService` existem, mas **nao cobrem explicitamente token expirado**
- [x] **2.20** Testes de `LoginCommandHandler`
- [~] **2.21** Testes de `RefreshTokenCommandHandler` existem, mas **nao cobrem explicitamente token expirado**
- [x] **2.22** Testes de `ChangePasswordCommandHandler`
- [x] **2.23** Testes de `CreateUserCommandHandler`
- [~] **2.24** Testes de validators existem, mas **nao cobrem complexidade completa de senha forte**
- [x] **2.25** Build validado (`dotnet build`)

Legenda: [x] completo | [~] parcial | [ ] nao atendido

## 3) Descobertas da analise de regras

- **Arquitetura/Clean Architecture:** interfaces na Application e implementacoes na Infra estao corretas.
- **FluentValidation:** validadores existem, mas regras de senha forte exigidas pela tarefa/techspec nao estao completas.
- **Seguranca (techspec):** requisito de refresh token persistido com hash nao foi atendido.
- **Testes:** cobertura ampla e com AAA; ainda faltam alguns cenarios explicitos do spec (expiracao de tokens e senha forte completa).

## 4) Issues encontrados (com severidade)

### Critical

1. **Refresh token persistido em texto plano (nao hash)**
   - Evidencia:
     - `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Auth/TokenService.cs:52`
     - `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Entity/RefreshToken.cs:6`
     - `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/Repository/RefreshTokenRepository.cs:22`
   - Impacto: vazamento de base permite reutilizacao imediata de refresh tokens.
   - Necessario: armazenar hash (ex.: SHA-256/HMAC) e comparar hash na busca/validacao.

### Major

2. **Subtarefa 2.15 nao implementada (paginacao base ausente)**
   - Evidencia: nao ha `PagedResult`, `PaginationMetadata`, `PaginationQuery` em `2-Application/Common`.

3. **Regras de senha forte incompletas (ChangePassword/CreateUser)**
   - Evidencia:
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Auth/ChangePasswordCommandValidator.cs:12`
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/User/CreateUserCommandValidator.cs:19`
   - Impacto: nao atende criterio de seguranca do task spec (maiuscula, minuscula, numero, especial).

4. **Contrato de refresh response divergente do spec (`TokenRefreshResponse` ausente)**
   - Evidencia:
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Auth/RefreshTokenCommand.cs:6`
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Dtos/AuthResponse.cs:3`
   - Impacto: risco de desalinhamento com contrato esperado para endpoint de refresh.

5. **Registro de `JwtSettings` incompleto na DI (sem bind de configuracao)**
   - Evidencia: `backend/4-Infra/GestorFinanceiro.Financeiro.Infra/DependencyInjection/ServiceCollectionExtensions.cs:33`
   - Impacto: em runtime, `TokenService` pode receber valores default vazios (secret/issuer/audience), quebrando emissao/validacao.

### Minor

6. **Handlers instanciam validators manualmente (`new Validator()`)**
   - Evidencia:
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Auth/LoginCommandHandler.cs:38`
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Auth/RefreshTokenCommandHandler.cs:35`
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Auth/ChangePasswordCommandHandler.cs:33`
     - `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/User/CreateUserCommandHandler.cs:32`
   - Recomendacao: injetar validators via DI para manter consistencia e testabilidade.

7. **Lacunas de testes pontuais vs spec**
   - Token expirado nao testado explicitamente em `TokenServiceTests`/`RefreshTokenCommandHandlerTests`.
   - Cenarios de senha forte completa nao testados nos validators.

## 5) Problemas enderecados e resolucoes

- Esta etapa foi de **review** (sem alteracao de codigo). Nenhum problema foi corrigido neste ciclo.
- Correcoes necessarias estao listadas acima para retorno ao implementador.

## 6) Build e testes executados

- `dotnet build` em `backend/` -> **sucesso**
- `dotnet test` em `backend/` -> **sucesso** (unit + integration + e2e, com 1 teste de integracao skipado)

## 7) Status final da review

- **Status:** `CHANGES REQUESTED` (equivalente a `REJECTED`)

## 8) Conclusao e prontidao para deploy

- **Tarefa 2.0 ainda nao esta pronta para deploy** devido a pendencias criticas/maiores de seguranca e aderencia ao spec.
- Recomendacao: corrigir os itens 1 a 5 e reexecutar review.
