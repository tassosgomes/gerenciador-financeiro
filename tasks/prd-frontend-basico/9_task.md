---
status: pending
parallelizable: true
blocked_by: ["3.0"]
---

<task_context>
<domain>frontend/admin</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>tanstack-query, react-hook-form, zod</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 9.0: Painel Administrativo (Usuários e Backup)

## Visão Geral

Implementar o painel administrativo com duas funcionalidades: gestão de usuários (listagem, criação, toggle de status) e backup (exportar/importar JSON). O painel é acessível apenas para usuários com role Admin, protegido por guard de rota. O layout deve seguir o mockup `screen-examples/administrativo/index.html`.

## Requisitos

### Gestão de Usuários (PRD F6)
- PRD req. 39: Tela acessível apenas pelo admin
- PRD req. 40: Listagem com nome, e-mail, papel, status (ativo/inativo)
- PRD req. 41: Formulário de criação: nome, e-mail, senha temporária, papel (admin/membro)
- PRD req. 42: Botão para desativar/reativar usuário

### Backup (PRD F7)
- PRD req. 43: Botão "Exportar Backup" que faz download do JSON completo
- PRD req. 44: Botão "Importar Backup" com upload de arquivo JSON e confirmação
- PRD req. 45: Mensagem de sucesso ou erro após operação
- PRD req. 46: Aviso claro de que o import substitui dados existentes

## Subtarefas

### Guard de Rota Admin

- [ ] 9.1 Criar `src/shared/components/layout/AdminRoute.tsx` — wrapper que verifica `user.role === 'Admin'` do authStore; se não admin, exibe mensagem de acesso negado ou redirect para `/dashboard`

### Tipos e API

- [ ] 9.2 Criar `src/features/admin/types/admin.ts` — interfaces: `UserResponse` (id, name, email, role, isActive, createdAt), `CreateUserRequest` (name, email, password, role), `RoleType` (Admin, Member)
- [ ] 9.3 Criar `src/features/admin/api/usersApi.ts` — funções: `getUsers()`, `createUser(data)`, `toggleUserStatus(id, isActive)` usando apiClient
- [ ] 9.4 Criar `src/features/admin/api/backupApi.ts` — funções: `exportBackup()` (retorna Blob/download), `importBackup(file: File)` usando apiClient com timeout estendido (120s)

### Hooks

- [ ] 9.5 Criar `src/features/admin/hooks/useUsers.ts` — hooks: `useUsers()`, `useCreateUser()`, `useToggleUserStatus()` com mutations e invalidação de cache
- [ ] 9.6 Criar `src/features/admin/hooks/useBackup.ts` — hooks: `useExportBackup()` (mutation que dispara download), `useImportBackup()` (mutation com upload)

### Componentes de Usuários

- [ ] 9.7 Criar `src/features/admin/components/UserTable.tsx` — tabela (Shadcn Table) com colunas: Nome, E-mail, Papel (badge Admin/Membro), Status (badge Ativo/Inativo), Ações (toggle status). Usar cores: Admin = badge azul, Membro = badge cinza; Ativo = verde, Inativo = vermelho
- [ ] 9.8 Criar schema Zod: `createUserSchema` — nome (obrigatório, min 2), email (formato válido), password (min 8 chars, 1 maiúscula, 1 número), role (obrigatório)
- [ ] 9.9 Criar `src/features/admin/components/UserForm.tsx` — modal com formulário: campos nome, e-mail, senha temporária (com indicador de força), papel (Select: Admin/Membro). Validação inline
- [ ] 9.10 Implementar toggle de status de usuário com ConfirmationModal — mensagem de confirmação antes de desativar/reativar

### Componentes de Backup

- [ ] 9.11 Criar `src/features/admin/components/BackupExport.tsx` — card com botão "Exportar Backup", descrição do que será exportado, ícone `download`. Ao clicar, dispara download do JSON via `window.location.href` ou Blob URL
- [ ] 9.12 Criar `src/features/admin/components/BackupImport.tsx` — card com área de upload (drag & drop ou botão), preview do arquivo selecionado (nome, tamanho), aviso em destaque: "⚠️ Atenção: A importação substituirá TODOS os dados existentes. Esta ação é irreversível.", botão "Importar" que abre ConfirmationModal antes de executar, loading state durante upload e processamento

### Página e Rotas

- [ ] 9.13 Criar `src/features/admin/pages/AdminPage.tsx` — layout com tabs ou seções: "Usuários" e "Backup". Seção Usuários: header + botão "Novo Usuário" + UserTable. Seção Backup: BackupExport + BackupImport lado a lado
- [ ] 9.14 Criar `src/features/admin/index.ts` — barrel export
- [ ] 9.15 Atualizar rotas: `/admin` → AdminRoute → AdminPage

### Testes

- [ ] 9.16 Criar MSW handlers: mock de GET/POST/PATCH `/api/v1/users`, GET `/api/v1/backup/export`, POST `/api/v1/backup/import`
- [ ] 9.17 Testes unitários: UserTable (renderização, toggle), UserForm (validação, submit), BackupImport (upload, confirmação), AdminRoute (acesso admin vs não-admin)
- [ ] 9.18 Teste de integração: fluxo criar usuário → aparece na lista; toggle status → badge atualizado; export backup → download iniciado

## Sequenciamento

- Bloqueado por: 3.0 (Auth — role guard depende do auth store)
- Desbloqueia: 10.0 (Polimento)
- Paralelizável: Sim, com 5.0 (Dashboard), 6.0 (Contas), 7.0 (Categorias)

## Detalhes de Implementação

### AdminRoute Guard

```typescript
function AdminRoute() {
  const { user } = useAuthStore();

  if (!user || user.role !== 'Admin') {
    return (
      <div className="flex items-center justify-center h-full">
        <Card className="p-8 text-center">
          <span className="material-icons text-danger text-4xl mb-4">block</span>
          <h2 className="text-xl font-bold mb-2">Acesso Restrito</h2>
          <p className="text-slate-500">
            Apenas administradores podem acessar esta área.
          </p>
          <Link to="/dashboard" className="text-primary mt-4 inline-block">
            Voltar ao Dashboard
          </Link>
        </Card>
      </div>
    );
  }

  return <Outlet />;
}
```

### Backup Export — Download via Blob

```typescript
async function exportBackup(): Promise<void> {
  const response = await apiClient.get('/api/v1/backup/export', {
    responseType: 'blob',
  });

  const url = window.URL.createObjectURL(new Blob([response.data]));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', `backup-${format(new Date(), 'yyyy-MM-dd')}.json`);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
```

### Backup Import — Upload com FormData

```typescript
async function importBackup(file: File): Promise<void> {
  const formData = new FormData();
  formData.append('file', file);

  await apiClient.post('/api/v1/backup/import', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 120000, // 2 minutos para imports grandes
  });
}
```

### Referência Visual — AdminPage (mockup `administrativo/`)

```
┌──────────────────────────────────────────────────────────┐
│ Painel Administrativo                                    │
│ Gerenciamento do sistema e configurações                 │
├──────────────────────────────────────────────────────────┤
│ [👥 Usuários]  [💾 Backup]                               │
├──────────────────────────────────────────────────────────┤
│ Gestão de Usuários                    [+ Novo Usuário]   │
│ ┌──────────────────────────────────────────────────────┐│
│ │ Nome          E-mail            Papel    Status Ações││
│ │ Carlos Silva  carlos@email.com  Admin    🟢Ativo  ⏻ ││
│ │ Maria Santos  maria@email.com   Membro   🟢Ativo  ⏻ ││
│ │ João Lima     joao@email.com    Membro   🔴Inativo ⏻ ││
│ └──────────────────────────────────────────────────────┘│
├──────────────────────────────────────────────────────────┤
│ Backup & Restauração                                     │
│ ┌────────────────────┐  ┌──────────────────────────────┐│
│ │ 📥 Exportar Backup │  │ 📤 Importar Backup          ││
│ │ Download JSON      │  │ Upload JSON + confirmação    ││
│ │ [Exportar Agora]   │  │ ⚠️ Substitui todos os dados  ││
│ │                    │  │ [Selecionar Arquivo]         ││
│ └────────────────────┘  └──────────────────────────────┘│
└──────────────────────────────────────────────────────────┘
```

## Critérios de Sucesso

- Rota `/admin` acessível apenas para usuários com role Admin
- Usuário não-admin vê mensagem de acesso restrito ao tentar acessar `/admin`
- Item "Admin" na sidebar visível apenas para admins
- Listagem de usuários exibe todos os campos com badges coloridos
- Criação de usuário: formulário validado, toast de sucesso, lista atualizada
- Toggle status: confirmação, toast de feedback, badge atualizado
- Export backup: download do arquivo JSON inicia corretamente
- Import backup: upload funciona, confirmação exibida, aviso de substituição claro
- Import com erro: mensagem de erro exibida
- Testes unitários e de integração passam
