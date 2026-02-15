import { Edit } from 'lucide-react';

import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import { Badge, Button, Card, CardContent, Switch } from '@/shared/components/ui';
import { ACCOUNT_TYPE_ICONS, ACCOUNT_TYPE_LABELS } from '@/shared/utils/constants';
import { formatCurrency } from '@/shared/utils/formatters';
import { cn } from '@/shared/utils';

interface AccountCardProps {
  account: AccountResponse;
  onEdit: (account: AccountResponse) => void;
  onToggleStatus: (id: string, isActive: boolean) => void;
}

const ACCOUNT_COLORS: Record<AccountType, { bar: string; icon: string }> = {
  [AccountType.Corrente]: { bar: 'bg-primary', icon: 'bg-primary/10 text-primary' },
  [AccountType.Cartao]: { bar: 'bg-purple-500', icon: 'bg-purple-500/10 text-purple-600' },
  [AccountType.Investimento]: { bar: 'bg-success', icon: 'bg-success/10 text-success' },
  [AccountType.Carteira]: { bar: 'bg-warning', icon: 'bg-warning/10 text-warning' },
};

export function AccountCard({ account, onEdit, onToggleStatus }: AccountCardProps): JSX.Element {
  const colors = ACCOUNT_COLORS[account.type];
  const icon = ACCOUNT_TYPE_ICONS[account.type];
  const label = ACCOUNT_TYPE_LABELS[account.type];

  const handleToggle = (checked: boolean): void => {
    onToggleStatus(account.id, checked);
  };

  return (
    <Card className="relative overflow-hidden">
      <div className={cn('h-1 w-full', colors.bar)} />
      <CardContent className="p-4 space-y-4">
        {/* Header com ícone, nome e botão editar */}
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-start gap-3 flex-1 min-w-0">
            <div className={cn('rounded-lg p-2 shrink-0', colors.icon)}>
              <span className="material-icons text-xl">{icon}</span>
            </div>
            <div className="flex-1 min-w-0">
              <h3 className="font-semibold text-base truncate">{account.name}</h3>
              <p className="text-sm text-slate-600">{label}</p>
            </div>
          </div>
          <Button
            size="icon"
            variant="ghost"
            onClick={() => onEdit(account)}
            className="shrink-0"
            title="Editar conta"
          >
            <Edit className="h-4 w-4" />
          </Button>
        </div>

        {/* Saldo atual */}
        <div>
          <p className="text-xs text-slate-600 mb-1">Saldo Atual</p>
          <p
            className={cn('text-2xl font-bold', account.balance < 0 ? 'text-danger' : 'text-slate-900')}
          >
            {formatCurrency(account.balance)}
          </p>
        </div>

        {/* Footer com toggle e status */}
        <div className="flex items-center justify-between pt-2 border-t">
          <div className="flex items-center gap-2">
            <Switch
              checked={account.isActive}
              onCheckedChange={handleToggle}
              aria-label="Toggle status da conta"
            />
            <span className="text-sm font-medium">
              {account.isActive ? 'Ativa' : 'Inativa'}
            </span>
          </div>
          {account.allowNegativeBalance && (
            <Badge variant="secondary" className="text-xs">
              Permite saldo negativo
            </Badge>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
