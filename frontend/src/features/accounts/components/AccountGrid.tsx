import { Wallet } from 'lucide-react';

import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountCard } from '@/features/accounts/components/AccountCard';
import { EmptyState } from '@/shared/components/ui';

interface AccountGridProps {
  accounts: AccountResponse[];
  onEdit: (account: AccountResponse) => void;
  onToggleStatus: (id: string, isActive: boolean) => void;
}

export function AccountGrid({ accounts, onEdit, onToggleStatus }: AccountGridProps): JSX.Element {
  if (accounts.length === 0) {
    return (
      <EmptyState
        icon={Wallet}
        title="Nenhuma conta encontrada"
        description="Adicione sua primeira conta para começar a gerenciar suas finanças"
      />
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3 2xl:grid-cols-4 gap-4">
      {accounts.map((account) => (
        <AccountCard
          key={account.id}
          account={account}
          onEdit={onEdit}
          onToggleStatus={onToggleStatus}
        />
      ))}
    </div>
  );
}
