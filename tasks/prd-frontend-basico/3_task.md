---
status: pending
parallelizable: false
blocked_by: ["1.0", "2.0"]
---

<task_context>
<domain>frontend/auth</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>zustand, axios, react-hook-form, zod</dependencies>
<unblocks>"5.0", "6.0", "7.0", "8.0", "9.0"</unblocks>
</task_context>

# Tarefa 3.0: Feature de Autenticação

## Visão Geral

Implementar a feature completa de autenticação: tela de login com validação inline, Zustand auth store para gerenciamento de tokens JWT, interceptors do Axios para injeção automática do token e tratamento de 401 (refresh/logout), componente ProtectedRoute funcional e fluxo completo de login → dashboard → logout. Esta é a feature que "abre a porta" para todas as outras.

## Requisitos

- PRD F1: Tela de login com e-mail e senha (req. 1)
- PRD F1: Token JWT armazenado de forma segura (req. 2) — Zustand + localStorage
- PRD F1: Mensagem genérica "Credenciais inválidas" em erro de login (req. 3)
- PRD F1: Redirect para dashboard após login (req. 4)
- PRD F1: Sessão expirada → redirect para login com mensagem (req. 5)
- PRD F1: Botão de logout acessível em todas as telas (req. 6)
- Tela de login fiel ao mockup `screen-examples/login/index.html`
- Refresh token automático via interceptor
- Persistência de sessão entre reloads (localStorage)

## Subtarefas

- [ ] 3.1 Criar `src/features/auth/types/auth.ts` — interfaces: `LoginRequest` (email, password), `AuthResponse` (accessToken, refreshToken, user), `UserResponse` (id, name, email, role)
- [ ] 3.2 Criar `src/features/auth/api/authApi.ts` — funções: `loginApi(email, password)`, `refreshTokenApi(refreshToken)`, `logoutApi(refreshToken)` usando apiClient
- [ ] 3.3 Criar `src/features/auth/store/authStore.ts` — Zustand store com: `accessToken`, `refreshToken`, `user`, `isAuthenticated`, `isLoading`, `login()`, `logout()`, `refreshSession()`, `setTokens()`, `hydrate()`. Persistir tokens em localStorage; hidratar no init
- [ ] 3.4 Atualizar `src/shared/services/apiClient.ts` — Request interceptor: injetar `Authorization: Bearer <token>` do authStore. Response interceptor: em 401, tentar refresh; se falhar, chamar `logout()` e redirect para `/login`
- [ ] 3.5 Criar schema de validação Zod: `loginSchema` — email obrigatório (formato e-mail válido), senha obrigatória (mínimo 6 caracteres)
- [ ] 3.6 Criar `src/features/auth/components/LoginForm.tsx` — formulário com react-hook-form + zod: campos e-mail e senha com ícones (mail_outline, lock_outline), botão "Entrar", validação inline, estado de loading no botão, mensagem de erro genérica ("Credenciais inválidas"), checkbox "Lembrar de mim" (desabilitado no MVP)
- [ ] 3.7 Criar `src/features/auth/pages/LoginPage.tsx` — página de login com layout centralizado, logo GestorFinanceiro, frase "Bem-vindo de volta", footer com "Ambiente seguro e criptografado", fundo com blobs decorativos — fiel ao mockup `screen-examples/login/`
- [ ] 3.8 Atualizar `src/shared/components/layout/ProtectedRoute.tsx` — verificar `isAuthenticated` do authStore; se não autenticado, redirect para `/login`; se autenticado, renderizar `<Outlet />`
- [ ] 3.9 Atualizar `src/shared/components/layout/Topbar.tsx` — exibir nome do usuário logado do authStore, botão de logout funcional que chama `authStore.logout()` e navega para `/login`
- [ ] 3.10 Atualizar `src/shared/components/layout/Sidebar.tsx` — exibir nome do usuário no footer da sidebar; esconder item "Admin" se usuário não tem role Admin
- [ ] 3.11 Criar `src/features/auth/index.ts` — barrel export da feature
- [ ] 3.12 Atualizar rotas em `routes.tsx` — `/login` renderiza LoginPage (pública); rotas internas wrapped em ProtectedRoute
- [ ] 3.13 Criar MSW handlers de auth: `src/features/auth/test/handlers.ts` — mock de POST `/api/v1/auth/login` (sucesso e erro), POST `/api/v1/auth/refresh`, POST `/api/v1/auth/logout`
- [ ] 3.14 Testes unitários: `LoginForm` (validação, submit com sucesso, erro de credencial), `authStore` (login, logout, refresh, hydrate), interceptors (injeção de token, tratamento 401)
- [ ] 3.15 Teste de integração: fluxo Login → redirect Dashboard → Logout → redirect Login

## Sequenciamento

- Bloqueado por: 1.0 (Scaffold), 2.0 (Layout)
- Desbloqueia: 5.0 (Dashboard), 6.0 (Contas), 7.0 (Categorias), 8.0 (Transações), 9.0 (Admin)
- Paralelizável: Não (depende do layout para funcionar)

## Detalhes de Implementação

### Auth Store (Zustand)

```typescript
interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserResponse | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<boolean>;
  setTokens: (access: string, refresh: string, user: UserResponse) => void;
  hydrate: () => void;
}

// Persistência: salvar tokens em localStorage
// Hidratação: ao iniciar, ler tokens do localStorage e validar via refresh
```

### Axios Interceptors

```typescript
// Request: injetar token
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Response: tratar 401
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retry) {
      error.config._retry = true;
      const refreshed = await useAuthStore.getState().refreshSession();
      if (refreshed) {
        error.config.headers.Authorization =
          `Bearer ${useAuthStore.getState().accessToken}`;
        return apiClient(error.config);
      }
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  }
);
```

### Login Schema (Zod)

```typescript
const loginSchema = z.object({
  email: z.string().email('E-mail inválido').min(1, 'E-mail obrigatório'),
  password: z.string().min(6, 'Senha deve ter no mínimo 6 caracteres'),
});
```

### Referência Visual — LoginPage

Fiel ao mockup `screen-examples/login/index.html`:
- Container centralizado, `max-w-md`, card branco com sombra
- Logo com ícone `account_balance_wallet` em círculo azul
- Título "Bem-vindo de volta"
- Subtítulo "Acesse sua conta para gerenciar suas finanças."
- Campo e-mail com ícone `mail_outline`
- Campo senha com ícone `lock_outline`
- Checkbox "Lembrar de mim" + link "Esqueceu a senha?" (ambos desabilitados/placeholder no MVP)
- Botão "Entrar" full-width, `bg-primary`
- Footer: ícone de cadeado + "Ambiente seguro e criptografado."
- Fundo com blobs decorativos em `bg-primary/5` e `bg-primary/10`

## Critérios de Sucesso

- Login com credenciais válidas → token armazenado, redirect para `/dashboard`
- Login com credenciais inválidas → mensagem "Credenciais inválidas" exibida
- Acesso a rota protegida sem token → redirect para `/login`
- Token expirado durante navegação → refresh automático transparente
- Refresh falha → logout automático + redirect para `/login`
- Botão de logout na topbar → limpa tokens, redirect para `/login`
- Sessão persiste após reload da página (tokens em localStorage)
- Tela de login visualmente fiel ao mockup
- Validação inline funciona (e-mail inválido, senha curta)
- Todos os testes unitários passam
- Teste de integração do fluxo completo passa
