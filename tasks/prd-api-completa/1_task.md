```markdown
---
status: pending
parallelizable: false
blocked_by: []
---

<task_context>
<domain>engine/domínio+infra</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"2.0", "3.0"</unblocks>
</task_context>

# Tarefa 1.0: Entidades e Infraestrutura de Usuário

## Visão Geral

Criar toda a fundação de usuários e refresh tokens no domínio e infraestrutura: entidades `User` e `RefreshToken`, enum `UserRole`, exceções de autenticação, interfaces de repositório, configurações EF Core (Fluent API) e migration para as novas tabelas `users` e `refresh_tokens`. Esta tarefa é pré-requisito para toda a autenticação e, transitivamente, para todos os endpoints protegidos.

## Requisitos

- Techspec: Novas entidades `User` e `RefreshToken` herdando de `BaseEntity`
- Techspec: Enum `UserRole` com valores `Admin = 1` e `Member = 2`
- Techspec: Interfaces `IUserRepository` e `IRefreshTokenRepository`
- PRD F1 req 5: Admin cria usuários com nome, e-mail, senha e papel
- PRD F1 req 6: Admin pode desativar usuário sem exclusão física
- Techspec: Migration aditiva (ADD TABLE), sem ALTER em tabelas existentes
- `rules/dotnet-architecture.md`: Domain sem dependências externas
- `rules/dotnet-coding-standards.md`: Código em inglês, PascalCase

## Subtarefas

### Enum

- [ ] 1.1 Criar `UserRole` em `3-Domain/Entity/Enum/UserRole.cs` com valores `Admin = 1` e `Member = 2`

### Entidades

- [ ] 1.2 Criar entidade `User` em `3-Domain/Entity/User.cs`:
  - Propriedades: `Name`, `Email`, `PasswordHash`, `Role` (UserRole), `IsActive`, `MustChangePassword`
  - Factory method `Create(name, email, passwordHash, role, createdByUserId)`
  - Métodos: `Deactivate(userId)`, `Activate(userId)`, `ChangePassword(newPasswordHash, userId)`
  - Validações no construtor/factory: nome obrigatório (3-150 chars), email obrigatório e formato válido
- [ ] 1.3 Criar entidade `RefreshToken` em `3-Domain/Entity/RefreshToken.cs`:
  - Propriedades: `UserId`, `Token`, `ExpiresAt`, `IsRevoked`, `RevokedAt`
  - Computed: `IsExpired` (`DateTime.UtcNow >= ExpiresAt`), `IsActive` (`!IsRevoked && !IsExpired`)
  - Factory method `Create(userId, token, expiresAt)`
  - Método: `Revoke()`

### Exceções de Autenticação

- [ ] 1.4 Criar exceções em `3-Domain/Exception/`:
  - `InvalidCredentialsException` — login com credenciais inválidas
  - `InactiveUserException` — tentativa de login com usuário inativo
  - `EmailAlreadyExistsException` — criação de usuário com email duplicado
  - `InvalidRefreshTokenException` — refresh token inválido, expirado ou revogado
  - `PasswordChangeRequiredException` — (opcional) sinalizar que senha temporária precisa ser trocada

### Interfaces de Repositório

- [ ] 1.5 Criar `IUserRepository` em `3-Domain/Interface/IUserRepository.cs`:
  - Herda de `IRepository<User>`
  - Métodos: `GetByEmailAsync(email, ct)`, `ExistsByEmailAsync(email, ct)`, `GetAllAsync(ct)`
- [ ] 1.6 Criar `IRefreshTokenRepository` em `3-Domain/Interface/IRefreshTokenRepository.cs`:
  - Métodos: `GetByTokenAsync(token, ct)`, `AddAsync(refreshToken, ct)`, `RevokeByUserIdAsync(userId, ct)`, `CleanupExpiredAsync(ct)`

### Configurações EF Core

- [ ] 1.7 Criar `UserConfiguration` em `4-Infra/Config/UserConfiguration.cs`:
  - Tabela `users`, mapeamento snake_case
  - `Name` VARCHAR(150) NOT NULL
  - `Email` VARCHAR(255) NOT NULL com índice único
  - `PasswordHash` VARCHAR(500) NOT NULL
  - `Role` INTEGER NOT NULL
  - `IsActive` BOOLEAN NOT NULL DEFAULT TRUE
  - `MustChangePassword` BOOLEAN NOT NULL DEFAULT TRUE
  - Campos de auditoria herdados de `BaseEntity`
- [ ] 1.8 Criar `RefreshTokenConfiguration` em `4-Infra/Config/RefreshTokenConfiguration.cs`:
  - Tabela `refresh_tokens`
  - `UserId` UUID NOT NULL com FK para `users(id)`
  - `Token` VARCHAR(500) NOT NULL com índice único
  - `ExpiresAt` TIMESTAMP NOT NULL
  - `IsRevoked` BOOLEAN NOT NULL DEFAULT FALSE
  - `RevokedAt` TIMESTAMP nullable
  - Índice em `user_id`

### Repositórios

- [ ] 1.9 Criar `UserRepository` em `4-Infra/Repository/UserRepository.cs`:
  - Implementa `IUserRepository`, herda de `Repository<User>`
  - Implementar `GetByEmailAsync`, `ExistsByEmailAsync`, `GetAllAsync`
- [ ] 1.10 Criar `RefreshTokenRepository` em `4-Infra/Repository/RefreshTokenRepository.cs`:
  - Implementa `IRefreshTokenRepository`
  - Implementar todos os métodos (usar `ExecuteDeleteAsync` para cleanup)

### DbContext e Migration

- [ ] 1.11 Adicionar `DbSet<User>` e `DbSet<RefreshToken>` ao `FinanceiroDbContext`
- [ ] 1.12 Registrar `UserConfiguration` e `RefreshTokenConfiguration` no `OnModelCreating`
- [ ] 1.13 Criar migration EF Core para as novas tabelas (aditiva, sem alterar tabelas existentes)
- [ ] 1.14 Registrar `IUserRepository` e `IRefreshTokenRepository` na DI (`ServiceCollectionExtensions`)

### Testes Unitários

- [ ] 1.15 Testes para `User`:
  - `Create` com dados válidos
  - `Create` com nome vazio/curto → exceção
  - `Create` com email inválido → exceção
  - `Deactivate` e `Activate` alternam `IsActive`
  - `ChangePassword` atualiza hash e marca `MustChangePassword = false`
- [ ] 1.16 Testes para `RefreshToken`:
  - `Create` com dados válidos
  - `IsExpired` retorna true quando `ExpiresAt` no passado
  - `IsActive` retorna false quando revogado
  - `Revoke` marca token como revogado com timestamp

### Validação

- [ ] 1.17 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: Nenhum (primeira tarefa da Fase 2)
- Desbloqueia: 2.0 (Auth Services), 3.0 (Pipeline HTTP)
- Paralelizável: Não (é a tarefa fundacional)

## Detalhes de Implementação

### Entidade User (conforme techspec)

```csharp
public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool MustChangePassword { get; private set; } = true;

    protected User() { } // EF Core

    public static User Create(string name, string email, string passwordHash,
        UserRole role, string createdByUserId)
    {
        // Validações de domínio
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            MustChangePassword = true
        };
        user.SetAuditOnCreate(createdByUserId);
        return user;
    }

    public void Deactivate(string userId) { IsActive = false; SetAuditOnUpdate(userId); }
    public void Activate(string userId) { IsActive = true; SetAuditOnUpdate(userId); }
    public void ChangePassword(string newPasswordHash, string userId)
    {
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        SetAuditOnUpdate(userId);
    }
}
```

### Entidade RefreshToken (conforme techspec)

```csharp
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    protected RefreshToken() { } // EF Core

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}
```

### Esquema SQL esperado

```sql
CREATE TABLE users (
    id              UUID PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    email           VARCHAR(255) NOT NULL,
    password_hash   VARCHAR(500) NOT NULL,
    role            INTEGER NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    must_change_password BOOLEAN NOT NULL DEFAULT TRUE,
    created_by      VARCHAR(100) NOT NULL,
    created_at      TIMESTAMP NOT NULL,
    updated_by      VARCHAR(100),
    updated_at      TIMESTAMP
);
CREATE UNIQUE INDEX ix_users_email ON users (email);

CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id),
    token           VARCHAR(500) NOT NULL,
    expires_at      TIMESTAMP NOT NULL,
    is_revoked      BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at      TIMESTAMP,
    created_by      VARCHAR(100) NOT NULL,
    created_at      TIMESTAMP NOT NULL,
    updated_by      VARCHAR(100),
    updated_at      TIMESTAMP
);
CREATE UNIQUE INDEX ix_refresh_tokens_token ON refresh_tokens (token);
CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
```

## Critérios de Sucesso

- Entidades `User` e `RefreshToken` compilam e seguem o padrão `BaseEntity` existente
- Enum `UserRole` com valores corretos
- Exceções de auth criadas e herdando de `DomainException`
- Interfaces de repositório definidas no Domain sem dependências externas
- Configurações EF Core geram o schema SQL esperado
- Repositórios implementados e registrados na DI
- Migration criada com sucesso (aditiva)
- Testes unitários passam para ambas as entidades
- Build compila sem erros
```
