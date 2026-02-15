import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import { formatCurrency } from '@/shared/utils/formatters';
import { cn } from '@/shared/utils';

interface AccountSummaryFooterProps {
  accounts: AccountResponse[];
}

export function AccountSummaryFooter({ accounts }: AccountSummaryFooterProps): JSX.Element {
  const activeAccounts = accounts.filter((account) => account.isActive);

  const totalBalance = activeAccounts.reduce((sum, account) => sum + account.balance, 0);

  const activeCount = activeAccounts.length;

  const creditCardDebt = activeAccounts
    .filter((account) => account.type === AccountType.Cartao && account.balance < 0)
    .reduce((sum, account) => sum + Math.abs(account.balance), 0);

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-white border-t shadow-lg lg:left-64">
      <div className="container mx-auto px-6 py-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Patrimônio Total */}
          <div>
            <p className="text-xs text-slate-600 mb-1">Patrimônio Total</p>
            <p className={cn('text-xl font-bold', totalBalance < 0 ? 'text-danger' : 'text-slate-900')}>
              {formatCurrency(totalBalance)}
            </p>
          </div>

          {/* Contas Ativas */}
          <div>
            <p className="text-xs text-slate-600 mb-1">Contas Ativas</p>
            <p className="text-xl font-bold text-slate-900">
              {activeCount} {activeCount === 1 ? 'conta' : 'contas'}
            </p>
          </div>

          {/* Dívida Total de Cartões */}
          <div>
            <p className="text-xs text-slate-600 mb-1">Dívida de Cartões</p>
            <p className={cn('text-xl font-bold', creditCardDebt > 0 ? 'text-warning' : 'text-slate-900')}>
              {formatCurrency(creditCardDebt)}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
