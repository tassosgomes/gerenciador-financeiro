import { useState } from 'react';
import { Users } from 'lucide-react';

import type { UserResponse } from '@/features/admin/types/admin';
import { Badge, Button, EmptyState, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui';
import { ConfirmationModal } from '@/shared/components/ui/ConfirmationModal';
import { useToggleUserStatus } from '@/features/admin/hooks/useUsers';
import { formatDate } from '@/shared/utils/formatters';
import { useAuthStore } from '@/features/auth/store/authStore';

interface UserTableProps {
  users: UserResponse[];
}

export function UserTable({ users }: UserTableProps): JSX.Element {
  const [confirmToggleModal, setConfirmToggleModal] = useState<{
    userId: string;
    userName: string;
    isActive: boolean;
  } | null>(null);

  const toggleUserStatus = useToggleUserStatus();
  const currentUser = useAuthStore((state) => state.user);

  const handleToggleClick = (user: UserResponse) => {
    setConfirmToggleModal({
      userId: user.id,
      userName: user.name,
      isActive: !user.isActive,
    });
  };

  const handleConfirmToggle = () => {
    if (confirmToggleModal) {
      toggleUserStatus.mutate(
        {
          id: confirmToggleModal.userId,
          isActive: confirmToggleModal.isActive,
        },
        {
          onSuccess: () => {
            setConfirmToggleModal(null);
          },
        }
      );
    }
  };

  if (users.length === 0) {
    return (
      <EmptyState
        icon={Users}
        title="Nenhum usuário encontrado"
        description="Crie o primeiro usuário para começar a gerenciar acessos"
      />
    );
  }

  return (
    <>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Nome</TableHead>
            <TableHead>E-mail</TableHead>
            <TableHead>Papel</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Criado em</TableHead>
            <TableHead className="w-24">Ações</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell className="font-medium">{user.name}</TableCell>
              <TableCell>{user.email}</TableCell>
              <TableCell>
                {user.role === 'Admin' ? (
                  <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100">
                    Admin
                  </Badge>
                ) : (
                  <Badge className="bg-gray-100 text-gray-800 hover:bg-gray-100">
                    Membro
                  </Badge>
                )}
              </TableCell>
              <TableCell>
                {user.isActive ? (
                  <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
                    Ativo
                  </Badge>
                ) : (
                  <Badge className="bg-red-100 text-red-800 hover:bg-red-100">
                    Inativo
                  </Badge>
                )}
              </TableCell>
              <TableCell>{formatDate(user.createdAt)}</TableCell>
              <TableCell>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleClick(user)}
                  disabled={toggleUserStatus.isPending || user.id === currentUser?.id}
                  title={user.id === currentUser?.id ? 'Não é possível alterar o status do próprio usuário' : undefined}
                >
                  <span className="material-icons text-base">
                    {user.isActive ? 'block' : 'check_circle'}
                  </span>
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {confirmToggleModal && (
        <ConfirmationModal
          open={!!confirmToggleModal}
          onOpenChange={(open) => !open && setConfirmToggleModal(null)}
          onConfirm={handleConfirmToggle}
          onCancel={() => setConfirmToggleModal(null)}
          title={`${confirmToggleModal.isActive ? 'Ativar' : 'Inativar'} Usuário`}
          message={`Tem certeza que deseja ${confirmToggleModal.isActive ? 'ativar' : 'inativar'} o usuário "${confirmToggleModal.userName}"?`}
          confirmLabel={confirmToggleModal.isActive ? 'Ativar' : 'Inativar'}
          variant={confirmToggleModal.isActive ? 'warning' : 'danger'}
        />
      )}
    </>
  );
}
