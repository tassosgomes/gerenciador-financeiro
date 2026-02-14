```markdown
---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>engine/aplicação+infra</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"4.0"</unblocks>
</task_context>

# Tarefa 2.0: Serviços de Autenticação (JWT + Refresh Token)

## Visão Geral

Implementar os serviços de autenticação: `IPasswordHasher` (BCrypt), `ITokenService` (JWT + refresh token), e todos os Commands/Queries/Handlers/Validators CQRS de autenticação — Login, Refresh, Logout, ChangePassword, CreateUser, ListUsers, GetUser, UpdateUserStatus. Inclui DTOs de autenticação e registro na DI.

Esta tarefa cria toda a lógica de autenticação e gestão de usuários, sem ainda expor via HTTP (controllers vêm na tarefa 4.0).

## Requisitos

- Techspec: JWT Bearer com HMAC SHA-256, expiração configurável (padrão 24h)
- Techspec: Refresh token com string aleatória 256 bits, persistido com hash no banco
- Techspec: BCrypt com work factor ≥ 12
- PRD F1 req 1-9: Login, refresh, logout, change password, CRUD de usuários
- `rules/dotnet-architecture.md`: Interfaces na Application, implementações na Infra
- `rules/dotnet-libraries-config.md`: FluentValidation para validação

## Subtarefas

### Interfaces (Application Layer)

- [ ] 2.1 Criar `IPasswordHasher` em `2-Application/Common/IPasswordHasher.cs`:
  - `string Hash(string password)`
  - `bool Verify(string password, string hash)`
- [ ] 2.2 Criar `ITokenService` em `2-Application/Common/ITokenService.cs`:
  - `string GenerateAccessToken(User user)`
  - `RefreshToken GenerateRefreshToken(Guid userId)`
  - `ClaimsPrincipal? ValidateAccessToken(string token)`

### Implementações (Infra Layer)

- [ ] 2.3 Criar pasta `4-Infra/Auth/` e implementar `PasswordHasher`:
  - Usar `BCrypt.Net-Next` com work factor 12
  - Adicionar pacote NuGet `BCrypt.Net-Next` ao projeto Infra
- [ ] 2.4 Implementar `TokenService` em `4-Infra/Auth/TokenService.cs`:
  - Receber configurações JWT via `IOptions<JwtSettings>`
  - Gerar access token com claims: `sub` (userId), `name`, `email`, `role`
  - Gerar refresh token com `RandomNumberGenerator` (256 bits → Base64)
  - Validar access token (retornar ClaimsPrincipal ou null)
- [ ] 2.5 Criar classe `JwtSettings` em `4-Infra/Auth/JwtSettings.cs`:
  - Propriedades: `SecretKey`, `Issuer`, `Audience`, `AccessTokenExpirationMinutes` (padrão 1440 = 24h), `RefreshTokenExpirationDays` (padrão 7)

### DTOs de Autenticação

- [ ] 2.6 Criar DTOs em `2-Application/Dtos/`:
  - `LoginResponse` (AccessToken, RefreshToken, ExpiresIn, UserDto)
  - `UserDto` (Id, Name, Email, Role, IsActive, MustChangePassword)
  - `TokenRefreshResponse` (AccessToken, RefreshToken, ExpiresIn)

### Commands de Autenticação

- [ ] 2.7 `LoginCommand` + `LoginCommandHandler` + `LoginValidator`:
  - Input: Email, Password
  - Validar credenciais via `IPasswordHasher`
  - Verificar se usuário está ativo
  - Gerar access + refresh token
  - Persistir refresh token
  - Retornar `LoginResponse`
  - Validator: email obrigatório e formato válido, password obrigatória
- [ ] 2.8 `RefreshTokenCommand` + `RefreshTokenCommandHandler`:
  - Input: RefreshToken
  - Buscar token no banco, verificar se ativo
  - Revogar token antigo
  - Gerar novo access + refresh token (rotação)
  - Retornar `TokenRefreshResponse`
- [ ] 2.9 `LogoutCommand` + `LogoutCommandHandler`:
  - Input: UserId (do token JWT)
  - Revogar todos os refresh tokens do usuário
- [ ] 2.10 `ChangePasswordCommand` + `ChangePasswordCommandHandler` + `ChangePasswordValidator`:
  - Input: UserId, CurrentPassword, NewPassword
  - Verificar senha atual
  - Hash da nova senha
  - Atualizar usuário
  - Revogar todos os refresh tokens
  - Validator: senha atual obrigatória, nova senha obrigatória (mínimo 8 chars, pelo menos 1 maiúscula, 1 minúscula, 1 número, 1 especial)

### Commands de Gestão de Usuários

- [ ] 2.11 `CreateUserCommand` + `CreateUserCommandHandler` + `CreateUserValidator`:
  - Input: Name, Email, Password, Role, CreatedByUserId
  - Verificar email duplicado
  - Hash da senha
  - Criar entidade User
  - Retornar `UserDto`
  - Validator: nome (3-150 chars), email válido, password forte, role válido
- [ ] 2.12 `UpdateUserStatusCommand` + `UpdateUserStatusCommandHandler`:
  - Input: UserId, IsActive, UpdatedByUserId
  - Ativar ou desativar usuário
  - Se desativar: revogar todos os refresh tokens

### Queries de Gestão de Usuários

- [ ] 2.13 `ListUsersQuery` + `ListUsersQueryHandler`:
  - Retornar lista de `UserDto`
  - Usar `AsNoTracking()`
- [ ] 2.14 `GetUserByIdQuery` + `GetUserByIdQueryHandler`:
  - Input: UserId
  - Retornar `UserDto` ou lançar exceção se não encontrado

### Paginação (base para queries futuras)

- [ ] 2.15 Criar records de paginação em `2-Application/Common/`:
  - `PagedResult<T>(IEnumerable<T> Data, PaginationMetadata Pagination)`
  - `PaginationMetadata(int Page, int Size, int Total, int TotalPages)`
  - `PaginationQuery` (Page, Size) com defaults (Page=1, Size=20, MaxSize=100)

### Registro na DI

- [ ] 2.16 Registrar `IPasswordHasher`, `ITokenService`, `JwtSettings` na DI (estender `ServiceCollectionExtensions` da Infra)
- [ ] 2.17 Registrar novos handlers e validators na DI (estender extensões da Application)

### Testes Unitários

- [ ] 2.18 Testes para `PasswordHasher`:
  - Hash gera string não vazia
  - Verify com senha correta retorna true
  - Verify com senha incorreta retorna false
- [ ] 2.19 Testes para `TokenService`:
  - GenerateAccessToken gera JWT válido com claims corretos
  - GenerateRefreshToken gera string Base64 não vazia
  - ValidateAccessToken com token válido retorna ClaimsPrincipal
  - ValidateAccessToken com token expirado retorna null
- [ ] 2.20 Testes para `LoginCommandHandler`:
  - Login com credenciais válidas retorna tokens
  - Login com email inexistente lança `InvalidCredentialsException`
  - Login com senha incorreta lança `InvalidCredentialsException`
  - Login com usuário inativo lança `InactiveUserException`
- [ ] 2.21 Testes para `RefreshTokenCommandHandler`:
  - Refresh com token válido retorna novos tokens
  - Refresh com token expirado lança `InvalidRefreshTokenException`
  - Refresh com token revogado lança `InvalidRefreshTokenException`
- [ ] 2.22 Testes para `ChangePasswordCommandHandler`:
  - Sucesso: senha atualizada, tokens revogados
  - Senha atual incorreta lança exceção
- [ ] 2.23 Testes para `CreateUserCommandHandler`:
  - Sucesso: usuário criado com hash
  - Email duplicado lança `EmailAlreadyExistsException`
- [ ] 2.24 Testes para Validators (LoginValidator, CreateUserValidator, ChangePasswordValidator)

### Validação

- [ ] 2.25 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 1.0 (necessita das entidades User, RefreshToken e repositórios)
- Desbloqueia: 4.0 (Controllers de Auth e Users)
- Paralelizável: Sim (pode ser executada em paralelo com 3.0)

## Detalhes de Implementação

### JwtSettings (configuração)

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 1440; // 24h
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

### LoginCommandHandler (fluxo)

```
1. Buscar usuário por email (IUserRepository.GetByEmailAsync)
2. Se não encontrado → InvalidCredentialsException
3. Verificar senha (IPasswordHasher.Verify)
4. Se inválida → InvalidCredentialsException
5. Se !IsActive → InactiveUserException
6. Gerar access token (ITokenService.GenerateAccessToken)
7. Gerar refresh token (ITokenService.GenerateRefreshToken)
8. Persistir refresh token (IRefreshTokenRepository.AddAsync)
9. Commit (IUnitOfWork)
10. Retornar LoginResponse
```

### RefreshTokenCommand (fluxo com rotação)

```
1. Buscar refresh token no banco (IRefreshTokenRepository.GetByTokenAsync)
2. Se não encontrado ou !IsActive → InvalidRefreshTokenException
3. Revogar token antigo
4. Gerar novo access + refresh token
5. Persistir novo refresh token
6. Commit
7. Retornar TokenRefreshResponse
```

## Critérios de Sucesso

- `IPasswordHasher` com BCrypt funcional (hash + verify)
- `ITokenService` gera JWT válidos com claims corretos
- Login retorna access + refresh token para credenciais válidas
- Refresh token com rotação (revoga antigo, gera novo)
- Logout revoga todos os refresh tokens do usuário
- Change password valida senha atual e atualiza
- CRUD de usuários funcional com validações
- Todos os validators cobrem cenários de entrada inválida
- Records de paginação prontos para uso nos controllers
- Testes unitários passam para todos os handlers
- Build compila sem erros
```
