import { useState } from 'react';
import { Users, Database, Settings } from 'lucide-react';

import { useUsers } from '@/features/admin/hooks/useUsers';
import { UserTable } from '@/features/admin/components/UserTable';
import { UserForm } from '@/features/admin/components/UserForm';
import { BackupExport } from '@/features/admin/components/BackupExport';
import { BackupImport } from '@/features/admin/components/BackupImport';
import { ResetSystem } from '@/features/admin/components/ResetSystem';
import { Button, Card, Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui';

export function AdminPage(): JSX.Element {
  const [isUserFormOpen, setIsUserFormOpen] = useState(false);
  const { data: users = [], isLoading } = useUsers();

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="h-10 w-10 animate-spin rounded-full border-4 border-primary/20 border-t-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Painel Administrativo</h1>
        <p className="text-slate-500">Gerenciamento do sistema e configurações</p>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="users" className="w-full">
        <TabsList>
          <TabsTrigger value="users" className="flex items-center gap-2">
            <Users className="h-4 w-4" />
            Usuários
          </TabsTrigger>
          <TabsTrigger value="backup" className="flex items-center gap-2">
            <Database className="h-4 w-4" />
            Backup
          </TabsTrigger>
          <TabsTrigger value="system" className="flex items-center gap-2">
            <Settings className="h-4 w-4" />
            Sistema
          </TabsTrigger>
        </TabsList>

        {/* Users Tab */}
        <TabsContent value="users" className="space-y-4">
          <Card>
            <div className="flex items-center justify-between border-b p-6">
              <div>
                <h2 className="text-xl font-semibold">Gestão de Usuários</h2>
                <p className="text-sm text-slate-500">
                  Gerencie os usuários que têm acesso ao sistema
                </p>
              </div>
              <Button onClick={() => setIsUserFormOpen(true)}>
                <span className="material-icons mr-2 text-base">add</span>
                Novo Usuário
              </Button>
            </div>
            <div className="p-6 pt-0">
              <UserTable users={users} />
            </div>
          </Card>
        </TabsContent>

        {/* Backup Tab */}
        <TabsContent value="backup" className="space-y-4">
          <div>
            <h2 className="text-xl font-semibold mb-2">Backup & Restauração</h2>
            <p className="text-sm text-slate-500">
              Exporte e importe os dados do sistema
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-2">
            <BackupExport />
            <BackupImport />
          </div>
        </TabsContent>

        {/* System Tab */}
        <TabsContent value="system" className="space-y-4">
          <div>
            <h2 className="text-xl font-semibold mb-2">Configurações do Sistema</h2>
            <p className="text-sm text-slate-500">
              Gerenciamento avançado e manutenção do sistema
            </p>
          </div>

          <div className="max-w-2xl">
            <ResetSystem />
          </div>
        </TabsContent>
      </Tabs>

      {/* User Form Modal */}
      <UserForm open={isUserFormOpen} onOpenChange={setIsUserFormOpen} />
    </div>
  );
}

export default AdminPage;
