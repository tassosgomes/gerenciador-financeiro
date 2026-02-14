```markdown
---
status: pending
parallelizable: false
blocked_by: ["2.0", "3.0"]
---

<task_context>
<domain>engine/serviços</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"7.0", "9.0"</unblocks>
</task_context>

# Tarefa 4.0: Controllers de Auth e Users

## Visão Geral

Implementar os controllers REST para autenticação (`AuthController`) e gestão de usuários (`UsersController`). Estes são os primeiros endpoints testáveis ponta-a-ponta — delegam para os command/query handlers criados na tarefa 2.0 via `IDispatcher`. Incluem atributos de autorização, validação de entrada e documentação Swagger.

## Requisitos

- Techspec: Controllers MVC com `[ApiController]`, atributos de autorização declarativos
- Techspec F1: Endpoints de autenticação e gestão de usuários
- PRD F1 req 1-9: Login, refresh, logout, change password, CRUD de usuários
- `rules/restful.md`: Recursos em inglês/plural, kebab-case, versionamento `/api/v1/`
- `rules/dotnet-coding-standards.md`: Métodos ≤ 50 linhas, PascalCase

## Subtarefas

### AuthController

- [ ] 4.1 Criar `AuthController` em `1-Services/Controllers/AuthController.cs`:
  - Route: `api/v1/auth`
  - Atributo `[ApiController]`
  - Injetar `IDispatcher`
- [ ] 4.2 Endpoint `POST /api/v1/auth/login` (público):
  - Request body: `{ "email": "...", "password": "..." }`
  - Despachar `LoginCommand`
  - Retornar 200 com `LoginResponse`
  - Atributo `[AllowAnonymous]`
- [ ] 4.3 Endpoint `POST /api/v1/auth/refresh` (público):
  - Request body: `{ "refreshToken": "..." }`
  - Despachar `RefreshTokenCommand`
  - Retornar 200 com `TokenRefreshResponse`
  - Atributo `[AllowAnonymous]`
- [ ] 4.4 Endpoint `POST /api/v1/auth/logout` (autenticado):
  - Extrair `userId` do JWT claims
  - Despachar `LogoutCommand`
  - Retornar 204 No Content
  - Atributo `[Authorize]`
- [ ] 4.5 Endpoint `POST /api/v1/auth/change-password` (autenticado):
  - Request body: `{ "currentPassword": "...", "newPassword": "..." }`
  - Extrair `userId` do JWT claims
  - Despachar `ChangePasswordCommand`
  - Retornar 204 No Content
  - Atributo `[Authorize]`

### UsersController

- [ ] 4.6 Criar `UsersController` em `1-Services/Controllers/UsersController.cs`:
  - Route: `api/v1/users`
  - Atributos `[ApiController]`, `[Authorize(Policy = "AdminOnly")]`
  - Injetar `IDispatcher`
- [ ] 4.7 Endpoint `POST /api/v1/users` (admin):
  - Request body: `{ "name": "...", "email": "...", "password": "...", "role": "Admin|Member" }`
  - Extrair `userId` do JWT claims (criador)
  - Despachar `CreateUserCommand`
  - Retornar 201 Created com `UserDto` e header `Location`
- [ ] 4.8 Endpoint `GET /api/v1/users` (admin):
  - Despachar `ListUsersQuery`
  - Retornar 200 com lista de `UserDto`
- [ ] 4.9 Endpoint `GET /api/v1/users/{id}` (admin):
  - Despachar `GetUserByIdQuery`
  - Retornar 200 com `UserDto`
- [ ] 4.10 Endpoint `PATCH /api/v1/users/{id}/status` (admin):
  - Request body: `{ "isActive": true|false }`
  - Extrair `userId` do JWT claims (executor)
  - Despachar `UpdateUserStatusCommand`
  - Retornar 204 No Content

### Request DTOs

- [ ] 4.11 Criar request DTOs em `1-Services/Controllers/` ou `2-Application/Dtos/`:
  - `LoginRequest` (Email, Password)
  - `RefreshTokenRequest` (RefreshToken)
  - `ChangePasswordRequest` (CurrentPassword, NewPassword)
  - `CreateUserRequest` (Name, Email, Password, Role)
  - `UpdateUserStatusRequest` (IsActive)

### Helper para extrair userId do JWT

- [ ] 4.12 Criar extension method ou helper para extrair `userId` dos claims do JWT:
  - `HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
  - Pode ser método de extensão em `ControllerBase` ou helper estático

### Testes Unitários

- [ ] 4.13 Testes para `AuthController`:
  - Login com sucesso retorna 200 + tokens
  - Refresh com sucesso retorna 200 + novos tokens
  - Logout retorna 204
  - Change password retorna 204
  - (Erros são testados nos testes de integração)
- [ ] 4.14 Testes para `UsersController`:
  - Create user retorna 201 com Location header
  - List users retorna 200 com lista
  - Get user retorna 200 com UserDto
  - Update status retorna 204

### Validação

- [ ] 4.15 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 2.0 (handlers de auth), 3.0 (pipeline HTTP, DI, JWT config)
- Desbloqueia: 7.0 (Auditoria), 9.0 (Testes de Integração)
- Paralelizável: Não (depende de 2.0 e 3.0 simultaneamente)

## Detalhes de Implementação

### AuthController (exemplo)

```csharp
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AuthController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _dispatcher.SendAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _dispatcher.SendAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new LogoutCommand(userId);
        await _dispatcher.SendAsync(command, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        await _dispatcher.SendAsync(command, ct);
        return NoContent();
    }
}
```

### UsersController (exemplo)

```csharp
[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new CreateUserCommand(request.Name, request.Email,
            request.Password, request.Role, userId);
        var result = await _dispatcher.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new UpdateUserStatusCommand(id, request.IsActive, userId);
        await _dispatcher.SendAsync(command, ct);
        return NoContent();
    }
}
```

### Exemplo de request/response

```
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "admin@familia.com", "password": "SenhaSegura123!" }

→ 200 OK
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "dGVzdC1y...",
  "expiresIn": 86400,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Admin",
    "email": "admin@familia.com",
    "role": "Admin"
  }
}
```

## Critérios de Sucesso

- Endpoints de login, refresh, logout e change-password funcionais
- CRUD de usuários acessível apenas por admin
- Atributos de autorização corretos (`[AllowAnonymous]`, `[Authorize]`, `[Authorize(Policy = "AdminOnly")]`)
- `userId` extraído corretamente do JWT em todas as operações
- Respostas com status codes corretos (200, 201, 204)
- Request DTOs bem definidos
- Testes unitários de controllers passam
- Build compila sem erros
```
