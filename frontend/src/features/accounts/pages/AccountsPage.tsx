import { useState, useMemo } from 'react';
import { Plus } from 'lucide-react';

import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import { useAccounts, useToggleAccountStatus } from '@/features/accounts/hooks/useAccounts';
import { AccountGrid } from '@/features/accounts/components/AccountGrid';
import { AccountForm } from '@/features/accounts/components/AccountForm';
import { AccountSummaryFooter } from '@/features/accounts/components/AccountSummaryFooter';
import { Button, Card, CardContent, Skeleton, Tabs, TabsList, TabsTrigger } from '@/shared/components/ui';
import { ConfirmationModal } from '@/shared/components/ui/ConfirmationModal';

type FilterType = 'all' | 'banking' | 'cards';

export default function AccountsPage(): JSX.Element {
  const [filterType, setFilterType] = useState<FilterType>('all');
  const [formOpen, setFormOpen] = useState(false);
  const [selectedAccount, setSelectedAccount] = useState<AccountResponse | null>(null);
  const [confirmToggleOpen, setConfirmToggleOpen] = useState(false);
  const [pendingToggle, setPendingToggle] = useState<{ id: string; isActive: boolean } | null>(null);

  const { data: accounts = [], isLoading } = useAccounts();
  const toggleMutation = useToggleAccountStatus();

  const filteredAccounts = useMemo(() => {
    if (filterType === 'all') return accounts;
    if (filterType === 'banking') {
      return accounts.filter(
        (account) =>
          account.type === AccountType.Corrente ||
          account.type === AccountType.Investimento ||
          account.type === AccountType.Carteira
      );
    }
    if (filterType === 'cards') {
      return accounts.filter((account) => account.type === AccountType.Cartao);
    }
    return accounts;
  }, [accounts, filterType]);

  function handleAddAccount(): void {
    setSelectedAccount(null);
    setFormOpen(true);
  }

  function handleEditAccount(account: AccountResponse): void {
    setSelectedAccount(account);
    setFormOpen(true);
  }

  function handleToggleStatusRequest(id: string, isActive: boolean): void {
    setPendingToggle({ id, isActive });
    setConfirmToggleOpen(true);
  }

  async function handleConfirmToggle(): Promise<void> {
    if (!pendingToggle) return;

    await toggleMutation.mutateAsync(pendingToggle);
    setConfirmToggleOpen(false);
    setPendingToggle(null);
  }

  function handleCancelToggle(): void {
    setConfirmToggleOpen(false);
    setPendingToggle(null);
  }

  const toggleMessage = pendingToggle?.isActive
    ? 'Tem certeza que deseja ativar esta conta? Ela ficará disponível para transações.'
    : 'Tem certeza que deseja inativar esta conta? Ela não ficará disponível para novas transações.';

  return (
    <div className="space-y-6 pb-24">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-slate-900">Minhas Contas</h1>
          <p className="text-slate-600 mt-1">Gerencie suas contas bancárias, cartões e investimentos</p>
        </div>
        <Button onClick={handleAddAccount}>
          <Plus className="mr-2 h-4 w-4" />
          Adicionar Conta
        </Button>
      </div>

      {/* Filtros */}
      <Card>
        <CardContent className="pt-6">
          <Tabs value={filterType} onValueChange={(value) => setFilterType(value as FilterType)}>
            <TabsList>
              <TabsTrigger value="all">Todas</TabsTrigger>
              <TabsTrigger value="banking">Bancárias</TabsTrigger>
              <TabsTrigger value="cards">Cartões</TabsTrigger>
            </TabsList>
          </Tabs>
        </CardContent>
      </Card>

      {/* Grid de contas */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3 2xl:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-4 space-y-4">
                <Skeleton className="h-6 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-8 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <AccountGrid
          accounts={filteredAccounts}
          onEdit={handleEditAccount}
          onToggleStatus={handleToggleStatusRequest}
        />
      )}

      {/* Footer com resumo */}
      {!isLoading && accounts.length > 0 && <AccountSummaryFooter accounts={accounts} />}

      {/* Modal de criação/edição */}
      <AccountForm open={formOpen} onOpenChange={setFormOpen} account={selectedAccount} />

      {/* Modal de confirmação de toggle */}
      <ConfirmationModal
        open={confirmToggleOpen}
        title="Confirmar Alteração"
        message={toggleMessage}
        onConfirm={handleConfirmToggle}
        onCancel={handleCancelToggle}
        onOpenChange={setConfirmToggleOpen}
        confirmLabel={pendingToggle?.isActive ? 'Ativar' : 'Inativar'}
        variant="warning"
      />
    </div>
  );
}
