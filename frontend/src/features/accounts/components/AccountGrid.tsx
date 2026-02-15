import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountCard } from '@/features/accounts/components/AccountCard';

interface AccountGridProps {
  accounts: AccountResponse[];
  onEdit: (account: AccountResponse) => void;
  onToggleStatus: (id: string, isActive: boolean) => void;
}

export function AccountGrid({ accounts, onEdit, onToggleStatus }: AccountGridProps): JSX.Element {
  if (accounts.length === 0) {
    return (
      <div className="flex items-center justify-center py-16">
        <div className="text-center">
          <span className="material-icons text-6xl text-slate-300 mb-4">account_balance_wallet</span>
          <p className="text-slate-600 text-lg font-medium">Nenhuma conta encontrada</p>
          <p className="text-slate-500 text-sm">Adicione sua primeira conta para começar</p>
        </div>
      </div>
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
