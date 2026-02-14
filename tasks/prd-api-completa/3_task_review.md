# Review Task 3.0 - Pipeline HTTP e Tratamento de Erros

## 1) Resultados da validacao da definicao da tarefa

- Escopo validado contra `tasks/prd-api-completa/3_task.md`, `tasks/prd-api-completa/prd.md` (F7 req 39-44) e `tasks/prd-api-completa/techspec.md`.
- Implementacao cobre os pilares da task: DI no `Program.cs`, JWT Bearer, Authorization policy `AdminOnly`, CORS, Swagger condicional em Development, Health Check em `/health`, `GlobalExceptionHandler` com Problem Details e `ValidationActionFilter`.
- Build e testes executados nesta review:
  - `dotnet build` em `backend/`: OK
  - `dotnet test 5-Tests/GestorFinanceiro.Financeiro.UnitTests/GestorFinanceiro.Financeiro.UnitTests.csproj`: 203/203 passando
- Conclusao de aderencia funcional: **parcialmente aderente**. Existem gaps de mapeamento de excecao e seguranca que bloqueiam aprovacao.

## 2) Descobertas da analise de regras

Regras analisadas:
- `rules/dotnet-index.md`
- `rules/dotnet-architecture.md`
- `rules/dotnet-coding-standards.md`
- `rules/dotnet-logging.md`
- `rules/dotnet-observability.md`
- `rules/dotnet-testing.md`
- `rules/restful.md`

Conformidades observadas:
- Tratamento global com `IExceptionHandler` implementado (alinhado a `rules/dotnet-architecture.md`).
- Erros retornando Problem Details com `type/title/status/detail/instance` e `ValidationProblemDetails` para validacao (alinhado a `rules/restful.md`).
- Pipeline principal na ordem correta (UseExceptionHandler -> UseCors -> UseAuthentication -> UseAuthorization -> endpoints).
- Health Checks com self + DbContext + Npgsql e endpoint publico `/health` (alinhado a `rules/dotnet-observability.md`).

Nao conformidades / riscos:
- Nem todas as excecoes de entidade "not found" estao mapeadas para 404.
- Configuracao CORS abre `AllowAnyOrigin()` quando configuracao nao existe.
- Segredos/senhas padrao versionados em `appsettings`.
- Validacao de robustez da chave JWT esta fraca (somente not-empty).

## 3) Resumo da revisao de codigo

- `Program.cs` esta bem estruturado e cobre os requisitos principais de pipeline, autenticacao, autorizacao, Swagger e health checks.
- `GlobalExceptionHandler` tem boa cobertura de excecoes de dominio e separa logging de erros esperados (`LogWarning`) vs inesperados (`LogError`).
- `ValidationActionFilter` retorna 400 consistente com Problem Details e estrutura de erros por campo.
- Testes unitarios adicionados para middleware/filtro validam cenarios essenciais, mas ainda nao cobrem todos os mapeamentos sensiveis de excecao.

## 4) Problemas identificados (pendencias) e recomendacoes

1. **[HIGH] Mapeamento incompleto de excecao not found para 404**
   - Arquivo: `backend/1-Services/GestorFinanceiro.Financeiro.API/Middleware/GlobalExceptionHandler.cs:93`
   - Problema: `RecurrenceTemplateNotFoundException` (e quaisquer not found nao explicitamente listadas) cai no branch generico `DomainException -> 400`, contrariando o requisito de "entity not found -> 404" da task/techspec.
   - Recomendacao: adicionar mapeamento explicito para `RecurrenceTemplateNotFoundException` com `StatusCodes.Status404NotFound` e validar outros `*NotFoundException`.

2. **[HIGH] Segredos/senhas hardcoded em configuracao versionada**
   - Arquivos:
     - `backend/1-Services/GestorFinanceiro.Financeiro.API/appsettings.json:3`
     - `backend/1-Services/GestorFinanceiro.Financeiro.API/appsettings.json:20`
     - `backend/1-Services/GestorFinanceiro.Financeiro.API/appsettings.Development.json:22`
   - Problema: senha de banco e senha de admin seed estao em texto plano nos arquivos versionados.
   - Recomendacao: mover valores sensiveis para variaveis de ambiente/secret manager e manter apenas placeholders seguros nos `appsettings`.

3. **[MEDIUM] Fallback de CORS inseguro**
   - Arquivo: `backend/1-Services/GestorFinanceiro.Financeiro.API/Program.cs:100`
   - Problema: ausencia de `CorsSettings:AllowedOrigins` resulta em `AllowAnyOrigin()`, abrindo toda a API para qualquer origem.
   - Recomendacao: falhar no startup quando `AllowedOrigins` estiver vazio em ambientes nao Development, ou aplicar politica restrita padrao.

4. **[MEDIUM] Validacao de chave JWT insuficiente**
   - Arquivo: `backend/1-Services/GestorFinanceiro.Financeiro.API/Program.cs:42`
   - Problema: apenas verifica se `SecretKey` nao e vazia; nao valida tamanho minimo recomendado (>= 256 bits para HMAC).
   - Recomendacao: validar comprimento minimo e bloquear inicializacao com chave fraca.

5. **[LOW] Cobertura de testes de mapeamento pode melhorar**
   - Arquivo: `backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/API/GlobalExceptionHandlerTests.cs:29`
   - Problema: testes nao cobrem explicitamente todos os casos de auth/not found adicionados no handler (ex.: `InactiveUserException`, `InvalidRefreshTokenException`, `UserNotFoundException`, `RecurrenceTemplateNotFoundException`).
   - Recomendacao: adicionar cenarios parametrizados para reduzir risco de regressao nos codigos HTTP.

## 5) Status da review

**CHANGES_REQUESTED**

## 6) Confirmacao de conclusao e prontidao para deploy

- **Nao pronto para deploy** no estado atual devido aos bloqueios de seguranca e mapeamento de erros listados acima.
- Apos correcoes e nova validacao de build/testes, a task pode ser reavaliada para aprovacao.
