# Plano Técnico - Migração de Autenticação para Cookies HttpOnly

**Data:** 18/02/2026  
**Escopo:** Backend (.NET) + Frontend (React)  
**Objetivo:** Remover exposição de tokens em JavaScript e migrar o fluxo para cookies HttpOnly com rollout seguro.

---

## 1) Estado Atual (resumo técnico)

- Backend retorna `AccessToken` e `RefreshToken` no corpo de `/api/v1/auth/login` e `/api/v1/auth/refresh`.
- Frontend mantém `accessToken` em memória e `refreshToken` + `user` persistidos em `localStorage`.
- O interceptor Axios envia `Authorization: Bearer ...` e, em `401`, faz refresh com token no body.
- CORS já permite credenciais (`AllowCredentials`) no backend.

### Risco remanescente atual

- `refreshToken` em `localStorage` ainda é legível por JavaScript e vulnerável a XSS.

---

## 2) Meta de Segurança

Migrar para:

- `refresh_token` em cookie **HttpOnly + Secure + SameSite**.
- `access_token` preferencialmente também em cookie HttpOnly (ou em memória apenas durante transição curta).
- Frontend sem persistência de tokens em `localStorage`.

---

## 3) Estratégia de Migração (sem quebra)

## Fase 1 - Compatibilidade (recomendada para próxima sprint)

### Backend

1. Criar configuração tipada de cookies de autenticação:
   - Nome dos cookies
   - Domínio
   - Caminho
   - Expiração
   - `SecurePolicy`, `HttpOnly`, `SameSite`

2. Em `POST /api/v1/auth/login`:
   - Continuar retornando `AuthResponse` (compatibilidade)
   - Adicionar `Set-Cookie` de `refresh_token` HttpOnly
   - Opcional na fase 1: também setar `access_token` em cookie HttpOnly

3. Em `POST /api/v1/auth/refresh`:
   - Ler refresh token do cookie **ou** do body (fallback temporário)
   - Rotacionar e sobrescrever cookie
   - Manter retorno no body por compatibilidade

4. Em `POST /api/v1/auth/logout`:
   - Revogar sessão no backend
   - Expirar cookies (`Expires` no passado)

5. Ajustar JWT bearer para aceitar token de cookie (se optar por access token em cookie já na fase 1):
   - `JwtBearerOptions.Events.OnMessageReceived` para extrair do cookie quando header não existir.

### Frontend

1. Definir `withCredentials: true` no `apiClient`.
2. Manter fluxo atual de bearer token durante transição.
3. Alterar `refreshTokenApi` para enviar body apenas quando existir fallback de compatibilidade.
4. Remover persistência de `refreshToken` do `localStorage` e manter apenas `user`/estado mínimo.

### Testes

Backend:
- Login deve retornar `Set-Cookie`.
- Refresh deve funcionar com cookie e com body (fase de transição).
- Logout deve limpar cookie.

Frontend:
- Regressão do fluxo de refresh no interceptor.
- Sessão expirada deve redirecionar para login.

---

## Fase 2 - Endurecimento

1. Remover `RefreshToken` do `AuthResponse`.
2. Remover leitura de refresh token via body no endpoint `/refresh`.
3. Remover qualquer persistência de token no cliente.
4. Ajustar documentação OpenAPI e consumidores.

---

## 4) Mudanças por arquivo (estimativa)

### Backend

- `backend/1-Services/GestorFinanceiro.Financeiro.API/Controllers/AuthController.cs`
  - escrita/limpeza de cookies em login, refresh e logout.

- `backend/1-Services/GestorFinanceiro.Financeiro.API/Program.cs`
  - binding de opções de cookie e eventos do `JwtBearer` para leitura via cookie.

- `backend/2-Application/GestorFinanceiro.Financeiro.Application/Dtos/AuthResponse.cs`
  - fase 2: remover `RefreshToken`.

- testes HTTP e unitários de auth
  - validar `Set-Cookie`, refresh por cookie, limpeza no logout.

### Frontend

- `frontend/src/shared/services/apiClient.ts`
  - ativar `withCredentials`.

- `frontend/src/features/auth/api/authApi.ts`
  - refresh sem dependência obrigatória do body na fase final.

- `frontend/src/features/auth/store/authStore.ts`
  - remover persistência de refresh token e adequar hidratação.

- testes da store e integração auth
  - ajustar mocks e asserts para novo contrato.

---

## 5) Regras de Cookie recomendadas

Produção:

- `HttpOnly = true`
- `Secure = true`
- `SameSite = None` (quando frontend e backend estiverem em domínios diferentes e houver CORS com credenciais)
- `Path` restrito a `/api/v1/auth` para refresh token quando possível

Desenvolvimento local:

- manter `Secure` configurável por ambiente
- usar `SameSite=Lax` quando possível no mesmo site

---

## 6) Critérios de aceite

1. Não existe token sensível persistido em `localStorage`.
2. Login e refresh funcionam com cookies HttpOnly.
3. Logout invalida sessão no backend e remove cookies no navegador.
4. Fluxo de auto-refresh no frontend continua funcional sem regressão.
5. Testes de auth (unitários + integração HTTP) cobrindo cenários de cookie passam.

---

## 7) Riscos e mitigação

- **Risco:** quebra de ambiente local por política `Secure`/`SameSite`.  
  **Mitigação:** flags por ambiente e testes CORS locais.

- **Risco:** clientes antigos dependendo de `refreshToken` no body.  
  **Mitigação:** fase de compatibilidade (Fase 1) com janela de depreciação definida.

- **Risco:** diferenças entre subdomínios em produção.  
  **Mitigação:** validar `Domain` e `SameSite` em staging antes de produção.

---

## 8) Estimativa de esforço

- Fase 1: 1 a 2 dias úteis (incluindo testes e ajustes de ambiente).
- Fase 2: 0,5 a 1 dia útil após estabilização da Fase 1.

---

## 9) Próxima execução recomendada

1. Implementar Fase 1 no backend primeiro.
2. Ajustar frontend para `withCredentials` e remoção de persistência de refresh token.
3. Rodar suíte de testes HTTP de auth + testes frontend de sessão.
4. Liberar em staging com validação cross-origin real.
