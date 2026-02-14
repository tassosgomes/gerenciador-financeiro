```markdown
---
status: pending
parallelizable: true
blocked_by: ["3.0"]
---

<task_context>
<domain>engine/aplicação+serviços</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>"9.0"</unblocks>
</task_context>

# Tarefa 8.0: Backup Manual (Export/Import)

## Visão Geral

Implementar o sistema de backup manual via JSON: endpoint de exportação (`GET /api/v1/backup/export`) que serializa todos os dados do sistema em um arquivo JSON, e endpoint de importação (`POST /api/v1/backup/import`) que faz substituição completa dos dados a partir de um JSON validado. Ambos restritos ao admin.

O import é destrutivo (truncate + insert) e transacional — qualquer erro reverte toda a operação. A validação pré-import garante integridade referencial antes de aplicar as mudanças.

## Requisitos

- Techspec F6: Endpoints de backup (export e import)
- PRD F6 req 34-38: Export JSON, import validado e transacional
- Techspec: Export inclui users (sem password_hash), accounts, categories, transactions, recurrenceTemplates
- Techspec: Import com substituição completa (truncate + insert em transação)
- Techspec: Ordem de insert: users → accounts → categories → recurrenceTemplates → transactions

## Subtarefas

### DTOs de Backup

- [ ] 8.1 Criar DTOs em `2-Application/Dtos/Backup/`:
  - `BackupExportDto`:
    ```csharp
    public record BackupExportDto(
        DateTime ExportedAt,
        string Version,
        BackupDataDto Data
    );
    ```
  - `BackupDataDto`:
    ```csharp
    public record BackupDataDto(
        IEnumerable<UserBackupDto> Users,
        IEnumerable<AccountBackupDto> Accounts,
        IEnumerable<CategoryBackupDto> Categories,
        IEnumerable<TransactionBackupDto> Transactions,
        IEnumerable<RecurrenceTemplateBackupDto> RecurrenceTemplates
    );
    ```
  - DTOs individuais: `UserBackupDto` (sem PasswordHash), `AccountBackupDto`, `CategoryBackupDto`, `TransactionBackupDto`, `RecurrenceTemplateBackupDto`
  - Cada DTO deve conter todos os campos necessários para reconstituir a entidade (incluindo Ids)

### Query de Exportação

- [ ] 8.2 Criar `ExportBackupQuery` + `ExportBackupQueryHandler`:
  - Sem parâmetros de input
  - Buscar todas as entidades com `AsNoTracking()` para performance
  - Mapear para DTOs de backup (excluindo `PasswordHash` dos usuários)
  - Retornar `BackupExportDto` com timestamp e version "1.0"
  - Ordem de serialização: users → accounts → categories → transactions → recurrenceTemplates

### Command de Importação

- [ ] 8.3 Criar `ImportBackupCommand` + `ImportBackupCommandHandler`:
  - Input: `BackupDataDto` (dados desserializados do JSON)
  - Fluxo:
    1. Validar formato e campos obrigatórios
    2. Validar integridade referencial (accountIds em transactions existem na lista de accounts, etc.)
    3. Abrir transação explícita
    4. Truncar todas as tabelas na ordem inversa de FK: audit_logs → refresh_tokens → transactions → recurrence_templates → categories → accounts → users
    5. Inserir dados na ordem correta: users → accounts → categories → recurrenceTemplates → transactions
    6. Commit
  - Em caso de erro: rollback automático
  - **Importante**: Usuários importados SEM PasswordHash — marcar `MustChangePassword = true` e gerar hash temporário, OU exigir que import inclua PasswordHash (decisão de segurança)
- [ ] 8.4 Criar `ImportBackupValidator`:
  - Validar que o JSON contém todas as seções obrigatórias
  - Validar que as referências entre entidades são consistentes:
    - Transactions → Accounts existentes no backup
    - Transactions → Categories existentes no backup
    - RecurrenceTemplates → Accounts e Categories existentes no backup
  - Validar formatos de dados (GUIDs, datas, enums)

### Validação de Integridade Referencial

- [ ] 8.5 Criar serviço `BackupIntegrityValidator` em `2-Application/Common/`:
  - Receber `BackupDataDto`
  - Verificar que todos os `AccountId` referenciados em transactions existem em accounts
  - Verificar que todos os `CategoryId` referenciados em transactions existem em categories
  - Verificar que `OriginalTransactionId` (quando preenchido) referencia transaction válida
  - Verificar que `SourceAccountId`/`DestinationAccountId` de transferências existem
  - Retornar lista de erros de integridade (ou vazio se OK)

### Controller de Backup

- [ ] 8.6 Criar `BackupController` em `1-Services/Controllers/BackupController.cs`:
  - Route: `api/v1/backup`
  - Atributo `[Authorize(Policy = "AdminOnly")]`
  - Injetar `IDispatcher`
- [ ] 8.7 Endpoint `GET /api/v1/backup/export`:
  - Despachar `ExportBackupQuery`
  - Retornar 200 com `BackupExportDto` como JSON
  - Header `Content-Disposition: attachment; filename="backup_{timestamp}.json"`
  - Configurar timeout estendido (endpoint potencialmente lento)
- [ ] 8.8 Endpoint `POST /api/v1/backup/import`:
  - Request body: `BackupDataDto` (JSON)
  - Despachar `ImportBackupCommand`
  - Retornar 200 com resumo do import (contagem de entidades importadas)
  - Em caso de erro de validação: retornar 400 com detalhes dos erros
  - Configurar timeout estendido (5 min)

### Mapeamento Mapster

- [ ] 8.9 Configurar mapeamentos Mapster para backup:
  - Entity → BackupDto (export)
  - BackupDto → Entity (import — reconstrução)
  - User → UserBackupDto (excluir PasswordHash)

### Testes Unitários

- [ ] 8.10 Testes para `ExportBackupQueryHandler`:
  - Export com dados de exemplo serializa corretamente
  - Export exclui PasswordHash dos usuários
  - Export inclui todas as entidades
  - Export com banco vazio retorna listas vazias
- [ ] 8.11 Testes para `ImportBackupCommandHandler`:
  - Import com dados válidos insere todas as entidades
  - Import com referência inválida (accountId inexistente) falha com erro claro
  - Import é transacional — falha parcial reverte tudo
- [ ] 8.12 Testes para `BackupIntegrityValidator`:
  - Dados válidos → sem erros
  - Transaction referenciando Account inexistente → erro
  - Transaction referenciando Category inexistente → erro
  - Dados duplicados (IDs repetidos) → erro
- [ ] 8.13 Testes para `BackupController`:
  - Export retorna 200 com Content-Disposition
  - Import com dados válidos retorna 200
  - Import com dados inválidos retorna 400

### Validação

- [ ] 8.14 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 3.0 (Pipeline HTTP, DI, JWT config)
- Desbloqueia: 9.0 (Testes de Integração)
- Paralelizável: Sim (pode ser executada em paralelo com 4.0, 5.0, 6.0 — isolada)

## Detalhes de Implementação

### Formato do Export

```json
{
  "exportedAt": "2026-02-14T10:00:00Z",
  "version": "1.0",
  "data": {
    "users": [
      {
        "id": "550e8400-...",
        "name": "Admin",
        "email": "admin@familia.com",
        "role": "Admin",
        "isActive": true,
        "mustChangePassword": false,
        "createdBy": "system",
        "createdAt": "2026-01-01T00:00:00Z"
      }
    ],
    "accounts": [ ... ],
    "categories": [ ... ],
    "transactions": [ ... ],
    "recurrenceTemplates": [ ... ]
  }
}
```

### Fluxo de Import (pseudocódigo)

```
1. Deserializar JSON → BackupDataDto
2. Validar integridade referencial
3. Se erros → retornar 400 com lista de erros
4. Abrir transação (IUnitOfWork.BeginTransactionAsync)
5. Truncar na ordem: audit_logs, refresh_tokens, transactions,
   recurrence_templates, categories, accounts, users
6. Inserir na ordem:
   a. Users (com MustChangePassword = true, sem password_hash)
   b. Accounts
   c. Categories
   d. RecurrenceTemplates
   e. Transactions
7. Commit
8. Retornar resumo { users: N, accounts: N, ... }
```

### Considerações de Performance

- Export: usar `AsNoTracking()` em todas as queries
- Import: usar `AddRange` + `SaveChanges` em batches se volume for grande
- Timeout do endpoint de import: 5 minutos (configurável)
- Para volume familiar, `AddRange` é suficiente (sem necessidade de BulkInsert)

### Decisão sobre PasswordHash no Import

O export NÃO inclui password_hash (segurança). No import:
- Usuários são importados com `MustChangePassword = true`
- Admin deve redefinir senhas após import
- Alternativa: permitir que o import inclua um campo `tempPassword` que será hasheado

## Critérios de Sucesso

- Export gera JSON completo com todos os dados (exceto senhas)
- Import substitui completamente os dados do banco
- Import é transacional (falha reverte tudo)
- Validação de integridade referencial antes do import
- Apenas admin pode acessar ambos os endpoints
- Timeout estendido para operações potencialmente lentas
- Testes unitários para export, import e validação passam
- Build compila sem erros
```
