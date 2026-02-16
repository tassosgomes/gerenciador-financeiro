import { useState } from 'react';
import { Edit } from 'lucide-react';

import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import { Badge, Button, Card, CardContent, Switch } from '@/shared/components/ui';
import { ACCOUNT_TYPE_ICONS, ACCOUNT_TYPE_LABELS } from '@/shared/utils/constants';
import { formatCurrency } from '@/shared/utils/formatters';
import { cn } from '@/shared/utils';
import { InvoiceDrawer } from './InvoiceDrawer';

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
  const [isInvoiceDrawerOpen, setIsInvoiceDrawerOpen] = useState(false);
  const colors = ACCOUNT_COLORS[account.type];
  const icon = ACCOUNT_TYPE_ICONS[account.type];
  const label = ACCOUNT_TYPE_LABELS[account.type];

  const isCreditCard = account.type === AccountType.Cartao && account.creditCard !== null;

  const handleToggle = (checked: boolean): void => {
    onToggleStatus(account.id, checked);
  };

  const handleOpenInvoice = (): void => {
    setIsInvoiceDrawerOpen(true);
  };

  return (
    <>
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

          {/* Seção de saldo/fatura - condicional por tipo */}
          {isCreditCard ? (
            <div className="space-y-3">
              {/* Fatura Atual */}
              <div>
                <p className="text-xs text-slate-600 mb-1">Fatura Atual</p>
                <p className="text-2xl font-bold text-slate-900">
                  {formatCurrency(Math.abs(account.balance))}
                </p>
              </div>

              {/* Limite e Disponível */}
              <div className="flex justify-between text-sm">
                <span className="text-slate-600">
                  Limite: <span className="font-medium text-slate-900">{formatCurrency(account.creditCard!.creditLimit)}</span>
                </span>
                <span className="text-slate-600">
                  Disponível: <span className="font-medium text-slate-900">{formatCurrency(account.creditCard!.availableLimit)}</span>
                </span>
              </div>

              {/* Fechamento e Vencimento */}
              <p className="text-xs text-slate-600">
                Fecha dia {account.creditCard!.closingDay} | Vence dia {account.creditCard!.dueDay}
              </p>

              {/* Alertas e Badges */}
              <div className="flex flex-wrap gap-2">
                {/* Alerta de limite esgotado */}
                {account.creditCard!.availableLimit <= 0 && (
                  <Badge variant="destructive" className="text-xs">
                    Limite esgotado
                  </Badge>
                )}

                {/* Alerta de limite baixo */}
                {account.creditCard!.availableLimit > 0 &&
                  account.creditCard!.availableLimit / account.creditCard!.creditLimit < 0.2 && (
                    <Badge variant="secondary" className="text-xs bg-yellow-100 text-yellow-800 border-yellow-300">
                      Limite baixo ({Math.round((account.creditCard!.availableLimit / account.creditCard!.creditLimit) * 100)}% disponível)
                    </Badge>
                  )}

                {/* Crédito a favor */}
                {account.balance > 0 && (
                  <Badge variant="secondary" className="text-xs bg-green-100 text-green-800 border-green-300">
                    Crédito disponível: {formatCurrency(account.balance)}
                  </Badge>
                )}
              </div>

              {/* Botão Ver Fatura */}
              <Button variant="outline" size="sm" onClick={handleOpenInvoice} className="w-full">
                Ver Fatura
              </Button>
            </div>
          ) : (
            // Exibição padrão para contas regulares
            <div>
              <p className="text-xs text-slate-600 mb-1">Saldo Atual</p>
              <p
                className={cn('text-2xl font-bold', account.balance < 0 ? 'text-danger' : 'text-slate-900')}
              >
                {formatCurrency(account.balance)}
              </p>
            </div>
          )}

          {/* Footer com toggle e status */}
          <div className="flex items-center justify-between pt-2 border-t">
            <div className="flex items-center gap-2">
              <Switch
                checked={account.isActive}
                onCheckedChange={handleToggle}
                aria-label="Toggle status da conta"
              />
              <span className="text-sm font-medium">{account.isActive ? 'Ativa' : 'Inativa'}</span>
            </div>
            {account.allowNegativeBalance && !isCreditCard && (
              <Badge variant="secondary" className="text-xs">
                Permite saldo negativo
              </Badge>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Drawer de Fatura */}
      {isCreditCard && (
        <InvoiceDrawer
          accountId={account.id}
          accountName={account.name}
          isOpen={isInvoiceDrawerOpen}
          onClose={() => setIsInvoiceDrawerOpen(false)}
        />
      )}
    </>
  );
}
